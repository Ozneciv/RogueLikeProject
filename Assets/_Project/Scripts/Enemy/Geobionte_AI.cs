using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// IA do Geobionte — Criatura simbiótica que se funde a minérios.
/// 
/// CICLO DE VIDA:
/// 1. IDLE: Vaga pelo cenário, não-hostil, não pode ser atacado
/// 2. SEEKING ORE: Ao detectar o player, busca o minério mais próximo (atacável — 7 hits para impedir)
/// 3. FUSING: Funde-se ao minério (visual muda, cresce, vira cubo)
/// 4. TRANSFORMED (Bismutado): Cubo que persegue player e usa golpe giratório horizontal
/// 5. DEFEATED → Reverte ao Geobionte padrão (esfera passiva)
/// 6. INTERRUPTED: Se impedido durante SeekingOre, foge para um canto e despawna
/// 
/// [SENTINELA] (Semi-boss futuro): Usa as pernas maiores + campo de cristais debuffer
/// 
/// COMPONENTES NECESSÁRIOS no Prefab:
/// - Rigidbody
/// - Collider (SphereCollider recomendado)
/// - DummyHealth (para a forma transformada)
/// - EnemyDrops (drops ao ser "derrotado")
/// - EnemyIdentity (para o bestiário)
/// 
/// SETUP NO INSPECTOR:
/// 1. Coloque o prefab na sala com pelo menos um OreNode por perto
/// 2. Configure o Crystal Tuner se quiser que ele seja buffável
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(DummyHealth))]
public class Geobionte_AI : MonoBehaviour
{
    // ==================== ESTADOS ====================

    public enum GeobionteState
    {
        Idle,           // Vagando pacificamente
        SeekingOre,     // Buscando minério (atacável — pode ser impedido)
        Fusing,         // Fundindo-se ao minério
        Transformed,    // Bismutado — ataca com debuffs
        Fleeing,        // Fugindo após ser derrotado
        Interrupted     // Impedido de transformar — fugindo para canto e despawnando
    }

    [Header("Estado Atual")]
    [SerializeField] private GeobionteState currentState = GeobionteState.Idle;

    // ==================== REFERÊNCIAS ====================

    [Header("Referências")]
    private Transform playerTransform;
    private Rigidbody rb;
    private DummyHealth health;

    // ==================== ATIVAÇÃO ====================

    [Header("Ativação")]
    [Tooltip("Distância para detectar o player e começar a buscar minério (valor alto = sala inteira)")]
    public float activationDistance = 200f;
    private bool isActivated = false;

    // ==================== MIMIC — HOVER ====================

    [Header("Mimic — Corpo Flutuante")]
    [Tooltip("Altura do corpo acima do chão (para as pernas do Mimic)")]
    public float bodyHoverHeight = 1.2f;
    [Tooltip("Velocidade de ajuste da altura")]
    public float hoverLerpSpeed = 8f;

    // ==================== IDLE (Wandering) ====================

    [Header("Idle — Movimento Passivo")]
    [Tooltip("Velocidade ao vagar aleatoriamente")]
    public float wanderSpeed = 2f;
    [Tooltip("Intervalo entre mudanças de direção")]
    public float wanderChangeInterval = 3f;
    private Vector3 wanderDirection;
    private float wanderTimer;

    // ==================== BUSCA DE MINÉRIO ====================

    [Header("Busca de Minério")]
    [Tooltip("Raio de busca por minérios (valor alto = sala inteira)")]
    public float oreSearchRadius = 500f;
    [Tooltip("Velocidade ao ir até o minério")]
    public float seekSpeed = 5f;
    [Tooltip("Distância para considerar que chegou ao minério")]
    public float oreReachDistance = 2.5f;

    private OreNode targetOre;
    private bool hasFused = false; // Funde apenas com 1 cristal
    private float oreStallTimer = 0f; // Timer para forçar absorção se ficar perto do cristal

    // ==================== PREVENÇÃO (Impedimento de Fusão) ====================

    [Header("Prevenção (Impedimento)")]
    [Tooltip("Número de hits que o player precisa dar para impedir a fusão")]
    public int preventionMaxHP = 7;
    [Tooltip("Velocidade de fuga ao ser impedido (indo para o canto da fase)")]
    public float interruptedFleeSpeed = 8f;

    private int preventionCurrentHP;
    private bool isPrevented = false; // Já foi impedido?
    private Vector3 interruptedTargetCorner; // Canto da fase para onde vai fugir

    // ==================== FUSÃO ====================

    [Header("Fusão")]
    [Tooltip("Tempo da animação de fusão (segundos)")]
    public float fusionDuration = 1.5f;
    [Tooltip("Escala do Geobionte após a fusão")]
    public float transformedScale = 2.5f;

    private Vector3 originalScale;
    private int absorbedOreValue = 0; // Valor do cristal absorvido
    private MimicSpace.Mimic mimicComponent; // Referência ao sistema de pernas procedurais
    private int originalNumberOfLegs;
    private int originalPartsPerLeg;
    private float originalNewLegRadius;

    // ==================== BISMUTADO (Transformado — Cubo) ====================

    [Header("Bismutado — Combate (Cubo)")]
    [Tooltip("Velocidade de perseguição ao player")]
    public float chaseSpeed = 4f;
    [Tooltip("Velocidade de rotação")]
    public float rotationSpeed = 8f;
    [Tooltip("Distância ideal para atacar")]
    public float attackRange = 5f;

    [Header("Bismutado — Golpe Giratório")]
    [Tooltip("Dano do golpe horizontal giratório")]
    public int sweepDamage = 8;
    [Tooltip("Raio do golpe giratório (alcance ao redor do corpo)")]
    public float sweepRange = 2f;
    [Tooltip("Cooldown entre golpes giratórios (segundos)")]
    public float sweepCooldown = 6f;
    [Tooltip("Duração da animação do golpe (segundos)")]
    public float sweepDuration = 0.6f;
    [Tooltip("Cor do indicador visual do golpe")]
    public Color sweepIndicatorColor = new Color(1f, 0.2f, 0.2f, 0.4f);

    private float sweepTimer = 0f;
    private bool isSweeping = false;

    // ==================== CONFIGURAÇÃO DO TIPO ====================

    [Header("Configuração de Tipo (Sentinela vs Bismutado)")]
    [Tooltip("Se true, este Geobionte se comportará como o semi-boss Sentinela. Se false, como o Bismutado padrão.")]
    public bool isSentinel = false;
    [System.NonSerialized]
    public float bismutadoScale = 1.5f;

    // ==================== SENTINELA (Semi-boss — Futuro) ====================

    [Header("Sentinela — Campo de Cristais (Semi-boss)")]
    [Tooltip("Cooldown entre criações de campo de cristais (usado pelo Sentinela)")]
    public float fieldCooldown = 6f;
    [Tooltip("Duração de cada campo de cristais")]
    public float fieldDuration = 10f;
    [Tooltip("Raio de cada campo de cristais")]
    public float fieldRadius = 4f;

    private float fieldTimer = 0f;
    private bool hasSpeedBuff = false;  // Ganhou speed ao roubar do player

    // ==================== VISUAL — MESH ====================

    private GameObject bodyMeshObject; // Referência ao mesh do corpo (esfera ou cubo)
    private Vector3 originalMeshLocalScale; // Escala original do mesh do corpo
    private Vector3 mimicBodyLocalScale; // Escala reduzida do mesh do corpo para quando tem pernas

    // ==================== FUGA ====================

    [Header("Fuga")]
    [Tooltip("Velocidade de fuga após ser derrotado")]
    public float fleeSpeed = 10f;
    [Tooltip("Distância do player para ser destruído durante a fuga")]
    public float fleeDestroyDistance = 50f;

    // ==================== RESPAWN ====================

    [Header("Respawn")]
    [Tooltip("Tempo para respawnar após fugir (segundos). 0 = não respawna.")]
    public float respawnDelay = 30f;
    private Vector3 spawnPosition;
    private Quaternion spawnRotation;
    private static Geobionte_AI pendingRespawn;

    // ==================== BUFF (Crystal Tuner) ====================

    [Header("Buff")]
    private bool isBuffed = false;
    private float originalChaseSpeed;
    private float originalFieldCooldown;

    // ==================== VISUAL ====================

    private Renderer geobionteRenderer;
    private Material geoMaterial;
    
    [Header("Cores do Geobionte (Corpo)")]
    [Tooltip("Cor na forma neutra (andando sem atacar)")]
    public Color baseColor = new Color(0.03f, 0.01f, 0.05f, 1f); // Preto/Roxo muito escuro
    [Tooltip("Cor quando se funde ao cristal (modo de ataque)")]
    public Color transformedColor = new Color(0.7f, 0.3f, 0.6f, 1f); // Bismuto roxo/rosa
    
    private int originalLayer;

    // ==================== BESTIÁRIO ====================
    private bool registradoNoBestiario = false;

    // ========================================================================
    // START
    // ========================================================================

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        health = GetComponent<DummyHealth>();

        // Configurar Rigidbody
        rb.freezeRotation = true;

        // Salvar estado inicial
        originalScale = transform.localScale;
        spawnPosition = transform.position;
        spawnRotation = transform.rotation;
        originalChaseSpeed = chaseSpeed;
        originalFieldCooldown = fieldCooldown;
        originalLayer = gameObject.layer;

        // Configurar Mimic (pernas procedurais)
        mimicComponent = GetComponent<MimicSpace.Mimic>();
        if (mimicComponent != null)
        {
            originalNumberOfLegs = mimicComponent.numberOfLegs;
            originalPartsPerLeg = mimicComponent.partsPerLeg;
            originalNewLegRadius = mimicComponent.newLegRadius;

            // Remove o Movement.cs do Mimic (IA do Geobionte controla o movimento)
            MimicSpace.Movement mimicMovement = GetComponent<MimicSpace.Movement>();
            if (mimicMovement != null) Destroy(mimicMovement);

            // Desativar gravidade — o hover controla a altura
            rb.useGravity = false;
        }

        // Encontrar player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
        else
        {
            Debug.LogError("[GEOBIONTE] Player não encontrado! Verifique a tag 'Player'.");
        }

        // Configurar visual
        SetupVisual();

        // FORMA BASE: não atacável
        // DummyHealth fica invulnerável e escondemos a health bar
        if (health != null)
        {
            health.isInvulnerable = true;
            // O health bar já começa escondido por padrão no DummyHealth
        }

        // Mudar layer para Default (neutro) — WeaponHitbox não detecta
        gameObject.layer = LayerMask.NameToLayer("Default");

        // Configurar override de morte
        if (health != null)
        {
            health.onDeathOverride = OnDefeated;
        }

        // Inicializar prevenção HP
        preventionCurrentHP = preventionMaxHP;

        // Iniciar wandering
        PickNewWanderDirection();

        Debug.Log("[GEOBIONTE] Inicializado em modo passivo. Escala: " + originalScale);
    }

    // ========================================================================
    // UPDATE
    // ========================================================================

    void Update()
    {
        if (playerTransform == null) return;

        switch (currentState)
        {
            case GeobionteState.Idle:
                HandleIdle();
                break;
            case GeobionteState.SeekingOre:
                HandleSeekingOre();
                break;
            case GeobionteState.Fusing:
                // Controlado por coroutine
                break;
            case GeobionteState.Transformed:
                HandleTransformed();
                break;
            case GeobionteState.Fleeing:
                HandleFleeing();
                break;
            case GeobionteState.Interrupted:
                HandleInterrupted();
                break;
        }
    }

    void FixedUpdate()
    {
        switch (currentState)
        {
            case GeobionteState.Idle:
                MoveWander();
                break;
            case GeobionteState.SeekingOre:
                MoveToOre();
                break;
            case GeobionteState.Transformed:
                MoveChasePlayer();
                break;
            case GeobionteState.Fleeing:
                MoveFlee();
                break;
            case GeobionteState.Interrupted:
                MoveInterrupted();
                break;
        }

        // Manter corpo flutuando para as pernas do Mimic
        HandleBodyHover();
    }

    // ========================================================================
    // ESTADO: IDLE (Passivo)
    // ========================================================================

    void HandleIdle()
    {
        // Timer de wandering
        wanderTimer -= Time.deltaTime;
        if (wanderTimer <= 0f)
        {
            PickNewWanderDirection();
        }

        // Se já foi impedido, não busca mais minério
        if (isPrevented) return;

        // Verifica proximidade do player para ativar
        float distToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        if (distToPlayer < activationDistance)
        {
            if (!isActivated)
            {
                isActivated = true;
                Debug.Log("[GEOBIONTE] Player detectado a " + distToPlayer.ToString("F1") + "m! Buscando minério...");

                // Registrar no bestiário
                if (!registradoNoBestiario)
                {
                    registradoNoBestiario = true;
                    EnemyIdentity id = GetComponent<EnemyIdentity>() ?? GetComponentInChildren<EnemyIdentity>() ?? GetComponentInParent<EnemyIdentity>();
                    if (id != null && BestiarioManager.instancia != null)
                        BestiarioManager.instancia.Registrar(id);
                }
            }

            // Só busca minério se ainda não fundiu
            if (!hasFused)
            {
                FindNearestOre();
                if (targetOre != null)
                {
                    EnterSeekingOre();
                }
            }
            // Se já fundiu ou não encontrou minério, continua vagando
        }
    }

    void PickNewWanderDirection()
    {
        wanderDirection = new Vector3(
            Random.Range(-1f, 1f),
            0f,
            Random.Range(-1f, 1f)
        ).normalized;

        wanderTimer = wanderChangeInterval + Random.Range(-1f, 1f);
    }

    void MoveWander()
    {
        Vector3 targetVelocity = wanderDirection * wanderSpeed;
        rb.linearVelocity = new Vector3(targetVelocity.x, rb.linearVelocity.y, targetVelocity.z);
        UpdateMimicVelocity();

        // Rotação suave na direção do movimento
        if (wanderDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(wanderDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 3f * Time.fixedDeltaTime);
        }
    }

    // ========================================================================
    // ESTADO: SEEKING ORE (Buscando Minério)
    // ========================================================================

    /// <summary>
    /// Entra no estado SeekingOre: torna o Geobionte atacável com prevenção HP.
    /// O player pode impedir a fusão acertando 7 hits.
    /// </summary>
    void EnterSeekingOre()
    {
        ChangeState(GeobionteState.SeekingOre);

        // Torna atacável durante a busca — player pode impedir a fusão
        if (health != null)
        {
            // Configura HP de prevenção (7 hits para impedir)
            health.maxHealth = preventionMaxHP;
            preventionCurrentHP = preventionMaxHP;
            health.ResetHealth();
            health.isInvulnerable = false;

            // Cada hit causa exatamente 1 de dano (7 hits = 7 HP)
            health.fixedDamageOverride = 1;

            // Override de morte: impedimento ao invés de morte
            health.onDeathOverride = OnPrevented;
        }

        // Mudar layer para Enemy — agora pode ser atacado
        gameObject.layer = originalLayer;

        // Cria health bar de prevenção
        CreateHealthBarIfNeeded();

        // Cor da barra: amarela para indicar prevenção (diferente do vermelho de combate)
        if (health != null && health.healthBarSlider != null)
        {
            Image fillImage = health.healthBarSlider.fillRect?.GetComponent<Image>();
            if (fillImage != null)
                fillImage.color = new Color(1f, 0.8f, 0.2f, 1f); // Amarelo/dourado
        }

        Debug.Log("[GEOBIONTE] Buscando minério — ATACÁVEL! " + preventionMaxHP + " hits para impedir.");
    }

    void HandleSeekingOre()
    {
        // Verifica se o minério alvo ainda é válido
        if (targetOre == null || !targetOre.IsAvailable())
        {
            // Busca outro minério
            FindNearestOre();
            if (targetOre == null)
            {
                Debug.Log("[GEOBIONTE] Sem minério disponível! Voltando a vagar.");
                ExitSeekingOre();
                ChangeState(GeobionteState.Idle);
                oreStallTimer = 0f;
                return;
            }
        }

        // Verifica se chegou ao minério (ignora o Y para não dar problema se ele estiver flutuando alto)
        Vector2 geobiontePos2D = new Vector2(transform.position.x, transform.position.z);
        Vector2 orePos2D = new Vector2(targetOre.transform.position.x, targetOre.transform.position.z);
        float distToOre = Vector2.Distance(geobiontePos2D, orePos2D);
        
        // Absorção direta se chegou perto o suficiente
        if (distToOre <= oreReachDistance)
        {
            Debug.Log("[GEOBIONTE] Alcançou o minério! Iniciando fusão...");
            oreStallTimer = 0f;
            ExitSeekingOre();
            StartCoroutine(FusionSequence());
            return;
        }
        
        // Segurança: se está relativamente perto mas não conseguiu absorver,
        // conta um timer e força a absorção após 2 segundos
        if (distToOre <= oreReachDistance * 2.5f)
        {
            oreStallTimer += Time.deltaTime;
            if (oreStallTimer >= 2f)
            {
                Debug.Log("[GEOBIONTE] Perto do minério por muito tempo — forçando absorção! Dist: " + distToOre.ToString("F2"));
                oreStallTimer = 0f;
                ExitSeekingOre();
                StartCoroutine(FusionSequence());
            }
        }
        else
        {
            oreStallTimer = 0f;
        }
    }

    /// <summary>
    /// Sai do estado SeekingOre: torna invulnerável novamente e esconde a health bar.
    /// </summary>
    void ExitSeekingOre()
    {
        if (health != null)
        {
            health.isInvulnerable = true;
            health.fixedDamageOverride = 0; // Volta ao dano normal

            // Esconde health bar de prevenção
            if (health.healthBarSlider != null)
                health.healthBarSlider.gameObject.SetActive(false);
        }

        // Volta para layer Default (não atacável)
        gameObject.layer = LayerMask.NameToLayer("Default");
    }

    // ========================================================================
    // PREVENÇÃO — Player impediu a fusão
    // ========================================================================

    /// <summary>
    /// Chamado quando o player dá 7 hits no Geobionte enquanto ele busca minério.
    /// O Geobionte é impedido de transformar, foge para um canto da fase e despawna.
    /// </summary>
    void OnPrevented()
    {
        Debug.Log("[GEOBIONTE] IMPEDIDO! Player conseguiu impedir a fusão!");

        isPrevented = true;
        hasFused = true; // Marca como fundido para não tentar de novo

        // Torna invulnerável (não pode ser atacado durante a fuga)
        if (health != null)
        {
            health.isInvulnerable = true;
            if (health.healthBarSlider != null)
                health.healthBarSlider.gameObject.SetActive(false);
        }

        // Volta para layer Default (não atacável)
        gameObject.layer = LayerMask.NameToLayer("Default");

        // Encontra o canto mais distante da fase
        interruptedTargetCorner = FindFarthestCorner();

        // Entra no estado Interrupted
        ChangeState(GeobionteState.Interrupted);

        Debug.Log("[GEOBIONTE] Fugindo para o canto em " + interruptedTargetCorner + " e despawnando...");
    }

    /// <summary>
    /// Encontra o canto mais distante da fase usando os limites dos renderers da cena
    /// ou os colliders de chão.
    /// </summary>
    Vector3 FindFarthestCorner()
    {
        // Tenta encontrar os limites da sala usando o chão ou as paredes
        // Busca todos os colliders de chão (normalmente no layer Default ou com tag "Ground")
        Bounds roomBounds = new Bounds(transform.position, Vector3.one * 10f);
        bool foundBounds = false;

        // Busca pelo chão/paredes da sala
        Collider[] allColliders = FindObjectsByType<Collider>(FindObjectsSortMode.None);
        foreach (Collider col in allColliders)
        {
            // Ignora triggers e o próprio Geobionte
            if (col.isTrigger) continue;
            if (col.gameObject == gameObject) continue;
            if (col.GetComponent<Rigidbody>() != null) continue; // Ignora objetos dinâmicos

            if (!foundBounds)
            {
                roomBounds = col.bounds;
                foundBounds = true;
            }
            else
            {
                roomBounds.Encapsulate(col.bounds);
            }
        }

        // Define os 4 cantos no nível do chão
        float y = transform.position.y;
        Vector3[] corners = new Vector3[4]
        {
            new Vector3(roomBounds.min.x, y, roomBounds.min.z),
            new Vector3(roomBounds.min.x, y, roomBounds.max.z),
            new Vector3(roomBounds.max.x, y, roomBounds.min.z),
            new Vector3(roomBounds.max.x, y, roomBounds.max.z)
        };

        // Encontra o canto mais distante do player
        Vector3 farthest = corners[0];
        float maxDist = 0f;
        foreach (Vector3 corner in corners)
        {
            float dist = Vector3.Distance(playerTransform.position, corner);
            if (dist > maxDist)
            {
                maxDist = dist;
                farthest = corner;
            }
        }

        return farthest;
    }

    void HandleInterrupted()
    {
        // Verifica se chegou ao canto (ignora Y)
        Vector2 pos2D = new Vector2(transform.position.x, transform.position.z);
        Vector2 target2D = new Vector2(interruptedTargetCorner.x, interruptedTargetCorner.z);
        float dist = Vector2.Distance(pos2D, target2D);

        if (dist <= 2f)
        {
            Debug.Log("[GEOBIONTE] Chegou ao canto da fase. Despawnando...");
            StartCoroutine(DespawnSequence());
        }
    }

    void MoveInterrupted()
    {
        // Move em direção ao canto alvo
        Vector3 direction = (interruptedTargetCorner - transform.position).normalized;
        direction.y = 0;

        Vector3 targetVelocity = direction * interruptedFleeSpeed;
        rb.linearVelocity = new Vector3(targetVelocity.x, rb.linearVelocity.y, targetVelocity.z);
        UpdateMimicVelocity();

        // Rotação na direção da fuga
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 10f * Time.fixedDeltaTime);
        }
    }

    /// <summary>
    /// Sequência de despawn: encolhe gradualmente e se destrói.
    /// </summary>
    IEnumerator DespawnSequence()
    {
        // Para o movimento
        rb.linearVelocity = Vector3.zero;
        UpdateMimicVelocity();

        // Encolhe até sumir
        float shrinkDuration = 1f;
        float elapsed = 0f;
        Vector3 startScale = transform.localScale;

        while (elapsed < shrinkDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / shrinkDuration;

            // Encolhe com easing
            float easedT = t * t;
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, easedT);

            // Escurece gradualmente
            if (geoMaterial != null)
            {
                Color fadeColor = Color.Lerp(baseColor, Color.black, easedT);
                geoMaterial.color = fadeColor;
                geoMaterial.SetColor("_EmissionColor", fadeColor * 0.5f);
                if (geoMaterial.HasProperty("_BaseColor"))
                    geoMaterial.SetColor("_BaseColor", fadeColor);
            }

            yield return null;
        }

        Debug.Log("[GEOBIONTE] Despawnado após ser impedido.");
        Destroy(gameObject);
    }

    void MoveToOre()
    {
        if (targetOre == null) return;

        Vector3 direction = (targetOre.transform.position - transform.position).normalized;
        direction.y = 0;

        Vector3 targetVelocity = direction * seekSpeed;
        rb.linearVelocity = new Vector3(targetVelocity.x, rb.linearVelocity.y, targetVelocity.z);
        UpdateMimicVelocity();

        // Rotação na direção do minério
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
        }
    }

    void FindNearestOre()
    {
        OreNode[] allOres = FindObjectsByType<OreNode>(FindObjectsSortMode.None);
        float closestDist = float.MaxValue;
        targetOre = null;

        foreach (OreNode ore in allOres)
        {
            if (!ore.IsAvailable()) continue;

            // Busca qualquer OreNode na cena (sem filtro de distância)
            float dist = Vector3.Distance(transform.position, ore.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                targetOre = ore;
            }
        }

        if (targetOre != null)
            Debug.Log("[GEOBIONTE] Minério encontrado: " + targetOre.oreName + " a " + closestDist.ToString("F1") + "m");
    }

    // ========================================================================
    // ESTADO: FUSING (Fusão)
    // ========================================================================

    IEnumerator FusionSequence()
    {
        ChangeState(GeobionteState.Fusing);
        hasFused = true;

        // Para o movimento
        rb.linearVelocity = Vector3.zero;
        UpdateMimicVelocity();

        // Grava o valor do cristal e consome
        if (targetOre != null)
        {
            absorbedOreValue = targetOre.oreValue;
            targetOre.Consume(); // Destrói o cristal (some da cena)
        }

        Debug.Log("[GEOBIONTE] FUSÃO INICIADA! Cristal absorvido (valor: " + absorbedOreValue + "). Transformando em Bismutado...");

        // Animação de fusão: cresce gradualmente
        float elapsed = 0f;
        Vector3 startScale = transform.localScale;
        float targetScaleMultiplier = isSentinel ? transformedScale : bismutadoScale;
        Vector3 endScale = originalScale * targetScaleMultiplier;

        while (elapsed < fusionDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fusionDuration;

            // Crescimento com easing
            float easedT = t * t * (3f - 2f * t); // smoothstep
            transform.localScale = Vector3.Lerp(startScale, endScale, easedT);

            // Mudança de cor gradual
            if (geoMaterial != null)
            {
                Color currentColor = Color.Lerp(baseColor, transformedColor, easedT);
                geoMaterial.color = currentColor;
                geoMaterial.SetColor("_EmissionColor", currentColor * 2f);
                if (geoMaterial.HasProperty("_BaseColor"))
                    geoMaterial.SetColor("_BaseColor", currentColor);
            }

            yield return null;
        }

        // TRANSFORMAÇÃO COMPLETA
        transform.localScale = endScale;

        if (!isSentinel)
        {
            // Troca o mesh do corpo: esfera → cubo (Bismutado)
            SwapBodyMeshToCube();
            // Desativa as pernas para o Bismutado
            if (mimicComponent != null)
            {
                mimicComponent.SetLegsActive(false);
            }
        }
        else
        {
            // [SENTINELA] Transforma as pernas do Mimic
            TransformMimicLegs();
        }

        TransformIntoBismutado();
    }

    void TransformIntoBismutado()
    {
        // Ativa combate — restaura HP de combate (diferente do HP de prevenção)
        if (health != null)
        {
            health.maxHealth = 100; // HP de combate (original do DummyHealth)
            health.isInvulnerable = false;
            health.fixedDamageOverride = 0; // Volta ao dano normal da arma
            health.ResetHealth();

            // Override de morte: Bismutado derrotado → volta ao Geobionte padrão
            health.onDeathOverride = OnDefeated;
        }

        // Cria health bar e damage canvas por código se não existir
        CreateHealthBarIfNeeded();

        // Restaura cor da barra para vermelho (combate)
        if (health != null && health.healthBarSlider != null)
        {
            Image fillImage = health.healthBarSlider.fillRect?.GetComponent<Image>();
            if (fillImage != null)
                fillImage.color = Color.red;
        }

        // Mudar layer para Enemy — agora pode ser atacado pelo WeaponHitbox
        gameObject.layer = originalLayer;

        // Reset de timers
        sweepTimer = 0f;
        isSweeping = false;

        ChangeState(GeobionteState.Transformed);
        Debug.Log("[BISMUTADO] TRANSFORMAÇÃO COMPLETA! Forma de CUBO — golpe giratório ativado!");
    }

    // ========================================================================
    // ESTADO: TRANSFORMED (Bismutado — Combate)
    // ========================================================================

    void HandleTransformed()
    {
        if (health != null && health.CurrentHealth <= 0) return;

        // Timers
        if (sweepTimer > 0) sweepTimer -= Time.deltaTime;
        if (fieldTimer > 0) fieldTimer -= Time.deltaTime;

        // Rotação para olhar o player
        HandleRotation();

        // Combate — golpe giratório / campo de cristais
        HandleCombat();
    }

    void HandleRotation()
    {
        if (isSweeping) return; // Não rotaciona durante o golpe

        Vector3 dirToPlayer = (playerTransform.position - transform.position).normalized;
        dirToPlayer.y = 0;

        if (dirToPlayer != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(dirToPlayer);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    void MoveChasePlayer()
    {
        if (health != null && health.CurrentHealth <= 0) return;
        if (isSweeping) return; // Não se move durante o golpe

        float distToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        float effectiveAttackRange = isSentinel ? attackRange : sweepRange;

        // Persegue o player se está fora do range de ataque
        if (distToPlayer > effectiveAttackRange * 0.8f)
        {
            Vector3 direction = (playerTransform.position - transform.position).normalized;
            direction.y = 0;

            float speed = hasSpeedBuff ? chaseSpeed * 1.3f : chaseSpeed;
            Vector3 targetVelocity = direction * speed;
            rb.linearVelocity = new Vector3(targetVelocity.x, rb.linearVelocity.y, targetVelocity.z);
            UpdateMimicVelocity();
        }
        else
        {
            // Para perto do player
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
            UpdateMimicVelocity();
        }
    }

    void HandleCombat()
    {
        float distToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        // Cria campo de cristais quando está perto e cooldown pronto (usado por Bismutado e Sentinela)
        if (distToPlayer <= attackRange && fieldTimer <= 0)
        {
            CreateCrystalField();
        }

        // Golpe giratório quando está perto e cooldown pronto (usado por Bismutado e Sentinela)
        if (distToPlayer <= sweepRange && sweepTimer <= 0 && !isSweeping)
        {
            StartCoroutine(SweepAttack());
        }
    }

    // ========================================================================
    // GOLPE GIRATÓRIO (Bismutado)
    // ========================================================================

    /// <summary>
    /// Executa o golpe em meia-lua frontal do Bismutado.
    /// O corpo gira 360° durante o ataque, mas o dano e o visual
    /// são baseados na direção inicial (meia-lua fixa na frente).
    /// </summary>
    IEnumerator SweepAttack()
    {
        isSweeping = true;
        sweepTimer = sweepCooldown;

        // Para o movimento durante o golpe
        rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
        UpdateMimicVelocity();

        // Rotaciona para olhar para o player antes do golpe para alinhar a meia lua
        if (playerTransform != null)
        {
            Vector3 lookDir = (playerTransform.position - transform.position).normalized;
            lookDir.y = 0;
            if (lookDir != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(lookDir);
            }
        }

        // Salva a direção frontal inicial (mundo) — o arco visual e dano usam essa referência fixa
        Vector3 initialForward = transform.forward;
        Vector3 initialRight = transform.right;
        Quaternion initialRotation = transform.rotation;

        Debug.Log("[BISMUTADO] Golpe meia-lua frontal com giro completo!");

        int pointsCount = 30;

        // === FASE 1: Indicador de aviso (0.4s) ===
        // Mostra o arco de 180° na frente + linhas radiais para preencher a zona de perigo
        GameObject warningObj = new GameObject("SweepWarning");
        
        // Arco externo (borda da meia-lua)
        LineRenderer warningArcLR = warningObj.AddComponent<LineRenderer>();
        warningArcLR.startWidth = 0.2f;
        warningArcLR.endWidth = 0.2f;
        warningArcLR.useWorldSpace = true;
        Material warningMat = CreateSweepMaterial(new Color(1f, 0.8f, 0f, 0.6f));
        warningArcLR.material = warningMat;
        warningArcLR.positionCount = pointsCount + 2; // +2 para fechar com linhas até o centro

        // Linhas radiais de preenchimento (mostram a área da meia-lua)
        int fillLineCount = 5;
        GameObject[] fillLines = new GameObject[fillLineCount];
        Material[] fillMats = new Material[fillLineCount];
        for (int f = 0; f < fillLineCount; f++)
        {
            fillLines[f] = new GameObject("FillLine_" + f);
            fillLines[f].transform.SetParent(warningObj.transform);
            LineRenderer flr = fillLines[f].AddComponent<LineRenderer>();
            flr.startWidth = 0.08f;
            flr.endWidth = 0.08f;
            flr.useWorldSpace = true;
            flr.positionCount = 2;
            fillMats[f] = CreateSweepMaterial(new Color(1f, 0.8f, 0f, 0.3f));
            flr.material = fillMats[f];
        }

        float warningDuration = 0.4f;
        float elapsed = 0f;
        while (elapsed < warningDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / warningDuration;

            Vector3 center = transform.position;
            float pulseAlpha = 0.4f + Mathf.Sin(t * Mathf.PI * 4f) * 0.2f;

            // Arco de 180° + linhas de fechamento até o centro
            Vector3[] arcPoints = new Vector3[pointsCount + 2];
            arcPoints[0] = center; // Linha do centro ao início do arco
            for (int i = 0; i < pointsCount; i++)
            {
                float angle = Mathf.Lerp(-90f, 90f, (float)i / (pointsCount - 1));
                float rad = angle * Mathf.Deg2Rad;
                // Usa a direção inicial fixa (não gira com o corpo)
                Vector3 worldPos = center + (initialRight * Mathf.Sin(rad) + initialForward * Mathf.Cos(rad)) * sweepRange;
                arcPoints[i + 1] = worldPos;
            }
            arcPoints[pointsCount + 1] = center; // Linha do fim do arco de volta ao centro
            warningArcLR.SetPositions(arcPoints);

            // Linhas radiais de preenchimento
            for (int f = 0; f < fillLineCount; f++)
            {
                float fillAngle = Mathf.Lerp(-90f, 90f, (float)(f + 1) / (fillLineCount + 1));
                float fillRad = fillAngle * Mathf.Deg2Rad;
                Vector3 endPos = center + (initialRight * Mathf.Sin(fillRad) + initialForward * Mathf.Cos(fillRad)) * sweepRange * t;
                LineRenderer flr = fillLines[f].GetComponent<LineRenderer>();
                flr.SetPosition(0, center);
                flr.SetPosition(1, endPos);
            }

            yield return null;
        }

        // Limpa aviso
        if (warningObj != null) Destroy(warningObj);
        if (warningMat != null) Destroy(warningMat);
        foreach (Material fm in fillMats) { if (fm != null) Destroy(fm); }

        // === FASE 2: Golpe com giro de 360° (sweepDuration) ===
        
        // Rastro do arco (mostra a meia-lua já varrida)
        GameObject trailArcObj = new GameObject("SweepTrailArc");
        LineRenderer trailLR = trailArcObj.AddComponent<LineRenderer>();
        trailLR.startWidth = 0.35f;
        trailLR.endWidth = 0.35f;
        trailLR.useWorldSpace = true;
        Material trailMat = CreateSweepMaterial(new Color(sweepIndicatorColor.r, sweepIndicatorColor.g, sweepIndicatorColor.b, 0.3f));
        trailLR.material = trailMat;

        // Lâmina (linha que varre da esquerda para a direita)
        GameObject bladeObj = new GameObject("SweepBlade");
        LineRenderer bladeLR = bladeObj.AddComponent<LineRenderer>();
        bladeLR.startWidth = 0.6f;
        bladeLR.endWidth = 0.1f;
        bladeLR.useWorldSpace = true;
        Material bladeMat = CreateSweepMaterial(sweepIndicatorColor);
        bladeLR.material = bladeMat;
        bladeLR.positionCount = 2;

        bool playerHit = false;
        elapsed = 0f;
        float totalBodyRotation = 0f;

        while (elapsed < sweepDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / sweepDuration;

            Vector3 center = transform.position;

            // --- Giro de 360° do corpo ---
            float rotationThisFrame = 360f * (Time.deltaTime / sweepDuration);
            transform.Rotate(0, rotationThisFrame, 0);
            totalBodyRotation += rotationThisFrame;

            // --- Visual da lâmina varrendo -90° até +90° (baseado na direção inicial) ---
            float bladeAngle = Mathf.Lerp(-90f, 90f, t);
            float bladeRad = bladeAngle * Mathf.Deg2Rad;
            Vector3 bladeDir = (initialRight * Mathf.Sin(bladeRad) + initialForward * Mathf.Cos(bladeRad)).normalized;
            Vector3 bladeEnd = center + bladeDir * sweepRange;

            bladeLR.SetPosition(0, center);
            bladeLR.SetPosition(1, bladeEnd);

            // Cor da lâmina com brilho pulsante
            float bladePulse = 0.8f + Mathf.Sin(t * Mathf.PI * 6f) * 0.2f;
            if (bladeMat != null)
            {
                bladeMat.color = new Color(
                    sweepIndicatorColor.r * bladePulse,
                    sweepIndicatorColor.g * bladePulse,
                    sweepIndicatorColor.b * bladePulse,
                    Mathf.Lerp(0.9f, 0.3f, t)
                );
            }

            // --- Rastro: arco mostrando a área já varrida ---
            float sweptAngle = Mathf.Lerp(-90f, 90f, t); // Ângulo atual da lâmina
            int trailPoints = Mathf.Max(2, Mathf.RoundToInt(pointsCount * t));
            trailLR.positionCount = trailPoints;
            Vector3[] trailPositions = new Vector3[trailPoints];
            for (int i = 0; i < trailPoints; i++)
            {
                float a = Mathf.Lerp(-90f, sweptAngle, (float)i / (trailPoints - 1));
                float aRad = a * Mathf.Deg2Rad;
                trailPositions[i] = center + (initialRight * Mathf.Sin(aRad) + initialForward * Mathf.Cos(aRad)) * sweepRange;
            }
            trailLR.SetPositions(trailPositions);

            // Fade do rastro
            if (trailMat != null)
            {
                trailMat.color = new Color(sweepIndicatorColor.r, sweepIndicatorColor.g, sweepIndicatorColor.b, Mathf.Lerp(0.4f, 0.05f, t));
            }

            // --- Detecção de dano baseada na direção inicial (meia-lua fixa) ---
            if (!playerHit && playerTransform != null)
            {
                float distToPlayer = Vector3.Distance(transform.position, playerTransform.position);
                Vector3 dirToPlayer = (playerTransform.position - transform.position).normalized;
                // Usa a direção inicial para detectar se o player está na meia-lua frontal
                float dot = Vector3.Dot(initialForward, dirToPlayer);

                // Meia lua: dentro do alcance E na frente da direção inicial (dot > 0)
                if (distToPlayer <= sweepRange && dot >= 0f)
                {
                    // Verifica se a lâmina já passou pela posição do player
                    float playerAngle = Mathf.Atan2(
                        Vector3.Dot(initialRight, dirToPlayer),
                        Vector3.Dot(initialForward, dirToPlayer)
                    ) * Mathf.Rad2Deg;
                    
                    // A lâmina varre de -90° a +90°, acerta quando passa pelo ângulo do player
                    if (playerAngle <= bladeAngle + 15f) // +15° de tolerância
                    {
                        PlayerHealth playerHealth = playerTransform.GetComponent<PlayerHealth>();
                        if (playerHealth != null)
                        {
                            playerHealth.TakeDamage(sweepDamage, gameObject);
                            playerHit = true;
                            Debug.Log("[BISMUTADO] Golpe meia-lua acertou o player! Dano: " + sweepDamage);
                        }
                    }
                }
            }

            yield return null;
        }

        // Garante que o corpo completou os 360° exatos
        transform.rotation = initialRotation;

        // Limpa visuais
        if (trailArcObj != null) Destroy(trailArcObj);
        if (bladeObj != null) Destroy(bladeObj);
        if (trailMat != null) Destroy(trailMat);
        if (bladeMat != null) Destroy(bladeMat);

        isSweeping = false;
    }

    /// <summary>
    /// Cria um material transparente para os indicadores visuais do golpe giratório.
    /// </summary>
    Material CreateSweepMaterial(Color color)
    {
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Legacy Shaders/Particles/Alpha Blended Premultiply");
        if (shader == null) shader = Shader.Find("Standard");

        Material mat = new Material(shader);
        mat.color = color;
        return mat;
    }

    // ========================================================================
    // CAMPO DE CRISTAIS — [SENTINELA] (Semi-boss, futuro)
    // ========================================================================

    /// <summary>
    /// [SENTINELA] Cria campo de cristais debuffer. Reservado para o Geobionte Sentinela.
    /// </summary>
    void CreateCrystalField()
    {
        fieldTimer = fieldCooldown;

        // Posiciona o campo na posição horizontal atual do player, mas no chão
        Vector3 fieldPos = playerTransform.position;
        RaycastHit groundHit;
        // Faz um raycast para baixo a partir de 2.0 unidades acima da posição do player para achar o chão
        if (Physics.Raycast(fieldPos + Vector3.up * 2f, Vector3.down, out groundHit, 10f))
        {
            fieldPos.y = groundHit.point.y + 0.05f;
        }
        else
        {
            fieldPos.y = 0.05f; // Fallback caso não ache o chão
        }

        GameObject fieldObj = new GameObject("BismuthCrystalField");
        fieldObj.transform.position = fieldPos;

        BismuthCrystalField field = fieldObj.AddComponent<BismuthCrystalField>();
        field.fieldDuration = fieldDuration;
        field.fieldRadius = fieldRadius;
        field.ownerBismutado = this;
        field.stealBuffTime = 3f; // 3 segundos

        Debug.Log($"[{(isSentinel ? "SENTINELA" : "BISMUTADO")}] Campo de cristais criado na posição do player!");
    }

    /// <summary>
    /// Chamado pelo BismuthCrystalField quando rouba um buff de speed do player.
    /// O Bismutado/Sentinela ganha um buff de velocidade para si.
    /// </summary>
    public void OnStoleSpeedBuff()
    {
        hasSpeedBuff = true;
        Debug.Log($"[{(isSentinel ? "SENTINELA" : "BISMUTADO")}] Ganhou buff de velocidade roubado do player!");
    }

    // ========================================================================
    // DERROTA → REVERSÃO (Bismutado volta ao Geobionte padrão)
    // ========================================================================

    /// <summary>
    /// Chamado pelo DummyHealth.onDeathOverride quando HP do Bismutado chega a 0.
    /// Ao invés de morrer, o Bismutado reverte ao Geobionte padrão (forma base).
    /// </summary>
    void OnDefeated()
    {
        Debug.Log("[BISMUTADO] Derrotado! Revertendo ao Geobionte padrão...");

        // Cancela qualquer sweep em andamento
        isSweeping = false;

        // 1. Dropar loot
        EnemyDrops drops = GetComponent<EnemyDrops>()
                        ?? GetComponentInChildren<EnemyDrops>()
                        ?? GetComponentInParent<EnemyDrops>();
        if (drops != null)
        {
            drops.OnDeath();
        }

        // 2. Dropar 60% do valor do cristal absorvido como essência extra
        if (absorbedOreValue > 0 && drops != null && drops.essencePrefab != null)
        {
            int refundAmount = Mathf.RoundToInt(absorbedOreValue * 0.6f);
            if (refundAmount > 0)
            {
                Vector3 spawnPos = transform.position + Vector3.up * 0.5f;
                GameObject essenceObj = Instantiate(drops.essencePrefab, spawnPos, Quaternion.identity);
                EssencePickup essencePickup = essenceObj.GetComponent<EssencePickup>();
                if (essencePickup != null)
                {
                    essencePickup.essenceValue = refundAmount;
                }
                // Aplica impulso para cima
                Rigidbody essenceRb = essenceObj.GetComponent<Rigidbody>();
                if (essenceRb != null)
                {
                    essenceRb.linearDamping = 5f;
                    essenceRb.AddForce(Vector3.up * 3f, ForceMode.Impulse);
                }
                Debug.Log("[BISMUTADO] Devolveu 60% do cristal: " + refundAmount + " essência (cristal valia " + absorbedOreValue + ")");
            }
        }

        // 3. Restaurar buffs roubados do player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            PlayerDebuffs playerDebuffs = player.GetComponent<PlayerDebuffs>();
            if (playerDebuffs != null)
            {
                playerDebuffs.RestoreStolenBuffs(gameObject);
                playerDebuffs.RemoveSlow(); // Remove qualquer slow ativo
            }
        }

        // 4. Reverter ao Geobionte padrão (animação)
        StartCoroutine(RevertToBaseSequence());
    }

    /// <summary>
    /// Sequência de reversão: Bismutado (cubo) → Geobionte padrão (esfera).
    /// Encolhe, troca mesh, muda cor e volta ao estado Idle.
    /// </summary>
    IEnumerator RevertToBaseSequence()
    {
        // Torna invulnerável durante a reversão
        if (health != null)
        {
            health.isInvulnerable = true;
        }

        // Esconde health bar
        if (health != null && health.healthBarSlider != null)
        {
            health.healthBarSlider.gameObject.SetActive(false);
        }

        // Volta para layer Default (não atacável)
        gameObject.layer = LayerMask.NameToLayer("Default");

        // Encolhe de volta ao tamanho original + muda cor
        float shrinkDuration = 0.8f;
        float elapsed = 0f;
        Vector3 startScale = transform.localScale;

        while (elapsed < shrinkDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / shrinkDuration;
            float easedT = t * t * (3f - 2f * t); // smoothstep
            transform.localScale = Vector3.Lerp(startScale, originalScale, easedT);

            // Volta à cor base
            if (geoMaterial != null)
            {
                Color currentColor = Color.Lerp(transformedColor, baseColor, easedT);
                geoMaterial.color = currentColor;
                geoMaterial.SetColor("_EmissionColor", currentColor * 0.5f);
                if (geoMaterial.HasProperty("_BaseColor"))
                    geoMaterial.SetColor("_BaseColor", currentColor);
            }

            yield return null;
        }

        transform.localScale = originalScale;

        // Troca o mesh de volta: cubo → esfera
        SwapBodyMeshToSphere();

        // Restaura pernas do Mimic
        if (mimicComponent != null)
        {
            if (isSentinel)
            {
                RestoreMimicLegs();
            }
            else
            {
                // Recalcula parâmetros para o tamanho original antes de reativar as pernas
                mimicComponent.numberOfLegs = originalNumberOfLegs;
                mimicComponent.partsPerLeg = originalPartsPerLeg;
                mimicComponent.newLegRadius = originalNewLegRadius;
                mimicComponent.RecalculateParameters();
                mimicComponent.SetLegsActive(true);
            }
        }

        // Reset de estado para Geobionte padrão
        hasFused = false;
        isPrevented = false; // Permite buscar minérios novamente
        absorbedOreValue = 0;
        hasSpeedBuff = false;
        targetOre = null;
        sweepTimer = 0f;
        isSweeping = false;

        // Procura por minérios na sala novamente
        FindNearestOre();
        if (targetOre != null)
        {
            EnterSeekingOre();
        }
        else
        {
            ChangeState(GeobionteState.Idle);
            PickNewWanderDirection();
        }

        Debug.Log("[GEOBIONTE] Revertido ao Geobionte padrão! Procurando novos minérios ou vagando pacificamente.");
    }

    // ========================================================================
    // FUGA — [SENTINELA] (mantido para o semi-boss)
    // ========================================================================

    IEnumerator FleeSequence()
    {
        ChangeState(GeobionteState.Fleeing);

        // Torna invulnerável durante a fuga
        if (health != null)
        {
            health.isInvulnerable = true;
        }

        // Esconde health bar
        if (health != null && health.healthBarSlider != null)
        {
            health.healthBarSlider.gameObject.SetActive(false);
        }

        // Volta para layer Default (não atacável)
        gameObject.layer = LayerMask.NameToLayer("Default");

        // Restaura pernas do Mimic para forma base
        RestoreMimicLegs();

        // Encolhe de volta ao tamanho original rapidamente
        float shrinkDuration = 0.5f;
        float elapsed = 0f;
        Vector3 startScale = transform.localScale;

        while (elapsed < shrinkDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / shrinkDuration;
            transform.localScale = Vector3.Lerp(startScale, originalScale, t);

            // Volta à cor base
            if (geoMaterial != null)
            {
                Color currentColor = Color.Lerp(transformedColor, baseColor, t);
                geoMaterial.color = currentColor;
                geoMaterial.SetColor("_EmissionColor", currentColor * 2f);
                if (geoMaterial.HasProperty("_BaseColor"))
                    geoMaterial.SetColor("_BaseColor", currentColor);
            }

            yield return null;
        }

        transform.localScale = originalScale;

        Debug.Log("[GEOBIONTE] Fuga iniciada! Correndo para longe...");

        // A fuga é gerenciada pelo FixedUpdate/MoveFlee
    }

    void HandleFleeing()
    {
        // Verifica distância do player — se longe o bastante, some
        float distToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        if (distToPlayer > fleeDestroyDistance)
        {
            Debug.Log("[GEOBIONTE] Fugiu com sucesso! Distância: " + distToPlayer.ToString("F1") + "m");

            // Agenda respawn se configurado
            if (respawnDelay > 0f)
            {
                StartCoroutine(ScheduleRespawn());
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }

    void MoveFlee()
    {
        // Corre na direção oposta ao player
        Vector3 fleeDirection = (transform.position - playerTransform.position).normalized;
        fleeDirection.y = 0;

        Vector3 targetVelocity = fleeDirection * fleeSpeed;
        rb.linearVelocity = new Vector3(targetVelocity.x, rb.linearVelocity.y, targetVelocity.z);
        UpdateMimicVelocity();

        // Rotação na direção da fuga
        if (fleeDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(fleeDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 10f * Time.fixedDeltaTime);
        }
    }

    // ========================================================================
    // RESPAWN
    // ========================================================================

    IEnumerator ScheduleRespawn()
    {
        // Não podemos usar SetActive(false) porque coroutines param em objetos desativados.
        // Em vez disso, "escondemos" o objeto: desativa renderer, collider e rigidbody.
        if (geobionteRenderer != null) geobionteRenderer.enabled = false;
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;
        rb.linearVelocity = Vector3.zero;
        UpdateMimicVelocity();
        rb.isKinematic = true;

        Debug.Log("[GEOBIONTE] Respawn agendado em " + respawnDelay + " segundos...");

        yield return new WaitForSeconds(respawnDelay);

        // Verifica se o GameObject ainda existe (pode ter sido destruído pela troca de cena)
        if (this == null || gameObject == null) yield break;

        // Respawna na posição original
        transform.position = spawnPosition;
        transform.rotation = spawnRotation;
        transform.localScale = originalScale;

        // Reset de estado
        currentState = GeobionteState.Idle;
        isActivated = false;
        hasSpeedBuff = false;
        hasFused = false;
        isPrevented = false;
        preventionCurrentHP = preventionMaxHP;
        absorbedOreValue = 0;
        targetOre = null;
        fieldTimer = 0f;

        // Restaura pernas do Mimic para forma base
        RestoreMimicLegs();

        // Reset de HP
        if (health != null)
        {
            health.isInvulnerable = true;
            health.ResetHealth();
        }

        // Volta à layer Default
        gameObject.layer = LayerMask.NameToLayer("Default");

        // Restaura visual base
        if (geoMaterial != null)
        {
            geoMaterial.color = baseColor;
            geoMaterial.SetColor("_EmissionColor", baseColor * 2f);
            if (geoMaterial.HasProperty("_BaseColor"))
                geoMaterial.SetColor("_BaseColor", baseColor);
        }

        // Reativa componentes
        if (geobionteRenderer != null) geobionteRenderer.enabled = true;
        if (col != null) col.enabled = true;
        rb.isKinematic = false;

        // Reinicia wandering
        PickNewWanderDirection();

        Debug.Log("[GEOBIONTE] Respawnado na posição original!");
    }

    // ========================================================================
    // BUFF (Crystal Tuner)
    // ========================================================================

    /// <summary>
    /// Chamado pelo Crystal Tuner quando buffa/desbuffa.
    /// Quando buffado: persegue mais rápido e campo de cristais tem cooldown menor.
    /// </summary>
    public void SetBuff(bool active)
    {
        if (active && !isBuffed)
        {
            isBuffed = true;
            chaseSpeed *= 1.3f;
            fieldCooldown *= 0.6f;
            Debug.Log("[GEOBIONTE] BUFFADO pelo Crystal Tuner! Speed +30%, Cooldown -40%");
        }
        else if (!active && isBuffed)
        {
            isBuffed = false;
            chaseSpeed = originalChaseSpeed;
            fieldCooldown = originalFieldCooldown;
            Debug.Log("[GEOBIONTE] Buff do Crystal Tuner removido.");
        }
    }

    // ========================================================================
    // VISUAL
    // ========================================================================

    void SetupVisual()
    {
        geobionteRenderer = GetComponentInChildren<Renderer>();
        
        if (geobionteRenderer != null)
        {
            // Guardar referência ao mesh do corpo
            bodyMeshObject = geobionteRenderer.gameObject;
            
            // Salva escala original do mesh (completa, antes de reduzir)
            originalMeshLocalScale = geobionteRenderer.transform.localScale;

            // Se usar o Mimic, reduz a esfera para 1/3 do tamanho para servir de corpo central
            if (mimicComponent != null)
            {
                geobionteRenderer.transform.localScale *= 0.33f;
            }
            
            // Salva a escala do corpo com pernas (reduzida)
            mimicBodyLocalScale = geobionteRenderer.transform.localScale;

            // URP usa "Universal Render Pipeline/Lit", fallback para "Standard"
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");

            geoMaterial = new Material(shader);
            geoMaterial.color = baseColor;

            // Emissão (funciona em ambos URP e Built-in)
            geoMaterial.EnableKeyword("_EMISSION");
            // Brilho mais sutil na forma base para combinar com a cor escura
            geoMaterial.SetColor("_EmissionColor", baseColor * 0.5f);

            // Suporte URP (_BaseColor) e Built-in (_Color)
            if (geoMaterial.HasProperty("_BaseColor"))
                geoMaterial.SetColor("_BaseColor", baseColor);

            geobionteRenderer.material = geoMaterial;
        }
    }

    // ========================================================================
    // TROCA DE MESH (Esfera ↔ Cubo)
    // ========================================================================

    /// <summary>
    /// Troca o mesh do corpo de esfera para cubo (transformação Bismutado).
    /// Preserva a hierarquia, escala e material.
    /// </summary>
    void SwapBodyMeshToCube()
    {
        if (bodyMeshObject == null) return;

        // Guarda referências
        Transform parent = bodyMeshObject.transform.parent;
        Vector3 localPos = bodyMeshObject.transform.localPosition;
        Quaternion localRot = bodyMeshObject.transform.localRotation;

        // Destrói o mesh antigo (esfera)
        Destroy(bodyMeshObject);

        // Cria cubo
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = "BismutadoBody";
        cube.transform.SetParent(parent);
        cube.transform.localPosition = localPos;
        cube.transform.localScale = originalMeshLocalScale; // Usa escala original não-reduzida
        cube.transform.localRotation = localRot;

        // Remove o collider do cubo (o Geobionte já tem seu próprio collider)
        Collider cubeCol = cube.GetComponent<Collider>();
        if (cubeCol != null) Destroy(cubeCol);

        // Aplica o material existente
        Renderer cubeRenderer = cube.GetComponent<Renderer>();
        if (cubeRenderer != null && geoMaterial != null)
        {
            cubeRenderer.material = geoMaterial;
        }

        // Atualiza referências
        bodyMeshObject = cube;
        geobionteRenderer = cubeRenderer;

        Debug.Log("[BISMUTADO] Mesh do corpo trocado para CUBO (tamanho original).");
    }

    /// <summary>
    /// Troca o mesh do corpo de cubo para esfera (reversão ao Geobionte padrão).
    /// Preserva a hierarquia, escala e material.
    /// </summary>
    void SwapBodyMeshToSphere()
    {
        if (bodyMeshObject == null) return;

        // Guarda referências
        Transform parent = bodyMeshObject.transform.parent;
        Vector3 localPos = bodyMeshObject.transform.localPosition;
        Quaternion localRot = bodyMeshObject.transform.localRotation;

        // Destrói o mesh antigo (cubo)
        Destroy(bodyMeshObject);

        // Cria esfera
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = "GeobionteBody";
        sphere.transform.SetParent(parent);
        sphere.transform.localPosition = localPos;
        sphere.transform.localScale = mimicBodyLocalScale; // Usa escala reduzida para as pernas do Mimic
        sphere.transform.localRotation = localRot;

        // Remove o collider da esfera (o Geobionte já tem seu próprio collider)
        Collider sphereCol = sphere.GetComponent<Collider>();
        if (sphereCol != null) Destroy(sphereCol);

        // Aplica o material existente (com cor base)
        Renderer sphereRenderer = sphere.GetComponent<Renderer>();
        if (sphereRenderer != null && geoMaterial != null)
        {
            geoMaterial.color = baseColor;
            geoMaterial.SetColor("_EmissionColor", baseColor * 0.5f);
            if (geoMaterial.HasProperty("_BaseColor"))
                geoMaterial.SetColor("_BaseColor", baseColor);
            sphereRenderer.material = geoMaterial;
        }

        // Atualiza referências
        bodyMeshObject = sphere;
        geobionteRenderer = sphereRenderer;

        Debug.Log("[GEOBIONTE] Mesh do corpo restaurado para ESFERA (tamanho reduzido).");
    }

    // ========================================================================
    // UTILS
    // ========================================================================

    void ChangeState(GeobionteState newState)
    {
        if (currentState == newState) return;
        Debug.Log("[GEOBIONTE] Estado: " + currentState + " → " + newState);
        currentState = newState;
    }

    // ========================================================================
    // MIMIC — HOVER (Corpo Flutuante)
    // ========================================================================

    /// <summary>
    /// Mantém o corpo do Geobionte flutuando acima do chão para que
    /// as pernas do Mimic tenham espaço para se estender.
    /// Substitui a lógica do Movement.cs original do Mimic.
    /// </summary>
    void HandleBodyHover()
    {
        if (mimicComponent == null) return;

        Collider[] cols = GetComponentsInChildren<Collider>();
        foreach(var c in cols) c.enabled = false;

        RaycastHit[] hits = Physics.RaycastAll(transform.position + Vector3.up * 5f, Vector3.down, 15f);
        RaycastHit groundHit = new RaycastHit();
        bool hitGround = false;
        float highestGroundY = -float.MaxValue;

        foreach (var hit in hits)
        {
            if (hit.collider.isTrigger) continue;
            if (hit.collider.CompareTag("Player")) continue;
            if (hit.collider.gameObject == gameObject || hit.collider.transform.IsChildOf(transform)) continue;
            if (hit.collider.CompareTag("Enemy")) continue;

            // Encontra o chão válido mais alto abaixo de nós
            if (hit.point.y > highestGroundY)
            {
                highestGroundY = hit.point.y;
                groundHit = hit;
                hitGround = true;
            }
        }

        foreach(var c in cols) c.enabled = true;

        if (hitGround)
        {
            float currentHoverHeight = (currentState == GeobionteState.Transformed && !isSentinel) ? bodyHoverHeight * 0.5f : bodyHoverHeight;
            float targetY = groundHit.point.y + currentHoverHeight;
            float yError = targetY - transform.position.y;
            float yVelocity = Mathf.Clamp(yError * hoverLerpSpeed, -10f, 10f);
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, yVelocity, rb.linearVelocity.z);
        }
        else
        {
            // Fallback: sem chão detectado, simula gravidade
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, rb.linearVelocity.y - 9.81f * Time.fixedDeltaTime, rb.linearVelocity.z);
        }
    }

    // ========================================================================
    // TRANSFORMAÇÃO VISUAL: MIMIC LEGS
    // ========================================================================

    /// <summary>
    /// Aumenta pernas do Mimic para forma Bismutado.
    /// </summary>
    void TransformMimicLegs()
    {
        if (mimicComponent == null) return;

        // Aumenta número de pernas e alcance para forma Bismutado
        mimicComponent.numberOfLegs = originalNumberOfLegs * 2;
        mimicComponent.partsPerLeg = originalPartsPerLeg + 1;
        mimicComponent.newLegRadius = originalNewLegRadius * transformedScale;
        mimicComponent.RecalculateParameters();

        Debug.Log("[GEOBIONTE] Pernas Mimic transformadas para forma Bismutado!");
    }

    /// <summary>
    /// Restaura parâmetros originais do Mimic.
    /// </summary>
    void RestoreMimicLegs()
    {
        if (mimicComponent == null) return;

        mimicComponent.numberOfLegs = originalNumberOfLegs;
        mimicComponent.partsPerLeg = originalPartsPerLeg;
        mimicComponent.newLegRadius = originalNewLegRadius;
        mimicComponent.RecalculateParameters();

        Debug.Log("[GEOBIONTE] Pernas Mimic restauradas para forma base.");
    }

    /// <summary>
    /// Atualiza o velocity do Mimic para posicionamento correto das pernas.
    /// </summary>
    void UpdateMimicVelocity()
    {
        if (mimicComponent != null)
        {
            mimicComponent.velocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        }
    }

    // ========================================================================
    // HEALTH BAR (criada por código)
    // ========================================================================

    /// <summary>
    /// Cria o Canvas World Space + Slider de vida + configura o DamageCanva por código.
    /// Só é chamado na transformação, pois a forma base é invulnerável.
    /// </summary>
    void CreateHealthBarIfNeeded()
    {
        if (health == null) return;

        // Se já tem slider configurado, apenas mostra
        if (health.healthBarSlider != null)
        {
            health.healthBarSlider.gameObject.SetActive(true);
            return;
        }

        // Criar Canvas World Space
        GameObject canvasObj = new GameObject("HealthBarCanvas");
        canvasObj.transform.SetParent(transform);
        canvasObj.transform.localPosition = new Vector3(0, 2.5f, 0);
        canvasObj.transform.localScale = Vector3.one * 0.01f;

        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        // Billboard: faz o canvas sempre olhar para a câmera
        BillboardUI billboard = canvasObj.AddComponent<BillboardUI>();

        RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(200, 30);

        // Background do slider
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(canvasObj.transform, false);
        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        // Slider
        GameObject sliderObj = new GameObject("HealthSlider");
        sliderObj.transform.SetParent(canvasObj.transform, false);
        Slider slider = sliderObj.AddComponent<Slider>();
        RectTransform sliderRect = sliderObj.GetComponent<RectTransform>();
        sliderRect.anchorMin = Vector2.zero;
        sliderRect.anchorMax = Vector2.one;
        sliderRect.offsetMin = new Vector2(4, 4);
        sliderRect.offsetMax = new Vector2(-4, -4);

        // Fill Area
        GameObject fillAreaObj = new GameObject("Fill Area");
        fillAreaObj.transform.SetParent(sliderObj.transform, false);
        RectTransform fillAreaRect = fillAreaObj.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.offsetMin = Vector2.zero;
        fillAreaRect.offsetMax = Vector2.zero;

        // Fill
        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(fillAreaObj.transform, false);
        Image fillImage = fillObj.AddComponent<Image>();
        fillImage.color = Color.red;
        RectTransform fillRect = fillObj.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        // Configurar slider
        slider.fillRect = fillRect;
        slider.minValue = 0;
        slider.maxValue = 1;
        slider.value = 1;
        slider.interactable = false;
        // Remove os handles do slider (não é interativo)
        slider.transition = Selectable.Transition.None;

        // Registrar no DummyHealth
        health.healthBarSlider = slider;

        // Tentar buscar o DamageCanva_Text prefab na cena
        if (health.floatingDamageTextPrefab == null)
        {
            // Procurar em Resources ou usar referência de outro inimigo
            DummyHealth[] allHealth = FindObjectsByType<DummyHealth>(FindObjectsSortMode.None);
            foreach (var h in allHealth)
            {
                if (h != health && h.floatingDamageTextPrefab != null)
                {
                    health.floatingDamageTextPrefab = h.floatingDamageTextPrefab;
                    Debug.Log("[GEOBIONTE] DamageCanva_Text copiado de: " + h.gameObject.name);
                    break;
                }
            }
        }

        Debug.Log("[GEOBIONTE] Health bar criada por código!");
    }

    void OnDestroy()
    {
        // Limpa referências para evitar erros
        if (geoMaterial != null) Destroy(geoMaterial);

        // Remove qualquer slow residual no player
        if (playerTransform != null)
        {
            PlayerDebuffs debuffs = playerTransform.GetComponent<PlayerDebuffs>();
            if (debuffs != null)
            {
                debuffs.RestoreStolenBuffs(gameObject);
            }
        }
    }

    // ========================================================================
    // GIZMOS
    // ========================================================================

    void OnDrawGizmosSelected()
    {
        // Range de ativação
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, activationDistance);

        // Range de busca de minério
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, oreSearchRadius);

        // Range de ataque (campo de cristais)
        Gizmos.color = new Color(0.7f, 0.3f, 0.6f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Range de alcance do minério
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, oreReachDistance);
    }
}
