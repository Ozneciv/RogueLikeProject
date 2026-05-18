using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// IA do Shard Swarm - Enxame de fragmentos de cristal que ataca em grupo
/// Usa ShardSwarmHealth para sistema de vida (com SetHealth para split)
/// Fragmentos podem se separar ao tomar dano pesado
/// Possui 3 padrões de ataque e entra em Enrage com HP baixo
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(ShardSwarmHealth))]
public class ShardSwarm_AI : MonoBehaviour
{
    [Header("Referências")]
    private Transform playerTransform;
    private Rigidbody rb;
    private ShardSwarmHealth health;

    [Header("Fragmentos")]
    [Tooltip("Lista de GameObjects filhos que são os fragmentos")]
    public List<GameObject> shards = new List<GameObject>();
    [Tooltip("Raio da órbita dos fragmentos")]
    public float orbitRadius = 1.5f;
    [Tooltip("Velocidade de órbita base")]
    public float orbitSpeed = 2f;

    [Header("Ativação")]
    public float activationDistance = 20f;
    private bool isActivated = false;

    [Header("Movimento")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 8f;
    public float attackRange = 5f;

    [Header("Ataque")]
    public float attackCooldown = 2f;
    public int damagePerShard = 5;
    public int combinedDamage = 20;
    public float attackDuration = 0.5f;

    [Header("Split (Divisão)")]
    [Tooltip("% do HP máximo que triggera split quando perdido de uma vez")]
    [Range(0.1f, 0.5f)]
    public float splitThresholdPercent = 0.3f;
    [Tooltip("Distância lateral em que o clone vai aparecer")]
    public float splitSpawnOffset = 3f;
    [Tooltip("Se false, este enxame é um clone e não pode se dividir novamente")]
    public bool canSplit = true;
    [Tooltip("VFX de partícula ao fazer split")]
    public GameObject splitVFX;

    [Header("Enrage")]
    [Tooltip("% do HP que ativa o modo enrage")]
    [Range(0.1f, 0.5f)]
    public float enrageThreshold = 0.3f;
    [Tooltip("Multiplicador de velocidade de órbita durante enrage")]
    public float enrageOrbitMultiplier = 1.5f;
    [Tooltip("Multiplicador de velocidade de movimento durante enrage")]
    public float enrageMoveMultiplier = 1.3f;
    private bool isEnraged = false;

    [Header("Morte")]
    public float deathExplosionRadius = 3f;
    public int deathExplosionDamage = 15;
    public GameObject deathExplosionVFX;

    // Estados internos
    private bool isAttacking = false;
    private float attackTimer = 0f;
    private int lastKnownHP;
    private float damageAccumulator = 0f;
    private bool hasSplit = false;
    private bool isClone = false;

    // Tracking
    private int shardsAlive;
    private List<Vector3> originalShardPositions = new List<Vector3>();
    private float anchoredY;

    // Velocidades base (para enrage)
    private float baseOrbitSpeed;
    private float baseMoveSpeed;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        health = GetComponent<ShardSwarmHealth>();

        rb.useGravity = false;
        rb.freezeRotation = true;

        anchoredY = transform.position.y;

        // Salva velocidades base para enrage
        baseOrbitSpeed = orbitSpeed;
        baseMoveSpeed = moveSpeed;

        // Encontra o player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
        else
        {
            Debug.LogError("[SHARD SWARM] Player não encontrado! Verifique a tag 'Player'.");
        }

        InitializeShards();
        lastKnownHP = health.CurrentHealth;
    }

    void InitializeShards()
    {
        if (shards.Count == 0)
        {
            foreach (Transform child in transform)
            {
                if (child.GetComponent<Collider>() != null)
                {
                    shards.Add(child.gameObject);
                }
            }
        }

        shardsAlive = shards.Count;
        originalShardPositions.Clear();

        foreach (GameObject shard in shards)
        {
            originalShardPositions.Add(shard.transform.localPosition);
        }

        Debug.Log("[SHARD SWARM] Inicializado com " + shards.Count + " fragmentos. HP: " + health.maxHealth + (isClone ? " (CLONE)" : ""));
    }

    void Update()
    {
        if (playerTransform == null) return;
        if (health.CurrentHealth <= 0) return;

        DetectDamage();
        CheckEnrage();

        if (attackTimer > 0) attackTimer -= Time.deltaTime;

        if (!isActivated)
        {
            float dist = Vector3.Distance(transform.position, playerTransform.position);
            if (dist < activationDistance)
            {
                isActivated = true;

                EnemyIdentity id = GetComponent<EnemyIdentity>() ?? GetComponentInChildren<EnemyIdentity>() ?? GetComponentInParent<EnemyIdentity>();
                if (id != null && BestiarioManager.instancia != null)
                    BestiarioManager.instancia.Registrar(id);
            }
            return;
        }

        UpdateShardOrbit();

        if (isAttacking) return;

        HandleRotation();
        HandleCombat();
        UpdateShardVisibility();
    }

    void FixedUpdate()
    {
        if (!isActivated || isAttacking) return;
        if (health.CurrentHealth <= 0) return;

        HandleMovement();
    }

    // ==================== DETECÇÃO DE DANO ====================

    void DetectDamage()
    {
        int currentHP = health.CurrentHealth;
        if (currentHP < lastKnownHP)
        {
            int damageTaken = lastKnownHP - currentHP;
            damageAccumulator += damageTaken;

            float splitThreshold = health.maxHealth * splitThresholdPercent;
            if (canSplit && !hasSplit && damageAccumulator >= splitThreshold && currentHP > 1)
            {
                Split();
                damageAccumulator = 0;
            }
        }
        lastKnownHP = currentHP;

        if (currentHP <= 0)
        {
            Die();
        }
    }

    // ==================== ENRAGE ====================

    void CheckEnrage()
    {
        if (isEnraged) return;

        float hpPercent = (float)health.CurrentHealth / health.maxHealth;
        if (hpPercent <= enrageThreshold)
        {
            isEnraged = true;
            orbitSpeed = baseOrbitSpeed * enrageOrbitMultiplier;
            moveSpeed = baseMoveSpeed * enrageMoveMultiplier;
            Debug.Log("[SHARD SWARM] ENRAGE! Velocidade aumentada!");
        }
    }

    // ==================== FRAGMENTOS ====================

    void UpdateShardOrbit()
    {
        float time = Time.time * orbitSpeed;
        int shardCount = GetActiveShardCount();
        if (shardCount == 0) return;

        int index = 0;
        foreach (GameObject shard in shards)
        {
            if (shard == null || !shard.activeSelf) continue;

            float angle = (360f / shardCount) * index + time * 50f;
            float rad = angle * Mathf.Deg2Rad;

            Vector3 offset = new Vector3(
                Mathf.Cos(rad) * orbitRadius,
                Mathf.Sin(time + index) * 0.3f,
                Mathf.Sin(rad) * orbitRadius
            );

            shard.transform.localPosition = Vector3.Lerp(
                shard.transform.localPosition,
                offset,
                Time.deltaTime * 5f
            );

            index++;
        }
    }

    void UpdateShardVisibility()
    {
        float hpPercent = (float)health.CurrentHealth / health.maxHealth;
        int targetShards = Mathf.CeilToInt(hpPercent * shards.Count);
        targetShards = Mathf.Max(1, targetShards);

        int activeCount = GetActiveShardCount();
        if (activeCount > targetShards)
        {
            int toDisable = activeCount - targetShards;
            for (int i = shards.Count - 1; i >= 0 && toDisable > 0; i--)
            {
                if (shards[i] != null && shards[i].activeSelf)
                {
                    shards[i].SetActive(false);
                    toDisable--;
                    activeCount--;
                }
            }
        }
    }

    // ==================== MOVIMENTO ====================

    void HandleRotation()
    {
        Vector3 directionToPlayer = (playerTransform.position - transform.position).normalized;
        directionToPlayer.y = 0;

        if (directionToPlayer != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    void HandleMovement()
    {
        float distToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        Vector3 direction = Vector3.zero;

        if (distToPlayer > attackRange)
        {
            direction = (playerTransform.position - transform.position).normalized;
        }
        else if (distToPlayer < attackRange * 0.5f)
        {
            direction = (transform.position - playerTransform.position).normalized;
        }

        float yCorrection = (anchoredY - transform.position.y) * 5f;

        if (direction != Vector3.zero)
        {
            direction.y = 0;
            Vector3 targetVelocity = direction * moveSpeed;
            rb.linearVelocity = new Vector3(targetVelocity.x, yCorrection, targetVelocity.z);
        }
        else
        {
            rb.linearVelocity = new Vector3(0, yCorrection, 0);
        }
    }

    // ==================== COMBATE ====================

    void HandleCombat()
    {
        float distToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        if (distToPlayer <= attackRange && attackTimer <= 0)
        {
            // Seleciona ataque aleatório baseado no HP
            float hpPercent = (float)health.CurrentHealth / health.maxHealth;

            if (hpPercent > 0.5f)
            {
                // HP alto: Swarm Attack ou Orbit Attack
                if (Random.value > 0.5f)
                    StartCoroutine(PerformSwarmAttack());
                else
                    StartCoroutine(PerformOrbitAttack());
            }
            else
            {
                // HP baixo: Barrage Attack ou Swarm Attack (mais agressivo)
                if (Random.value > 0.5f)
                    StartCoroutine(PerformBarrageAttack());
                else
                    StartCoroutine(PerformSwarmAttack());
            }
        }
    }

    /// <summary>
    /// Ataque original: todos os shards voam em direção ao player de uma vez
    /// </summary>
    IEnumerator PerformSwarmAttack()
    {
        isAttacking = true;
        attackTimer = attackCooldown;

        // Salva as posições locais iniciais dos shards para retorno
        Dictionary<GameObject, Vector3> startLocalPos = new Dictionary<GameObject, Vector3>();
        foreach (GameObject shard in shards)
        {
            if (shard != null && shard.activeSelf)
                startLocalPos[shard] = shard.transform.localPosition;
        }

        // Todos voam para o player
        float elapsed = 0;
        while (elapsed < attackDuration)
        {
            elapsed += Time.deltaTime;
            Vector3 targetPos = playerTransform.position + Vector3.up;

            foreach (GameObject shard in shards)
            {
                if (shard == null || !shard.activeSelf) continue;

                Vector3 dirToTarget = (targetPos - shard.transform.position).normalized;
                shard.transform.position += dirToTarget * moveSpeed * 2f * Time.deltaTime;
            }

            yield return null;
        }

        // Verifica dano no player
        CheckPlayerHit(combinedDamage);

        // Retorna shards às posições orbitais suavemente
        yield return StartCoroutine(ReturnShardsToOrbit(startLocalPos, 0.3f));

        isAttacking = false;
    }

    /// <summary>
    /// Barrage Attack: shards atacam o player um por um em sequência
    /// </summary>
    IEnumerator PerformBarrageAttack()
    {
        isAttacking = true;
        attackTimer = attackCooldown * 1.2f; // Cooldown um pouco maior

        float delayBetweenShards = 0.15f;
        int totalDamage = 0;

        foreach (GameObject shard in shards)
        {
            if (shard == null || !shard.activeSelf) continue;

            Vector3 startLocal = shard.transform.localPosition;
            Vector3 targetPos = playerTransform.position + Vector3.up;

            // Shard voa até o player
            float elapsed = 0;
            float duration = attackDuration * 0.6f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                if (shard == null) break;

                Vector3 dir = (targetPos - shard.transform.position).normalized;
                shard.transform.position += dir * moveSpeed * 3f * Time.deltaTime;
                yield return null;
            }

            // Verifica hit individual
            if (shard != null)
            {
                Collider[] hits = Physics.OverlapSphere(shard.transform.position, 0.5f);
                foreach (Collider hit in hits)
                {
                    if (hit.CompareTag("Player"))
                    {
                        totalDamage += damagePerShard;
                        break;
                    }
                }

                // Retorna este shard rapidamente
                float returnElapsed = 0;
                while (returnElapsed < 0.2f)
                {
                    returnElapsed += Time.deltaTime;
                    shard.transform.localPosition = Vector3.Lerp(shard.transform.localPosition, startLocal, returnElapsed / 0.2f);
                    yield return null;
                }
            }

            yield return new WaitForSeconds(delayBetweenShards);
        }

        // Aplica dano acumulado
        if (totalDamage > 0)
        {
            PlayerHealth playerHealth = playerTransform.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(totalDamage, gameObject);
            }
        }

        isAttacking = false;
    }

    /// <summary>
    /// Orbit Attack: shards expandem rapidamente a órbita como uma serra giratória
    /// </summary>
    IEnumerator PerformOrbitAttack()
    {
        isAttacking = true;
        attackTimer = attackCooldown;

        float originalRadius = orbitRadius;
        float expandedRadius = attackRange * 0.9f;
        float expandDuration = 0.3f;
        float holdDuration = 0.8f;
        float retractDuration = 0.4f;

        bool hitRegistered = false;

        // Expande a órbita rapidamente
        float elapsed = 0;
        while (elapsed < expandDuration)
        {
            elapsed += Time.deltaTime;
            orbitRadius = Mathf.Lerp(originalRadius, expandedRadius, elapsed / expandDuration);
            UpdateShardOrbit();
            yield return null;
        }

        // Mantém expandido — verifica hits durante o hold
        elapsed = 0;
        while (elapsed < holdDuration)
        {
            elapsed += Time.deltaTime;
            UpdateShardOrbit();

            // Checa colisão com player a cada frame
            if (!hitRegistered)
            {
                foreach (GameObject shard in shards)
                {
                    if (shard == null || !shard.activeSelf) continue;

                    Collider[] hits = Physics.OverlapSphere(shard.transform.position, 0.4f);
                    foreach (Collider hit in hits)
                    {
                        if (hit.CompareTag("Player"))
                        {
                            PlayerHealth playerHealth = hit.GetComponent<PlayerHealth>();
                            if (playerHealth != null)
                            {
                                playerHealth.TakeDamage(combinedDamage, gameObject);
                            }
                            hitRegistered = true;
                            break;
                        }
                    }
                    if (hitRegistered) break;
                }
            }

            yield return null;
        }

        // Retrai a órbita
        elapsed = 0;
        while (elapsed < retractDuration)
        {
            elapsed += Time.deltaTime;
            orbitRadius = Mathf.Lerp(expandedRadius, originalRadius, elapsed / retractDuration);
            UpdateShardOrbit();
            yield return null;
        }

        orbitRadius = originalRadius;
        isAttacking = false;
    }

    /// <summary>
    /// Retorna shards suavemente às posições orbitais
    /// </summary>
    IEnumerator ReturnShardsToOrbit(Dictionary<GameObject, Vector3> targetPositions, float duration)
    {
        float elapsed = 0;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            foreach (var kvp in targetPositions)
            {
                if (kvp.Key != null && kvp.Key.activeSelf)
                {
                    kvp.Key.transform.localPosition = Vector3.Lerp(
                        kvp.Key.transform.localPosition,
                        kvp.Value,
                        t
                    );
                }
            }

            yield return null;
        }
    }

    /// <summary>
    /// Verifica se algum shard atingiu o player e aplica dano
    /// </summary>
    void CheckPlayerHit(int damage)
    {
        foreach (GameObject shard in shards)
        {
            if (shard == null || !shard.activeSelf) continue;

            Collider[] hits = Physics.OverlapSphere(shard.transform.position, 0.5f);
            foreach (Collider hit in hits)
            {
                if (hit.CompareTag("Player"))
                {
                    PlayerHealth playerHealth = hit.GetComponent<PlayerHealth>();
                    if (playerHealth != null)
                    {
                        playerHealth.TakeDamage(damage, gameObject);
                    }
                    return; // Dano aplicado uma vez só
                }
            }
        }
    }

    // ==================== SPLIT ====================

    void Split()
    {
        hasSplit = true;
        Debug.Log("[SHARD SWARM] SPLIT! Duplicando enxame!");

        // VFX do split
        if (splitVFX != null)
        {
            Instantiate(splitVFX, transform.position, Quaternion.identity);
        }

        // Calcula HP dividido ANTES de criar o clone
        int halfHP = Mathf.Max(1, health.CurrentHealth / 2);

        // Cria clone ao lado do original
        Vector3 cloneOffset = transform.right * splitSpawnOffset;
        Vector3 clonePos = transform.position + cloneOffset;
        clonePos.y = anchoredY;
        GameObject clone = Instantiate(gameObject, clonePos, transform.rotation);

        // Reset do Rigidbody do clone
        Rigidbody cloneRb = clone.GetComponent<Rigidbody>();
        if (cloneRb != null)
        {
            cloneRb.linearVelocity = Vector3.zero;
            cloneRb.angularVelocity = Vector3.zero;
        }

        // Configura o clone
        ShardSwarm_AI cloneAI = clone.GetComponent<ShardSwarm_AI>();
        if (cloneAI != null)
        {
            cloneAI.anchoredY = anchoredY;
            cloneAI.canSplit = false;
            cloneAI.hasSplit = true;
            cloneAI.isClone = true;

            // Clone tem shards 20% menores (diferenciação visual)
            foreach (GameObject shard in cloneAI.shards)
            {
                if (shard != null)
                {
                    shard.transform.localScale *= 0.8f;
                }
            }

            // Reseta posições dos fragmentos do clone
            int count = cloneAI.shards.Count;
            for (int i = 0; i < count; i++)
            {
                if (cloneAI.shards[i] != null)
                {
                    float angle = (360f / Mathf.Max(1, count)) * i * Mathf.Deg2Rad;
                    cloneAI.shards[i].transform.localPosition = new Vector3(
                        Mathf.Cos(angle) * cloneAI.orbitRadius,
                        0f,
                        Mathf.Sin(angle) * cloneAI.orbitRadius
                    );
                }
            }
        }

        // Remove drops do clone (evita exploit)
        EnemyDrops cloneDrops = clone.GetComponent<EnemyDrops>();
        if (cloneDrops != null) Destroy(cloneDrops);

        // Divide HP: metade pro original, metade pro clone
        ShardSwarmHealth cloneHealth = clone.GetComponent<ShardSwarmHealth>();
        if (cloneHealth != null)
        {
            cloneHealth.SetHealth(halfHP);
        }
        health.SetHealth(halfHP);

        // Atualiza o lastKnownHP para não triggerar DetectDamage no próximo frame
        lastKnownHP = health.CurrentHealth;
    }

    // ==================== MORTE ====================

    void Die()
    {
        Debug.Log("[SHARD SWARM] Destruído!" + (isClone ? " (Clone)" : ""));

        Collider[] hits = Physics.OverlapSphere(transform.position, deathExplosionRadius);
        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                PlayerHealth playerHealth = hit.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(deathExplosionDamage, gameObject);
                }
            }
        }

        if (deathExplosionVFX != null)
        {
            Instantiate(deathExplosionVFX, transform.position, Quaternion.identity);
        }
    }

    // ==================== UTILS ====================

    int GetActiveShardCount()
    {
        int count = 0;
        foreach (GameObject shard in shards)
        {
            if (shard != null && shard.activeSelf) count++;
        }
        return count;
    }

    /// <summary>
    /// Pode ser buffado pelo CrystalTuner
    /// </summary>
    public void SetBuff(bool active)
    {
        if (active)
        {
            damagePerShard = Mathf.RoundToInt(damagePerShard * 1.5f);
            combinedDamage = Mathf.RoundToInt(combinedDamage * 1.5f);
        }
        else
        {
            damagePerShard = Mathf.RoundToInt(damagePerShard / 1.5f);
            combinedDamage = Mathf.RoundToInt(combinedDamage / 1.5f);
        }
    }

    // ==================== EDITOR ====================

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, activationDistance);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, orbitRadius);

        Gizmos.color = new Color(1, 0.5f, 0, 0.3f);
        Gizmos.DrawSphere(transform.position, deathExplosionRadius);
    }
}
