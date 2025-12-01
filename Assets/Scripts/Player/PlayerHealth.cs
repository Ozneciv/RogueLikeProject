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

    [Header("UI")]
    public Slider healthBarSlider;
    public Image gooBarImage;
    public Image armorBarImage;
    public TextMeshProUGUI armorText;

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

        if (SceneManager.GetActiveScene().name == "BaseLab")
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

        // --- CORREÇÃO AQUI ---
        // TRAVA a física enquanto a animação de levantar acontece.
        // Isso impede que a gravidade brigue com o Root Motion (o bug de orbitar).
        if (rb != null)
        {
            rb.isKinematic = true; 
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        StartCoroutine(PlaySpawnAnimation());
    }

IEnumerator PlaySpawnAnimation()
    {
        // 1. Trava tudo
        if (playerMovement != null) playerMovement.enabled = false;
        if (playerAttack != null) playerAttack.enabled = false;
        if (rb != null) { rb.isKinematic = true; rb.linearVelocity = Vector3.zero; }

        if (playerAnimator != null)
        {
            playerAnimator.applyRootMotion = true;
            
            // Escolhe a animação
            string triggerName = "Revive1";
            if (Time.time < 1f) 
            {
                if (Random.value > 0.5f) triggerName = "Revive2";
            }
            else
            {
                if (!diedFallingForward) triggerName = "Revive2";
            }

            playerAnimator.SetTrigger(triggerName);

            // --- A CORREÇÃO: ESPERA INTELIGENTE ---

            // Passo A: Espera o Animator sair do estado atual e começar a transição
            yield return new WaitForSeconds(0.15f);

            // Passo B: Espera até que o estado ATUAL seja realmente um dos Revives.
            // Isso garante que não pegaremos a duração do "Idle" por engano.
            float timeout = 0f;
            while (!playerAnimator.GetCurrentAnimatorStateInfo(0).IsName("Revive1") && 
                   !playerAnimator.GetCurrentAnimatorStateInfo(0).IsName("Revive2"))
            {
                yield return null; // Espera mais um frame
                timeout += Time.deltaTime;
                if (timeout > 2f) break; // Segurança para não travar o jogo se os nomes estiverem errados
            }

            // Passo C: Agora que estamos na animação certa, pegamos a duração dela
            float animationLength = playerAnimator.GetCurrentAnimatorStateInfo(0).length;
            
            Debug.Log("Animação detectada. Duração exata: " + animationLength);

            // Passo D: Espera a duração real da animação
            yield return new WaitForSeconds(animationLength);
        }
        
        // 3. DESTRAVA O JOGADOR
        UnlockPlayer();
    }
    // Renomeei para ficar mais claro, mas a função é a mesma
    public void UnlockPlayer()
    {
        Debug.Log("Spawn finalizado. Destravando jogador.");

        if (playerAnimator != null) playerAnimator.applyRootMotion = false;
        
        isDead = false;
        
        // Reativa scripts
        if (playerMovement != null) playerMovement.enabled = true;
        if (playerAttack != null) playerAttack.enabled = true;
        
        // Restaura camada física
        gameObject.layer = playerLayer; 

        // --- DESTRAVA A FÍSICA (CRUCIAL) ---
        if (rb != null) 
        {
            rb.isKinematic = false;
            rb.detectCollisions = true;
            // Força a gravidade a agir imediatamente para evitar flutuar
            rb.WakeUp(); 
        }
    }

    // Mantemos esta função pública caso algum evento antigo ainda tente chamá-la
    public void HandleReviveCompletion()
    {
        UnlockPlayer();
    }

    
    public void FindUIReferences()
    {
        try
        {
            GameObject healthObj = GameObject.Find("HealthBar_Slider");
            if (healthObj != null) healthBarSlider = healthObj.GetComponent<Slider>();
            GameObject gooObj = GameObject.Find("Goo_Fill");
            if (gooObj != null) gooBarImage = gooObj.GetComponent<Image>();
            GameObject armorObj = GameObject.Find("ArmorBar_Fill");
            if (armorObj != null) armorBarImage = armorObj.GetComponent<Image>();
            GameObject textObj = GameObject.Find("ArmorText");
            if (textObj != null) armorText = textObj.GetComponent<TextMeshProUGUI>();

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
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
        }
        if (playerAnimator != null)
        {
            playerAnimator.Rebind();
            playerAnimator.Update(0f);
        }
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
        
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
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
        if (healthBarSlider != null) healthBarSlider.value = (float)currentHealth / maxHealth;
        if (gooBarImage != null)
        {
            if (cursedHealthLost > 0) { gooBarImage.enabled = true; gooBarImage.fillAmount = (float)(currentHealth + cursedHealthLost) / maxHealth; }
            else { gooBarImage.enabled = false; }
        }
    }

    private void UpdateArmorBar()
    {
        if (armorBarImage != null) armorBarImage.fillAmount = (float)currentArmor / maxArmor;
        if (armorText != null) armorText.text = currentArmor + "/" + maxArmor;
    }
    
    public void SetCurrentSpawnPoint(Transform newSpawnPoint) { }
}