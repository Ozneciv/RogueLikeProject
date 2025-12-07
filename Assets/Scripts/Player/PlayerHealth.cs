using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour
{
    [Header("Configurações")]
    public int maxHealth = 100;
    public int maxArmor = 200;
    public int currentHealth { get; private set; }
    private int currentArmor;
    private int cursedHealthLost = 0;

    [Header("Componentes")]
    public PlayerM playerMovement;
    public PrimaryAttackKnife playerAttack;
    public Animator playerAnimator;
    public ScreenFader screenFader;
    private Rigidbody rb;

    [Header("UI (Interface)")]
    public Image healthFillImage; // A barra verde (Filled)
    public Image gooBarImage;     // A barra de gosma (Filled)
    public Image armorBarImage;   // A barra de armadura (Filled)
    
    public TextMeshProUGUI armorText;
    public TextMeshProUGUI percentageText; // O texto de %

    [Header("Pactos")]
    [HideInInspector] public float damageMultiplier = 1.0f;
    [HideInInspector] public float damageTakenMultiplier = 1.0f;

    private bool isDead = false;
    private int playerLayer;
    private bool diedFallingForward = false;

    void Start()
    {
        playerLayer = gameObject.layer;
        rb = GetComponent<Rigidbody>();

        FindUIReferences();
        FullHeal();

        if (SceneManager.GetActiveScene().name == "Base")
        {
            TriggerBaseRespawn();
        }
    }

    public void TriggerBaseRespawn()
    {
        FindUIReferences();
        FullHeal();
        isDead = false;
        gameObject.layer = playerLayer;

        // Trava a física para a animação
        if (rb != null)
        {
            if (!rb.isKinematic) rb.linearVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        StartCoroutine(PlaySpawnAnimation());
    }

    IEnumerator PlaySpawnAnimation()
    {
        if (playerMovement != null) playerMovement.enabled = false;
        if (playerAttack != null) playerAttack.enabled = false;
        
        // Reforça a trava física
        if (rb != null) 
        { 
            if (!rb.isKinematic) rb.linearVelocity = Vector3.zero;
            rb.isKinematic = true; 
        }

        if (playerAnimator != null)
        {
            playerAnimator.applyRootMotion = true;
            yield return new WaitForEndOfFrame();

            // --- LÓGICA FORÇADA PARA REVIVE 2 (Seguro) ---
            string triggerName = "Revive2";

            /* LÓGICA ORIGINAL (Comentada para uso futuro)
            string triggerName = "Revive1";
            if (Time.time < 1f) 
            {
                if (Random.value > 0.5f) triggerName = "Revive2";
            }
            else
            {
                if (!diedFallingForward) triggerName = "Revive2";
            }
            */

            playerAnimator.SetTrigger(triggerName);

            // --- ESPERA INTELIGENTE ---
            yield return new WaitForSeconds(0.1f);
            float timeout = 0f;
            
            // Espera até que o Animator esteja tocando o Revive2
            while (!playerAnimator.GetCurrentAnimatorStateInfo(0).IsName("Revive2"))
            {
                yield return null;
                timeout += Time.deltaTime;
                if (timeout > 2f) break;
            }

            float animationLength = playerAnimator.GetCurrentAnimatorStateInfo(0).length;
            yield return new WaitForSeconds(animationLength);
        }
        
        // Destrava o jogador
        UnlockPlayer();
    }

    public void UnlockPlayer()
    {
        if (playerAnimator != null) playerAnimator.applyRootMotion = false;
        isDead = false;
        if (playerMovement != null) playerMovement.enabled = true;
        if (playerAttack != null) playerAttack.enabled = true;
        gameObject.layer = playerLayer; 
        
        // Destrava a física para o jogo
        if (rb != null) 
        { 
            rb.isKinematic = false; 
            rb.detectCollisions = true; 
            rb.WakeUp(); 
        }
    }

    public void FindUIReferences()
    {
        try
        {
            GameObject healthObj = GameObject.Find("Health_Fill");
            if (healthObj != null) healthFillImage = healthObj.GetComponent<Image>();
            
            GameObject gooObj = GameObject.Find("Goo_Fill");
            if (gooObj != null) gooBarImage = gooObj.GetComponent<Image>();

            GameObject armorObj = GameObject.Find("ArmorBar_Fill"); 
            if (armorObj != null) armorBarImage = armorObj.GetComponent<Image>();

            GameObject textArmor = GameObject.Find("ArmorText");
            if (textArmor != null) armorText = textArmor.GetComponent<TextMeshProUGUI>();
            
            GameObject textPercent = GameObject.Find("Text_Percentage");
            if (textPercent != null) percentageText = textPercent.GetComponent<TextMeshProUGUI>();

            if (playerMovement == null) playerMovement = GetComponent<PlayerM>();
            if (playerAnimator == null) playerAnimator = GetComponentInChildren<Animator>();
            if (playerAttack == null) playerAttack = GetComponent<PrimaryAttackKnife>();
        }
        catch (System.Exception) { }
        
        UpdateHealthBar();
        UpdateArmorBar();
    }

    public void ResetPlayerState()
    {
        isDead = false;
        FullHeal(); 
        if (playerMovement != null) playerMovement.enabled = true;
        if (playerAttack != null) playerAttack.enabled = true;
        gameObject.layer = playerLayer; 
        
        if (rb != null) { rb.isKinematic = false; rb.linearVelocity = Vector3.zero; }
        if (playerAnimator != null) { playerAnimator.Rebind(); playerAnimator.Update(0f); }
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;
        int finalDamage = Mathf.RoundToInt(damage * damageTakenMultiplier);
        if (currentArmor > 0)
        {
            int damageToArmor = Mathf.Min(finalDamage, currentArmor);
            currentArmor -= damageToArmor;
            finalDamage -= damageToArmor;
            UpdateArmorBar();
        }
        if (finalDamage > 0)
        {
            int damageableHealth = currentHealth - cursedHealthLost;
            int damageToHealth = Mathf.Min(finalDamage, damageableHealth);
            currentHealth -= damageToHealth;
            UpdateHealthBar();
        }
        if (currentHealth <= 0 && !isDead) Die();
    }

    public void TakeCursedDamage(int amount)
    {
        if (isDead) return;
        int actualCost = Mathf.Min(amount, currentHealth);
        currentHealth -= actualCost;
        cursedHealthLost += actualCost;
        if (currentHealth <= 0) Die();
        UpdateHealthBar();
    }
    
    void Die()
    {
        isDead = true;
        if (playerMovement != null) playerMovement.enabled = false;
        if (playerAttack != null) playerAttack.enabled = false;
        gameObject.layer = LayerMask.NameToLayer("DeadBody");
        
        // Trava física ao morrer também
        if (rb != null) 
        { 
            if (!rb.isKinematic) rb.linearVelocity = Vector3.zero;
            rb.isKinematic = true; 
        }
        
        if (playerAnimator != null)
        {
            if (Random.value > 0.5f) { diedFallingForward = true; playerAnimator.SetTrigger("DeathForward"); }
            else { diedFallingForward = false; playerAnimator.SetTrigger("DeathBackward"); }
        }
        StartCoroutine(RespawnSequence());
    }

    IEnumerator RespawnSequence()
    {
        yield return new WaitForSeconds(2.0f);
        if (screenFader != null) yield return StartCoroutine(screenFader.FadeOut());
        
        if (GameManager.instance != null) GameManager.instance.ReturnToBase();
        else SceneManager.LoadScene("BaseLab");
    }

    public void HandleReviveCompletion() { UnlockPlayer(); }

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
        if (currentArmor > maxArmor) currentArmor = maxArmor;
        UpdateArmorBar();
    }

    private void UpdateHealthBar()
    {
        if (healthFillImage != null)
        {
            healthFillImage.fillAmount = (float)currentHealth / maxHealth;
        }

        if (percentageText != null)
        {
            int percent = Mathf.RoundToInt(((float)currentHealth / maxHealth) * 100);
            percentageText.text = percent + "%";
        }

        if (gooBarImage != null)
        {
            if (cursedHealthLost > 0)
            {
                gooBarImage.enabled = true;
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
        if (armorBarImage != null) armorBarImage.fillAmount = (float)currentArmor / maxArmor;
        if (armorText != null) armorText.text = currentArmor + "/" + maxArmor;
    }
    
    public void SetCurrentSpawnPoint(Transform newSpawnPoint) { }
}