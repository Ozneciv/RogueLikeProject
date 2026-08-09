using UnityEngine;
using System;
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
    public bool OverrideMovement { get; set; } = false;

    [Header("Animação")]
    [Tooltip("O Animator do boss. Se nulo, tentará encontrar nos filhos.")]
    public Animator animator;

    [Tooltip("Triggers do Animator a serem sorteados nos ataques corpo a corpo.")]
    public string[] meleeAttackTriggers = new string[] { "bossSwipe", "bossPunch", "Attack1", "Attack2", "Spell" };


    [Header("Sangue Ácido (Invisibilidade)")]
    [Tooltip("Prefab do sangue ácido que pinga no chão durante a invisibilidade.")]
    [SerializeField] private GameObject toxicBloodPrefab;

    [Tooltip("Intervalo em segundos entre cada gota de sangue ácido.")]
    [SerializeField] private float toxicBloodInterval = 0.4f;

    [Tooltip("Transform posicionado no pé do Boss para spawnar o sangue no chão.")]
    [SerializeField] private Transform footSpawnPoint;

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

    // Sangue ácido
    private float toxicBloodTimer = 0f;

    // =====================================================
    // UNITY LIFECYCLE
    // =====================================================

    void Awake()
    {
        health = GetComponent<DummyHealth>();
        agent = GetComponent<NavMeshAgent>();
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        // 1. Garante que a Fase 2 (Refração / Invisibilidade) esteja sempre presente
        if (GetComponent<BossPhase2_Refraction>() == null)
        {
            gameObject.AddComponent<BossPhase2_Refraction>();
        }

        // 2. Tenta carregar o prefab de Sangue Ácido se estiver nulo
        if (toxicBloodPrefab == null)
        {
            toxicBloodPrefab = Resources.Load<GameObject>("ToxicBlood") 
                            ?? Resources.Load<GameObject>("Enemies/Boss/ToxicBlood");
        }
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
                agent.isStopped = true; // Parado até a luta começar (mantém aderência ao NavMesh)
            }
        }
        else
        {
            Debug.LogWarning("[BossController] ⚠️ BossPhaseConfig não atribuído! Usando defaults do DummyHealth.");
        }

        lastCheckedHP = health.maxHealth;

        // Configura o override de morte do DummyHealth para redirecionar para nossa lógica
        health.onDeathOverride = OnBossDeath;

        // Encontra o player com múltiplos fallbacks
        playerTransform = FindPlayerTransform();

        // Começa em Idle — espera o BossCombatTrigger ou auto-start em cenas de teste
        CurrentState = BossState.Idle;

        //StartFight();
    }

    private Transform FindPlayerTransform()
    {
        if (playerTransform != null && playerTransform.gameObject.activeInHierarchy)
            return playerTransform;

        // 1. Tenta por Tag "Player"
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) return p.transform;

        // 2. Tenta por Componente PlayerHealth
        PlayerHealth ph = FindObjectOfType<PlayerHealth>();
        if (ph != null) return ph.transform;

        // 3. Tenta qualquer GameObject cujo nome contenha "player"
        GameObject[] all = FindObjectsOfType<GameObject>();
        foreach (GameObject obj in all)
        {
            if (obj.name.ToLower().Contains("player"))
                return obj.transform;
        }

        return null;
    }

    void Update()
    {
        UpdateAnimationState();

        if (playerTransform == null)
            playerTransform = FindPlayerTransform();

        // Se o boss estiver em Idle, auto-inicia o combate se o player existir na cena
        if (CurrentState == BossState.Idle)
        {
            if (playerTransform != null || (health != null && health.CurrentHealth < health.maxHealth))
            {
                StartFight();
            }
        }

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

        // Sangue ácido durante invisibilidade
        HandleToxicBloodDrip();
    }

    private void UpdateAnimationState()
    {
        if (animator == null) return;

        if (CurrentState == BossState.Dead)
        {
            animator.SetBool("IsWalking", false);
            return;
        }

        if (CurrentState == BossState.Stunned)
        {
            animator.SetBool("IsWalking", false);
            return;
        }

        bool isMoving = false;
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            isMoving = agent.velocity.magnitude > 0.15f;
        }
        animator.SetBool("IsWalking", isMoving);
    }

    private Vector3 lastDripPosition;

    private void HandleToxicBloodDrip()
    {
        if (!IsInvisible) return;

        if (toxicBloodPrefab == null)
        {
            toxicBloodPrefab = Resources.Load<GameObject>("ToxicBlood")
                            ?? Resources.Load<GameObject>("Enemies/Boss/ToxicBlood");
        }

        if (toxicBloodPrefab == null)
        {
            toxicBloodPrefab = CreateFallbackToxicBloodPrefab();
        }

        // Ponto de origem: usa footSpawnPoint se atribuído; projeta raycast para o chão
        Vector3 origin = footSpawnPoint != null ? footSpawnPoint.position : transform.position + Vector3.up * 1f;
        Vector3 spawnPos = origin;

        // Projeta um Raycast para baixo para colar a poça de sangue perfeitamente na superfície do piso
        if (Physics.Raycast(origin + Vector3.up * 0.5f, Vector3.down, out RaycastHit hit, 5.0f))
        {
            spawnPos = hit.point + Vector3.up * 0.02f;
        }
        else
        {
            spawnPos.y = transform.position.y + 0.02f;
        }

        // Dripa sangue a cada intervalo
        toxicBloodTimer -= Time.deltaTime;
        if (toxicBloodTimer <= 0f)
        {
            toxicBloodTimer = (toxicBloodInterval > 0f) ? toxicBloodInterval : 0.35f;

            Quaternion randomRot = Quaternion.Euler(90f, UnityEngine.Random.Range(0f, 360f), 0f);
            GameObject bloodDrop = Instantiate(toxicBloodPrefab, spawnPos, randomRot);
            
            float scaleMult = UnityEngine.Random.Range(0.9f, 1.3f);
            bloodDrop.transform.localScale = Vector3.Scale(bloodDrop.transform.localScale, new Vector3(scaleMult, scaleMult, 1f));

            if (showDebugLog)
                Debug.Log($"[BossController] 🩸 Sangue ácido pingou em {spawnPos}");
        }
    }

    private GameObject CreateFallbackToxicBloodPrefab()
    {
        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = "ToxicBlood_Fallback";
        quad.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);
        Destroy(quad.GetComponent<Collider>());
        
        Renderer r = quad.GetComponent<Renderer>();
        if (r != null)
        {
            Material m = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Standard"));
            m.color = new Color(0.8f, 0.05f, 0.1f, 0.85f);
            r.material = m;
        }

        ToxicBlood tb = quad.AddComponent<ToxicBlood>();
        tb.lifetime = 3.5f;
        quad.SetActive(false);
        return quad;
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
            playerTransform = FindPlayerTransform();

        // Tenta ancorar no NavMesh se estiver solto
        if (agent != null && agent.enabled && !agent.isOnNavMesh)
        {
            if (UnityEngine.AI.NavMesh.SamplePosition(transform.position, out UnityEngine.AI.NavMeshHit hit, 10.0f, UnityEngine.AI.NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
            }
        }

        // Libera o NavMeshAgent para se mover
        if (agent != null && agent.isOnNavMesh) agent.isStopped = false;

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
    public event Action OnTookDamage;

    private void CheckHealthTransitions()
    {
        if (health.CurrentHealth == lastCheckedHP) return;

        int previousHP = lastCheckedHP;
        lastCheckedHP = health.CurrentHealth;
        float hpPercent = HealthPercent;

        // Dispara evento de dano se o HP diminuiu
        if (health.CurrentHealth < previousHP)
        {
            OnTookDamage?.Invoke(); 
        }

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

        // Garante que o agent está despausado ao mudar de fase
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = false;
        }

        // Notifica todos os sistemas
        BossEvents.RaisePhaseChanged(newPhase);
    }

    // =====================================================
    // COMBATE
    // =====================================================

    private void HandleCombatUpdate()
    {
        // Durante o ataque ou override de movimento, ignora atualização normal
        if (isAttacking || OverrideMovement) return;

        if (playerTransform == null)
            playerTransform = FindPlayerTransform();

        if (playerTransform == null) return;

        float distToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        float meleeRange = phaseConfig != null ? phaseConfig.baseMeleeRange : 4f;

        // Se estiver no alcance do ataque melee (com tolerância de 20%) e fora de cooldown, inicia o ataque
        if (distToPlayer <= (meleeRange * 1.2f) && meleeTimer <= 0f)
        {
            HandleRotation();
            StartCoroutine(PerformMeleeAttack());
            return;
        }

        float speed = phaseConfig != null ? phaseConfig.baseSpeed : 3.5f;

        if (agent != null && agent.enabled)
        {
            if (!agent.isOnNavMesh)
            {
                if (UnityEngine.AI.NavMesh.SamplePosition(transform.position, out UnityEngine.AI.NavMeshHit hit, 5.0f, UnityEngine.AI.NavMesh.AllAreas))
                {
                    agent.Warp(hit.position);
                }
            }

            if (agent.isOnNavMesh)
            {
                agent.isStopped = false;
                agent.SetDestination(playerTransform.position);
                agent.stoppingDistance = meleeRange * 0.8f;
                return;
            }
        }

        // FALLBACK PARA CENAS SEM NAVMESH BAKED (ex: Boss_Test sem NavMesh Surface):
        // Move o Transform diretamente em direção ao player para nunca ficar parado!
        Vector3 target = playerTransform.position;
        target.y = transform.position.y;

        float meleeStopRange = meleeRange * 0.8f;
        if (distToPlayer > meleeStopRange)
        {
            transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);

            Vector3 lookDir = target - transform.position;
            if (lookDir.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.LookRotation(lookDir);
        }
    }
    // --------------------

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
        float baseCooldown = phaseConfig != null ? phaseConfig.baseMeleeCooldown : 2.5f;
        // Enquanto invisível, reduz o cooldown em 40% para atacar com maior frequência
        float cooldown = IsInvisible ? (baseCooldown * 0.6f) : baseCooldown;
        meleeTimer = cooldown;

        // Wind-up (telegrafagem)
        if (showDebugLog) Debug.Log("[BossController] 👊 ATAQUE MELEE — Preparando...");

        // Para de se mover durante o ataque
        if (agent != null && agent.enabled && agent.isOnNavMesh)
            agent.isStopped = true;

        if (animator != null && meleeAttackTriggers != null && meleeAttackTriggers.Length > 0)
        {
            string selectedTrigger = meleeAttackTriggers[UnityEngine.Random.Range(0, meleeAttackTriggers.Length)];
            if (showDebugLog) Debug.Log($"[BossController] 🎬 Disparando Trigger de Animação: {selectedTrigger}");
            animator.SetTrigger(selectedTrigger);
        }
        else if (animator == null && showDebugLog)
        {
            Debug.LogWarning("[BossController] ⚠️ Componente Animator não encontrado no Boss!");
        }

        float windUp = IsInvisible ? 0.2f : 0.4f;
        yield return new WaitForSeconds(windUp);

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
