using UnityEngine;
using System;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

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
    [Tooltip("Distância máxima do player em metros para ativar a luta e a barra de vida.")]
    public float detectionDistance = 20f;

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

    [Tooltip("Triggers do Animator a serem sorteados nos ataques corpo a corpo (somente golpes físicos).")]
    public string[] meleeAttackTriggers = new string[] { "bossSwipe", "bossPunch" };

    [Tooltip("Triggers do Animator a serem sorteados nos ataques de Magia/Spell (visível ou invisível).")]
    public string[] spellAttackTriggers = new string[] { "bossSpell", "SimpleCast", "BossSpellWide", "PowerUp" };

    [Header("Hitbox da Mão / Ataques Físicos")]
    [Tooltip("Arraste aqui o Box Collider do seu objeto 'HITBOX HAND'.")]
    public Collider handCollider;

    [Tooltip("Componente BossHandHitbox (configurado automaticamente ao arrastar o handCollider acima).")]
    public BossHandHitbox handHitbox;

    [Header("Mímica do Golem (Stun do Céu)")]
    [Tooltip("Prefab do marcador de telegrafagem no chão (se nulo, usa indicador dinâmico).")]
    public GameObject stunMarkerPrefab;

    [Tooltip("Prefab do raio de energia que cai do céu (se nulo, usa StunBeam dinâmico).")]
    public GameObject stunBeamPrefab;

    [Header("Super Ataque (BossSpellWide)")]
    [Tooltip("Prefab do espinho de cristal a ser invocado no chão (se nulo, autodetectará o espinhoPrefab do MestreDoSolo).")]
    public GameObject wideSpinhoPrefab;

    [Tooltip("Dano de cada espinho do BossSpellWide.")]
    public int wideSpikeDamage = 45;


    [Header("Sangue Ácido (Invisibilidade)")]
    [Tooltip("Prefab do sangue ácido que pinga no chão durante a invisibilidade.")]
    [SerializeField] private GameObject toxicBloodPrefab;

    [Tooltip("Intervalo em segundos entre cada gota de sangue ácido.")]
    [SerializeField] private float toxicBloodInterval = 0.4f;

    [Tooltip("Transform posicionado no pé do Boss para spawnar o sangue no chão.")]
    [SerializeField] private Transform footSpawnPoint;

    [Header("UI da Barra de Vida")]
    [Tooltip("Prefab do Canvas da barra de vida do Boss. Se atribuído (ou em Resources/BossHealthBar_Canvas), será instanciado automaticamente no mapa.")]
    public GameObject bossHealthBarPrefab;

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

    // Sangue ácido — timer iniciado positivo para nunca spawnar no primeiro frame
    private float toxicBloodTimer = 2f;

    // =====================================================
    // UNITY LIFECYCLE
    // =====================================================

    void Awake()
    {
        health = GetComponent<DummyHealth>();
        agent = GetComponent<NavMeshAgent>();

        // Rigidbody do Boss: ultra pesado para o player NUNCA conseguir empurrá-lo
        Rigidbody rbBoss = GetComponent<Rigidbody>();
        if (rbBoss != null)
        {
            rbBoss.mass = 5000f;
            rbBoss.constraints = RigidbodyConstraints.FreezeRotation;
        }

        // Auto-conecta o componente Animator presente no modelo filho (Orc Idle)
        if (animator == null)
            animator = GetComponentInChildren<Animator>(true);

        if (animator != null)
        {
            animator.applyRootMotion = false; // Desativa Root Motion para evitar que animações elevem o Boss no ar
        }

#if UNITY_EDITOR
        if (animator != null && animator.runtimeAnimatorController == null)
        {
            animator.runtimeAnimatorController = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/_Project/Enemies/Boss/BossAnimation.controller");
        }

        if (toxicBloodPrefab == null)
        {
            toxicBloodPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Enemies/Boss/ToxicBlood.prefab");
        }
#endif

        if (toxicBloodPrefab == null)
        {
            toxicBloodPrefab = Resources.Load<GameObject>("ToxicBlood") 
                            ?? Resources.Load<GameObject>("Enemies/Boss/ToxicBlood");
        }

        // Auto-detecta o osso/transform do pé (footSpawnPoint) criado pelo Matheus
        if (footSpawnPoint == null)
        {
            Transform[] allTransforms = GetComponentsInChildren<Transform>(true);
            foreach (Transform t in allTransforms)
            {
                string nameLower = t.name.ToLower();
                if (nameLower.Contains("foot") || nameLower.Contains("pé") || nameLower.Contains("ankle"))
                {
                    footSpawnPoint = t;
                    break;
                }
            }
        }

        // Auto-detecta e configura a hitbox da mão a partir do handCollider (ex: HITBOX HAND)
        if (handCollider != null && handHitbox == null)
        {
            handHitbox = handCollider.GetComponent<BossHandHitbox>();
            if (handHitbox == null)
                handHitbox = handCollider.gameObject.AddComponent<BossHandHitbox>();
            handHitbox.handCollider = handCollider;
        }

        if (handHitbox == null)
            handHitbox = GetComponentInChildren<BossHandHitbox>(true);

        if (handHitbox == null)
        {
            Transform[] allTransforms = GetComponentsInChildren<Transform>(true);
            foreach (Transform t in allTransforms)
            {
                string n = t.name.ToLower();
                if (n.Contains("hand") || n.Contains("mão") || n.Contains("fist") || n.Contains("wrist") || n.Contains("arm"))
                {
                    Collider col = t.GetComponent<Collider>();
                    if (col != null)
                    {
                        handCollider = col;
                        handHitbox = t.gameObject.AddComponent<BossHandHitbox>();
                        handHitbox.handCollider = col;
                        break;
                    }
                }
            }
        }

        // 1. Garante que a Fase 2 (Refração / Invisibilidade) esteja sempre presente
        if (GetComponent<BossPhase2_Refraction>() == null)
        {
            gameObject.AddComponent<BossPhase2_Refraction>();
        }
    }

    void LateUpdate()
    {
        // Garante que o modelo filho (Orc Idle) permaneça perfeitamente alinhado no chão sem flutuar no ar
        if (animator != null && animator.transform != transform)
        {
            Vector3 localPos = animator.transform.localPosition;
            if (Mathf.Abs(localPos.y) > 0.01f || Mathf.Abs(localPos.x) > 0.01f || Mathf.Abs(localPos.z) > 0.01f)
            {
                animator.transform.localPosition = Vector3.zero;
            }
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

        // Garante que o Canvas da barra de vida exista na cena do Boss
        EnsureHealthBarUI();

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

        // Se o boss estiver em Idle, auto-inicia o combate se o player estiver dentro da distância de detecção ou se o boss tomar dano
        if (CurrentState == BossState.Idle)
        {
            if (playerTransform != null)
            {
                float distToPlayer = Vector3.Distance(transform.position, playerTransform.position);
                if (distToPlayer <= detectionDistance || (health != null && health.CurrentHealth < health.maxHealth))
                {
                    StartFight();
                }
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
        if (animator == null)
            animator = GetComponentInChildren<Animator>(true);

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

        // Se estiver executando qualquer ataque melee ou de magia (Spell), desativa IsWalking para não cancelar os triggers!
        BossPhase1_MestreDoSolo mestre = GetComponent<BossPhase1_MestreDoSolo>();
        if (isAttacking || (mestre != null && mestre.Atacando))
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

#if UNITY_EDITOR
        if (toxicBloodPrefab == null)
        {
            toxicBloodPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Enemies/Boss/ToxicBlood.prefab");
        }
#endif

        if (toxicBloodPrefab == null)
        {
            toxicBloodPrefab = Resources.Load<GameObject>("ToxicBlood")
                            ?? Resources.Load<GameObject>("Enemies/Boss/ToxicBlood");
        }

        if (toxicBloodPrefab == null) return;

        // Ponto de origem do sangue
        Vector3 origin = footSpawnPoint != null ? footSpawnPoint.position : transform.position + Vector3.up * 0.5f;
        Vector3 spawnPos = origin;

        if (Physics.Raycast(origin + Vector3.up * 0.5f, Vector3.down, out RaycastHit hit, 5.0f))
        {
            spawnPos = hit.point + Vector3.up * 0.02f;
        }
        else
        {
            spawnPos.y = transform.position.y + 0.02f;
        }

        toxicBloodTimer -= Time.deltaTime;
        float distFromLast = Vector3.Distance(spawnPos, lastDripPosition);

        // Só dropa se o timer expirar E o Boss tiver se movido pelo menos 2.2 metros longe da poça anterior!
        if (toxicBloodTimer <= 0f && (lastDripPosition == Vector3.zero || distFromLast >= 2.2f))
        {
            toxicBloodTimer = 1.2f;
            lastDripPosition = spawnPos;

            Quaternion rot = toxicBloodPrefab.transform.rotation;
            GameObject bloodDrop = Instantiate(toxicBloodPrefab, spawnPos, rot);
            bloodDrop.transform.localScale = toxicBloodPrefab.transform.localScale;

            if (showDebugLog)
                Debug.Log($"[BossController] 🩸 Sangue ácido (Matheus) pingou com espaçamento de {distFromLast:F1}m em {spawnPos}");
        }
    }

    // Fallback removido — sem o prefab ToxicBlood conectado, simplesmente não spawna nada.

    void OnDestroy()
    {
        // Limpa todos os eventos para evitar referências fantasmas
        BossEvents.ClearAll();
    }

    // =====================================================
    // API PÚBLICA
    // =====================================================

    // Colisão sólida: o Boss possui Rigidbody de 5000kg e CapsuleCollider ativo, agindo como uma parede sólida contra o player.

    /// <summary>
    /// Dispara uma das animações de magia/spell do Boss (ex: invocação de pilares).
    /// Congela a movimentação 100% durante o feitiço.
    /// </summary>
    public void TriggerSpellAnimation()
    {
        if (animator == null) return;

        FreezeMovementForSpell(1.4f);

        if (spellAttackTriggers != null && spellAttackTriggers.Length > 0)
        {
            string chosen = spellAttackTriggers[UnityEngine.Random.Range(0, spellAttackTriggers.Length)];
            animator.SetTrigger(chosen);
        }
        else
        {
            animator.SetTrigger("bossSpell");
        }
    }

    /// <summary>
    /// Congela a movimentação do Boss 100% (agent.speed = 0) durante o cast de feitiços.
    /// </summary>
    public void FreezeMovementForSpell(float duration = 1.4f)
    {
        StartCoroutine(SpellFreezeRoutine(duration));
    }

    private IEnumerator SpellFreezeRoutine(float duration)
    {
        if (agent == null) yield break;

        float baseNavSpeed = phaseConfig != null ? phaseConfig.baseSpeed : 4.8f;
        
        // Congela 100% a movimentação do NavMeshAgent durante o feitiço
        agent.speed = 0f;
        agent.isStopped = true;

        yield return new WaitForSeconds(duration);

        if (agent != null && !IsStunned && !IsDead)
        {
            // Restaura a movimentação e velocidade normal
            agent.speed = baseNavSpeed;
            agent.isStopped = false;
        }
    }

    /// <summary>
    /// Dispara a animação PowerUP durante a transição da Fase 1 para a Fase 2.
    /// O Boss fica INVULNERÁVEL e congelado durante toda a animação.
    /// </summary>
    public void TriggerPowerUP(float invulnerabilityDuration = 2.5f)
    {
        FreezeMovementForSpell(invulnerabilityDuration);
        StartCoroutine(PowerUPInvulnerabilityRoutine(invulnerabilityDuration));
    }

    private IEnumerator PowerUPInvulnerabilityRoutine(float duration)
    {
        if (health != null) health.isInvulnerable = true;

        if (animator != null)
        {
            animator.SetTrigger("PowerUp");
        }

        if (showDebugLog) Debug.Log($"[BossController] 🛡️ Boss INVULNERÁVEL e CONGELADO durante o PowerUP ({duration}s)!");

        yield return new WaitForSeconds(duration);

        if (health != null && !IsDead) health.isInvulnerable = false;

        if (showDebugLog) Debug.Log("[BossController] ⚔️ Invulnerabilidade do PowerUP encerrada! Boss pronto para combate na Fase 2.");
    }

    /// <summary>
    /// Ataque Mímico do Golem (SimpleCast): invoca um raio RÁPIDO do céu diretamente no jogador (0.45s de telegrafagem).
    /// </summary>
    public void PerformGolemStunCast(Vector3 targetPosition = default, float stunRadius = 5.0f, float stunDuration = 2.5f, float telegraphTime = 0.45f)
    {
        FreezeMovementForSpell(telegraphTime + 0.6f);
        StartCoroutine(GolemStunCastRoutine(stunRadius, stunDuration, telegraphTime));
    }

    private IEnumerator GolemStunCastRoutine(float stunRadius, float stunDuration, float telegraphTime)
    {
        if (animator != null) animator.SetTrigger("SimpleCast");

        // Rastreia a posição exata do player no momento do cast
        if (playerTransform == null) playerTransform = FindPlayerTransform();

        Vector3 targetPos = (playerTransform != null) ? playerTransform.position : transform.position + transform.forward * 4f;

        // Vira o Boss instantaneamente para encarar o player
        if (playerTransform != null)
        {
            Vector3 lookDir = (playerTransform.position - transform.position).normalized;
            lookDir.y = 0f;
            if (lookDir != Vector3.zero) transform.rotation = Quaternion.LookRotation(lookDir);
        }

        Vector3 groundPos = new Vector3(targetPos.x, targetPos.y + 0.05f, targetPos.z);

        GameObject marker = null;
        if (stunMarkerPrefab != null)
        {
            marker = Instantiate(stunMarkerPrefab, groundPos, Quaternion.Euler(90, 0, 0));
            marker.transform.localScale = new Vector3(stunRadius * 2, stunRadius * 2, 1);
        }

        yield return new WaitForSeconds(telegraphTime);

        if (marker != null) Destroy(marker);

        // Instancia o raio de stun caindo do céu diretamente na posição onde o player estava (Versão Empoderada do Boss)
        if (stunBeamPrefab != null)
        {
            GameObject beam = Instantiate(stunBeamPrefab, groundPos, Quaternion.identity);
            StunBeam stunScript = beam.GetComponent<StunBeam>();
            if (stunScript != null)
            {
                stunScript.beamColor = new Color(0.65f, 0.15f, 0.95f); // Roxo Místico Imperial
                stunScript.flashColor = new Color(1.0f, 0.85f, 0.3f);  // Flash Dourado
                stunScript.pillarHeight = 14f;
                stunScript.particleCount = 65;
                stunScript.ringWidth = 1.0f;
                stunScript.Initialize(stunRadius, stunDuration);
            }
            Destroy(beam, 1.5f);
        }
        else
        {
            GameObject beamObj = new GameObject("Boss_Empowered_StunBeam");
            beamObj.transform.position = groundPos;
            StunBeam stunScript = beamObj.AddComponent<StunBeam>();
            stunScript.beamColor = new Color(0.65f, 0.15f, 0.95f); // Roxo Místico Imperial
            stunScript.flashColor = new Color(1.0f, 0.85f, 0.3f);  // Flash Dourado
            stunScript.pillarHeight = 14f;
            stunScript.particleCount = 65;
            stunScript.ringWidth = 1.0f;
            stunScript.Initialize(stunRadius, stunDuration);
        }

        if (showDebugLog) Debug.Log($"[BossController] ⚡ Stun mímico do Golem disparado DIRETAMENTE no player em {groundPos}!");
    }

    /// <summary>
    /// Super Ataque Devastador (BossSpellWide): Invoca uma formação massiva de espinhos de cristal em onda 360° com telegrafagem.
    /// </summary>
    public void PerformBossSpellWide()
    {
        StartCoroutine(BossSpellWideRoutine());
    }

    private IEnumerator BossSpellWideRoutine()
    {
        FreezeMovementForSpell(3.4f);

        if (animator != null) animator.SetTrigger("BossSpellWide");

        if (showDebugLog) Debug.Log("[BossController] 🌊 BossSpellWide ativado! Carregando super ataque de espinhos 360°...");

        GameObject prefabUse = wideSpinhoPrefab;
        if (prefabUse == null)
        {
            BossPhase1_MestreDoSolo mestre = GetComponent<BossPhase1_MestreDoSolo>();
            if (mestre != null && mestre.espinhoPrefab != null)
                prefabUse = mestre.espinhoPrefab;
        }

        Vector3 centerPos = transform.position;
        List<Vector3> spikePositions = new List<Vector3>();

        // Anel 1 (Raio 4m - 8 espinhos)
        int count1 = 8;
        for (int i = 0; i < count1; i++)
        {
            float angle = i * (360f / count1) * Mathf.Deg2Rad;
            Vector3 pos = centerPos + new Vector3(Mathf.Cos(angle) * 4f, 0.05f, Mathf.Sin(angle) * 4f);
            spikePositions.Add(pos);
        }

        // Anel 2 (Raio 8.5m - 12 espinhos)
        int count2 = 12;
        for (int i = 0; i < count2; i++)
        {
            float angle = (i * (360f / count2) + 15f) * Mathf.Deg2Rad;
            Vector3 pos = centerPos + new Vector3(Mathf.Cos(angle) * 8.5f, 0.05f, Mathf.Sin(angle) * 8.5f);
            spikePositions.Add(pos);
        }

        // Anel 3 (Raio 13m - 16 espinhos)
        int count3 = 16;
        for (int i = 0; i < count3; i++)
        {
            float angle = (i * (360f / count3) + 30f) * Mathf.Deg2Rad;
            Vector3 pos = centerPos + new Vector3(Mathf.Cos(angle) * 13f, 0.05f, Mathf.Sin(angle) * 13f);
            spikePositions.Add(pos);
        }

        // 1. Fase de Telegrafagem (1.2s): Marcações visuais de aviso no chão
        List<GameObject> indicators = new List<GameObject>();
        foreach (Vector3 p in spikePositions)
        {
            GameObject ind = null;
            if (stunMarkerPrefab != null)
            {
                ind = Instantiate(stunMarkerPrefab, p, Quaternion.Euler(90, 0, 0));
                ind.transform.localScale = new Vector3(2.5f, 2.5f, 1f);
            }
            else
            {
                ind = GameObject.CreatePrimitive(PrimitiveType.Quad);
                Destroy(ind.GetComponent<Collider>());
                ind.transform.position = p + Vector3.up * 0.02f;
                ind.transform.rotation = Quaternion.Euler(90, 0, 0);
                ind.transform.localScale = Vector3.one * 2.2f;
                
                Renderer r = ind.GetComponent<Renderer>();
                if (r != null)
                {
                    r.material.color = new Color(0.95f, 0.15f, 0.15f, 0.5f);
                }
            }
            if (ind != null) indicators.Add(ind);
        }

        TriggerCameraShake(0.4f, 0.15f);

        // Espera a telegrafagem para o jogador conseguir ver e desviar
        yield return new WaitForSeconds(1.2f);

        // Remove os indicadores
        foreach (GameObject ind in indicators)
        {
            if (ind != null) Destroy(ind);
        }

        TriggerCameraShake(0.5f, 0.25f);

        // 2. Fase de Erupção dos Espinhos do Chão
        List<Transform> spawnedSpikes = new List<Transform>();
        foreach (Vector3 p in spikePositions)
        {
            Vector3 spawnUnderground = p + Vector3.down * 3.5f;
            Quaternion rot = Quaternion.LookRotation(p - centerPos);
            if (prefabUse != null)
            {
                GameObject spikeObj = Instantiate(prefabUse, spawnUnderground, rot);
                spawnedSpikes.Add(spikeObj.transform);

                SpikeDamageDealer dealer = spikeObj.GetComponent<SpikeDamageDealer>();
                if (dealer == null) dealer = spikeObj.AddComponent<SpikeDamageDealer>();
                dealer.damage = wideSpikeDamage;

                Destroy(spikeObj, 4.5f);
            }
        }

        // Ergue todos os espinhos simultaneamente do chão em 0.35s
        float elapsed = 0f;
        float emergeDuration = 0.35f;
        List<Vector3> startPositions = new List<Vector3>();
        foreach (Transform s in spawnedSpikes) startPositions.Add(s.position);

        while (elapsed < emergeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / emergeDuration;
            for (int i = 0; i < spawnedSpikes.Count; i++)
            {
                if (spawnedSpikes[i] != null)
                {
                    Vector3 targetPos = spikePositions[i];
                    spawnedSpikes[i].position = Vector3.Lerp(startPositions[i], targetPos, t);
                }
            }
            yield return null;
        }

        if (showDebugLog) Debug.Log("[BossController] waves Espinhos do BossSpellWide surgiram com sucesso!");
    }

    public void EnsureHealthBarUI()
    {
        if (FindObjectOfType<BossHealthBarUI>() == null)
        {
            if (bossHealthBarPrefab == null)
            {
                bossHealthBarPrefab = Resources.Load<GameObject>("BossHealthBar_Canvas")
                                ?? Resources.Load<GameObject>("UI/BossHealthBar_Canvas")
                                ?? Resources.Load<GameObject>("BossCanvas");
            }

            if (bossHealthBarPrefab != null)
            {
                Instantiate(bossHealthBarPrefab);
                if (showDebugLog) Debug.Log("[BossController] 🎨 Canvas da Barra de Vida instanciado no mapa!");
            }
        }
    }

    /// <summary>
    /// Inicia a luta com o boss. Chamado pelo BossCombatTrigger.
    /// Transiciona de Idle para Phase1.
    /// </summary>
    public void StartFight()
    {
        if (CurrentState != BossState.Idle) return;

        EnsureHealthBarUI();

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

        // Ao sair da invisibilidade, reseta o timer e posicao do drip
        // para que o sangue nao apareca imediatamente na proxima ativacao
        if (!invisible)
        {
            toxicBloodTimer = 1.2f;
            lastDripPosition = Vector3.zero;
        }

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

        // Dispara evento de dano se o HP diminuiu
        if (health.CurrentHealth < previousHP)
        {
            OnTookDamage?.Invoke(); 
        }

        if (phaseConfig != null)
        {
            int phase2HP = Mathf.RoundToInt(phaseConfig.phase2Threshold * health.maxHealth);
            int phase3HP = Mathf.RoundToInt(phaseConfig.phase3Threshold * health.maxHealth);

            float phaseHpPercent = 1.0f;

            if (CurrentPhase == 1)
            {
                phaseHpPercent = Mathf.InverseLerp(phase2HP, health.maxHealth, health.CurrentHealth);
            }
            else if (CurrentPhase == 2)
            {
                phaseHpPercent = Mathf.InverseLerp(phase3HP, phase2HP, health.CurrentHealth);
            }
            else if (CurrentPhase == 3)
            {
                phaseHpPercent = Mathf.InverseLerp(0, phase3HP, health.CurrentHealth);
            }

            // Notifica mudança de HP da fase atual (1.0 a 0.0) para a UI
            BossEvents.RaiseBossHealthChanged(phaseHpPercent);

            // Transição para Fase 2 ao quebrar o Casulo
            if (CurrentPhase == 1 && health.CurrentHealth <= phase2HP)
            {
                health.SetHealth(phase2HP);
                lastCheckedHP = phase2HP;
                TransitionToPhase(2);
                return;
            }
            // Transição para Fase 3
            else if (CurrentPhase == 2 && health.CurrentHealth <= phase3HP)
            {
                health.SetHealth(phase3HP);
                lastCheckedHP = phase3HP;
                if (IsInvisible) SetRefraction(false);
                TransitionToPhase(3);
                return;
            }
        }
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
                TriggerCameraShake(0.5f, 0.20f);
                StartCoroutine(CocoonShatterLightPulseRoutine());
                TriggerPowerUP();
                break;
            case 3:
                CurrentState = BossState.Phase3;
                TriggerCameraShake(0.5f, 0.22f);
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

    private IEnumerator CocoonShatterLightPulseRoutine()
    {
        GameObject flashLightObj = new GameObject("Phase2_Cocoon_Shatter_Flash");
        flashLightObj.transform.position = transform.position + Vector3.up * 2.0f;

        Light flashLight = flashLightObj.AddComponent<Light>();
        flashLight.color = new Color(0.95f, 0.30f, 1.00f);
        flashLight.intensity = 35f;
        flashLight.range = 30f;

        float elapsed = 0f;
        float duration = 0.85f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            if (flashLight != null)
            {
                flashLight.intensity = Mathf.Lerp(35f, 0f, t * t);
                flashLight.range = Mathf.Lerp(15f, 40f, t);
            }

            yield return null;
        }

        Destroy(flashLightObj);
    }

    // =====================================================
    // COMBATE
    // =====================================================

    private void HandleCombatUpdate()
    {
        if (isAttacking) return;

        // Se o player não existir, cancela a perseguição de combate
        if (playerTransform == null) return;

        // Na Fase 1 (Casulo) OU quando Invisível (Refração), o Boss NÃO ataca corpo a corpo (somente spells/mobs)!
        if (CurrentPhase == 1 || IsInvisible) return;

        // Orientação em direção ao player
        HandleRotation();

        // Checa distância para ataque melee
        float meleeRange = phaseConfig != null ? phaseConfig.baseMeleeRange : 6.5f;
        float distToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        if (distToPlayer <= meleeRange && meleeTimer <= 0f)
        {
            StartCoroutine(PerformMeleeAttack());
            return;
        }

        // Movimentação em direção ao player via NavMeshAgent
        float speed = phaseConfig != null ? phaseConfig.baseSpeed : 4f;

        // Enquanto invisível na Fase 2, o boss ganha +35% de velocidade para se mover com maior fluidez
        if (IsInvisible) speed *= 1.35f;

        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.speed = speed;
            agent.isStopped = false;
            agent.SetDestination(playerTransform.position);

            // Ajusta a distância de parada para colar no player no ataque
            agent.stoppingDistance = meleeRange * 0.8f;
            return;
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
        float cooldown = IsInvisible ? (baseCooldown * 0.6f) : baseCooldown;
        meleeTimer = cooldown;

        // Se estiver invisível, revela temporariamente o Boss para a animação do ataque ser visível!
        BossPhase2_Refraction refractionComp = GetComponent<BossPhase2_Refraction>();
        if (refractionComp != null && IsInvisible)
        {
            refractionComp.SetTemporaryVisibility(true);
        }

        if (showDebugLog) Debug.Log("[BossController] 👊 ATAQUE DO BOSS — Executando golpe exclusivo!");

        if (agent != null && agent.enabled && agent.isOnNavMesh)
            agent.isStopped = true;

        Vector3 blastPos = transform.position + transform.forward * 1.5f;

        if (animator != null && meleeAttackTriggers != null && meleeAttackTriggers.Length > 0)
        {
            string selectedTrigger = meleeAttackTriggers[UnityEngine.Random.Range(0, meleeAttackTriggers.Length)];
            
            // Mapeia o nome do gatilho (ex: bossSwipe) para o nome exato do Estado no Animator (ex: BossSwipe)
            string stateName = selectedTrigger;
            if (selectedTrigger == "bossSwipe") stateName = "BossSwipe";
            if (selectedTrigger == "bossPunch") stateName = "BossPunch";
            if (selectedTrigger == "Spell") stateName = "Spell";

            animator.ResetTrigger("bossSwipe");
            animator.ResetTrigger("bossPunch");
            animator.ResetTrigger("Spell");

            animator.SetTrigger(selectedTrigger);
            animator.Play(stateName, 0, 0f);
        }

        float windUp = IsInvisible ? 0.10f : 0.15f;
        
        float elapsedWindUp = 0f;
        while (elapsedWindUp < windUp)
        {
            elapsedWindUp += Time.deltaTime;
            HandleRotation();
            yield return null;
        }

        // Ativa a Hitbox física da mão do Boss (ou fallback automático frontal se o componente não for arrastado)
        int dmg = phaseConfig != null ? phaseConfig.baseMeleeDamage : 35;
        if (handHitbox != null)
        {
            handHitbox.EnableHitbox(0.5f, dmg, 16f);
        }
        else
        {
            // Fallback automático de Hitbox da Mão no raio do soco/swipe
            Vector3 handHitPos = transform.position + transform.forward * 2.2f + Vector3.up * 1.2f;
            Collider[] hits = Physics.OverlapSphere(handHitPos, 2.5f);
            foreach (Collider h in hits)
            {
                if (h.CompareTag("Player"))
                {
                    PlayerHealth ph = h.GetComponent<PlayerHealth>();
                    if (ph != null) ph.TakeDamage(dmg, gameObject);

                    Rigidbody pRb = h.GetComponent<Rigidbody>();
                    if (pRb != null && !pRb.isKinematic)
                    {
                        Vector3 push = (h.transform.position - transform.position).normalized;
                        push.y = 0.2f;
                        pRb.AddForce(push * 16f, ForceMode.Impulse);
                    }
                }
            }
        }

        // Dispara a Onda de Choque Exclusiva do Boss (AoE Knockback de 7.5 metros)
        blastPos = transform.position + transform.forward * 1.5f;
        BossAoEShockwave.TriggerBossExplosion(blastPos, 7.5f, 35, 16.0f);
        TriggerCameraShake(0.2f, 0.10f);

        // Recovery rápido e fluido
        yield return new WaitForSeconds(0.20f);

        // Se estava refratando, volta para o estado invisível para continuar fugindo
        if (refractionComp != null && IsInvisible)
        {
            refractionComp.SetTemporaryVisibility(false);
        }

        if (agent != null && agent.enabled && agent.isOnNavMesh)
            agent.isStopped = false;

        isAttacking = false;
    }

    public static void TriggerCameraShake(float duration = 0.35f, float magnitude = 0.12f)
    {
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCam.GetComponent<MonoBehaviour>()?.StartCoroutine(CameraShakeRoutine(mainCam.transform, duration, magnitude));
        }
    }

    private static IEnumerator CameraShakeRoutine(Transform camTransform, float duration, float magnitude)
    {
        Vector3 originalPos = camTransform.localPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float x = UnityEngine.Random.Range(-1f, 1f) * magnitude;
            float y = UnityEngine.Random.Range(-1f, 1f) * magnitude;

            camTransform.localPosition = originalPos + new Vector3(x, y, 0);
            yield return null;
        }

        camTransform.localPosition = originalPos;
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
