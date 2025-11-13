using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro; // Necessário para o texto da armadura

public class PlayerHealth : MonoBehaviour
{
    [Header("Configurações de Vida e Armadura")]
    public int maxHealth = 100;
    public int maxArmor = 200;
    public int currentHealth { get; private set; } // Deixei público para o Mercador checar
    private int currentArmor;
    private int cursedHealthLost = 0; // "Memória" da vida amaldiçoada

    [Header("Componentes e Referências")]
    public PlayerM playerMovement;
    public PrimaryAttackKnife playerAttack;
    public Animator playerAnimator;
    public ScreenFader screenFader;
    public Transform initialSpawnPoint;
    private Transform currentSpawnPoint;

    [Header("UI (Interface)")]
    public Slider healthBarSlider; // A barra de vida verde
    public Image gooBarImage; // A barra de "gosma" que fica por baixo
    public Image armorBarImage;
    public TextMeshProUGUI armorText;

    [Header("Modificadores de Pacto")]
    [HideInInspector] public float damageMultiplier = 1.0f; // Multiplicador de dano causado
    [HideInInspector] public float damageTakenMultiplier = 1.0f; // Multiplicador de dano recebido

    private bool isDead = false;
    private int playerLayer;
    private bool diedFallingForward;

    void Start()
    {
        try
        {
            healthBarSlider = GameObject.Find("HealthBar_Slider").GetComponent<Slider>();
            gooBarImage = GameObject.Find("Goo_Fill").GetComponent<Image>();
            armorBarImage = GameObject.Find("ArmorBar_Fill").GetComponent<Image>();
            armorText = GameObject.Find("ArmorText").GetComponent<TextMeshProUGUI>();
        
         // (Encontra os outros componentes do jogador)
            playerMovement = GetComponent<PlayerM>();
            playerAnimator = GetComponentInChildren<Animator>();
        }
        catch (System.Exception e)
    {
        Debug.LogError("PlayerHealth: Falha ao encontrar componentes da UI! Verifique os NOMES dos objetos no Canvas. Erro: " + e.Message);
    }
        playerLayer = gameObject.layer;
        if (healthBarSlider == null)
        {
        healthBarSlider = GameObject.Find("HealthBar_Slider").GetComponent<Slider>();
        }
        if (gooBarImage == null)
        {
        gooBarImage = GameObject.Find("Goo_Fill").GetComponent<Image>();
        }
        if (armorBarImage == null)
        {
        armorBarImage = GameObject.Find("ArmorBar_Fill").GetComponent<Image>();
        }
        if (armorText == null)
        {
        armorText = GameObject.Find("ArmorText").GetComponent<TextMeshProUGUI>();
        }

        if (currentSpawnPoint == null)
        {
            currentSpawnPoint = initialSpawnPoint;
        }
        if (currentSpawnPoint != null)
        {
            transform.position = currentSpawnPoint.position;
            transform.rotation = currentSpawnPoint.rotation;
        }
        
        FullHeal();
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        int finalDamage = Mathf.RoundToInt(damage * damageTakenMultiplier);

        // Dano é aplicado primeiro à armadura
        if (currentArmor > 0)
        {
            int damageToArmor = Mathf.Min(finalDamage, currentArmor);
            currentArmor -= damageToArmor;
            finalDamage -= damageToArmor;
            UpdateArmorBar();
        }

        // Dano restante vai para a vida "normal" (não a amaldiçoada)
        if (finalDamage > 0)
        {
            int damageableHealth = currentHealth - cursedHealthLost;
            int damageToHealth = Mathf.Min(finalDamage, damageableHealth);
            
            currentHealth -= damageToHealth;
            UpdateHealthBar();
        }

        if (currentHealth <= 0 && !isDead)
        {
            Die();
        }
    }

    // Função para o Mercador usar (dano amaldiçoado)
    public void TakeCursedDamage(int amount)
    {
        if (isDead) return;

        // Garante que o dano não seja maior que a vida disponível
        int actualCost = Mathf.Min(amount, currentHealth);

        currentHealth -= actualCost;
        cursedHealthLost += actualCost; // A "gosma" aumenta
        
        if (currentHealth <= 0)
        {
            Die();
        }
        UpdateHealthBar();
    }
    
    void Die()
    {
        isDead = true;
        Debug.Log("O jogador morreu! Iniciando sequência de respawn.");

        // Desativa os controles e muda a camada de física
        playerMovement.enabled = false;
        if (playerAttack != null) playerAttack.enabled = false;
        gameObject.layer = LayerMask.NameToLayer("DeadBody");
        
        // Escolhe e dispara uma animação de morte aleatória
        if (playerAnimator != null)
        {
            if (Random.value > 0.5f)
            {
                diedFallingForward = true;
                playerAnimator.SetTrigger("DeathForward");
            }
            else
            {
                diedFallingForward = false;
                playerAnimator.SetTrigger("DeathBackward");
            }
        }
        StartCoroutine(RespawnSequence());
    }

    IEnumerator RespawnSequence()
    {
        yield return new WaitForSeconds(2.0f);

        if (screenFader != null)
        {
            yield return StartCoroutine(screenFader.FadeOut());
        }
        
        // Ação ocorre com a tela preta
        transform.position = currentSpawnPoint.position;
        transform.rotation = currentSpawnPoint.rotation;
        FullHeal();

        if (playerAnimator != null)
        {
            playerAnimator.applyRootMotion = true;
            if (diedFallingForward)
            {
                playerAnimator.SetTrigger("Revive1");
            }
            else
            {
                playerAnimator.SetTrigger("Revive2");
            }
        }
        
        if (screenFader != null)
        {
            yield return StartCoroutine(screenFader.FadeIn());
        }
    }

    // Função pública chamada pelo PlayerAnimationEvents
    public void HandleReviveCompletion()
    {
        Debug.Log("Animação de reviver completa. Devolvendo controle ao jogador.");

        if (playerAnimator != null)
        {
            playerAnimator.applyRootMotion = false;
        }
        
        isDead = false;
        playerMovement.enabled = true;
        if (playerAttack != null) playerAttack.enabled = true;
        gameObject.layer = playerLayer; 
    }

    // Reseta vida, armadura e a "gosma"
    void FullHeal()
    {
        cursedHealthLost = 0;
        currentHealth = maxHealth;
        currentArmor = maxArmor;
        UpdateHealthBar();
        UpdateArmorBar();
    }
    
    public void RestoreArmor(int amount)
    {
        if (isDead) return;
        currentArmor += amount;
        if (currentArmor > maxArmor)
        {
            currentArmor = maxArmor;
        }
        UpdateArmorBar();
    }

    private void UpdateHealthBar()
    {
        if (healthBarSlider != null)
        {
            // A barra verde mostra a vida atual
            healthBarSlider.value = (float)currentHealth / maxHealth;
        }

        if (gooBarImage != null)
        {
            // Controla a visibilidade e o preenchimento da gosma
            if (cursedHealthLost > 0)
            {
                gooBarImage.enabled = true;
                // A gosma preenche a barra de "vida perdida"
                gooBarImage.fillAmount = (float)(currentHealth + cursedHealthLost) / maxHealth;
            }
            else
            {
                gooBarImage.enabled = false;
            }
        }
    }

    private void UpdateArmorBar()
    {
        if (armorBarImage != null)
        {
            armorBarImage.fillAmount = (float)currentArmor / maxArmor;
        }
        if (armorText != null)
        {
            armorText.text = currentArmor + "/" + maxArmor;
        }
    }

    // Função para a AnaLu usar
    public void SetCurrentSpawnPoint(Transform newSpawnPoint)
    {
        currentSpawnPoint = newSpawnPoint;
        if (newSpawnPoint != null)
        {
            transform.position = newSpawnPoint.position;
            transform.rotation = newSpawnPoint.rotation;
        }
    }
}