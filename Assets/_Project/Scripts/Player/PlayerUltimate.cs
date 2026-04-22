using UnityEngine;
using System.Collections;

/// <summary>
/// Sistema de Ultimate do jogador - Placeholder funcional.
/// Permite ativar uma habilidade especial poderosa com cooldown.
/// </summary>
public class PlayerUltimate : MonoBehaviour
{
    [Header("Ultimate Settings")]
    [Tooltip("Tempo de recarga do Ultimate em segundos")]
    public float ultimateCooldown = 30f;
    
    [Tooltip("Duração do efeito do Ultimate em segundos")]
    public float ultimateDuration = 5f;
    
    [Tooltip("Multiplicador de dano durante Ultimate")]
    public float damageMultiplier = 2.0f;
    
    [Tooltip("Multiplicador de velocidade durante Ultimate")]
    public float speedMultiplier = 1.5f;
    
    [Tooltip("Invencibilidade durante Ultimate?")]
    public bool grantInvulnerability = true;
    
    [Header("VFX Colors")]
    [Tooltip("Cor principal dos efeitos")]
    public Color vfxPrimaryColor = new Color(0.2f, 0.8f, 1f, 1f);
    
    [Tooltip("Cor secundária dos efeitos")]
    public Color vfxSecondaryColor = new Color(1f, 0.4f, 0f, 1f);
    
    [Tooltip("Cor de destaque (raios, anéis)")]
    public Color vfxAccentColor = new Color(1f, 1f, 0.3f, 1f);
    
    [Header("VFX Intensity")]
    [Tooltip("Intensidade do brilho (1-10)")]
    [Range(1f, 10f)]
    public float vfxGlowIntensity = 5.0f;
    
    [Tooltip("Escala das partículas (0.5-3)")]
    [Range(0.5f, 3f)]
    public float vfxParticleScale = 1.0f;
    
    [Header("Screen Effects")]
    [Tooltip("Ativar tremor de tela")]
    public bool vfxEnableScreenShake = true;
    
    [Tooltip("Intensidade do tremor")]
    [Range(0f, 1f)]
    public float vfxShakeIntensity = 0.3f;
    
    [Header("Visual Effects (Optional)")]
    [Tooltip("Partículas ou efeito visual ao ativar (opcional)")]
    public GameObject ultimateVFX;
    
    [Header("Status")]
    [SerializeField] private bool isUltimateReady = true;
    [SerializeField] private bool isUltimateActive = false;
    [SerializeField] private float currentCooldown = 0f;
    
    // Referências
    private PlayerHealth playerHealth;
    private PlayerAttributesOffensive offensiveAttributes;
    private PlayerAttributesDefensive defensiveAttributes;
    private MonoBehaviour vfxController;
    
    // Estado original dos atributos (para restaurar após Ultimate)
    private float originalAttackSpeed;
    private float originalSpeed;
    
    void Start()
    {
        playerHealth = GetComponent<PlayerHealth>();
        offensiveAttributes = GetComponentInChildren<PlayerAttributesOffensive>();
        defensiveAttributes = GetComponent<PlayerAttributesDefensive>();
        
        if (playerHealth == null)
        {
            Debug.LogWarning("PlayerUltimate: PlayerHealth não encontrado!");
        }
        
        // Criar VFX controller PREMIUM automaticamente
        GameObject vfxObj = new GameObject("UltimateVFXPremium");
        vfxObj.transform.SetParent(transform);
        vfxObj.transform.localPosition = Vector3.zero;
        UltimateVFXPremium premiumVFX = vfxObj.AddComponent<UltimateVFXPremium>();
        
        // Aplicar configurações do Inspector
        premiumVFX.duration = ultimateDuration;
        premiumVFX.primaryColor = vfxPrimaryColor;
        premiumVFX.secondaryColor = vfxSecondaryColor;
        premiumVFX.accentColor = vfxAccentColor;
        premiumVFX.glowIntensity = vfxGlowIntensity;
        premiumVFX.particleScale = vfxParticleScale;
        premiumVFX.enableScreenShake = vfxEnableScreenShake;
        premiumVFX.shakeIntensity = vfxShakeIntensity;
        
        vfxController = premiumVFX;
        
        Debug.Log("✅ PlayerUltimate inicializado! Pressione U para ativar.");
    }
    
    void Update()
    {
        // Atualizar cooldown
        if (!isUltimateReady && !isUltimateActive)
        {
            currentCooldown -= Time.deltaTime;
            if (currentCooldown <= 0f)
            {
                isUltimateReady = true;
                currentCooldown = 0f;
                Debug.Log("⚡ ULTIMATE PRONTO! Pressione U para ativar.");
            }
        }
        
        // Ativar Ultimate com tecla U
        if (Input.GetKeyDown(KeyCode.U) && isUltimateReady && !isUltimateActive)
        {
            ActivateUltimate();
        }
    }
    
    /// <summary>
    /// Ativa o Ultimate do jogador.
    /// </summary>
    public void ActivateUltimate()
    {
        if (!isUltimateReady || isUltimateActive) return;
        
        Debug.Log("💥 ULTIMATE ATIVADO!");
        
        isUltimateActive = true;
        isUltimateReady = false;
        currentCooldown = ultimateCooldown;
        
        // Salvar valores originais
        if (offensiveAttributes != null)
        {
            originalAttackSpeed = offensiveAttributes.attackSpeedMelee;
            offensiveAttributes.attackSpeedMelee *= damageMultiplier;
        }
        
        if (defensiveAttributes != null)
        {
            originalSpeed = defensiveAttributes.speedMultiplier;
            defensiveAttributes.speedMultiplier *= speedMultiplier;
        }
        
        // Ativar invencibilidade se configurado
        if (grantInvulnerability && playerHealth != null)
        {
            playerHealth.isInvulnerable = true;
            Debug.Log("🛡️ INVENCIBILIDADE ATIVADA!");
        }
        
        // Ativar VFX procedural
        if (vfxController != null)
        {
            vfxController.SendMessage("PlayEffect", SendMessageOptions.DontRequireReceiver);
        }
        
        // Spawnar VFX se existir (legacy support)
        if (ultimateVFX != null)
        {
            GameObject vfx = Instantiate(ultimateVFX, transform.position, Quaternion.identity);
            vfx.transform.SetParent(transform);
            Destroy(vfx, ultimateDuration);
        }
        
        // Iniciar coroutine para desativar após duração
        StartCoroutine(DeactivateUltimateAfterDuration());
    }
    
    /// <summary>
    /// Desativa o Ultimate após a duração.
    /// </summary>
    private IEnumerator DeactivateUltimateAfterDuration()
    {
        yield return new WaitForSeconds(ultimateDuration);
        
        DeactivateUltimate();
    }
    
    /// <summary>
    /// Desativa o Ultimate e restaura valores originais.
    /// </summary>
    private void DeactivateUltimate()
    {
        Debug.Log("⏱️ Ultimate finalizado. Cooldown: " + ultimateCooldown + "s");
        
        isUltimateActive = false;
        
        // Restaurar valores originais
        if (offensiveAttributes != null)
        {
            offensiveAttributes.attackSpeedMelee = originalAttackSpeed;
        }
        
        if (defensiveAttributes != null)
        {
            defensiveAttributes.speedMultiplier = originalSpeed;
        }
        
        // Desativar invencibilidade
        if (grantInvulnerability && playerHealth != null)
        {
            playerHealth.isInvulnerable = false;
        }
        
        // Desativar VFX
        if (vfxController != null)
        {
            vfxController.SendMessage("StopEffect", SendMessageOptions.DontRequireReceiver);
        }
    }
    
    /// <summary>
    /// Retorna se o Ultimate está pronto para uso.
    /// </summary>
    public bool IsUltimateReady()
    {
        return isUltimateReady;
    }
    
    /// <summary>
    /// Retorna se o Ultimate está atualmente ativo.
    /// </summary>
    public bool IsUltimateActive()
    {
        return isUltimateActive;
    }
    
    /// <summary>
    /// Retorna o tempo restante de cooldown.
    /// </summary>
    public float GetCooldownRemaining()
    {
        return currentCooldown;
    }
    
    /// <summary>
    /// Retorna o progresso do cooldown (0-1).
    /// </summary>
    public float GetCooldownProgress()
    {
        if (isUltimateReady) return 1f;
        return 1f - (currentCooldown / ultimateCooldown);
    }
}
