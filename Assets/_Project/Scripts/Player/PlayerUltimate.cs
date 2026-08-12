using UnityEngine;
using System.Collections;

/// <summary>
/// Gerenciador Central de Ultimate do Jogador (Core Hub).
/// Captura a tecla U, verifica se a arma está empunhada e dispara o gatilho da animação.
/// Sem travamentos por script: libera o controle do jogador imediatamente após o golpe.
/// </summary>
public class PlayerUltimate : MonoBehaviour
{
    [Header("Ultimate Settings")]
    [Tooltip("Tecla de ativação")]
    public KeyCode ultimateKey = KeyCode.U;

    [Tooltip("Tempo de recarga do Ultimate em segundos")]
    public float ultimateCooldown = 20f;
    
    [Tooltip("Impulso físico para a frente durante o pulo do Ultimate")]
    public float forwardLeapImpulse = 6.0f;

    [Tooltip("Invencibilidade durante o pulo/slam do Ultimate?")]
    public bool grantInvulnerability = true;

    [Header("Status (Read Only)")]
    [SerializeField] private bool isUltimateReady = true;
    [SerializeField] private bool isUltimateActive = false;
    [SerializeField] private float currentCooldown = 0f;

    // Referências
    private PlayerHealth playerHealth;
    private Player_WeaponManager weaponManager;
    private PrimaryAttackKnife attackScript;
    private Rigidbody playerRb;
    private Animator animator;

    void OnEnable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        RebindReferences();
    }

    public void RebindReferences()
    {
        isUltimateActive = false; // Reset emergencial na troca de cena
        playerHealth = GetComponent<PlayerHealth>() ?? GetComponentInParent<PlayerHealth>();
        playerRb = GetComponent<Rigidbody>() ?? GetComponentInParent<Rigidbody>();
        weaponManager = GetComponent<Player_WeaponManager>() ?? GetComponentInChildren<Player_WeaponManager>();
        attackScript = GetComponent<PrimaryAttackKnife>() ?? GetComponentInChildren<PrimaryAttackKnife>();

        if (weaponManager != null && weaponManager.playerAnimator != null && weaponManager.playerAnimator.isActiveAndEnabled)
        {
            animator = weaponManager.playerAnimator;
        }
        else
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (animator != null)
        {
            PlayerAnimationEvents animEvents = animator.GetComponent<PlayerAnimationEvents>();
            if (animEvents == null)
            {
                animator.gameObject.AddComponent<PlayerAnimationEvents>();
            }
        }

        Ultimate_Axe axeUlt = GetComponent<Ultimate_Axe>() ?? GetComponentInChildren<Ultimate_Axe>();
        if (axeUlt == null)
        {
            gameObject.AddComponent<Ultimate_Axe>();
        }

        UltimateUI ui = GetComponent<UltimateUI>() ?? GetComponentInChildren<UltimateUI>();
        if (ui == null)
        {
            gameObject.AddComponent<UltimateUI>();
        }
    }

    void Start()
    {
        RebindReferences();
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
                Debug.Log("[PlayerUltimate] ULTIMATE PRONTO! Pressione U para ativar.");
            }
        }

        // Ativar Ultimate com a tecla U (ignora se o CheatConsole estiver aberto)
        if (!CheatConsole.IsOpen && (Input.GetKeyDown(ultimateKey) || Input.GetKeyDown(KeyCode.U)))
        {
            Debug.Log($"[PlayerUltimate] Tecla U pressionada! Status: Ready = {isUltimateReady}, Active = {isUltimateActive}, Cooldown = {currentCooldown:F1}s");

            if (!isUltimateReady)
            {
                Debug.LogWarning($"⏳ [PlayerUltimate] Ultimate em recarga! Aguarde {currentCooldown:F1}s.");
                return;
            }

            if (isUltimateActive)
            {
                Debug.LogWarning("⚠️ [PlayerUltimate] Ultimate já está em execução!");
                return;
            }

            ActivateUltimate();
        }
    }

    public void ActivateUltimate()
    {
        Debug.Log("🚀 [PlayerUltimate] ActivateUltimate() iniciado!");
        RebindReferences();

        PrimaryAttackKnife primaryAttack = GetComponent<PrimaryAttackKnife>() ?? GetComponentInChildren<PrimaryAttackKnife>() ?? GetComponentInParent<PrimaryAttackKnife>();
        if (primaryAttack != null && primaryAttack.isAttacking)
        {
            primaryAttack.CancelAttackForDash();
        }

        // 1. VERIFICAÇÃO E ATIVAÇÃO DE ARMA AUTOMÁTICA
        if (weaponManager != null)
        {
            if (weaponManager.currentWeapon != null && !weaponManager.currentWeapon.activeInHierarchy)
            {
                weaponManager.currentWeapon.SetActive(true);
                weaponManager.isWeaponDrawn = true;
            }

            if (!weaponManager.isWeaponDrawn && weaponManager.currentWeapon != null)
            {
                weaponManager.isWeaponDrawn = true;
            }
        }

        Debug.Log("💥 [PlayerUltimate] ULTIMATE DISPARADO!");
        isUltimateActive = true;
        isUltimateReady = false;
        currentCooldown = ultimateCooldown;

        // Invencibilidade temporária
        if (grantInvulnerability && playerHealth != null)
        {
            playerHealth.isInvulnerable = true;
        }

        // Obter Animator atualizado
        if (weaponManager != null && weaponManager.playerAnimator != null && weaponManager.playerAnimator.isActiveAndEnabled)
        {
            animator = weaponManager.playerAnimator;
        }
        else if (animator == null || !animator.isActiveAndEnabled)
        {
            animator = GetComponentInChildren<Animator>();
        }

        // 2. DISPARAR ANIMAÇÃO DO ULTIMATE
        if (animator != null)
        {
            Debug.Log($"[PlayerUltimate] Animator encontrado no GameObject: '{animator.gameObject.name}'. Buscando parâmetros...");
            
            bool triggerFound = false;
            foreach (var param in animator.parameters)
            {
                if (param.name.Equals("Ult", System.StringComparison.OrdinalIgnoreCase) ||
                    param.name.Equals("Ultimate", System.StringComparison.OrdinalIgnoreCase) ||
                    param.name.Equals("JumpAxe", System.StringComparison.OrdinalIgnoreCase) ||
                    param.name.Equals("UltAxe", System.StringComparison.OrdinalIgnoreCase))
                {
                    animator.ResetTrigger(param.name);
                    animator.SetTrigger(param.name);
                    triggerFound = true;
                    Debug.Log($"[PlayerUltimate] GATILHO DE ANIMAÇÃO '{param.name}' DISPARADO COM SUCESSO NO ANIMATOR!");
                }
            }

            if (!triggerFound)
            {
                Debug.LogWarning("[PlayerUltimate] AVISO: Nenhum parâmetro com nome 'Ult', 'Ultimate' ou 'JumpAxe' foi encontrado no Animator. Forçando 'Ult'...");
                animator.ResetTrigger("Ult");
                animator.SetTrigger("Ult");
            }
        }
        else
        {
            Debug.LogError("[PlayerUltimate] ERRO CRÍTICO: Nenhum componente Animator foi encontrado no Player!");
        }

        // Ativar o Rastro da Arma (Trail Renderer) durante o Ultimate
        if (attackScript != null)
        {
            attackScript.SetTrailsEmitting(true);
        }

        // Parar corrotinas anteriores da UI/Unlock antes de disparar a habilidade
        StopAllCoroutines();

        // Delegar execução da habilidade para o script específico da arma equipada
        ExecuteWeaponSpecificUltimate();

        // Destravar os controles automaticamente assim que o clipe de animação terminar
        StartCoroutine(AutomaticUnlockCoroutine());
    }

    private IEnumerator AutomaticUnlockCoroutine()
    {
        // Aguarda um frame para o Animator transicionar para a animação da Ult
        yield return null;

        float duration = 1.2f;
        if (animator != null)
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.length > 0.2f)
            {
                duration = stateInfo.length;
            }
        }

        yield return new WaitForSeconds(duration);

        if (isUltimateActive)
        {
            Debug.Log($"⏱️ [PlayerUltimate] Fim da animação ({duration:F2}s). Destravando controles do jogador!");
            EndUltimateSequence();
        }
    }

    private void ExecuteWeaponSpecificUltimate()
    {
        WeaponType activeType = GetEquippedWeaponType();

        Ultimate_Axe axeUlt = GetComponent<Ultimate_Axe>() ?? GetComponentInChildren<Ultimate_Axe>() ?? GetComponentInParent<Ultimate_Axe>();
        if (axeUlt != null)
        {
            Debug.Log("🎯 [PlayerUltimate] Ultimate_Axe localizado! Chamando ExecuteUltimate()...");
            axeUlt.ExecuteUltimate();
        }
        else
        {
            Debug.LogError("❌ [PlayerUltimate] Componente Ultimate_Axe NÃO ENCONTRADO no Player! Adicionando automaticamente...");
            axeUlt = gameObject.AddComponent<Ultimate_Axe>();
            axeUlt.ExecuteUltimate();
        }
    }

    private WeaponType GetEquippedWeaponType()
    {
        if (weaponManager != null && weaponManager.rightHand != null && weaponManager.rightHand.childCount > 0)
        {
            WeaponOffset offset = weaponManager.rightHand.GetChild(0).GetComponent<WeaponOffset>();
            if (offset != null)
            {
                return offset.weaponType;
            }
        }
        return WeaponType.Axe;
    }

    private IEnumerator FallbackUnlockCoroutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (isUltimateActive)
        {
            EndUltimateSequence();
        }
    }

    /// <summary>
    /// Chamado pelo evento de animação ou após o impacto do slam para liberar o controle do jogador.
    /// </summary>
    public void EndUltimateSequence()
    {
        if (!isUltimateActive) return;

        Debug.Log("⏱️ [PlayerUltimate] Finalizando sequência do Ultimate. Controle liberado!");
        isUltimateActive = false;

        // Desativar invencibilidade
        if (grantInvulnerability && playerHealth != null)
        {
            playerHealth.isInvulnerable = false;
        }

        // Resetar gatilhos e combos
        if (animator != null)
        {
            animator.ResetTrigger("Ult");
            animator.SetInteger("ComboStep", 0);
        }

        if (attackScript != null)
        {
            attackScript.SetTrailsEmitting(false);
            attackScript.ResetCombo();
        }
    }

    public bool IsUltimateReady() => isUltimateReady;
    public bool IsUltimateActive() => isUltimateActive;
    public float GetCooldownRemaining() => currentCooldown;
    public float GetCooldownProgress() => isUltimateReady ? 1f : (1f - (currentCooldown / ultimateCooldown));
}
