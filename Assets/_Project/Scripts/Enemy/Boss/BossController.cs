using UnityEngine;
using UnityEngine.AI;
using System.Collections;

/// <summary>
/// Controlador central do Boss Cromático — Máquina de Estados (FSM).
///
/// RESPONSABILIDADES:
///   • Gerencia as transições entre fases (Phase1 → Phase2 → Phase3 → Dead)
///   • Controla o NavMeshAgent (perseguição, velocidade por fase)
///   • Monitora o HP via DummyHealth e dispara BossEvents nas transições
///   • Gerencia o estado de Stun (interrompe movimento e ataques)
///   • Executa o ataque melee base (os colegas adicionam ataques específicos por fase)
///
/// COMO OS COLEGAS CONECTAM SUAS FASES:
///   Os colegas NÃO editam este script. Eles criam scripts separados que:
///   1. Se inscrevem em BossEvents.OnPhaseChanged no OnEnable()
///   2. Quando recebem a fase correspondente, ativam sua lógica
///   3. Cancelam a inscrição no OnDisable()
///
/// SETUP NO UNITY:
///   1. Adicione este componente no GameObject do Boss
///   2. O DummyHealth é adicionado automaticamente (RequireComponent)
///   3. Arraste um BossPhaseConfig no campo "phaseConfig"
///   4. Adicione um NavMeshAgent (configurado pelo script no Start)
/// </summary>
[RequireComponent(typeof(DummyHealth))]
public class BossController : MonoBehaviour
{
    // =====================================================
    // ESTADOS DO BOSS
    // =====================================================

    public enum BossState
    {
        Idle,       // Antes do combate iniciar
        Phase1,     // 100% a 70% HP — Mestre do Solo
        Phase2,     // 70% a 35% HP — Refração e Caça Invisível
        Phase3,     // 35% a 0% HP — Núcleo Instável
        Stunned,    // Atordoado (qualquer fase)
        Dead        // Derrotado
    }

    // =====================================================
    // INSPECTOR
    // =====================================================

    [Header("Configuração")]
    [Tooltip("ScriptableObject com todos os parâmetros de balanceamento.\n" +
             "Crie via Assets → Create → Boss → Boss Phase Config.")]
    public BossPhaseConfig phaseConfig;

    [Header("Referências da Arena")]
    [Tooltip("O selo/barreira que bloqueia a saída da arena.\n" +
             "Será destruído quando o boss morrer.")]
    public GameObject arenaSeal;

    [Header("Debug")]
    public bool showDebugLog = true;

    // =====================================================
    // ESTADO PÚBLICO (somente leitura)
    // =====================================================

    /// <summary>Estado atual do boss.</summary>
    public BossState CurrentState { get; private set; } = BossState.Idle;

    /// <summary>Fase numérica atual (1, 2 ou 3). Retorna 0 se Idle/Dead.</summary>
    public int CurrentPhase { get; private set; } = 0;

    /// <summary>Porcentagem de HP atual (0.0 a 1.0).</summary>
    public float HealthPercent => health != null ? (float)health.CurrentHealth / health.maxHealth : 1f;

    /// <summary>True se o boss está atordoado.</summary>
    public bool IsStunned => CurrentState == BossState.Stunned;

    /// <summary>True se o boss está morto.</summary>
    public bool IsDead => CurrentState == BossState.Dead;

    /// <summary>True se a luta já começou.</summary>
    public bool IsFighting => CurrentState != BossState.Idle && CurrentState != BossState.Dead;

    /// <summary>True se o boss está em refração (invisível). Gerenciado externamente pelo Gabriel.</summary>
    public bool IsInvisible { get; private set; } = false;

    // =====================================================
    // ESTADO INTERNO
    // =====================================================

    private DummyHealth health;
    private NavMeshAgent agent;
    private Transform playerTransform;

    // Ataque melee
    private float meleeTimer = 0f;
    private bool isAttacking = false;

    // Stun
    private BossState stateBeforeStun;
    private Coroutine stunCoroutine;

    // Cache do HP anterior para detectar mudanças
    private int lastCheckedHP;

    // =====================================================
    // UNITY LIFECYCLE
    // =====================================================

    void Awake()
    {
        health = GetComponent<DummyHealth>();
        agent = GetComponent<NavMeshAgent>();
    }

    void Start()
    {
        // Aplica config do ScriptableObject
        if (phaseConfig != null)
        {
            health.maxHealth = phaseConfig.maxHealth;
            health.ResetHealth();

            if (agent != null)
            {
                agent.speed = phaseConfig.baseSpeed;
                agent.angularSpeed = phaseConfig.rotationSpeed;
                agent.enabled = false; // Desativado até a luta começar
            }
        }
        else
        {
            Debug.LogWarning("[BossController] ⚠️ BossPhaseConfig não atribuído! Usando defaults do DummyHealth.");
        }

        lastCheckedHP = health.maxHealth;

        // Configura o override de morte do DummyHealth para redirecionar para nossa lógica
        health.onDeathOverride = OnBossDeath;

        // Encontra o player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            playerTransform = player.transform;

        // Começa em Idle — espera o BossCombatTrigger chamar StartFight()
        CurrentState = BossState.Idle;
    }

    void Update()
    {
        if (CurrentState == BossState.Idle || CurrentState == BossState.Dead) return;

        // Monitora HP para transições de fase e eventos
        CheckHealthTransitions();

        // Timers
        if (meleeTimer > 0) meleeTimer -= Time.deltaTime;

        // Lógica por estado
        switch (CurrentState)
        {
            case BossState.Phase1:
            case BossState.Phase2:
            case BossState.Phase3:
                HandleCombatUpdate();
                break;

            case BossState.Stunned:
                // Não faz nada — o stun coroutine controla a saída
                break;
        }
    }

    void OnDestroy()
    {
        // Limpa todos os eventos para evitar referências fantasmas
        BossEvents.ClearAll();
    }

    // =====================================================
    // API PÚBLICA
    // =====================================================

    /// <summary>
    /// Inicia a luta com o boss. Chamado pelo BossCombatTrigger.
    /// Transiciona de Idle para Phase1.
    /// </summary>
    public void StartFight()
    {
        if (CurrentState != BossState.Idle) return;

        if (showDebugLog) Debug.Log("[BossController] ⚔️ LUTA INICIADA!");

        // Regarante referência ao player
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerTransform = player.transform;
        }

        // Ativa o NavMeshAgent
        if (agent != null) agent.enabled = true;

        // Entra na Fase 1
        TransitionToPhase(1);

        // Notifica todos
        BossEvents.RaiseBossFightStarted();
    }

    /// <summary>
    /// Aplica stun no boss. Pode ser chamado de qualquer lugar.
    /// O boss para de se mover e fica vulnerável por [duration] segundos.
    /// </summary>
    public void ApplyStun(float duration)
    {
        if (CurrentState == BossState.Dead || CurrentState == BossState.Idle) return;
        if (CurrentState == BossState.Stunned) return; // Não empilha stun

        float stunTime = 3.0f; // Duração padrão do stun (pode ser ajustada)

        if (stunCoroutine != null) StopCoroutine(stunCoroutine);
        stunCoroutine = StartCoroutine(StunRoutine(stunTime));
    }

    /// <summary>
    /// Ativa ou desativa a refração (invisibilidade).
    /// Chamado pelo script do Gabriel ou pela lógica de Fase 2.
    /// </summary>
    public void SetRefraction(bool invisible)
    {
        if (IsInvisible == invisible) return;
        IsInvisible = invisible;
        BossEvents.RaiseRefractionToggle(invisible);
    }

    // =====================================================
    // TRANSIÇÃO DE FASES
    // =====================================================

    private void CheckHealthTransitions()
    {
        if (health.CurrentHealth == lastCheckedHP) return;

        lastCheckedHP = health.CurrentHealth;
        float hpPercent = HealthPercent;

        // Notifica mudança de HP
        BossEvents.RaiseBossHealthChanged(hpPercent);

        // Verifica transições (só avança, nunca regride)
        if (CurrentPhase == 1 && hpPercent <= phaseConfig.phase2Threshold)
        {
            TransitionToPhase(2);
        }
        else if (CurrentPhase == 2 && hpPercent <= phaseConfig.phase3Threshold)
        {
            // Desativa refração se estiver ativa ao mudar de fase
            if (IsInvisible) SetRefraction(false);
            TransitionToPhase(3);
        }
        // Morte é tratada pelo onDeathOverride do DummyHealth
    }

    private void TransitionToPhase(int newPhase)
    {
        if (showDebugLog) Debug.Log($"[BossController] 🔄 FASE {CurrentPhase} → FASE {newPhase} (HP: {HealthPercent:P0})");

        // Sai do stun se estiver stunado durante transição
        if (CurrentState == BossState.Stunned && stunCoroutine != null)
        {
            StopCoroutine(stunCoroutine);
            stunCoroutine = null;
        }

        CurrentPhase = newPhase;

        switch (newPhase)
        {
            case 1:
                CurrentState = BossState.Phase1;
                break;
            case 2:
                CurrentState = BossState.Phase2;
                break;
            case 3:
                CurrentState = BossState.Phase3;
                break;
        }

        // Notifica todos os sistemas
        BossEvents.RaisePhaseChanged(newPhase);
    }

    // =====================================================
    // COMBATE
    // =====================================================

    private void HandleCombatUpdate()
    {
        if (playerTransform == null || isAttacking) return;

        float distToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        float meleeRange = phaseConfig != null ? phaseConfig.baseMeleeRange : 4f;

        // Perseguição via NavMesh
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.SetDestination(playerTransform.position);

            // Para perto do player para atacar
            agent.stoppingDistance = meleeRange * 0.8f;
        }

        // Rotação suave para o player
        HandleRotation();

        // Ataque melee base
        if (distToPlayer <= meleeRange && meleeTimer <= 0)
        {
            StartCoroutine(PerformMeleeAttack());
        }
    }

    private void HandleRotation()
    {
        if (playerTransform == null) return;

        Vector3 directionToPlayer = (playerTransform.position - transform.position).normalized;
        directionToPlayer.y = 0;

        if (directionToPlayer != Vector3.zero)
        {
            float rotSpeed = phaseConfig != null ? phaseConfig.rotationSpeed : 120f;
            Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotSpeed * Time.deltaTime);
        }
    }

    private IEnumerator PerformMeleeAttack()
    {
        isAttacking = true;
        float cooldown = phaseConfig != null ? phaseConfig.baseMeleeCooldown : 2.5f;
        meleeTimer = cooldown;

        // Wind-up (telegrafagem)
        if (showDebugLog) Debug.Log("[BossController] 👊 ATAQUE MELEE — Preparando...");

        // Para de se mover durante o ataque
        if (agent != null && agent.enabled && agent.isOnNavMesh)
            agent.isStopped = true;

        yield return new WaitForSeconds(0.4f);

        // Verifica hit
        float meleeRange = phaseConfig != null ? phaseConfig.baseMeleeRange : 4f;
        int meleeDamage = phaseConfig != null ? phaseConfig.baseMeleeDamage : 25;
        float hitRadius = meleeRange * 0.6f;

        Collider[] hits = Physics.OverlapSphere(transform.position + transform.forward * 2f, hitRadius);
        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                PlayerHealth playerHealth = hit.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(meleeDamage, gameObject);
                    if (showDebugLog) Debug.Log($"[BossController] 💥 MELEE HIT! Dano: {meleeDamage}");
                }
            }
        }

        // Recovery
        yield return new WaitForSeconds(0.3f);

        // Retoma movimento
        if (agent != null && agent.enabled && agent.isOnNavMesh)
            agent.isStopped = false;

        isAttacking = false;
    }

    // =====================================================
    // STUN
    // =====================================================

    private IEnumerator StunRoutine(float duration)
    {
        stateBeforeStun = CurrentState;
        CurrentState = BossState.Stunned;

        if (showDebugLog) Debug.Log($"[BossController] ⚡ STUNNED por {duration}s!");

        // Para completamente
        if (agent != null && agent.enabled && agent.isOnNavMesh)
            agent.isStopped = true;

        // Notifica todos
        BossEvents.RaiseBossStunned(duration);

        yield return new WaitForSeconds(duration);

        // Recupera
        if (CurrentState == BossState.Stunned) // Garante que não mudou durante o stun
        {
            CurrentState = stateBeforeStun;

            if (agent != null && agent.enabled && agent.isOnNavMesh)
                agent.isStopped = false;

            if (showDebugLog) Debug.Log($"[BossController] 🔄 Stun acabou. Voltando para {CurrentState}.");
        }

        stunCoroutine = null;
    }

    // =====================================================
    // MORTE
    // =====================================================

    /// <summary>
    /// Chamado pelo DummyHealth.onDeathOverride quando o HP chega a 0.
    /// NÃO usa o Destroy padrão — controla a animação de derrota.
    /// </summary>
    private void OnBossDeath()
    {
        if (CurrentState == BossState.Dead) return;

        CurrentState = BossState.Dead;
        CurrentPhase = 0;

        if (showDebugLog) Debug.Log("[BossController] 💀 BOSS DERROTADO!");

        // Para tudo
        if (agent != null && agent.enabled)
        {
            agent.isStopped = true;
            agent.enabled = false;
        }

        StopAllCoroutines();

        // Desativa refração
        if (IsInvisible) SetRefraction(false);

        // Destrói o selo da arena
        if (arenaSeal != null)
        {
            Destroy(arenaSeal);
            if (showDebugLog) Debug.Log("[BossController] 🚪 Selo da arena destruído — caminho aberto!");
        }

        // Notifica todos
        BossEvents.RaiseBossDefeated();

        // Animação provisória de morte: encolhe e some
        StartCoroutine(DeathAnimation());
    }

    private IEnumerator DeathAnimation()
    {
        // Animação provisória: encolhe durante 2 segundos
        float duration = 2f;
        float elapsed = 0f;
        Vector3 startScale = transform.localScale;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
            yield return null;
        }

        // Destroi o GameObject do boss
        Destroy(gameObject);
    }

    // =====================================================
    // GIZMOS
    // =====================================================

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        float meleeRange = phaseConfig != null ? phaseConfig.baseMeleeRange : 4f;

        // Range de melee
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, meleeRange);

        // Hitbox de melee
        Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
        Gizmos.DrawSphere(transform.position + transform.forward * 2f, meleeRange * 0.6f);

        // Indicador de fase (cor muda por fase)
        switch (CurrentPhase)
        {
            case 1: Gizmos.color = Color.cyan; break;
            case 2: Gizmos.color = Color.magenta; break;
            case 3: Gizmos.color = Color.green; break;
            default: Gizmos.color = Color.gray; break;
        }
        Gizmos.DrawWireCube(transform.position + Vector3.up * 3f, Vector3.one * 0.5f);
    }
#endif
}
