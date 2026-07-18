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
    public int CurrentArmor => currentArmor;
    private int cursedHealthLost = 0;

    [Header("Armor Regen")]
    public float armorRegenRate = 5f;
    
    [Header("Invulnerability")]
    public bool isInvulnerable = false;
    
    [Header("Componentes")]
    public PlayerM playerMovement;
    public PrimaryAttackKnife playerAttack;
    public Animator playerAnimator;
    public ScreenFader screenFader;
    private Rigidbody rb;
    private PlayerAttributesDefensive playerAttributes;

    [Header("UI (Interface)")]
    public Image healthFillImage; // A barra verde (Filled)
    public Image gooBarImage;     // A barra de gosma (Filled)
    public Image armorBarImage;   // A barra de armadura (Filled)
    
    public TextMeshProUGUI armorText;
    public TextMeshProUGUI percentageText; // O texto de %

    [Header("Pactos")]
    [HideInInspector] public float damageMultiplier = 1.0f;
    [HideInInspector] public float damageTakenMultiplier = 1.0f;
    [HideInInspector] public bool canHeal = true;
    [HideInInspector] public bool hasDoubleLoot = false;
    [HideInInspector] public bool hasVampirism = false;
    [HideInInspector] public bool hasNecrosis = false;
    [HideInInspector] public float lastKillTime = 0f;

    [Header("Stun")]
    public bool isStunned { get; private set; } = false;
    private float stunImmunityTimer = 0f;
    private float stunImmunityDuration = 2f; // Imunidade após stun

    private bool isDead = false;
    private int playerLayer;
    private bool diedFallingForward = false;

    private float armorRegenAccumulator = 0f;
    private float healthRegenAccumulator = 0f;

    void Start()
    {
        playerLayer = gameObject.layer;
        rb = GetComponent<Rigidbody>();

        FindUIReferences();
        FullHeal();
        
        // Buscar PlayerAttributesDefensive
        playerAttributes = GetComponent<PlayerAttributesDefensive>();
        if (playerAttributes == null)
        {
            Debug.LogWarning("PlayerHealth: PlayerAttributesDefensive não encontrado! Atributos defensivos não funcionarão.");
        }

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
        // Animação de levantar desabilitada temporariamente a pedido do usuário
        UnlockPlayer();
        yield break;

#if false
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
#endif
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

        // Limpa o inventário de run (descarta itens comuns, mantém recursos de base)
        SaveManager.instance?.OnPlayerDied(this.gameObject);
        
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
        else SceneManager.LoadScene("Base");
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

    // ========== UPGRADES (INFUSÃO DE ATRIBUTOS) ==========
    
    /// <summary>
    /// Modifica atributos de Vida e Armadura
    /// </summary>
    public void ModifyAttribute(string attributeName, float value, bool isMultiplier = false)
    {
        switch (attributeName.ToLower())
        {
            case "maxhealth":
            case "health":
                int oldHealth = maxHealth;
                maxHealth = Mathf.Max(1, isMultiplier ? Mathf.RoundToInt(maxHealth * value) : maxHealth + Mathf.RoundToInt(value));
                
                // Cura o player com o novo bônus e garante que currentHealth respeita o novo limite
                currentHealth = Mathf.Clamp(currentHealth + (maxHealth - oldHealth), 1, maxHealth);
                UpdateHealthBar();
                Debug.Log($"[PLAYER HEALTH] Vida Maxima aumentada: {oldHealth} -> {maxHealth}");
                break;

            case "maxarmor":
            case "armor":
                int oldArmor = maxArmor;
                maxArmor = Mathf.Max(0, isMultiplier ? Mathf.RoundToInt(maxArmor * value) : maxArmor + Mathf.RoundToInt(value));
                
                currentArmor = Mathf.Clamp(currentArmor + (maxArmor - oldArmor), 0, maxArmor);
                UpdateArmorBar();
                Debug.Log($"[PLAYER HEALTH] Armadura Maxima aumentada: {oldArmor} -> {maxArmor}");
                break;

            default:
                Debug.LogWarning($"PlayerHealth: Atributo '{attributeName}' não encontrado!");
                break;
        }
    }

    // ========== SISTEMA DE STUN ==========
    
    /// <summary>
    /// Aplica stun ao jogador por uma duração específica.
    /// Chamado por inimigos como o Golem.
    /// </summary>
    public void ApplyStun(float duration)
    {
        // Não pode ser stunado se: já está stunado, morto, ou imune
        if (isStunned || isDead || stunImmunityTimer > 0) return;

        // Verifica se está em dash (não pode ser stunado durante dash)
        DashM dashScript = GetComponent<DashM>();
        if (dashScript != null && dashScript.isDashing) return;

        StartCoroutine(StunCoroutine(duration));
    }

    private IEnumerator StunCoroutine(float duration)
    {
        isStunned = true;

        // Desabilita controles
        if (playerMovement != null) playerMovement.enabled = false;
        if (playerAttack != null) playerAttack.enabled = false;

        // Para o movimento
        if (rb != null && !rb.isKinematic)
        {
            rb.linearVelocity = Vector3.zero;
        }

        // Animação de stun (opcional - usa o mesmo sistema de idle)
        if (playerAnimator != null)
        {
            playerAnimator.SetFloat("Speed", 0f);
        }

        Debug.Log("Player STUNADO por " + duration + " segundos!");

        yield return new WaitForSeconds(duration);

        // Restaura controles
        isStunned = false;
        if (playerMovement != null) playerMovement.enabled = true;
        if (playerAttack != null) playerAttack.enabled = true;

        // Ativa imunidade temporária
        stunImmunityTimer = stunImmunityDuration;

        Debug.Log("Player recuperou do stun. Imunidade por " + stunImmunityDuration + "s");
    }
    
    // ========== SISTEMA DE ATRIBUTOS DEFENSIVOS ==========
    
    void Update()
    {
        // Gerenciar timer de imunidade a stun
        if (stunImmunityTimer > 0)
        {
            stunImmunityTimer -= Time.deltaTime;
        }
        
        // === REGENERAÇÃO DE ARMADURA ===
        if (!isDead && currentArmor < maxArmor && playerAttributes != null)
        {
            armorRegenAccumulator += armorRegenRate * playerAttributes.armorRegen * Time.deltaTime;
            if (armorRegenAccumulator >= 1.0f)
            {
                int intRegen = Mathf.FloorToInt(armorRegenAccumulator);
                int previousArmor = currentArmor;
                currentArmor = Mathf.Min(maxArmor, currentArmor + intRegen);
                armorRegenAccumulator -= intRegen;
                
                // Log a cada segundo (aproximadamente)
                if (Time.frameCount % 60 == 0 && currentArmor != previousArmor)
                {
                    float regenPerSecond = armorRegenRate * playerAttributes.armorRegen;
                    Debug.Log($"🛡️ ARMOR REGEN! {previousArmor} → {currentArmor} | Taxa: {regenPerSecond:F1} pts/s");
                }
                    
                UpdateArmorBar();
            }
        }

        // === REGENERAÇÃO DE VIDA ===
        if (!isDead && currentHealth < maxHealth && playerAttributes != null && playerAttributes.healthRegen > 0f)
        {
            healthRegenAccumulator += playerAttributes.healthRegen * Time.deltaTime;
            if (healthRegenAccumulator >= 1.0f)
            {
                int intRegen = Mathf.FloorToInt(healthRegenAccumulator);
                currentHealth = Mathf.Min(maxHealth, currentHealth + intRegen);
                healthRegenAccumulator -= intRegen;
                
                UpdateHealthBar();
            }
        }

        // === NECROSE (Pacto do Parasita) ===
        if (hasNecrosis && !isDead)
        {
            if (Time.time - lastKillTime > 5f)
            {
                // Dano contínuo se ficar 5s sem matar
                int necroseDamage = Mathf.Max(1, Mathf.RoundToInt(maxHealth * 0.02f * Time.deltaTime)); 
                currentHealth -= necroseDamage;
                if (currentHealth <= 0) Die();
                UpdateHealthBar();
            }
        }
    }

    public void Heal(int amount)
    {
        if (isDead || !canHeal) return;
        currentHealth += amount;
        if (currentHealth > maxHealth) currentHealth = maxHealth;
        UpdateHealthBar();
    }
    
    /// <summary>
    /// Aplica dano ao jogador com todos os atributos defensivos.
    /// </summary>
    public void TakeDamage(int damage, GameObject attacker = null)
    {
        if (isDead || isInvulnerable) return;

        // Se estivermos no modo Endless e o level for maior que 3, aumenta o dano recebido pelo jogador
        if (RunManager.instance != null && RunManager.instance.isEndlessMode && RunManager.instance.currentLevel > 3)
        {
            float endlessDamageMultiplier = 1f + (RunManager.instance.currentLevel - 3) * 0.15f; // +15% de dano por level acima do 3
            damage = Mathf.RoundToInt(damage * endlessDamageMultiplier);
        }
        
        int finalDamage = Mathf.RoundToInt(damage * damageTakenMultiplier);
        
        // === DODGE (Esquiva) ===
        if (playerAttributes != null && playerAttributes.dodgeChance > 0)
        {
            float dodgeRoll = Random.Range(0f, 100f);
            if (dodgeRoll < playerAttributes.dodgeChance)
            {
                Debug.Log($"✨ ESQUIVOU! Dano anulado | Roll: {dodgeRoll:F1} < {playerAttributes.dodgeChance}% chance");
                return; // Anula completamente o dano
            }
        }
        
        // === DAMAGE NEGATION (Mitigação) ===
        if (playerAttributes != null && playerAttributes.damageNegation > 0)
        {
            int damageBeforeNegation = finalDamage;
            float reductionPercent = playerAttributes.damageNegation / 100f;
            finalDamage = Mathf.RoundToInt(finalDamage * (1f - reductionPercent));
            Debug.Log($"🛡️ DAMAGE NEGATION! Dano: {damageBeforeNegation} → {finalDamage} (-{playerAttributes.damageNegation}%)");
        }
        
        // Aplicar dano em armor primeiro, depois health
        if (currentArmor > 0)
        {
            int damageToArmor = Mathf.Min(finalDamage, currentArmor);
            currentArmor -= damageToArmor;
            finalDamage -= damageToArmor;
            UpdateArmorBar();
        }
        
        if (finalDamage > 0)
        {
            currentHealth -= finalDamage;
            UpdateHealthBar();
            
            Debug.Log($"❤️ Dano recebido: {finalDamage} | Health: {currentHealth}/{maxHealth} | Armor: {currentArmor}/{maxArmor}");
            
            if (currentHealth <= 0)
            {
                Die();
            }
        }
        
        // === THORNS (Dano Refletido) ===
        if (attacker != null && playerAttributes != null && playerAttributes.thorns > 0)
        {
            DummyHealth enemyHealth = attacker.GetComponent<DummyHealth>();
            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(playerAttributes.thorns);
                Debug.Log($"🌵 THORNS! Refletido {playerAttributes.thorns} de dano para {attacker.name}");
            }
        }
    }
}
