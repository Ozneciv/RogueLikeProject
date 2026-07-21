using UnityEngine;

/// <summary>
/// IA do Shard Swarm â€” 3 geraÃ§Ãµes de split.
///
///   Gen 0 (grande)  â†’ ao morrer: spawna 4Ã— Gen 1
///   Gen 1 (mÃ©dio)   â†’ ao morrer: spawna 2Ã— Gen 2
///   Gen 2 (pequeno) â†’ morre de verdade, dropa itens
///
/// Cada instÃ¢ncia Ã© um inimigo independente com seu prÃ³prio ShardSwarmHealth.
/// Ataque por contato (OnCollisionEnter). Movimento por Rigidbody.
///
/// DEPENDÃŠNCIAS: ShardSwarmHealth, Rigidbody, tag "Player"
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(ShardSwarmHealth))]
public class ShardSwarm_AI : MonoBehaviour
{
    // â”€â”€â”€ Split â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Header("Split")]
    [Tooltip("Arraste o prÃ³prio prefab aqui (self-reference). ObrigatÃ³rio.")]
    public GameObject shardSwarmPrefab;
    [Tooltip("0 = grande (spawna 4) | 1 = mÃ©dio (spawna 2) | 2 = pequeno (morre)")]
    public int generation = 0;
    [Tooltip("Escala dos filhos em relaÃ§Ã£o ao pai")]
    public float childScale = 0.5f;
    [Tooltip("Raio de dispersÃ£o dos filhos ao spawnar")]
    public float splitSpawnRadius = 1.5f;

    // â”€â”€â”€ Stats por GeraÃ§Ã£o â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Header("Stats â€” Gen 0 (Grande)")]
    public int gen0HP     = 150;
    public int gen0Damage = 25;

    [Header("Stats â€” Gen 1 (MÃ©dio)")]
    public int gen1HP     = 60;
    public int gen1Damage = 15;

    [Header("Stats â€” Gen 2 (Pequeno)")]
    public int gen2HP     = 25;
    public int gen2Damage = 8;

    // â”€â”€â”€ Movimento â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Header("Movimento")]
    public float moveSpeed          = 3f;
    public float rotationSpeed      = 8f;
    public float activationDistance = 20f;
    [Tooltip("Raio da órbita em volta do player.")]
    public float stopDistance       = 2.5f;
    [Tooltip("Velocidade da órbita em graus/segundo. Negativo inverte o sentido.")]
    public float orbitSpeed         = 80f;

    // â”€â”€â”€ Ataque por Contato â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Header("Ataque por Contato")]
    public float attackCooldown  = 1.5f;
    [Tooltip("Velocidade durante o charge (bote no player).")]
    public float chargeSpeed     = 9f;
    [Tooltip("Tempo de espera entre charges (segundos).")]
    public float chargeCooldown  = 3f;
    [Tooltip("Dura\u00e7\u00e3o do charge em linha reta.")]
    public float chargeDuration  = 0.35f;

    // â”€â”€â”€ Privado â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private Transform         playerTransform;
    private Rigidbody         rb;
    private ShardSwarmHealth  health;
    private EnemyDrops        drops;

    private enum MoveState { Orbit, Charge, Retreat }
    private MoveState moveState = MoveState.Orbit;

    private bool    isActivated    = false;
    private bool    isDead         = false;
    private float   attackTimer    = 0f;
    private int     contactDamage;
    private float   orbitAngle;         // \u00e2ngulo atual na \u00f3rbita (graus)
    private float   chargeTimer;        // countdown at\u00e9 o pr\u00f3ximo charge
    private float   chargePhaseTimer;   // tempo restante na fase atual
    private Vector3 chargeDir;          // dire\u00e7\u00e3o travada no in\u00edcio do charge

    // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    // Unity Callbacks
    // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    void Awake()
    {
        // Roda imediatamente no Instantiate — garante trigger antes de qualquer frame de física
        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    void Start()
    {
        rb     = GetComponent<Rigidbody>();
        health = GetComponent<ShardSwarmHealth>();
        drops  = GetComponent<EnemyDrops>();

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            playerTransform = player.transform;

        // Garante que o ShardSwarm flutue no ar (1.5m acima do chão/player) e não fique preso no chão
        Vector3 pos = transform.position;
        float minHeight = (playerTransform != null) ? (playerTransform.position.y + 1.4f) : 1.5f;
        if (pos.y < minHeight)
        {
            pos.y = minHeight;
            transform.position = pos;
        }

        rb.useGravity     = false;
        rb.freezeRotation = true;
        rb.constraints   |= RigidbodyConstraints.FreezePositionY;

        ApplyGenerationStats();

        // Ângulo inicial aleatório para que múltiplos inimigos não empilhem no mesmo ponto
        orbitAngle   = Random.Range(0f, 360f);
        // Jitter no primeiro charge para que inimigos não carreguem em sincronia
        chargeTimer  = chargeCooldown + Random.Range(0f, 1.5f);

        health.onDeathOverride = OnDeath;

        // Se o prefab também tiver DummyHealth, registra o override nele também.
        // Isso garante o split independente de qual componente receber o hit.
        DummyHealth dummy = GetComponent<DummyHealth>();
        if (dummy != null)
            dummy.onDeathOverride = OnDeath;
    }

    void Update()
    {
        if (playerTransform == null) return;

        attackTimer -= Time.deltaTime;

        if (!isActivated)
        {
            if (Vector3.Distance(transform.position, playerTransform.position) < activationDistance)
            {
                isActivated = true;

                EnemyIdentity id = GetComponent<EnemyIdentity>()
                                ?? GetComponentInChildren<EnemyIdentity>()
                                ?? GetComponentInParent<EnemyIdentity>();
                if (id != null && BestiarioManager.instancia != null)
                    BestiarioManager.instancia.Registrar(id);
            }
        }
        else
        {
            RotateTowardPlayer();
        }
    }

    void FixedUpdate()
    {
        if (!isActivated) return;
        MoveTowardPlayer();
    }

    // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    // Movimento
    // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    void MoveTowardPlayer()
    {
        switch (moveState)
        {
            case MoveState.Orbit:
                // Orbita em volta do player
                orbitAngle += orbitSpeed * Time.fixedDeltaTime;
                float rad = orbitAngle * Mathf.Deg2Rad;
                Vector3 orbitTarget = playerTransform.position
                                    + new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad)) * stopDistance;
                orbitTarget.y = transform.position.y;
                Vector3 toOrbit = orbitTarget - transform.position; toOrbit.y = 0f;
                rb.linearVelocity = toOrbit.normalized * moveSpeed;

                // Countdown para o charge
                chargeTimer -= Time.fixedDeltaTime;
                if (chargeTimer <= 0f)
                {
                    chargeDir        = HorizontalDirection();
                    chargePhaseTimer = chargeDuration;
                    moveState        = MoveState.Charge;
                }
                break;

            case MoveState.Charge:
                // Dispara em linha reta na direção travada
                rb.linearVelocity = new Vector3(chargeDir.x, 0f, chargeDir.z) * chargeSpeed;
                chargePhaseTimer -= Time.fixedDeltaTime;
                if (chargePhaseTimer <= 0f)
                {
                    // Inicia recuo
                    chargePhaseTimer = 0.5f;
                    moveState        = MoveState.Retreat;
                }
                break;

            case MoveState.Retreat:
                // Retorna ao ponto de órbita
                float radR = orbitAngle * Mathf.Deg2Rad;
                Vector3 retreatTarget = playerTransform.position
                                      + new Vector3(Mathf.Cos(radR), 0f, Mathf.Sin(radR)) * stopDistance;
                retreatTarget.y = transform.position.y;
                Vector3 toRetreat = retreatTarget - transform.position; toRetreat.y = 0f;
                rb.linearVelocity = toRetreat.normalized * moveSpeed;

                chargePhaseTimer -= Time.fixedDeltaTime;
                if (chargePhaseTimer <= 0f || toRetreat.magnitude < 0.6f)
                {
                    // De volta à órbita, reinicia o timer com pequeno jitter
                    chargeTimer = chargeCooldown + Random.Range(-0.3f, 0.5f);
                    moveState   = MoveState.Orbit;
                }
                break;
        }
    }

    void RotateTowardPlayer()
    {
        Vector3 dir = HorizontalDirection();
        if (dir == Vector3.zero) return;
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            Quaternion.LookRotation(dir),
            rotationSpeed * Time.deltaTime);
    }

    // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    // Ataque por Contato
    // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    // Trigger (sem física de colisão) — inimigo não empurra o player
    void OnTriggerEnter(Collider other)
    {
        if (!isActivated) return;
        if (attackTimer > 0f) return;
        if (!other.CompareTag("Player")) return;

        PlayerHealth ph = other.GetComponent<PlayerHealth>();
        if (ph == null) return;

        ph.TakeDamage(contactDamage, gameObject);
        attackTimer = attackCooldown;
        Debug.Log($"[SHARD SWARM] Gen{generation} acertou player! Dano={contactDamage}");
    }

    // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    // Morte e Split
    // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    void OnDeath()
    {
        if (isDead) return;
        isDead = true;

        int childGen   = generation + 1;
        int spawnCount = generation == 0 ? 4
                       : generation == 1 ? 2
                       : 0;

        if (spawnCount > 0 && shardSwarmPrefab != null)
        {
            for (int i = 0; i < spawnCount; i++)
            {
                float angle = (360f / spawnCount) * i * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(
                    Mathf.Cos(angle) * splitSpawnRadius,
                    0f,
                    Mathf.Sin(angle) * splitSpawnRadius);

                GameObject child = Instantiate(
                    shardSwarmPrefab,
                    transform.position + offset,
                    Quaternion.identity);

                child.transform.localScale = transform.localScale * childScale;

                ShardSwarm_AI childAI = child.GetComponent<ShardSwarm_AI>();
                if (childAI != null)
                {
                    childAI.generation       = childGen;
                    childAI.shardSwarmPrefab = shardSwarmPrefab;
                    childAI.childScale       = childScale;
                    childAI.splitSpawnRadius = splitSpawnRadius;
                    childAI.moveSpeed        = moveSpeed * 1.2f;
                    childAI.attackCooldown   = attackCooldown;
                    childAI.gen0HP           = gen0HP;
                    childAI.gen1HP           = gen1HP;
                    childAI.gen2HP           = gen2HP;
                    childAI.gen0Damage       = gen0Damage;
                    childAI.gen1Damage       = gen1Damage;
                    childAI.gen2Damage       = gen2Damage;
                }
            }

            Debug.Log($"[SHARD SWARM] Gen{generation} morreu â†’ {spawnCount}Ã— Gen{childGen} spawnados.");
        }
        else
        {
            // Gen 2: aciona drops antes de destruir
            if (drops != null)
                drops.OnDeath();

            Debug.Log($"[SHARD SWARM] Gen{generation} morreu. Fim de linha.");
        }

        Destroy(gameObject);
    }

    // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    // Buff (CrystalTuner)
    // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    public void SetBuff(bool active)
    {
        contactDamage = active
            ? Mathf.RoundToInt(contactDamage * 1.5f)
            : Mathf.Max(1, Mathf.RoundToInt(contactDamage / 1.5f));
    }

    // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    // Helpers
    // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    void ApplyGenerationStats()
    {
        switch (generation)
        {
            case 0:
                health.maxHealth = gen0HP;
                contactDamage    = gen0Damage;
                break;
            case 1:
                health.maxHealth = gen1HP;
                contactDamage    = gen1Damage;
                break;
            case 2:
                health.maxHealth = gen2HP;
                contactDamage    = gen2Damage;
                break;
            default:
                Debug.LogWarning($"[SHARD SWARM] GeraÃ§Ã£o invÃ¡lida: {generation}");
                health.maxHealth = gen0HP;
                contactDamage    = gen0Damage;
                break;
        }
        health.SetHealth(health.maxHealth);
    }

    float HorizontalDistance()
    {
        if (playerTransform == null) return 999f;
        Vector3 a = transform.position;        a.y = 0f;
        Vector3 b = playerTransform.position;  b.y = 0f;
        return Vector3.Distance(a, b);
    }

    Vector3 HorizontalDirection()
    {
        if (playerTransform == null) return Vector3.zero;
        Vector3 dir = playerTransform.position - transform.position;
        dir.y = 0f;
        return dir.normalized;
    }

    // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    // Gizmos
    // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, activationDistance);

        Gizmos.color = new Color(0.5f, 1f, 0.5f, 0.35f);
        Gizmos.DrawSphere(transform.position, splitSpawnRadius);
    }
}
