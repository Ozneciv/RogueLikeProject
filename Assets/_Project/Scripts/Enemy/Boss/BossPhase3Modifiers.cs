using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Fase 3 do Boss Cromático — Modificadores de Escala.
///
/// RESPONSABILIDADES:
///   • Ao entrar na Fase 3: aumenta a velocidade de perseguição do NavMeshAgent em 40%
///   • Expõe AttackCooldownMultiplier (0.5) para scripts de ataques terrestres futuros
///
/// COMO OS COLEGAS USAM (ataques terrestres futuros):
///   float cooldown = baseCooldown * GetComponent<BossPhase3Modifiers>().AttackCooldownMultiplier;
///
/// SETUP NO UNITY:
///   1. Adicione este componente no mesmo GameObject do BossController
///   2. Nenhuma referência manual necessária — tudo via GetComponent
/// </summary>
public class BossPhase3Modifiers : MonoBehaviour
{
    // =====================================================
    // INSPECTOR
    // =====================================================

    [Header("Modificadores da Fase 3")]
    [Tooltip("Multiplicador de velocidade do NavMeshAgent ao entrar na Fase 3.\n" +
             "1.4 = +40% de velocidade de perseguição.")]
    [Range(1f, 3f)]
    public float speedMultiplier = 1.4f;

    [Tooltip("Multiplicador de cooldown para ataques terrestres da Fase 3.\n" +
             "0.5 = 50% menos tempo de recarga. Lido por scripts de ataque externos.")]
    [Range(0.1f, 1f)]
    public float attackCooldownMultiplier = 0.5f;

    [Header("Debug")]
    public bool showDebugLog = true;

    // =====================================================
    // ESTADO INTERNO
    // =====================================================

    private NavMeshAgent agent;
    private float originalSpeed = -1f;
    private bool phase3Active = false;

    // =====================================================
    // API PÚBLICA
    // =====================================================

    /// <summary>True quando os modificadores da Fase 3 estão aplicados.</summary>
    public bool IsPhase3Active => phase3Active;

    /// <summary>
    /// Multiplicador de cooldown para ataques terrestres da Fase 3.
    /// 0.5 = 50% de redução no tempo de recarga.
    /// Retorna 1.0 se a Fase 3 ainda não foi ativada.
    /// </summary>
    public float AttackCooldownMultiplier => phase3Active ? attackCooldownMultiplier : 1f;

    // =====================================================
    // UNITY LIFECYCLE
    // =====================================================

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    void OnEnable()
    {
        BossEvents.OnPhaseChanged += OnPhaseChanged;
        BossEvents.OnBossDefeated += OnBossDefeated;
    }

    void OnDisable()
    {
        BossEvents.OnPhaseChanged -= OnPhaseChanged;
        BossEvents.OnBossDefeated -= OnBossDefeated;
    }

    // =====================================================
    // HANDLERS DE EVENTO
    // =====================================================

    private void OnPhaseChanged(int newPhase)
    {
        if (newPhase == 3)
            ActivatePhase3();
    }

    private void OnBossDefeated()
    {
        // Restaura a velocidade original para debug/reset de arena
        if (phase3Active && agent != null && originalSpeed > 0f)
        {
            agent.speed = originalSpeed;
            if (showDebugLog)
                Debug.Log($"[Phase3Modifiers] Boss derrotado — speed restaurado para {originalSpeed:F1}");
        }
    }

    // =====================================================
    // ATIVAÇÃO DA FASE 3
    // =====================================================

    private void ActivatePhase3()
    {
        if (phase3Active) return;

        if (agent == null)
        {
            Debug.LogWarning("[Phase3Modifiers] ⚠️ NavMeshAgent não encontrado no Boss!");
            return;
        }

        originalSpeed    = agent.speed;
        agent.speed     *= speedMultiplier;
        phase3Active     = true;

        if (showDebugLog)
            Debug.Log($"[Phase3Modifiers] ⚡ Fase 3 ativada!\n" +
                      $"  • NavMesh speed: {originalSpeed:F2} → {agent.speed:F2} (+{(speedMultiplier - 1f) * 100:F0}%)\n" +
                      $"  • Attack cooldown multiplier: {attackCooldownMultiplier:F2} " +
                      $"(-{(1f - attackCooldownMultiplier) * 100:F0}%)");
    }
}
