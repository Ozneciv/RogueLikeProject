using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro; // Necessário para o texto da armadura

public class PlayerHealth : MonoBehaviour
{
    [Header("Configurações de Vida e Armadura")]
    public int maxHealth = 100;
    public int maxArmor = 200;
    public int currentHealth { get; private set; } // Público para o Mercador ler
    private int currentArmor;
    private int cursedHealthLost = 0; // "Memória" da vida amaldiçoada

    [Header("Componentes e Referências")]
    public PlayerM playerMovement;
    public PrimaryAttackKnife playerAttack;
    public Animator playerAnimator;
    public ScreenFader screenFader;
    // 'initialSpawnPoint' foi removido pois o GameManager define a posição
    private Transform currentSpawnPoint;

    [Header("UI (Interface)")]
    public Slider healthBarSlider; // A barra de vida verde
    public Image gooBarImage;      // A barra de "gosma" que fica por baixo
    public Image armorBarImage;    // A imagem de preenchimento da armadura
    public TextMeshProUGUI armorText;

    [Header("Modificadores de Pacto")]
    [HideInInspector] public float damageMultiplier = 1.0f; // Multiplicador de dano causado
    [HideInInspector] public float damageTakenMultiplier = 1.0f; // Multiplicador de dano recebido

    private bool isDead = false;
    private int playerLayer;
    private bool diedFallingForward;

    void Start()
        {
        playerLayer = gameObject.layer;
        
        // Tenta encontrar a UI na primeira cena (pode falhar se for a Base, e tudo bem)
        FindUIReferences();
        
        FullHeal();
        }

    // --- NOVA FUNÇÃO PÚBLICA ---
    // O GameManager vai chamar isso toda vez que entrar na GameScene
        public void FindUIReferences()
        {
            try
            {
            // Busca pelos nomes EXATOS da sua hierarquia
            GameObject healthObj = GameObject.Find("HealthBar_Slider"); 
            if (healthObj != null) healthBarSlider = healthObj.GetComponent<Slider>();

            GameObject gooObj = GameObject.Find("Goo_Fill");
            if (gooObj != null) gooBarImage = gooObj.GetComponent<Image>();

            GameObject armorObj = GameObject.Find("ArmorBar_Fill"); 
            if (armorObj != null) armorBarImage = armorObj.GetComponent<Image>();

            GameObject textObj = GameObject.Find("ArmorText");
            if (textObj != null) armorText = textObj.GetComponent<TextMeshProUGUI>();

            // Conecta componentes internos
            if (playerMovement == null) playerMovement = GetComponent<PlayerM>();
            if (playerAnimator == null) playerAnimator = GetComponentInChildren<Animator>();
            if (playerAttack == null) playerAttack = GetComponent<PrimaryAttackKnife>();

            // Atualiza visualmente se encontrou
            UpdateHealthBar();
            UpdateArmorBar();
            }
            catch (System.Exception e)
            {
               Debug.LogWarning("PlayerHealth: UI não encontrada (normal se estiver na Base).");
            }
        }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        // Aplica o multiplicador de dano recebido (dos pactos)
        int finalDamage = Mathf.RoundToInt(damage * damageTakenMultiplier);

        // Dano é aplicado primeiro à armadura
        if (currentArmor > 0)
        {
            int damageToArmor = Mathf.Min(finalDamage, currentArmor);
            currentArmor -= damageToArmor;
            finalDamage -= damageToArmor;
            UpdateArmorBar();
        }

        // Dano restante vai para a vida "normal" (respeitando o limite da gosma)
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

    // Função para o Mercador usar (dano amaldiçoado permanente na run)
    public void TakeCursedDamage(int amount)
    {
        if (isDead) return;

        // Garante que o dano não seja maior que a vida disponível
        int actualCost = Mathf.Min(amount, currentHealth);

        currentHealth -= actualCost;
        cursedHealthLost += actualCost; // A "gosma" aumenta, reduzindo a vida máxima efetiva
        
        if (currentHealth <= 0)
        {
            Die();
        }
        UpdateHealthBar();
    }
    
    void Die()
    {
        isDead = true;
        Debug.Log("O jogador morreu! Iniciando sequência de retorno.");

        // Desativa controles
        if (playerMovement != null) playerMovement.enabled = false;
        if (playerAttack != null) playerAttack.enabled = false;
        
        // Muda layer para não colidir com inimigos
        gameObject.layer = LayerMask.NameToLayer("DeadBody");
        
        // Toca animação
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
        // Espera a animação de morte terminar
        yield return new WaitForSeconds(2.0f);

        // Escurece a tela
        if (screenFader != null)
        {
            yield return StartCoroutine(screenFader.FadeOut());
        }
        
        // --- VOLTAR PARA A BASE ---
        // Como mudamos para a arquitetura "Clean Slate", a morte recarrega a cena da Base.
        if (GameManager.instance != null)
        {
            GameManager.instance.ReturnToBase();
        }
        else
        {
            // Fallback: Apenas recarrega a cena atual se não houver GameManager
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }
        
        // O código abaixo não será executado se a cena mudar, 
        // mas é mantido caso você queira usar respawn na mesma fase no futuro.
    }

    // Função pública chamada pelo PlayerAnimationEvents (para animação de Revive)
    public void HandleReviveCompletion()
    {
        // Esta função é usada quando o jogador revive NA MESMA CENA.
        // No fluxo atual de "Voltar para Base", ela pode não ser chamada,
        // mas é bom mantê-la para compatibilidade.
        
        if (playerAnimator != null)
        {
            playerAnimator.applyRootMotion = false;
        }
        
        isDead = false;
        if (playerMovement != null) playerMovement.enabled = true;
        if (playerAttack != null) playerAttack.enabled = true;
        gameObject.layer = playerLayer; 
    }

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
            healthBarSlider.value = (float)currentHealth / maxHealth;
        }

        if (gooBarImage != null)
        {
            // A gosma aparece se houver vida perdida permanentemente
            if (cursedHealthLost > 0)
            {
                gooBarImage.enabled = true;
                // A gosma preenche até onde a vida "normal" iria
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

    // Função usada pelo LevelGenerator
    public void SetCurrentSpawnPoint(Transform newSpawnPoint)
    {
        currentSpawnPoint = newSpawnPoint;
        if (newSpawnPoint != null)
        {
            transform.position = newSpawnPoint.position;
            transform.rotation = newSpawnPoint.rotation;
        }
    }
    // ... (dentro do PlayerHealth.cs)

    // Função chamada pelo GameManager ao voltar para a Base
    public void ResetPlayerState()
    {
        isDead = false;
        
        // 1. Reseta Vida e Armadura
        FullHeal(); 

        // 2. Reativa o Movimento e Ataque
        if (playerMovement != null) playerMovement.enabled = true;
        if (playerAttack != null) playerAttack.enabled = true;

        // 3. Reseta a Camada Física (para colidir com inimigos de novo na próxima run)
        gameObject.layer = playerLayer; 

        // 4. Reseta Animação (Volta para Idle)
        if (playerAnimator != null)
        {
            playerAnimator.Rebind(); // Reseta o Animator completamente
            playerAnimator.Update(0f);
        }

        Debug.Log("Estado do Jogador Resetado para a Base!");
    }
}