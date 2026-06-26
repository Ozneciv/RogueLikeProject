using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// IA do Geobionte — Criatura simbiótica que se funde a minérios.
/// 
/// CICLO DE VIDA:
/// 1. IDLE: Vaga pelo cenário, não-hostil, não pode ser atacado
/// 2. SEEKING ORE: Ao detectar o player, busca o minério mais próximo
/// 3. FUSING: Funde-se ao minério (visual muda, cresce)
/// 4. TRANSFORMED (Bismutado): Persegue player e cria campos de cristal debuffer
/// 5. FLEEING: Ao ter HP zerado, foge e some (não morre). Pode respawnar.
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
        SeekingOre,     // Buscando minério
        Fusing,         // Fundindo-se ao minério
        Transformed,    // Bismutado — ataca com debuffs
        Fleeing         // Fugindo após ser derrotado
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
    public float oreReachDistance = 1.5f;

    private OreNode targetOre;
    private bool hasFused = false; // Funde apenas com 1 cristal

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

    // ==================== BISMUTADO (Transformado) ====================

    [Header("Bismutado — Combate")]
    [Tooltip("Velocidade de perseguição ao player")]
    public float chaseSpeed = 4f;
    [Tooltip("Velocidade de rotação")]
    public float rotationSpeed = 8f;
    [Tooltip("Distância ideal para atacar (criar campo de cristais)")]
    public float attackRange = 8f;
    [Tooltip("Cooldown entre criações de campo de cristais")]
    public float fieldCooldown = 6f;
    [Tooltip("Duração de cada campo de cristais")]
    public float fieldDuration = 10f;
    [Tooltip("Raio de cada campo de cristais")]
    public float fieldRadius = 4f;

    private float fieldTimer = 0f;
    private bool hasSpeedBuff = false;  // Ganhou speed ao roubar do player

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
                    ChangeState(GeobionteState.SeekingOre);
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
                ChangeState(GeobionteState.Idle);
                return;
            }
        }

        // Verifica se chegou ao minério (ignora o Y para não dar problema se ele estiver flutuando alto)
        Vector2 geobiontePos2D = new Vector2(transform.position.x, transform.position.z);
        Vector2 orePos2D = new Vector2(targetOre.transform.position.x, targetOre.transform.position.z);
        float distToOre = Vector2.Distance(geobiontePos2D, orePos2D);
        
        if (distToOre <= oreReachDistance)
        {
            Debug.Log("[GEOBIONTE] Alcançou o minério! Iniciando fusão...");
            StartCoroutine(FusionSequence());
        }
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
        Vector3 endScale = originalScale * transformedScale;

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

        // TRANSFORMAÇÃO COMPLETA → Bismutado
        transform.localScale = endScale;

        // Transforma as pernas do Mimic para forma Bismutado
        TransformMimicLegs();

        TransformIntoBismutado();
    }

    void TransformIntoBismutado()
    {
        // Ativa combate
        if (health != null)
        {
            health.isInvulnerable = false;
            health.ResetHealth();
        }

        // Cria health bar e damage canvas por código se não existir
        CreateHealthBarIfNeeded();

        // Mudar layer para Enemy — agora pode ser atacado pelo WeaponHitbox
        gameObject.layer = originalLayer;

        // Reset de timers
        fieldTimer = 0f;

        ChangeState(GeobionteState.Transformed);
        Debug.Log("[GEOBIONTE] TRANSFORMAÇÃO COMPLETA! Agora é BISMUTADO — modo de combate ativado!");
    }

    // ========================================================================
    // ESTADO: TRANSFORMED (Bismutado — Combate)
    // ========================================================================

    void HandleTransformed()
    {
        if (health != null && health.CurrentHealth <= 0) return;

        // Timers
        if (fieldTimer > 0) fieldTimer -= Time.deltaTime;

        // Rotação para olhar o player
        HandleRotation();

        // Combate
        HandleCombat();
    }

    void HandleRotation()
    {
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

        float distToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        // Persegue o player se está fora do range de ataque
        if (distToPlayer > attackRange * 0.6f)
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

        // Cria campo de cristais quando está perto e cooldown pronto
        if (distToPlayer <= attackRange && fieldTimer <= 0)
        {
            CreateCrystalField();
        }
    }

    void CreateCrystalField()
    {
        fieldTimer = fieldCooldown;

        // Posiciona o campo na posição atual do player
        Vector3 fieldPos = new Vector3(playerTransform.position.x, 0.05f, playerTransform.position.z);

        GameObject fieldObj = new GameObject("BismuthCrystalField");
        fieldObj.transform.position = fieldPos;

        BismuthCrystalField field = fieldObj.AddComponent<BismuthCrystalField>();
        field.fieldDuration = fieldDuration;
        field.fieldRadius = fieldRadius;
        field.ownerBismutado = this;
        field.stealBuffTime = 3f; // 3 segundos

        Debug.Log("[BISMUTADO] Campo de cristais criado na posição do player!");
    }

    /// <summary>
    /// Chamado pelo BismuthCrystalField quando rouba um buff de speed do player.
    /// O Bismutado ganha um buff de velocidade para si.
    /// </summary>
    public void OnStoleSpeedBuff()
    {
        hasSpeedBuff = true;
        Debug.Log("[BISMUTADO] Ganhou buff de velocidade roubado do player!");
    }

    // ========================================================================
    // DERROTA → FUGA (não morre)
    // ========================================================================

    /// <summary>
    /// Chamado pelo DummyHealth.onDeathOverride quando HP chega a 0.
    /// Ao invés de morrer, o Geobionte "desmerge" e foge.
    /// </summary>
    void OnDefeated()
    {
        Debug.Log("[BISMUTADO] Derrotado! Desfazendo fusão e fugindo...");

        // 1. Dropar loot antes de fugir
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

        // 4. Voltar ao visual base (encolher)
        StartCoroutine(FleeSequence());
    }

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
            // Se usar o Mimic, reduz a esfera para 1/3 do tamanho para servir de corpo central
            if (mimicComponent != null)
            {
                geobionteRenderer.transform.localScale *= 0.33f;
            }

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

        RaycastHit hit;
        bool hitGround = Physics.Raycast(transform.position + Vector3.up * 5f, Vector3.down, out hit, 15f);

        foreach(var c in cols) c.enabled = true;

        if (hitGround)
        {
            float targetY = hit.point.y + bodyHoverHeight;
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
