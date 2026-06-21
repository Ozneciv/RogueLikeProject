using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// IA do Shard Swarm - Enxame de fragmentos de cristal que ataca em grupo
/// Usa DummyHealth para sistema de vida integrado (barra de HP, texto de dano, etc.)
/// Fragmentos podem se separar ao tomar dano pesado e reagrupar depois
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(DummyHealth))]
public class ShardSwarm_AI : MonoBehaviour
{
    [Header("Referências")]
    private Transform playerTransform;
    private Rigidbody rb;
    private DummyHealth health;

    [Header("Fragmentos")]
    [Tooltip("Lista de GameObjects filhos que são os fragmentos")]
    public List<GameObject> shards = new List<GameObject>();
    [Tooltip("Raio da órbita dos fragmentos")]
    public float orbitRadius = 1.5f;
    [Tooltip("Velocidade de órbita")]
    public float orbitSpeed = 2f;

    [Header("Ativação")]
    public float activationDistance = 20f;
    private bool isActivated = false;

    [Header("Movimento")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 8f;
    public float attackRange = 5f;
    public float retreatDistance = 8f;

    [Header("Ataque")]
    public float attackCooldown = 2f;
    public int damagePerShard = 5;
    public int combinedDamage = 20;
    public float attackDuration = 0.5f;

    [Header("Split (Fases)")]
    [Tooltip("Número de fases: cada hit dobra as esferas ativas. 3 = 1→2→4→8 esferas, depois morre.")]
    public int maxSplitPhase = 3;

    [Header("Morte")]
    public float deathExplosionRadius = 3f;
    public int deathExplosionDamage = 15;
    public GameObject deathExplosionVFX;

    [Header("Estados")]
    private bool isAttacking = false;
    private float attackTimer = 0f;
    private int lastKnownHP;
    private int splitPhase = 0; // 0=1 esfera, 1=2, 2=4, 3=8

    // Tracking de fragmentos
    private List<Vector3> originalShardPositions = new List<Vector3>();

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        health = GetComponent<DummyHealth>();

        rb.useGravity = false;
        rb.freezeRotation = true;
        rb.constraints |= RigidbodyConstraints.FreezePositionY;

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

        // Inicializa fragmentos
        InitializeShards();

        // Guarda HP inicial
        lastKnownHP = health.CurrentHealth;
    }

    void InitializeShards()
    {
        // Se não tiver fragmentos definidos, procura nos filhos
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

        foreach (GameObject shard in shards)
            originalShardPositions.Add(shard.transform.localPosition);

        // Começa com apenas 1 esfera ativa
        for (int i = 0; i < shards.Count; i++)
            shards[i].SetActive(i == 0);

        Debug.Log("[SHARD SWARM] Inicializado com " + shards.Count + " fragmentos. Fase 0 (1 esfera ativa).");
    }

    void Update()
    {
        if (playerTransform == null) return;
        if (health.CurrentHealth <= 0) return;

        // Timer
        if (attackTimer > 0) attackTimer -= Time.deltaTime;

        // Ativação por proximidade
        if (!isActivated)
        {
            float dist = Vector3.Distance(transform.position, playerTransform.position);
            if (dist < activationDistance)
            {
                isActivated = true;
                // Reseta snapshot de HP na ativação para evitar falso split por diferença de inicialização
                lastKnownHP = health.CurrentHealth;
                Debug.Log("[SHARD SWARM] Ativado! Player detectado a " + dist.ToString("F1") + "m");

                EnemyIdentity id = GetComponent<EnemyIdentity>() ?? GetComponentInChildren<EnemyIdentity>() ?? GetComponentInParent<EnemyIdentity>();
                Debug.Log("[SHARD] EnemyIdentity: " + (id != null ? id.nomeInimigo : "NULL") + " | BestiarioManager: " + (BestiarioManager.instancia != null));
                if (id != null && BestiarioManager.instancia != null)
                    BestiarioManager.instancia.Registrar(id);
            }
            return;
        }

        // Detecta dano recebido (via DummyHealth) — só quando ativado
        DetectDamage();

        // Atualiza órbita dos fragmentos
        UpdateShardOrbit();

        // Não faz nada enquanto ataca
        if (isAttacking) return;

        HandleRotation();
        HandleCombat();
    }

    void DetectDamage()
    {
        int currentHP = health.CurrentHealth;
        if (currentHP < lastKnownHP)
        {
            Debug.Log("[SHARD SWARM] Hit recebido! Fase atual: " + splitPhase + "/" + maxSplitPhase);

            if (splitPhase < maxSplitPhase)
            {
                // Avança fase: dobra as esferas visíveis
                splitPhase++;
                AdvanceSplitPhase();

                // Restaura HP para que a próxima fase precise de outro hit
                health.SetHealth(health.maxHealth);
                lastKnownHP = health.maxHealth;
                return;
            }
            // Fase máxima atingida: deixa HP drenar normalmente → morte
        }
        lastKnownHP = currentHP;

        if (currentHP <= 0)
            Die();
    }

    void AdvanceSplitPhase()
    {
        // Ativa shards = 2^splitPhase (1, 2, 4, 8)
        int targetActive = 1 << splitPhase;
        for (int i = 0; i < shards.Count; i++)
            if (shards[i] != null)
                shards[i].SetActive(i < targetActive);

        Debug.Log("[SHARD SWARM] Fase " + splitPhase + " → " + targetActive + " esferas ativas.");
    }



    void FixedUpdate()
    {
        if (!isActivated || isAttacking) return;
        if (health.CurrentHealth <= 0) return;

        HandleMovement();
    }

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
                Mathf.Sin(time + index) * 0.3f, // Movimento vertical suave
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
            // Aproxima do player
            direction = (playerTransform.position - transform.position).normalized;
        }
        else if (distToPlayer < attackRange * 0.5f)
        {
            // Muito perto, recua um pouco
            direction = (transform.position - playerTransform.position).normalized;
        }

        if (direction != Vector3.zero)
        {
            direction.y = 0;
            Vector3 targetVelocity = direction * moveSpeed;
            rb.linearVelocity = new Vector3(targetVelocity.x, 0f, targetVelocity.z);
        }
        else
        {
            rb.linearVelocity = new Vector3(0f, 0f, 0f);
        }
    }

    void HandleCombat()
    {
        float distToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        if (distToPlayer <= attackRange && attackTimer <= 0)
        {
            StartCoroutine(PerformSwarmAttack());
        }
    }

    IEnumerator PerformSwarmAttack()
    {
        isAttacking = true;
        attackTimer = attackCooldown;

        int activeShards = GetActiveShardCount();
        Debug.Log("[SHARD SWARM] SWARM ATTACK! " + activeShards + " fragmentos atacando!");

        // Todos os fragmentos voam em direção ao player
        Vector3 targetPos = playerTransform.position + Vector3.up;

        // Move cada fragmento para o player
        float elapsed = 0;
        while (elapsed < attackDuration)
        {
            elapsed += Time.deltaTime;

            foreach (GameObject shard in shards)
            {
                if (shard == null || !shard.activeSelf) continue;

                Vector3 worldPos = shard.transform.position;
                Vector3 direction = (targetPos - worldPos).normalized;
                shard.transform.position += direction * moveSpeed * 2f * Time.deltaTime;
            }

            yield return null;
        }

        // Verifica hits
        bool hitPlayer = false;
        foreach (GameObject shard in shards)
        {
            if (shard == null || !shard.activeSelf) continue;

            Collider[] hits = Physics.OverlapSphere(shard.transform.position, 0.5f);
            foreach (Collider hit in hits)
            {
                if (hit.CompareTag("Player"))
                {
                    hitPlayer = true;
                    break;
                }
            }
            if (hitPlayer) break;
        }

        if (hitPlayer)
        {
            PlayerHealth playerHealth = playerTransform.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                int damage = combinedDamage;
                playerHealth.TakeDamage(damage, gameObject);
                Debug.Log("[SHARD SWARM] HIT! Dano causado: " + damage);
            }
        }

        // Retorna fragmentos às posições orbitais
        yield return new WaitForSeconds(0.3f);

        isAttacking = false;
    }




    void Die()
    {
        Debug.Log("[SHARD SWARM] Destruído! Explosão final!");

        // Explosão ao morrer
        Collider[] hits = Physics.OverlapSphere(transform.position, deathExplosionRadius);
        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                PlayerHealth playerHealth = hit.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(deathExplosionDamage, gameObject);
                    Debug.Log("[SHARD SWARM] Explosão acertou o player! Dano: " + deathExplosionDamage);
                }
            }
        }

        // VFX de explosão
        if (deathExplosionVFX != null)
        {
            Instantiate(deathExplosionVFX, transform.position, Quaternion.identity);
        }

        // DummyHealth cuida da destruição
    }

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
            Debug.Log("[SHARD SWARM] BUFFED! Dano +50%");
        }
        else
        {
            damagePerShard = Mathf.RoundToInt(damagePerShard / 1.5f);
            combinedDamage = Mathf.RoundToInt(combinedDamage / 1.5f);
        }
    }

    // Visualização no Editor
    void OnDrawGizmosSelected()
    {
        // Range de ativação
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, activationDistance);

        // Range de ataque
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Órbita dos fragmentos
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, orbitRadius);

        // Explosão de morte
        Gizmos.color = new Color(1, 0.5f, 0, 0.3f);
        Gizmos.DrawSphere(transform.position, deathExplosionRadius);
    }
}
