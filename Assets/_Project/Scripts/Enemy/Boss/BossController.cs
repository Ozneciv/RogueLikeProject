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

    [Tooltip("Triggers do Animator a serem sorteados nos ataques corpo a corpo (4 ataques: Swipe, Punch, JumpAttack, Stomp).")]
    public string[] meleeAttackTriggers = new string[] { "bossSwipe", "bossPunch", "bossJumpAttack", "bossStomp" };

    [Tooltip("Triggers do Animator a serem sorteados nos ataques de Magia/Spell (visível ou invisível).")]
    public string[] spellAttackTriggers = new string[] { "bossSpell", "SimpleCast", "BossSpellWide", "PowerUp" };

    [Header("VFX de Impacto em Área (Stomp & Jump Attack)")]
    [Tooltip("Objeto de VFX de Pisada (Stomp) pré-posicionado como filho no Prefab do Boss.")]
    public GameObject stompVFXChildObject;

    [Tooltip("Prefab de VFX de onda de choque no solo para a Pisada (Stomp).")]
    public GameObject vfxStompPrefab;

    [Tooltip("Escala multiplicadora do VFX de Pisada (Padrão: 2.5 para uma pisada gigante e imponente).")]
    public float stompVFXScale = 2.5f;

    [Tooltip("Distância à frente do Boss onde o pé/onda de choque atinge o solo (Padrão: 0.8 metros).")]
    public float stompForwardOffset = 0.8f;

    [Tooltip("Objeto de VFX de Salto (Jump) pré-posicionado como filho no Prefab do Boss.")]
    public GameObject jumpAttackVFXChildObject;

    [Tooltip("Prefab de VFX de impacto explosivo no solo para o Salto Esmagador (Jump Attack).")]
    public GameObject vfxJumpAttackPrefab;

    [Tooltip("Escala multiplicadora do VFX de Salto Esmagador.")]
    public float jumpAttackVFXScale = 2.5f;

    [Header("Hitboxes & Trails das Mãos (Inspector)")]
    [Tooltip("Componente BossHandHitbox da Mão Esquerda.")]
    public BossHandHitbox leftHandHitbox;
    [Tooltip("Componente BossHandHitbox da Mão Direita.")]
    public BossHandHitbox rightHandHitbox;

    [Header("🎯 Inclinação de Ataque no Chão (Opção 1 - Spine Tilt)")]
    [Tooltip("Osso da coluna/peito (Spine/Chest) do seu Rig atual que será inclinado para baixo durante os ataques.")]
    public Transform spineBone;
    [Tooltip("Ângulo de inclinação em graus para baixo durante o golpe (0 = Postura normal ereta).")]
    public float attackTiltAngle = 0.0f;
    [Tooltip("Velocidade de suavização do movimento de inclinação.")]
    public float tiltSmoothSpeed = 12.0f;
    private float currentTilt = 0f;

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

    [Header("💧 Cuspida Ácida (Fase 3)")]
    [Tooltip("Prefab do projétil com AcidSpitProjectile + Rigidbody + Collider (non-trigger).")]
    public GameObject acidSpitProjectilePrefab;

    [Tooltip("Prefab da AcidPuddle — fallback se não houver AcidPuddleSpawner na cena.")]
    public GameObject acidPuddlePrefabForSpit;

    [Tooltip("Trigger do Animator no telegraph (cabeça recua). Troque pelo clip de spit quando disponível.")]
    public string acidSpitAnimTrigger = "SimpleCast";

    [Tooltip("Duração do telegraph visual antes de cuspir (segundos).")]
    public float acidSpitTelegraphDuration = 0.8f;

    [Tooltip("Cooldown entre cuspidas ácidas (segundos).")]
    public float acidSpitCooldown = 9f;

    [Tooltip("Distância mínima — boss não cuspe se player está no alcance melee.")]
    public float acidSpitMinRange = 4f;

    [Tooltip("Distância máxima para disparar a cuspida.")]
    public float acidSpitMaxRange = 16f;

    [Tooltip("Transform do osso/mão de onde o projétil sai. Se nulo usa chest + forward offset.")]
    public Transform acidSpitLaunchPoint;


    [Header("Sangue Ácido (Invisibilidade)")]
    [Tooltip("Prefab do sangue ácido que pinga no chão durante a invisibilidade.")]
    [SerializeField] private GameObject toxicBloodPrefab;

    [Tooltip("Prefab de partículas de gotejamento instanciado ao sofrer dano enquanto invisível.")]
    [SerializeField] private GameObject drippingParticlePrefab;

    [Tooltip("Intervalo em segundos entre cada gota de sangue ácido.")]
    [SerializeField] private float toxicBloodInterval = 0.4f;

    [Tooltip("Transform posicionado no pé do Boss para spawnar o sangue no chão.")]
    [SerializeField] private Transform footSpawnPoint;

    [Header("🎬 Configurações de VFX & Impact Frame")]
    [Tooltip("Velocidade de reprodução do Impact Frame no PowerUp (padrão 0.5 para efeito cinematográfico lento e estendido).")]
    [Range(0.1f, 5.0f)] public float powerUpImpactFrameSpeed = 0.5f;

    [Header("⚡ Teleporte / Dash Estilo SharpBlur (Alternativa ao Sprint)")]
    [Tooltip("Se ativado, o Boss usará Teleporte com Holograma de Predição (estilo SharpBlur) para surpreender o jogador em vez de correr (Sprint).")]
    public bool enableSharpBlurTeleport = true;

    [Tooltip("Chance (0% a 100%) do Boss escolher teleportar em vez de correr quando estiver à distância.")]
    [Range(0f, 100f)] public float teleportChance = 100f;

    [Tooltip("Tempo de antecipação que o holograma/fantasma fica visível no destino antes do Boss teleportar (padrão: 0.25s).")]
    public float teleportAnticipationTime = 0.25f;

    [Tooltip("Distância de aproximação que o Boss reaparece perto do jogador (padrão: 2.8m).")]
    public float teleportDistanceOffset = 2.8f;

    [Tooltip("Fator de predição do movimento do player (0 = onde ele está, 1 = onde ele estará no reflexo).")]
    [Range(0f, 1f)] public float teleportLeadPrediction = 0.5f;

    [Tooltip("Prefab do Holograma com DashHologram.cs (se nulo, autodetectará hologramPrefab).")]
    public GameObject teleportHologramPrefab;

    [Tooltip("Material translúcido para o holograma do teleporte.")]
    public Material teleportHologramMaterial;

    [Tooltip("Transparência do holograma fantasma (0.1 a 1.0).")]
    [Range(0.1f, 1f)] public float teleportHologramAlpha = 0.6f;

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

    // Ataque melee e Locomoção
    private float meleeTimer = 0f;
    private float acidSpitTimer = 0f;
    private bool isAttacking = false;
    private bool isSprinting = false;
    private string lastMeleeAttack = "";
    private float lastStompImpactTime = -10f;
    private float lastJumpImpactTime = -10f;
    [HideInInspector] public List<BossHandHitbox> allHandHitboxes = new List<BossHandHitbox>();

    public void DesativarTodosOsTrails()
    {
        foreach (BossHandHitbox hb in allHandHitboxes)
        {
            if (hb != null) hb.DisableHitbox();
        }
    }

    public void EnableMeleeHitboxBothHands(float duration, int attackDamage = 35, float pushForce = 15f)
    {
        foreach (BossHandHitbox hb in allHandHitboxes)
        {
            if (hb != null) hb.EnableHitbox(duration, attackDamage, pushForce);
        }
    }

    // Stun
    private BossState stateBeforeStun;
    private Coroutine stunCoroutine;

    // Cache do HP anterior para detectar mudanças
    private int lastCheckedHP;

    // Sangue ácido — timer iniciado positivo para nunca spawnar no primeiro frame
    private float toxicBloodTimer = 2f;

    /// <summary>
    /// Limpa e Sanitiza todos os Colisores do Boss:
    ///  • Garante que APENAS o CapsuleCollider principal da raiz (raio 0.45m, altura 2.8m) seja sólido.
    ///  • Converte TODOS os colisores nos objetos filhos (MeshColliders de FBX, Casulo, Hitboxes) para isTrigger = true!
    ///  • Elimina a barreira invisível gigante que impedia o player de se aproximar.
    /// </summary>
    [ContextMenu("🧹 Purge & Fix Extra Boss Colliders")]
    public void SanitizeBossColliders()
    {
        CapsuleCollider rootCapsule = GetComponent<CapsuleCollider>();
        if (rootCapsule == null)
        {
            rootCapsule = gameObject.AddComponent<CapsuleCollider>();
        }

        rootCapsule.isTrigger = false;
        rootCapsule.radius = 0.45f;
        rootCapsule.height = 2.8f;
        rootCapsule.center = new Vector3(0f, 1.4f, 0f);

        // Varre e sanitiza todos os colisores nos filhos
        Collider[] allChildColliders = GetComponentsInChildren<Collider>(true);
        foreach (Collider col in allChildColliders)
        {
            if (col != rootCapsule)
            {
                if (col is MeshCollider mc)
                {
                    mc.convex = true;
                }
                col.isTrigger = true;
            }
        }
        
        DebugListAllColliders();
    }

    [ContextMenu("🔍 List All Boss Colliders")]
    public void DebugListAllColliders()
    {
        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        Debug.Log($"<color=cyan>=== 🔍 DIAGNÓSTICO DE COLISORES DO BOSS ({colliders.Length} ENCONTRADOS) ===</color>");
        
        foreach (Collider col in colliders)
        {
            string path = GetGameObjectPath(col.gameObject);
            bool isRoot = col.gameObject == gameObject;
            string status = col.isTrigger ? "<color=green>[TRIGGER - OK]</color>" : "<color=red>⚠️ [SÓLIDO - PODE BLOQUEAR FISICAMENTE!]</color>";
            
            string info = "";
            if (col is CapsuleCollider cc) info = $"CapsuleCollider (Raio: {cc.radius:F2}, Altura: {cc.height:F2})";
            else if (col is SphereCollider sc) info = $"SphereCollider (Raio: {sc.radius:F2})";
            else if (col is BoxCollider bc) info = $"BoxCollider (Tamanho: {bc.size})";
            else if (col is MeshCollider mc) info = $"MeshCollider (Convex: {mc.convex})";
            else info = col.GetType().Name;

            Debug.Log($"👾 Objeto: <b>{col.gameObject.name}</b> | {status} | Tipo: {info} | Caminho: <i>{path}</i>");
        }
    }

    private string GetGameObjectPath(GameObject obj)
    {
        string path = obj.name;
        Transform curr = obj.transform.parent;
        while (curr != null)
        {
            path = curr.name + "/" + path;
            curr = curr.parent;
        }
        return path;
    }

    // Instância viva do sistema de partículas de gotejamento (dripping contínuo durante refração)
    private ParticleSystem drippingInstance;

    // =====================================================
    // UNITY LIFECYCLE
    // =====================================================

    void OnDisable()
    {
        if (Time.timeScale < 1.0f)
        {
            Time.timeScale = 1.0f;
            Time.fixedDeltaTime = 0.02f;
        }
    }

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

        // Sanitiza colisores: Garante que APENAS o CapsuleCollider raiz (raio 0.45m) seja sólido
        SanitizeBossColliders();

        // Auto-conecta o componente Animator presente no modelo filho (Orc Idle)
        if (animator == null)
            animator = GetComponentInChildren<Animator>(true);

        if (animator != null)
        {
            animator.applyRootMotion = false; // Desativa Root Motion para evitar que animações elevem o Boss no ar

            if (animator.GetComponent<BossAnimationEvents>() == null)
            {
                animator.gameObject.AddComponent<BossAnimationEvents>();
            }
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

        if (bossHealthBarPrefab == null)
        {
            bossHealthBarPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Enemies/Boss/Canvas-Boss.prefab");
        }
#endif

        if (bossHealthBarPrefab == null)
        {
            bossHealthBarPrefab = Resources.Load<GameObject>("BossHealthBar_Canvas")
                            ?? Resources.Load<GameObject>("Canvas-Boss")
                            ?? Resources.Load<GameObject>("UI/BossHealthBar_Canvas");
        }

#if UNITY_EDITOR
        if (drippingParticlePrefab == null)
        {
            drippingParticlePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Enemies/Boss/BossDrippingFX.prefab");
        }
#endif

        if (drippingParticlePrefab == null)
        {
            drippingParticlePrefab = Resources.Load<GameObject>("BossDrippingFX") 
                                  ?? Resources.Load<GameObject>("Enemies/Boss/BossDrippingFX");
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

        // Auto-detecta o osso da coluna/peito (spineBone) do Rig atual
        if (spineBone == null)
        {
            Transform[] allBones = GetComponentsInChildren<Transform>(true);
            foreach (Transform bone in allBones)
            {
                string n = bone.name.ToLower();
                if (n.Contains("spine") || n.Contains("chest") || n.Contains("coluna") || n.Contains("tronco") || n.Contains("torso"))
                {
                    spineBone = bone;
                    break;
                }
            }
        }

        // Auto-detecta e configura AS HITBOXES DE AMBAS AS MÃOS (Mão Esquerda e Mão Direita)
        BossHandHitbox[] existingHitboxes = GetComponentsInChildren<BossHandHitbox>(true);
        foreach (BossHandHitbox hb in existingHitboxes)
        {
            if (hb != null && !allHandHitboxes.Contains(hb))
            {
                allHandHitboxes.Add(hb);
            }
        }

        Transform[] handTransforms = GetComponentsInChildren<Transform>(true);
        foreach (Transform t in handTransforms)
        {
            string n = t.name.ToLower();
            if (n.Contains("hand") || n.Contains("mão") || n.Contains("fist") || n.Contains("wrist") || n.Contains("arm"))
            {
                BossHandHitbox hb = t.GetComponent<BossHandHitbox>() ?? t.gameObject.AddComponent<BossHandHitbox>();
                Collider col = t.GetComponent<Collider>();
                if (col != null) hb.handCollider = col;

                if (!allHandHitboxes.Contains(hb))
                {
                    allHandHitboxes.Add(hb);
                }
            }
        }

        foreach (BossHandHitbox hb in allHandHitboxes)
        {
            if (hb != null)
            {
                if (hb.handSide == HandSide.Left || hb.name.ToLower().Contains("left") || hb.name.ToLower().Contains("esquerda"))
                {
                    leftHandHitbox = hb;
                }
                else if (hb.handSide == HandSide.Right || hb.name.ToLower().Contains("right") || hb.name.ToLower().Contains("direita"))
                {
                    rightHandHitbox = hb;
                }
            }
        }

        if (leftHandHitbox == null && allHandHitboxes.Count > 0) leftHandHitbox = allHandHitboxes[0];
        if (rightHandHitbox == null && allHandHitboxes.Count > 1) rightHandHitbox = allHandHitboxes[1];

        if (GetComponent<BossPhase2_Refraction>() == null)
        {
            gameObject.AddComponent<BossPhase2_Refraction>();
        }

        DesativarTodosOsTrails();
    }

    // =====================================================
    // ANIMATION EVENTS (Separados por Mão Esquerda / Mão Direita / Ambas)
    // =====================================================
    public void AnimEvent_EnableRightHand()
    {
        if (rightHandHitbox != null) rightHandHitbox.OnAnimationEvent_EnableHitbox();
    }

    public void AnimEvent_DisableRightHand()
    {
        if (rightHandHitbox != null) rightHandHitbox.OnAnimationEvent_DisableHitbox();
    }

    public void AnimEvent_EnableLeftHand()
    {
        if (leftHandHitbox != null) leftHandHitbox.OnAnimationEvent_EnableHitbox();
    }

    public void AnimEvent_DisableLeftHand()
    {
        if (leftHandHitbox != null) leftHandHitbox.OnAnimationEvent_DisableHitbox();
    }

    public void AnimEvent_EnableBothHands()
    {
        if (leftHandHitbox != null) leftHandHitbox.OnAnimationEvent_EnableHitbox();
        if (rightHandHitbox != null) rightHandHitbox.OnAnimationEvent_EnableHitbox();
    }

    public void AnimEvent_DisableBothHands()
    {
        DesativarTodosOsTrails();
    }

    public void AnimEvent_GroundImpact()
    {
        if (leftHandHitbox != null) leftHandHitbox.OnAnimationEvent_GroundImpact();
        if (rightHandHitbox != null) rightHandHitbox.OnAnimationEvent_GroundImpact();
    }

    public void AnimEvent_StompImpact()
    {
        // Previne disparo duplo consecutivo (ex: AnimationEvent no FBX + chamada de segurança no script)
        if (Time.time - lastStompImpactTime < 0.35f)
        {
            return;
        }
        lastStompImpactTime = Time.time;

        // 1. Busca AUTOMATICAMENTE o objeto de VFX que é exclusivamente o Stomp (Pisada frontal no pé)
        if (stompVFXChildObject == null || stompVFXChildObject == jumpAttackVFXChildObject || stompVFXChildObject.name.ToLower().Contains("centro") || stompVFXChildObject.name.ToLower().Contains("radial"))
        {
            Transform found = transform.Find("Stomp") ?? transform.Find("VFX_Stomp") ?? transform.Find("Pisada") ?? transform.Find("StompVFX");
            if (found == null)
            {
                foreach (Transform child in GetComponentsInChildren<Transform>(true))
                {
                    string n = child.name.ToLower();
                    if ((n.Contains("stomp") || n.Contains("pisada")) && !n.Contains("centro") && !n.Contains("jump") && !n.Contains("radial"))
                    {
                        found = child;
                        break;
                    }
                }
            }
            if (found != null) stompVFXChildObject = found.gameObject;
        }

        // 2. Dispara o VFX nativo do Prefab mantendo sua posição no pé, rotação (-90° Y) e escala (0.65) intactas!
        if (stompVFXChildObject != null)
        {
            stompVFXChildObject.SetActive(true);

            ParticleSystem[] psList = stompVFXChildObject.GetComponentsInChildren<ParticleSystem>(true);
            if (psList != null && psList.Length > 0)
            {
                foreach (var ps in psList)
                {
                    ps.gameObject.SetActive(true);
                    ps.Clear(true);
                    ps.time = 0f;
                    ps.Play(true);
                }
            }

            // Dano progressivo em cone através do próprio VFX
            BossStompConeHitbox coneHitbox = stompVFXChildObject.GetComponent<BossStompConeHitbox>() ?? stompVFXChildObject.GetComponentInChildren<BossStompConeHitbox>();
            if (coneHitbox == null) coneHitbox = stompVFXChildObject.AddComponent<BossStompConeHitbox>();
            coneHitbox.StartWave(gameObject);

            Debug.Log($"🦶 [BOSS STOMP] ✅ Disparou VFX Nativo do Prefab: '{stompVFXChildObject.name}' (Posição, Rotação e Escala preservadas)!");
        }
        else
        {
            Vector3 pos = transform.position;
            if (Physics.Raycast(transform.position + Vector3.up * 1.5f, Vector3.down, out RaycastHit groundHit, 6.0f))
            {
                pos = groundHit.point + Vector3.up * 0.05f;
            }
            VFX_BossShockwave.CriarEfeitoOndaDeChoque(pos, 5.5f, new Color(0f, 0.95f, 1f, 0.95f), new Color(0.8f, 0.1f, 1f, 0f), 0.35f, 1.0f);
            AplicarDanoEKnockbackEmArea(pos, 5.0f, 35, 8.0f, 0.35f);
        }

        // Camera Shake, Ancoragem, VFX e Impact Frame da Vefects
        TriggerCameraShake(0.38f, 0.25f);
        VFXManager.Play(VFXType.BossStompShockwave, transform.position + transform.forward * 1.5f, transform.rotation);
        VFXManager.Play(VFXType.ImpactFrame, transform.position + transform.forward * 1.5f, transform.rotation);
        SnapToGround();
    }

    public void AnimEvent_JumpImpact()
    {
        // Previne disparo duplo consecutivo (ex: AnimationEvent no FBX + chamada de segurança no script)
        if (Time.time - lastJumpImpactTime < 0.35f)
        {
            return;
        }
        lastJumpImpactTime = Time.time;

        // 1. Busca AUTOMATICAMENTE o objeto de VFX de salto (Prioriza Stomp_Radial_360)
        if (jumpAttackVFXChildObject == null)
        {
            Transform found = transform.Find("Stomp_Radial_360") ?? transform.Find("JumpAttack") ?? transform.Find("VFX_JumpAttack") ?? transform.Find("Salto") ?? transform.Find("Jump") ?? transform.Find("centro");
            if (found == null)
            {
                foreach (Transform child in GetComponentsInChildren<Transform>(true))
                {
                    string n = child.name.ToLower();
                    if (n.Contains("radial") || n.Contains("jump") || n.Contains("salto") || n.Contains("centro"))
                    {
                        found = child;
                        break;
                    }
                }
            }
            if (found != null) jumpAttackVFXChildObject = found.gameObject;
        }

        // 2. Dispara o VFX nativo de salto do Prefab (Stomp_Radial_360) mantendo sua rotação (-90° Y) e escala (0.65) intactas!
        if (jumpAttackVFXChildObject != null)
        {
            jumpAttackVFXChildObject.SetActive(true);

            ParticleSystem[] psList = jumpAttackVFXChildObject.GetComponentsInChildren<ParticleSystem>(true);
            if (psList != null && psList.Length > 0)
            {
                foreach (var ps in psList)
                {
                    ps.gameObject.SetActive(true);
                    ps.Clear(true);
                    ps.time = 0f;
                    ps.Play(true);
                }
            }

            Debug.Log($"💥 [BOSS JUMP ATTACK] ✅ Disparou VFX Nativo de Salto (Stomp_Radial_360): '{jumpAttackVFXChildObject.name}'!");
        }
        else
        {
            Vector3 pos = transform.position;
            if (Physics.Raycast(transform.position + Vector3.up * 2.0f, Vector3.down, out RaycastHit groundHit, 6.0f))
            {
                pos = groundHit.point + Vector3.up * 0.05f;
            }
            VFX_BossShockwave.CriarEfeitoOndaDeChoque(pos, 7.0f, new Color(1f, 0.25f, 0.85f, 1.0f), new Color(0f, 0.9f, 1f, 0f), 0.45f, 1.5f);
        }

        // Knockback e dano de impacto massivo do Jump Attack (Raio: 8.5m, Dano: 45, Empurrão: 12.0f, Elevação: 0.45f)
        Vector3 impactPos = transform.position;
        AplicarDanoEKnockbackEmArea(impactPos, 8.5f, 45, 12.0f, 0.45f);
        TriggerCameraShake(0.55f, 0.40f);
        VFXManager.Play(VFXType.BossJumpShockwave, transform.position, Quaternion.identity);
        VFXManager.Play(VFXType.ImpactFrame, transform.position + Vector3.up * 0.5f, Quaternion.identity);
        Debug.Log("💥 [BOSS JUMP ATTACK] Salto esmagador com choque no solo e Impact Frame Vefects!");

        // Ancoragem de Solo: Impede que o Boss afunde 20cm ao pousar e se levantar!
        SnapToGround();
    }

    /// <summary>
    /// Ancoragem no solo no pouso do salto (impede que o Root Motion puxe o Boss para baixo do chão).
    /// </summary>
    public void SnapToGround()
    {
        StartCoroutine(SnapToGroundRoutine());
    }

    private IEnumerator SnapToGroundRoutine()
    {
        float timer = 0f;
        while (timer < 1.0f)
        {
            timer += Time.deltaTime;
            if (Physics.Raycast(transform.position + Vector3.up * 1.5f, Vector3.down, out RaycastHit hit, 5.0f))
            {
                if (transform.position.y < hit.point.y)
                {
                    transform.position = new Vector3(transform.position.x, hit.point.y, transform.position.z);
                }
            }
            yield return null;
        }

        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            if (UnityEngine.AI.NavMesh.SamplePosition(transform.position, out UnityEngine.AI.NavMeshHit navHit, 2.0f, UnityEngine.AI.NavMesh.AllAreas))
            {
                agent.Warp(navHit.position);
            }
        }
    }

    private void AplicarDanoEKnockbackEmArea(Vector3 centro, float raio, int dano, float forcaEmpurrao, float proporcaoVertical = 1.0f)
    {
        Collider[] hits = Physics.OverlapSphere(centro, raio);
        foreach (Collider col in hits)
        {
            if (col == null) continue;
            if (col.CompareTag("Player") || col.name.ToLower().Contains("player"))
            {
                PlayerHealth ph = col.GetComponent<PlayerHealth>() ?? col.GetComponentInParent<PlayerHealth>();
                if (ph != null) ph.TakeDamage(dano, gameObject);

                Rigidbody rbPlayer = col.GetComponent<Rigidbody>() ?? col.GetComponentInParent<Rigidbody>();
                if (rbPlayer != null && !rbPlayer.isKinematic)
                {
                    Vector3 dirHorizontal = (col.transform.position - centro);
                    dirHorizontal.y = 0f;
                    if (dirHorizontal == Vector3.zero) dirHorizontal = transform.forward;
                    dirHorizontal = dirHorizontal.normalized;

                    Vector3 dirFinal = (dirHorizontal + Vector3.up * proporcaoVertical).normalized;

                    // Reseta qualquer velocidade de queda atual para garantir um lançamento vertical perfeito no ar
                    Vector3 vel = rbPlayer.linearVelocity;
                    vel.y = 0f;
                    rbPlayer.linearVelocity = vel;

                    rbPlayer.AddForce(dirFinal * forcaEmpurrao, ForceMode.Impulse);
                }
            }
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

        // OPÇÃO 1: Inclinação suave da coluna (Spine Tilt) apontando os golpes para o chão durante os ataques
        if (spineBone != null)
        {
            bool isMeleeActive = isAttacking || 
                (leftHandHitbox != null && leftHandHitbox.enabled) || 
                (rightHandHitbox != null && rightHandHitbox.enabled);

            float targetTilt = isMeleeActive ? attackTiltAngle : 0f;
            currentTilt = Mathf.Lerp(currentTilt, targetTilt, Time.deltaTime * tiltSmoothSpeed);

            if (currentTilt > 0.1f)
            {
                spineBone.Rotate(Vector3.right * currentTilt, Space.Self);
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

        // Instancia o sistema de partículas de gotejamento como FILHO do Boss
        // para que caminhe junto com ele durante a refração (invisibilidade)
        if (drippingParticlePrefab != null && drippingInstance == null)
        {
            Vector3 offset = Vector3.up * 1.2f;
            GameObject drippingObj = Instantiate(drippingParticlePrefab, transform.position + offset, Quaternion.identity, transform);
            drippingObj.transform.localPosition = offset;
            drippingInstance = drippingObj.GetComponent<ParticleSystem>();
            if (drippingInstance != null)
            {
                drippingInstance.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

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
        if (acidSpitTimer > 0) acidSpitTimer -= Time.deltaTime;

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

        if (CurrentState == BossState.Dead || CurrentState == BossState.Stunned)
        {
            animator.SetBool("IsWalking", false);
            animator.SetBool("isSprinting", false);
            animator.SetFloat("Speed", 0f);
            return;
        }

        // Se estiver executando qualquer ataque melee ou de magia (Spell), desativa IsWalking e isSprinting para não cancelar os triggers!
        BossPhase1_MestreDoSolo mestre = GetComponent<BossPhase1_MestreDoSolo>();
        if (isAttacking || (mestre != null && mestre.Atacando))
        {
            animator.SetBool("IsWalking", false);
            animator.SetBool("isSprinting", false);
            animator.SetFloat("Speed", 0f);
            return;
        }

        bool isMoving = false;
        float currentMoveSpeed = 0f;
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            currentMoveSpeed = agent.velocity.magnitude;
            isMoving = currentMoveSpeed > 0.15f;
        }

        animator.SetBool("IsWalking", isMoving && !isSprinting);
        animator.SetBool("isSprinting", isMoving && isSprinting);
        animator.SetFloat("Speed", isMoving ? currentMoveSpeed : 0f);
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

    // TriggerDamageDripping removido — o gotejamento agora é contínuo via SetRefraction().

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
    /// <summary>
    /// Dispara uma das animações de magia/spell do Boss (ex: invocação de pilares).
    /// Congela a movimentação 100% durante o feitiço.
    /// </summary>
    public void TriggerSpellAnimation()
    {
        if (animator == null || !CanInitiateAction) return;

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
    public void FreezeMovementForSpell(float duration = 1.8f)
    {
        StartCoroutine(SpellFreezeRoutine(duration));
    }

    private IEnumerator SpellFreezeRoutine(float duration)
    {
        if (agent == null) yield break;

        float baseNavSpeed = phaseConfig != null ? phaseConfig.baseSpeed : 4.8f;
        
        // Congela 100% a movimentação do NavMeshAgent durante o feitiço
        agent.speed = 0f;
        agent.velocity = Vector3.zero;
        agent.isStopped = true;

        yield return new WaitForSeconds(duration);

        if (agent != null && !IsStunned && !IsDead && CanInitiateAction)
        {
            // Restaura a movimentação e velocidade normal se o Boss estiver livre
            agent.speed = baseNavSpeed;
            agent.isStopped = false;
        }
    }

    /// <summary>
    /// Dispara a animação PowerUP durante a transição da Fase 1 para a Fase 2.
    /// O Boss fica INVULNERÁVEL e congelado durante toda a animação.
    /// <summary>
    /// Dispara a animação PowerUP durante a transição de Fase.
    /// O Boss fica INVULNERÁVEL e 100% CONGELADO no lugar durante toda a animação.
    /// </summary>
    public void TriggerPowerUP(float invulnerabilityDuration = 2.5f)
    {
        DesativarTodosOsTrails();
        isCastingSpell = true;
        isSprinting = false;
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            agent.speed = 0f;
        }
        StartCoroutine(PowerUPInvulnerabilityRoutine(invulnerabilityDuration));
    }

    private IEnumerator PowerUPInvulnerabilityRoutine(float duration)
    {
        isCastingSpell = true;
        isSprinting = false;
        DesativarTodosOsTrails();
        if (health != null) health.isInvulnerable = true;

        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            agent.speed = 0f;
        }

        if (animator != null)
        {
            animator.SetBool("IsWalking", false);
            animator.SetBool("isSprinting", false);
            animator.SetFloat("Speed", 0f);
            animator.ResetTrigger("bossSwipe");
            animator.ResetTrigger("bossPunch");
            animator.ResetTrigger("bossJumpAttack");
            animator.ResetTrigger("bossStomp");
            animator.SetTrigger("PowerUp");
            animator.Play("PowerUp", 0, 0f);
        }

        // 🎬 CÂMERA LENTA TOTAL (Slow Motion 0.25x) + Impact Frame no PowerUp (com velocidade personalizada)!
        Time.timeScale = 0.25f;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;
        VFXManager.Play(VFXType.ImpactFrame, transform.position + Vector3.up * 1.5f, Quaternion.identity, 1.0f, powerUpImpactFrameSpeed);

        if (showDebugLog) Debug.Log($"[BossController] 🛡️⚡ Boss em POWERUP: Câmera Lenta Total (0.25x) e Impact Frame (Speed {powerUpImpactFrameSpeed}x) ativados por {duration}s!");

        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
            }
            yield return null;
        }

        // Restaura a velocidade normal do jogo
        Time.timeScale = 1.0f;
        Time.fixedDeltaTime = 0.02f;

        if (health != null && !IsDead) health.isInvulnerable = false;
        isCastingSpell = false;

        if (agent != null && agent.enabled && agent.isOnNavMesh && !IsStunned && !IsDead && CanInitiateAction)
        {
            agent.speed = phaseConfig != null ? phaseConfig.baseSpeed : 4.8f;
            agent.isStopped = false;
        }

        if (showDebugLog) Debug.Log("[BossController] ⚔️ PowerUP e Câmera Lenta encerrados! Velocidade normal restaurada.");
    }

    /// <summary>
    /// Ataque Mímico do Golem (SimpleCast): invoca um raio RÁPIDO do céu diretamente no jogador (0.45s de telegrafagem).
    /// </summary>
    public void PerformGolemStunCast(Vector3 targetPosition = default, float stunRadius = 5.0f, float stunDuration = 2.5f, float telegraphTime = 0.45f)
    {
        if (!CanInitiateAction) return;
        StartCoroutine(GolemStunCastRoutine(stunRadius, stunDuration, telegraphTime));
    }

    private IEnumerator GolemStunCastRoutine(float stunRadius, float stunDuration, float telegraphTime)
    {
        isCastingSpell = true;
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }

        try
        {
            FreezeMovementForSpell(telegraphTime + 0.6f);

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

            yield return new WaitForSeconds(0.4f);
        }
        finally
        {
            isCastingSpell = false;
            if (agent != null && agent.enabled && agent.isOnNavMesh && !IsStunned && !IsDead)
            {
                agent.isStopped = false;
            }
        }
    }

    /// <summary>
    /// Super Ataque Devastador (BossSpellWide): Invoca uma formação massiva de espinhos de cristal em onda 360° com telegrafagem.
    /// </summary>
    public void PerformBossSpellWide()
    {
        if (!CanInitiateAction) return;
        StartCoroutine(BossSpellWideRoutine());
    }

    private IEnumerator BossSpellWideRoutine()
    {
        isCastingSpell = true;
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }

        try
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
            float minDistance = 2.2f; // Espaçamento mínimo para NUNCA haver sobreposição

            // Gera 28 posições orgânicas e desorganizadas pela arena
            int totalSpikes = 28;
            int attempts = 0;
            while (spikePositions.Count < totalSpikes && attempts < 300)
            {
                attempts++;
                float radius = UnityEngine.Random.Range(3.5f, 14.5f);
                float angle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;

                Vector3 candidate = centerPos + new Vector3(Mathf.Cos(angle) * radius, 0.05f, Mathf.Sin(angle) * radius);

                bool overlap = false;
                foreach (Vector3 existing in spikePositions)
                {
                    if (Vector3.Distance(existing, candidate) < minDistance)
                    {
                        overlap = true;
                        break;
                    }
                }

                if (!overlap)
                {
                    spikePositions.Add(candidate);
                }
            }

            // 1. Fase de Telegrafagem (1.2s): Anéis Místicos Bonitos no Chão
            List<GameObject> indicators = new List<GameObject>();
            foreach (Vector3 p in spikePositions)
            {
                GameObject ind = null;
                if (stunMarkerPrefab != null)
                {
                    ind = Instantiate(stunMarkerPrefab, p, Quaternion.Euler(90, 0, 0));
                    ind.transform.localScale = new Vector3(2.4f, 2.4f, 1f);
                }
                else
                {
                    // Indicador Místico Cristalino Bonito em Anel (LineRenderer) em vez de quadrado vermelho
                    ind = new GameObject("BossSpikeTelegraphRing");
                    ind.transform.position = p + Vector3.up * 0.05f;

                    LineRenderer lr = ind.AddComponent<LineRenderer>();
                    lr.positionCount = 25;
                    lr.useWorldSpace = false;
                    lr.startWidth = 0.18f;
                    lr.endWidth = 0.18f;
                    lr.material = new Material(Shader.Find("Sprites/Default"));
                    lr.startColor = new Color(0.95f, 0.2f, 0.4f, 0.85f); // Vermelho Místico / Magenta
                    lr.endColor = new Color(0.65f, 0.15f, 0.9f, 0.85f);

                    float circleRadius = 1.1f;
                    for (int i = 0; i < 25; i++)
                    {
                        float a = (i / 24f) * Mathf.PI * 2f;
                        lr.SetPosition(i, new Vector3(Mathf.Cos(a) * circleRadius, 0f, Mathf.Sin(a) * circleRadius));
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
            List<Vector3> startPositions = new List<Vector3>();

            foreach (Vector3 p in spikePositions)
            {
                Vector3 spawnUnderground = p + Vector3.down * 3.5f;
                Quaternion rot = Quaternion.LookRotation(p - centerPos);
                if (prefabUse != null)
                {
                    GameObject spikeObj = Instantiate(prefabUse, spawnUnderground, rot);
                    spawnedSpikes.Add(spikeObj.transform);
                    startPositions.Add(spawnUnderground);

                    SpikeDamageDealer dealer = spikeObj.GetComponent<SpikeDamageDealer>();
                    if (dealer == null) dealer = spikeObj.AddComponent<SpikeDamageDealer>();
                    dealer.damage = wideSpikeDamage;
                }
            }

            // A. Ergue os espinhos rapidamente do chão (0.25s)
            float elapsed = 0f;
            float emergeDuration = 0.25f;
            while (elapsed < emergeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / emergeDuration;
                for (int i = 0; i < spawnedSpikes.Count; i++)
                {
                    if (spawnedSpikes[i] != null)
                    {
                        spawnedSpikes[i].position = Vector3.Lerp(startPositions[i], spikePositions[i], t);
                    }
                }
                yield return null;
            }

            // B. Permanece no topo por um breve instante para espetar (0.40s)
            yield return new WaitForSeconds(0.40f);

            // C. Retrai os espinhos de volta para o subsolo (0.30s)
            elapsed = 0f;
            float retractDuration = 0.30f;
            while (elapsed < retractDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / retractDuration;
                for (int i = 0; i < spawnedSpikes.Count; i++)
                {
                    if (spawnedSpikes[i] != null)
                    {
                        spawnedSpikes[i].position = Vector3.Lerp(spikePositions[i], startPositions[i], t);
                    }
                }
                yield return null;
            }

            // Destrói os objetos de espinho após retornarem ao chão
            foreach (Transform s in spawnedSpikes)
            {
                if (s != null) Destroy(s.gameObject);
            }

            if (showDebugLog) Debug.Log("[BossController] 🌊 Espinhos do BossSpellWide espetaram e recolheram!");

            yield return new WaitForSeconds(0.3f);
        }
        finally
        {
            isCastingSpell = false;
            if (agent != null && agent.enabled && agent.isOnNavMesh && !IsStunned && !IsDead)
            {
                agent.isStopped = false;
            }
        }
    }

    // =====================================================
    // 💧 CUSPIDA ÁCIDA (FASE 3)
    // =====================================================

    public void PerformAcidSpit()
    {
        if (!CanInitiateAction) return;
        StartCoroutine(AcidSpitRoutine());
    }

    private IEnumerator AcidSpitRoutine()
    {
        isCastingSpell = true;
        acidSpitTimer = acidSpitCooldown;

        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }

        FreezeMovementForSpell(acidSpitTelegraphDuration + 0.8f);

        if (playerTransform != null)
        {
            Vector3 lookDir = (playerTransform.position - transform.position).normalized;
            lookDir.y = 0f;
            if (lookDir != Vector3.zero) transform.rotation = Quaternion.LookRotation(lookDir);
        }

        if (animator != null && !string.IsNullOrEmpty(acidSpitAnimTrigger))
            animator.SetTrigger(acidSpitAnimTrigger);

        Vector3 targetPos = playerTransform != null
            ? playerTransform.position
            : transform.position + transform.forward * 6f;

        GameObject indicator = SpawnAcidIndicator(targetPos);

        try
        {
            yield return new WaitForSeconds(acidSpitTelegraphDuration);

            if (indicator != null) { Destroy(indicator); indicator = null; }

            Vector3 spawnPos = acidSpitLaunchPoint != null
                ? acidSpitLaunchPoint.position
                : transform.position + Vector3.up * 1.5f + transform.forward * 0.8f;
            GameObject spitObj = null;

            if (acidSpitProjectilePrefab != null)
            {
                spitObj = Instantiate(acidSpitProjectilePrefab, spawnPos, Quaternion.identity);
            }
            else
            {
                // Fallback: esfera verde para testar sem prefab
                spitObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                spitObj.transform.position = spawnPos;
                spitObj.transform.localScale = Vector3.one * 0.3f;
                if (spitObj.GetComponent<Rigidbody>() == null) spitObj.AddComponent<Rigidbody>();
                Renderer rend = spitObj.GetComponent<Renderer>();
                if (rend != null)
                {
                    Material mat = new Material(Shader.Find("Standard"));
                    mat.color = new Color(0.1f, 0.9f, 0.1f);
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", new Color(0f, 0.6f, 0f));
                    rend.material = mat;
                }
            }

            if (spitObj != null)
            {
                AcidSpitProjectile proj = spitObj.GetComponent<AcidSpitProjectile>()
                                       ?? spitObj.AddComponent<AcidSpitProjectile>();
                proj.acidPuddlePrefab = acidPuddlePrefabForSpit;
                proj.Launch(targetPos);

                // Impede que o projétil colida com o próprio boss no frame de spawn
                Collider spitCol = spitObj.GetComponent<Collider>() ?? spitObj.GetComponentInChildren<Collider>();
                Collider bossRoot = GetComponent<Collider>();
                if (spitCol != null && bossRoot != null)
                    Physics.IgnoreCollision(spitCol, bossRoot, true);
            }

            TriggerCameraShake(0.18f, 0.10f);
            if (showDebugLog) Debug.Log($"[BossController] 💧 AcidSpit lançado para {targetPos}!");

            yield return new WaitForSeconds(0.6f);
        }
        finally
        {
            if (indicator != null) Destroy(indicator);
            isCastingSpell = false;
            if (agent != null && agent.enabled && agent.isOnNavMesh && !IsStunned && !IsDead)
                agent.isStopped = false;
        }
    }

    // Anel verde-ácido no chão que marca onde o projétil vai aterrissar.
    private GameObject SpawnAcidIndicator(Vector3 worldPos)
    {
        GameObject ind = new GameObject("AcidSpitLandingIndicator");
        ind.transform.position = worldPos + Vector3.up * 0.04f;

        LineRenderer lr = ind.AddComponent<LineRenderer>();
        lr.positionCount = 25;
        lr.useWorldSpace = false;
        lr.startWidth = 0.22f;
        lr.endWidth = 0.22f;
        lr.loop = true;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = new Color(0.2f, 1f, 0.1f, 0.9f);
        lr.endColor   = new Color(0.6f, 1f, 0.1f, 0.5f);

        float radius = 1.4f;
        for (int i = 0; i < 25; i++)
        {
            float a = (i / 24f) * Mathf.PI * 2f;
            lr.SetPosition(i, new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius));
        }
        return ind;
    }

    // =====================================================
    // ⚔️ SISTEMA DE COMBOS INTELIGENTES DA IA
    // =====================================================

    [HideInInspector] public bool isExecutingCombo = false;

    /// <summary>
    /// Combo Tático: PRISÃO ESMAGADORA (Trap & Stomp)
    ///  1. Prende o jogador com a prisão de pilares/espinhos (se não houver pilares na cena).
    ///  2. Vira para o centro da prisão e desfere o Stomp na direção do jogador contido.
    /// </summary>
    public bool ExecuteTrapAndStompCombo()
    {
        if (!CanInitiateAction) return false;
        StartCoroutine(TrapAndStompComboRoutine());
        return true;
    }

    private IEnumerator TrapAndStompComboRoutine()
    {
        isExecutingCombo = true;
        isCastingSpell = true;

        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }

        if (showDebugLog) Debug.Log("⚔️ [BOSS COMBO] 🕸️ Iniciando Combo Tático: PRISÃO ESMAGADORA (Trap & Stomp)!");

        BossPhase1_MestreDoSolo mestre = GetComponent<BossPhase1_MestreDoSolo>();

        // Passo 1: Se não houver pilares e houver prefabs configurados, vira para o jogador e conjura os pilares ao redor dele
        if (mestre != null && !mestre.ExistemPilaresNaCena() && (mestre.pilarPrefab != null || mestre.espinhoPrefab != null))
        {
            if (playerTransform != null)
            {
                Vector3 lookDir = (playerTransform.position - transform.position).normalized;
                lookDir.y = 0f;
                if (lookDir.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(lookDir);
            }

            if (animator != null)
            {
                animator.ResetTrigger("bossSpell");
                animator.SetTrigger("bossSpell");
                animator.Play("SpellGround", 0, 0f);
            }

            bool spawned = mestre.InvocarPrisaoForcado(bypassActionCheck: true);

            // Aguarda os pilares emergirem e prenderem o jogador (duração completa de SpellGround: 1.80s)
            if (spawned)
            {
                yield return new WaitForSeconds(1.80f);
            }
        }

        isCastingSpell = false;

        // Passo 2: Vira para o centro da prisão e desfere o Stomp Esmagador!
        if (playerTransform != null)
        {
            Vector3 targetDir = (playerTransform.position - transform.position).normalized;
            targetDir.y = 0f;
            if (targetDir.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(targetDir);
        }

        isAttacking = true;

        if (animator != null)
        {
            animator.ResetTrigger("bossStomp");
            animator.SetTrigger("bossStomp");
            animator.Play("STOMP", 0, 0f);
        }

        // Telegrafagem precisa do Stomp (1.40s até o impacto do pé no chão com speed 0.5x)
        float stompWindup = 1.40f;
        float trackingCutoff = 0.45f;
        float elapsedStompWindup = 0f;

        while (elapsedStompWindup < stompWindup)
        {
            elapsedStompWindup += Time.deltaTime;
            if (elapsedStompWindup <= trackingCutoff)
            {
                HandleRotationSmooth(turnSmoothSpeed * 1.4f);
            }
            yield return null;
        }

        // Impacto do Stomp
        AnimEvent_StompImpact();

        // Janela de recuperação do golpe pesado (2.00s restantes da animação STOMP)
        isRecovering = true;
        yield return new WaitForSeconds(2.00f);
        isRecovering = false;

        isAttacking = false;
        isExecutingCombo = false;

        if (agent != null && agent.enabled && agent.isOnNavMesh && !IsStunned && !IsDead && CanInitiateAction)
            agent.isStopped = false;

        if (showDebugLog) Debug.Log("⚔️ [BOSS COMBO] ✅ Combo Prisão Esmagadora finalizado com sucesso!");
    }

    public void EnsureHealthBarUI()
    {
        BossHealthBarUI existingUI = FindFirstObjectByType<BossHealthBarUI>(FindObjectsInactive.Include);
        if (existingUI != null)
        {
            if (!existingUI.gameObject.activeInHierarchy)
            {
                existingUI.gameObject.SetActive(true);
            }
            existingUI.SetBarVisible(true);
            return;
        }

        if (bossHealthBarPrefab == null)
        {
            bossHealthBarPrefab = Resources.Load<GameObject>("BossHealthBar_Canvas")
                            ?? Resources.Load<GameObject>("Canvas-Boss")
                            ?? Resources.Load<GameObject>("UI/BossHealthBar_Canvas");
        }

#if UNITY_EDITOR
        if (bossHealthBarPrefab == null)
        {
            bossHealthBarPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Enemies/Boss/Canvas-Boss.prefab");
        }
#endif

        if (bossHealthBarPrefab != null)
        {
            GameObject spawnedUI = Instantiate(bossHealthBarPrefab);
            spawnedUI.name = "Canvas-Boss";
            BossHealthBarUI uiComp = spawnedUI.GetComponentInChildren<BossHealthBarUI>(true);
            if (uiComp != null)
            {
                uiComp.gameObject.SetActive(true);
                uiComp.SetBarVisible(true);
            }
            if (showDebugLog) Debug.Log("[BossController] 🎨 Canvas da Barra de Vida instanciado no mapa!");
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

    [Header("🛡️ Poise & Posture Break System")]
    [Tooltip("Limite máximo da barra de postura/poise do Boss.")]
    public float maxPoise = 100f;

    [Tooltip("Valor atual da postura/poise do Boss.")]
    public float currentPoise = 100f;

    [Tooltip("Taxa de regeneração de poise por segundo quando o player não ataca.")]
    public float poiseRegenRate = 12f;

    [Tooltip("Indica se o núcleo vulnerável está exposto (+50% dano crítico).")]
    public bool isVulnerableCoreExposed { get; private set; } = false;

    private float timeSinceLastDamage = 0f;
    private bool isInvulnerableDuringTransition = false;

    /// <summary>
    /// Aplica dano à barra de Poise/Postura do Boss.
    /// Quando a postura quebra, o Boss entra em Stagger com núcleo vulnerável exposto.
    /// </summary>
    public void ApplyPoiseDamage(float poiseDamage)
    {
        if (CurrentState == BossState.Dead || isInvulnerableDuringTransition) return;

        timeSinceLastDamage = Time.time;

        // Se o núcleo vulnerável estiver exposto, recebe +50% de bônus de dano de postura
        if (isVulnerableCoreExposed) poiseDamage *= 1.5f;

        currentPoise -= poiseDamage;

        if (currentPoise <= 0f && CurrentState != BossState.Stunned)
        {
            TriggerPoiseBreak();
        }
    }

    /// <summary>
    /// Executa a Quebra de Postura (Poise Break) com efeito de Slow-Mo (Time-Dilation) e estresse visual.
    /// </summary>
    public void TriggerPoiseBreak()
    {
        currentPoise = 0f;
        isVulnerableCoreExposed = true;

        // Dispara Time-Dilation (Slow-Mo de 0.15s a 25% de velocidade)
        StartCoroutine(TimeDilationRoutine(0.15f, 0.25f));

        // Tremor de Câmera Intenso de impacto
        TriggerCameraShake(0.45f, 0.30f);

        // Aplica Stagger de 4.0s com núcleo exposto para o player causar dano massivo
        ApplyStun(4.0f);

        Debug.Log("💥 [POISE BREAK] Postura do Boss QUEBRADA! Núcleo vulnerável exposto (+50% Dano Crítico)!");
    }

    private IEnumerator TimeDilationRoutine(float realTimeDuration, float targetTimeScale)
    {
        Time.timeScale = targetTimeScale;
        yield return new WaitForSecondsRealtime(realTimeDuration);
        Time.timeScale = 1.0f;
    }

    /// <summary>
    /// Aplica Stun / Stagger ao Boss.
    /// </summary>
    public void ApplyStun(float duration)
    {
        if (CurrentState == BossState.Dead || CurrentState == BossState.Idle) return;
        if (CurrentState == BossState.Stunned) return; // Não empilha stun

        float stunTime = duration > 0f ? duration : 3.0f;
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

        if (invisible)
        {
            BossTacticalEptinhoUI.TriggerTacticalNotice(BossTacticalEptinhoUI.CalloutType.Phase2RefractionInvisibility);
            if (drippingInstance != null)
            {
                drippingInstance.Play(true);
                if (showDebugLog) Debug.Log("[BossController] 💧 Dripping contínuo ATIVADO (refração ON).");
            }
        }
        else
        {
            if (drippingInstance != null)
            {
                drippingInstance.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                if (showDebugLog) Debug.Log("[BossController] 💧 Dripping contínuo DESATIVADO (refração OFF).");
            }
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

        // Dispara evento de dano e reduz a barra de Poise/Postura do Boss
        if (health.CurrentHealth < previousHP)
        {
            int damageTaken = previousHP - health.CurrentHealth;
            ApplyPoiseDamage(damageTaken * 0.85f);
            OnTookDamage?.Invoke();
            if (showDebugLog)
            {
                int p3HP = phaseConfig != null ? Mathf.RoundToInt(phaseConfig.phase3Threshold * health.maxHealth) : -1;
                Debug.Log($"[BossController] HP: {health.CurrentHealth}/{health.maxHealth} ({(float)health.CurrentHealth/health.maxHealth:P0}) | Fase: {CurrentPhase} | Threshold Fase3: {p3HP}");
            }
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

        // Aplica invulnerabilidade limpa de transição por 1.2s sem cancelar hitboxes do player
        StartCoroutine(PhaseTransitionInvulnerabilityRoutine(1.2f));

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
                TriggerPowerUP(2.5f);
                break;
            case 3:
                CurrentState = BossState.Phase3;
                TriggerCameraShake(0.5f, 0.22f);
                TriggerPowerUP(2.5f);
                break;
        }

        // Garante que o agent está posicionado no NavMesh e respeita travas de congelamento
        if (agent != null)
        {
            agent.enabled = true;
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 5.0f, NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
            }
            if (agent.isOnNavMesh && CanInitiateAction) agent.isStopped = false;
        }

        // Notifica todos os sistemas
        BossEvents.RaisePhaseChanged(newPhase);
    }

    private IEnumerator PhaseTransitionInvulnerabilityRoutine(float duration)
    {
        isInvulnerableDuringTransition = true;
        if (health != null) health.isInvulnerable = true;

        yield return new WaitForSeconds(duration);

        if (health != null) health.isInvulnerable = false;
        isInvulnerableDuringTransition = false;
    }

    private IEnumerator CocoonShatterLightPulseRoutine()
    {
        // 🍃 Dispara a explosão de folhas Hit Leaves A e o Impact Frame na transição para a Fase 2
        VFXManager.Play(VFXType.CocoonLeavesBurst, transform.position + Vector3.up * 1.5f, Quaternion.identity);
        VFXManager.Play(VFXType.ImpactFrame, transform.position + Vector3.up * 1.5f, Quaternion.identity);

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
        if (!CanInitiateAction)
        {
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
            }
            return;
        }

        // Se o player não existir, cancela a perseguição de combate
        if (playerTransform == null) return;

        // Na Fase 1 (Casulo) OU quando Invisível (Refração), o Boss NÃO ataca corpo a corpo (somente spells/mobs)!
        if (CurrentPhase == 1 || IsInvisible) return;

        // Orientação em direção ao player
        HandleRotation();

        // Checa distância para ataque melee
        float meleeRange = phaseConfig != null ? phaseConfig.baseMeleeRange : 4.5f;
        float distToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        // Checa se o player está atordoado/preso para disparar sprint agressivo de punição
        PlayerHealth playerHealthComp = playerTransform.GetComponent<PlayerHealth>() ?? playerTransform.GetComponentInParent<PlayerHealth>();
        bool isPlayerStunned = playerHealthComp != null && playerHealthComp.isStunned;

        // Se chegou no alcance de ataque melee:
        if (distToPlayer <= meleeRange)
        {
            // Se estava correndo (sprint) ou se o cooldown expirou, ataca de surpresa SEM frear/andar!
            if (isSprinting || meleeTimer <= 0f)
            {
                if (isSprinting && meleeTimer > 0f) meleeTimer = 0f; // Instantâneo na chegada da corrida
                StartCoroutine(PerformMeleeAttack());
                return;
            }
        }

        // Cuspida ácida — Phase 3 only, mid-range window
        if (CurrentPhase == 3 && acidSpitTimer <= 0f
            && distToPlayer >= acidSpitMinRange && distToPlayer <= acidSpitMaxRange
            && CanInitiateAction)
        {
            PerformAcidSpit();
            return;
        }

        // Decisão de Aproximação: Teleporte estilo SharpBlur OU Sprint tradicional
        float sprintThreshold = phaseConfig != null ? phaseConfig.sprintDistanceThreshold : 5.5f;
        float teleportThreshold = Mathf.Min(sprintThreshold, meleeRange + 0.5f);

        if (enableSharpBlurTeleport && distToPlayer > teleportThreshold && meleeTimer <= 0f && !isTeleporting && CanInitiateAction)
        {
            if (UnityEngine.Random.Range(0f, 100f) <= teleportChance)
            {
                StartCoroutine(SharpBlurTeleportRoutine());
                return;
            }
        }

        // Sprint de Aproximação:
        // Mantém a corrida contínua até encostar no player (não desacelera aos 5.5m)!
        bool shouldSprint = isSprinting ? (distToPlayer > meleeRange) : (distToPlayer > sprintThreshold || (isPlayerStunned && distToPlayer > 2.0f));

        isSprinting = shouldSprint;

        float sprintSpeedVal = phaseConfig != null ? phaseConfig.sprintSpeed : 8.5f;
        float baseSpeedVal = phaseConfig != null ? phaseConfig.baseSpeed : 4.8f;
        float speed = isSprinting ? sprintSpeedVal : baseSpeedVal;

        // Enquanto invisível na Fase 2, o boss ganha +35% de velocidade para se mover com maior fluidez
        if (IsInvisible) speed *= 1.35f;

        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.speed = speed;
            agent.acceleration = isSprinting ? 28f : 16f;
            agent.radius = 0.45f;
            agent.isStopped = false;
            agent.SetDestination(playerTransform.position);

            // Ajusta a distância de parada para COLAR no player (1.0m)
            agent.stoppingDistance = 1.0f;
            return;
        }

        // FALLBACK PARA CENAS SEM NAVMESH BAKED (ex: Boss_Test sem NavMesh Surface):
        // Move o Transform diretamente em direção ao player para nunca ficar parado!
        Vector3 target = playerTransform.position;
        target.y = transform.position.y;

        float meleeStopRange = 1.0f;
        if (distToPlayer > meleeStopRange)
        {
            transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);

            Vector3 lookDir = target - transform.position;
            if (lookDir.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.LookRotation(lookDir);
        }
    }
    // --------------------

    [Header("🏃 Root Motion & Locomotion")]
    [Tooltip("Usa o Root Motion da animação durante o Sprint para sincronização 100% perfeita com a passada do Boss.")]
    public bool useRootMotionOnSprint = false;

    /// <summary>
    /// Chamado pelo BossAnimationEvents no GameObject do Animator.
    /// O NavMeshAgent já conduz o Boss com precisão a 8.5m/s evitando duplicação indesejada de deslocamento.
    /// </summary>
    public void OnChildAnimatorMove(Animator anim)
    {
        if (anim == null) return;
    }

    [Header("🏃 Steering & Locomotion Easing")]
    [Tooltip("Velocidade de rotação suave em graus por segundo (Slerp Easing).")]
    public float turnSmoothSpeed = 10.0f;

    [Tooltip("Tempo de antecedência (em segundos) em que o Boss TRAVA o rastreamento antes do ataque (Tracking Cutoff). Ajustado para 0.25s para esquiva por reflexo justa.")]
    public float trackingCutoffWindow = 0.25f;

    private void HandleRotation()
    {
        if (playerTransform == null) return;

        Vector3 directionToPlayer = (playerTransform.position - transform.position).normalized;
        directionToPlayer.y = 0;

        if (directionToPlayer != Vector3.zero)
        {
            float rotSpeed = phaseConfig != null ? phaseConfig.rotationSpeed : 360f;
            Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
            
            // Slerp Easing: Rotação suave e ágil que acompanha o jogador
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * turnSmoothSpeed);
        }
    }

    [Header("🎮 Game Feel & Combat Responsiveness")]
    [Tooltip("Duração do micro-freeze de impacto (Hitstop) ao acertar golpes pesados.")]
    public float defaultHitstopDuration = 0.06f;

    private Coroutine hitstopCoroutine;
    private bool isRecovering = false;

    /// <summary>
    /// Dispara o efeito de Hitstop (Micro-Freeze / Frame Freeze) na animação para dar peso ao impacto.
    /// </summary>
    public void TriggerHitstop(float duration = 0.06f)
    {
        if (hitstopCoroutine != null) StopCoroutine(hitstopCoroutine);
        hitstopCoroutine = StartCoroutine(HitstopRoutine(duration));
    }

    private IEnumerator HitstopRoutine(float duration)
    {
        if (animator != null) animator.speed = 0.0f;
        yield return new WaitForSecondsRealtime(duration);
        if (animator != null) animator.speed = 1.0f;
        hitstopCoroutine = null;
    }

    private void HandleRotationSmooth(float slerpSpeed = 10f)
    {
        if (playerTransform == null) return;

        Vector3 directionToPlayer = (playerTransform.position - transform.position).normalized;
        directionToPlayer.y = 0;

        if (directionToPlayer != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * slerpSpeed);
        }
    }

    [HideInInspector] public bool isCastingSpell = false;
    [HideInInspector] public bool isTeleporting = false;

    /// <summary>
    /// Trava de Fluxo de Animação: Garante que o Boss NUNCA sobreponha ataques, magias ou staggers!
    /// </summary>
    public bool CanInitiateAction => !isAttacking && !isCastingSpell && !isExecutingCombo && !isRecovering && !isTeleporting && CurrentState != BossState.Stunned && CurrentState != BossState.Dead;

    /// <summary>
    /// Executa o Teleporte com Mímica do SharpBlur: Holograma de predição no destino, flash de VFX e ataque surpresa!
    /// </summary>
    public void TriggerSharpBlurTeleport()
    {
        if (playerTransform == null) playerTransform = FindPlayerTransform();
        if (playerTransform != null && !isTeleporting && !isAttacking && CanInitiateAction)
        {
            StartCoroutine(SharpBlurTeleportRoutine());
        }
    }

    private IEnumerator SharpBlurTeleportRoutine()
    {
        if (playerTransform == null || isTeleporting) yield break;

        isTeleporting = true;
        isSprinting = false;

        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }

        // 1. Calcula a posição preditiva do player (mímica SharpBlur)
        Vector3 playerPos = playerTransform.position;
        Rigidbody playerRb = playerTransform.GetComponent<Rigidbody>() ?? playerTransform.GetComponentInParent<Rigidbody>();
        if (playerRb != null && teleportLeadPrediction > 0f)
        {
            playerPos += playerRb.linearVelocity * 0.25f * teleportLeadPrediction;
        }

        // 2. Calcula posição de reaparecimento ao redor do player
        Vector3 dirFromPlayer = (transform.position - playerPos).normalized;
        dirFromPlayer.y = 0f;
        if (dirFromPlayer == Vector3.zero) dirFromPlayer = -playerTransform.forward;

        Vector3 targetTeleportPos = playerPos + (dirFromPlayer * teleportDistanceOffset);

        // Snap seguro no NavMesh
        if (UnityEngine.AI.NavMesh.SamplePosition(targetTeleportPos, out UnityEngine.AI.NavMeshHit navHit, 5.0f, UnityEngine.AI.NavMesh.AllAreas))
        {
            targetTeleportPos = navHit.position;
        }

        Vector3 lookDir = (playerPos - targetTeleportPos).normalized;
        lookDir.y = 0f;
        Quaternion targetRot = (lookDir != Vector3.zero) ? Quaternion.LookRotation(lookDir) : transform.rotation;

        // 3. Spawna o Holograma de Antecipação no destino
        SpawnTeleportHologram(targetTeleportPos, targetRot, teleportAnticipationTime);

        // VFX de partida (fumaça estilizada)
        VFXManager.Play(VFXType.WWExplosionVariant1, transform.position + Vector3.up * 0.5f, Quaternion.identity, 0.45f);

        if (showDebugLog) Debug.Log($"[BossController] ⚡ [SHARPBLUR TELEPORT] Holograma gerado em {targetTeleportPos}. Teleportando em {teleportAnticipationTime}s...");

        // Aguarda a antecipação
        yield return new WaitForSeconds(teleportAnticipationTime);

        // 4. Warp / Teleporte instantâneo
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.Warp(targetTeleportPos);
        }
        else
        {
            transform.position = targetTeleportPos;
        }
        transform.rotation = targetRot;

        // VFX de chegada (fumaça estilizada + leve tremor)
        VFXManager.Play(VFXType.WWExplosionVariant1, targetTeleportPos + Vector3.up * 0.5f, Quaternion.identity, 0.6f);
        TriggerCameraShake(0.22f, 0.15f);

        isTeleporting = false;

        // 5. Desfere ataque melee surpresa logo após teleportar
        StartCoroutine(PerformMeleeAttack());
    }

    private void SpawnTeleportHologram(Vector3 futurePos, Quaternion futureRot, float lifetime)
    {
        EnsureHologramAssets();

        GameObject holoObj = null;
        if (teleportHologramPrefab != null)
        {
            holoObj = Instantiate(teleportHologramPrefab);
        }
        else
        {
            holoObj = new GameObject("Boss_DashHologram");
            holoObj.AddComponent<DashHologram>();
        }

        DashHologram holoScript = holoObj.GetComponent<DashHologram>() ?? holoObj.AddComponent<DashHologram>();
        if (holoScript != null && teleportHologramMaterial != null)
        {
            holoScript.Init(transform, futurePos, futureRot, lifetime, teleportHologramMaterial, teleportHologramAlpha);
        }
    }

    private void EnsureHologramAssets()
    {
#if UNITY_EDITOR
        if (teleportHologramPrefab == null)
        {
            teleportHologramPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Effects/prefabs/hologramPrefab.prefab");
        }
        if (teleportHologramMaterial == null)
        {
            teleportHologramMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Effects/shaders/hologramEffect/hologramMaterial.mat");
        }
#endif
        if (teleportHologramMaterial == null)
        {
            Shader sh = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
            if (sh != null)
            {
                teleportHologramMaterial = new Material(sh);
                teleportHologramMaterial.color = new Color(0.8f, 0.2f, 1.0f, teleportHologramAlpha);
            }
        }
    }

    private IEnumerator PerformMeleeAttack()
    {
        if (!CanInitiateAction) yield break;

        bool wasSprinting = isSprinting;
        isAttacking = true;
        isSprinting = false;
        float baseCooldown = phaseConfig != null ? phaseConfig.baseMeleeCooldown : 2.5f;
        float cooldown = IsInvisible ? (baseCooldown * 0.6f) : baseCooldown;
        meleeTimer = cooldown;

        // Se estiver invisível, revela temporariamente o Boss para a animação do ataque ser visível!
        BossPhase2_Refraction refractionComp = GetComponent<BossPhase2_Refraction>();
        if (refractionComp != null && IsInvisible)
        {
            refractionComp.SetTemporaryVisibility(true);
        }

        if (showDebugLog) Debug.Log("[BossController] 👊 ATAQUE DO BOSS — Executando golpe!");

        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }

        string selectedTrigger = "bossSwipe";

        PlayerHealth targetHealth = playerTransform != null ? (playerTransform.GetComponent<PlayerHealth>() ?? playerTransform.GetComponentInParent<PlayerHealth>()) : null;
        bool isTargetStunned = (targetHealth != null && targetHealth.isStunned);

        // Define a lista de candidatos para o ataque
        List<string> candidateAttacks = new List<string>();

        if (isTargetStunned)
        {
            // Player atordoado: punição pesada com Stomp ou Punch
            candidateAttacks.Add("bossStomp");
            candidateAttacks.Add("bossPunch");
        }
        else if (wasSprinting)
        {
            // Chegando de Sprint em velocidade máxima: arsenal completo com variação rica
            candidateAttacks.Add("bossPunch");
            candidateAttacks.Add("bossSwipe");
            candidateAttacks.Add("bossStomp");
            candidateAttacks.Add("bossJumpAttack");
        }
        else if (animator != null && meleeAttackTriggers != null && meleeAttackTriggers.Length > 0)
        {
            candidateAttacks.AddRange(meleeAttackTriggers);
        }
        else
        {
            candidateAttacks.Add("bossSwipe");
            candidateAttacks.Add("bossPunch");
            candidateAttacks.Add("bossStomp");
            candidateAttacks.Add("bossJumpAttack");
        }

        // SISTEMA ANTI-REPETIÇÃO: Impede repetir o mesmo ataque consecutivamente (ex: Jump Attack 3x seguidas)
        if (candidateAttacks.Count > 1 && !string.IsNullOrEmpty(lastMeleeAttack))
        {
            candidateAttacks.RemoveAll(atk => atk == lastMeleeAttack);
            if (candidateAttacks.Count == 0)
            {
                candidateAttacks.Add("bossPunch");
                candidateAttacks.Add("bossSwipe");
            }
        }

        // Sorteia o ataque escolhido
        selectedTrigger = candidateAttacks[UnityEngine.Random.Range(0, candidateAttacks.Count)];
        lastMeleeAttack = selectedTrigger;

        if (animator != null)
        {
            string stateName = selectedTrigger;
            if (selectedTrigger == "bossSwipe") stateName = "BossSwipe";
            else if (selectedTrigger == "bossPunch") stateName = "BossPunch";
            else if (selectedTrigger == "bossJumpAttack") stateName = "JumpAttack";
            else if (selectedTrigger == "bossStomp") stateName = "STOMP";

            animator.ResetTrigger("bossSwipe");
            animator.ResetTrigger("bossPunch");
            animator.ResetTrigger("bossJumpAttack");
            animator.ResetTrigger("bossStomp");

            animator.SetTrigger(selectedTrigger);
            animator.Play(stateName, 0, 0f);
        }

        // === 1. ANTICIPATION, SALTO PERSEGUIDOR & TRACKING CUTOFF ===
        // Timings específicos por animação para o Boss NUNCA patinar nem cancelar a pose precocemente:
        // - STOMP (speed 0.5x, duração total: 3.40s): windup até impacto = 1.40s, recovery = 2.00s
        // - JumpAttack (speed 1.0x, duração total: 3.67s): windup/salto = 2.03s, recovery/pouso = 1.64s
        // - BossPunch (speed 1.0x, duração total: 2.67s): windup = 0.80s, recovery = 1.87s
        // - BossSwipe (speed 1.0x, duração total: 1.10s): windup = 0.45s, recovery = 0.65s
        float windUp = 0.45f;
        float recoveryWindow = 0.65f;

        if (selectedTrigger == "bossStomp")
        {
            windUp = 1.40f;
            recoveryWindow = 2.00f;
        }
        else if (selectedTrigger == "bossJumpAttack")
        {
            windUp = 2.03f;
            recoveryWindow = 1.64f;
        }
        else if (selectedTrigger == "bossPunch")
        {
            windUp = 0.80f;
            recoveryWindow = 1.87f;
        }
        else if (selectedTrigger == "bossSwipe")
        {
            windUp = 0.45f;
            recoveryWindow = 0.65f;
        }

        if (IsInvisible)
        {
            windUp *= 0.75f;
        }

        float trackingCutoffTime = Mathf.Max(0.10f, windUp - trackingCutoffWindow);
        float elapsedWindUp = 0f;

        // SALTO PERSEGUIDOR (Gap-Closer Aéreo no Jump Attack):
        if (selectedTrigger == "bossJumpAttack")
        {
            Vector3 startJumpPos = transform.position;
            Vector3 targetLandingPos = playerTransform != null ? playerTransform.position : (startJumpPos + transform.forward * 5f);
            if (UnityEngine.AI.NavMesh.SamplePosition(targetLandingPos, out UnityEngine.AI.NavMeshHit navHit, 6.0f, UnityEngine.AI.NavMesh.AllAreas))
            {
                targetLandingPos = navHit.position;
            }

            float leapTakeoff = 0.35f;
            float leapLand = 1.95f;
            float leapDuration = leapLand - leapTakeoff;

            while (elapsedWindUp < windUp)
            {
                elapsedWindUp += Time.deltaTime;

                if (elapsedWindUp < leapTakeoff)
                {
                    // Mira e gira suavemente em direção ao player antes da decolagem e atualiza ponto de pouso
                    if (playerTransform != null)
                    {
                        targetLandingPos = playerTransform.position;
                        if (UnityEngine.AI.NavMesh.SamplePosition(targetLandingPos, out UnityEngine.AI.NavMeshHit hit2, 6.0f, UnityEngine.AI.NavMesh.AllAreas))
                        {
                            targetLandingPos = hit2.position;
                        }
                    }
                    HandleRotationSmooth(turnSmoothSpeed * 2.0f);
                }
                else if (elapsedWindUp >= leapTakeoff && elapsedWindUp <= leapLand)
                {
                    float leapProgress = Mathf.Clamp01((elapsedWindUp - leapTakeoff) / leapDuration);
                    float t = Mathf.SmoothStep(0f, 1f, leapProgress);
                    Vector3 currentPos = Vector3.Lerp(startJumpPos, targetLandingPos, t);

                    if (agent != null && agent.enabled && agent.isOnNavMesh)
                    {
                        agent.Warp(currentPos);
                    }
                    else
                    {
                        transform.position = currentPos;
                    }
                }

                yield return null;
            }
        }
        else
        {
            while (elapsedWindUp < windUp)
            {
                elapsedWindUp += Time.deltaTime;
                
                // Rastreia o player responsivamente até os últimos 0.25s antes do golpe para esquiva justa de reflexo!
                if (elapsedWindUp <= trackingCutoffTime)
                {
                    HandleRotationSmooth(turnSmoothSpeed * 1.6f);
                }
                yield return null;
            }
        }

        // === 2. MOMENTO DO IMPACTO & HITSTOP ===
        int dmg = phaseConfig != null ? phaseConfig.baseMeleeDamage : 35;
        EnableMeleeHitboxBothHands(0.45f, dmg, 16f);

        // Se for Stomp ou JumpAttack e os AnimationEvents do clip não tiverem sido chamados via evento do model
        bool isHeavyAttack = (selectedTrigger == "bossJumpAttack" || selectedTrigger == "bossStomp");
        if (isHeavyAttack)
        {
            if (selectedTrigger == "bossStomp")
            {
                AnimEvent_StompImpact();
            }
            else if (selectedTrigger == "bossJumpAttack")
            {
                AnimEvent_JumpImpact();
            }
        }

        TriggerCameraShake(0.25f, 0.15f);

        // Aplica o Hitstop (Micro-freeze de 0.06s no momento exato do impacto)
        TriggerHitstop(defaultHitstopDuration);

        // === 3. RECOVERY FRAMES (Janela de Punição para o Player onde o Boss permanece 100% imóvel) ===
        isRecovering = true;
        float recoveryElapsed = 0f;

        while (recoveryElapsed < recoveryWindow)
        {
            recoveryElapsed += Time.deltaTime;
            yield return null;
        }

        isRecovering = false;

        // Restaura a invisibilidade se estiver na Fase 2
        if (refractionComp != null && IsInvisible)
        {
            refractionComp.SetTemporaryVisibility(false);
        }

        if (agent != null && agent.enabled && agent.isOnNavMesh && !IsStunned && !IsDead && CanInitiateAction)
        {
            agent.isStopped = false;
            if (playerTransform != null)
            {
                agent.SetDestination(playerTransform.position);
            }
        }

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

        if (showDebugLog) Debug.Log($"[BossController] ⚡ STUNNED / STAGGERED por {duration}s!");

        // Para completamente
        if (agent != null && agent.enabled && agent.isOnNavMesh)
            agent.isStopped = true;

        // Notifica todos
        BossEvents.RaiseBossStunned(duration);

        yield return new WaitForSeconds(duration);

        // Recupera da quebra de postura / stagger (Núcleo vulnerável fecha e Poise restaura)
        isVulnerableCoreExposed = false;
        currentPoise = maxPoise;

        if (CurrentState == BossState.Stunned) // Garante que não mudou durante o stun
        {
            CurrentState = stateBeforeStun;

            if (agent != null && agent.enabled && agent.isOnNavMesh)
                agent.isStopped = false;

            if (showDebugLog) Debug.Log($"[BossController] 🔄 Stun/Stagger acabou. Postura recuperada. Voltando para {CurrentState}.");
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
