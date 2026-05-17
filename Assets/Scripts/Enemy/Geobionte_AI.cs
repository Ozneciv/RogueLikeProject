using UnityEngine;
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
    [Tooltip("Distância para detectar o player e começar a buscar minério")]
    public float activationDistance = 20f;
    private bool isActivated = false;

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
    [Tooltip("Raio de busca por minérios próximos")]
    public float oreSearchRadius = 30f;
    [Tooltip("Velocidade ao ir até o minério")]
    public float seekSpeed = 5f;
    [Tooltip("Distância para considerar que chegou ao minério")]
    public float oreReachDistance = 1.5f;

    private OreNode targetOre;

    // ==================== FUSÃO ====================

    [Header("Fusão")]
    [Tooltip("Tempo da animação de fusão (segundos)")]
    public float fusionDuration = 1.5f;
    [Tooltip("Escala do Geobionte após a fusão")]
    public float transformedScale = 2.5f;

    private Vector3 originalScale;

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
    private Color baseColor = new Color(0.4f, 0.9f, 0.5f, 1f); // Verde orgânico
    private Color transformedColor = new Color(0.7f, 0.3f, 0.6f, 1f); // Bismuto roxo/rosa
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

            // Busca minério
            FindNearestOre();
            if (targetOre != null)
            {
                ChangeState(GeobionteState.SeekingOre);
            }
            // Se não encontrou minério, continua vagando
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

        // Verifica se chegou ao minério
        float distToOre = Vector3.Distance(transform.position, targetOre.transform.position);
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

            float dist = Vector3.Distance(transform.position, ore.transform.position);
            if (dist < closestDist && dist <= oreSearchRadius)
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

        // Para o movimento
        rb.linearVelocity = Vector3.zero;

        // Consome o minério
        if (targetOre != null)
        {
            targetOre.Consume();
        }

        Debug.Log("[GEOBIONTE] FUSÃO INICIADA! Transformando em Bismutado...");

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
        TransformIntoBismutado();
    }

    void TransformIntoBismutado()
    {
        // Ativa combate
        if (health != null)
        {
            health.isInvulnerable = false;
            // Restaura HP total para a forma transformada
            // (reseta para garantir que começa cheio)
        }

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
        }
        else
        {
            // Para perto do player
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
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

        // 2. Restaurar buffs roubados do player
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

        // 3. Voltar ao visual base (encolher)
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
        targetOre = null;
        fieldTimer = 0f;

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
            // URP usa "Universal Render Pipeline/Lit", fallback para "Standard"
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");

            geoMaterial = new Material(shader);
            geoMaterial.color = baseColor;

            // Emissão (funciona em ambos URP e Built-in)
            geoMaterial.EnableKeyword("_EMISSION");
            geoMaterial.SetColor("_EmissionColor", baseColor * 2f);

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
