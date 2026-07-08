using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// IA do Shard Swarm.
/// Geração 0 (bola grande): persegue o player, orbita e ataca.
///   → Ao morrer: spawna N bolas menores (geração 1).
/// Geração 1 (bolas pequenas): mesmo comportamento, sem split ao morrer.
///
/// DEPENDÊNCIAS: DummyHealth, Rigidbody, tag "Player"
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(DummyHealth))]
public class ShardSwarm_AI : MonoBehaviour
{
    // ─── Split ao Morrer ─────────────────────────────────────────

    [Header("Split ao Morrer")]
    [Tooltip("Arraste o próprio prefab aqui (self-reference). Obrigatório.")]
    public GameObject shardSwarmPrefab;
    [Tooltip("0 = bola grande (splita ao morrer) | 1 = bola pequena (morre sem split). Deixe 0 na cena.")]
    public int generation = 0;
    [Tooltip("Quantas bolas menores spawnam ao morrer")]
    public int splitCount = 4;
    [Tooltip("Escala das bolas filhas em relação à mãe")]
    public float childScale = 0.45f;
    [Tooltip("Raio de dispersão das bolas filhas ao spawnar")]
    public float splitSpawnRadius = 1.5f;

    // ─── Movimento ───────────────────────────────────────────────

    [Header("Movimento")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 8f;
    [Tooltip("Distância para iniciar ataque")]
    public float attackRange = 5f;
    [Tooltip("Distância para ativar o inimigo")]
    public float activationDistance = 20f;

    // ─── Órbita ──────────────────────────────────────────────────

    [Header("Órbita")]
    [Tooltip("Raio de órbita dos filhos visuais ao redor do centro")]
    public float orbitRadius = 0.8f;
    [Tooltip("Velocidade de órbita")]
    public float orbitSpeed = 2f;

    // ─── Ataque ──────────────────────────────────────────────────

    [Header("Ataque")]
    public float attackCooldown = 2f;
    public int contactDamage = 20;
    public float attackDuration = 0.5f;

    // ─── Morte ───────────────────────────────────────────────────

    [Header("Morte")]
    public float deathExplosionRadius = 3f;
    public int deathExplosionDamage = 15;
    public GameObject deathExplosionVFX;

    // ─── Privado ─────────────────────────────────────────────────

    private Transform playerTransform;
    private Rigidbody rb;
    private DummyHealth health;

    private bool isActivated = false;
    private bool isAttacking  = false;
    private float attackTimer = 0f;

    private List<Transform> orbitChildren = new List<Transform>();

    // ─────────────────────────────────────────────────────────────
    // Unity Callbacks
    // ─────────────────────────────────────────────────────────────

    void Start()
    {
        rb     = GetComponent<Rigidbody>();
        health = GetComponent<DummyHealth>();

        rb.useGravity     = false;
        rb.freezeRotation = true;
        rb.constraints   |= RigidbodyConstraints.FreezePositionY;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            playerTransform = player.transform;
        else
            Debug.LogError("[SHARD SWARM] Player não encontrado! Verifique a tag 'Player'.");

        // orbitChildren fica vazio intencionalmente → UpdateOrbit não faz nada
        // Os filhos ficam nas posições padrão do prefab

        // Registra split como callback de morte no DummyHealth
        health.onDeathOverride = OnDeath;
    }

    void Update()
    {
        if (playerTransform == null) return;

        attackTimer -= Time.deltaTime;

        if (!isActivated)
        {
            float dist = Vector3.Distance(transform.position, playerTransform.position);
            if (dist < activationDistance)
            {
                isActivated = true;
                Debug.Log($"[SHARD SWARM] Gen{generation} ativado.");

                EnemyIdentity id = GetComponent<EnemyIdentity>()
                                ?? GetComponentInChildren<EnemyIdentity>()
                                ?? GetComponentInParent<EnemyIdentity>();
                if (id != null && BestiarioManager.instancia != null)
                    BestiarioManager.instancia.Registrar(id);
            }
            return;
        }

        UpdateOrbit();

        if (!isAttacking)
        {
            RotateTowardPlayer();
            TryCombat();
        }
    }

    void FixedUpdate()
    {
        if (!isActivated || isAttacking) return;
        MoveTowardPlayer();
    }

    // ─────────────────────────────────────────────────────────────
    // Movimento
    // ─────────────────────────────────────────────────────────────

    void MoveTowardPlayer()
    {
        float dist = HorizontalDistance();
        Vector3 dir = HorizontalDirection();

        Vector3 velocity = Vector3.zero;
        if (dist > attackRange)
            velocity = dir * moveSpeed;
        else if (dist < attackRange * 0.5f)
            velocity = -dir * moveSpeed;

        rb.linearVelocity = new Vector3(velocity.x, 0f, velocity.z);
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

    void UpdateOrbit()
    {
        float t = Time.time * orbitSpeed;
        int count = orbitChildren.Count;
        if (count == 0) return;

        for (int i = 0; i < count; i++)
        {
            if (orbitChildren[i] == null) continue;
            float angle = (360f / count) * i + t * 60f;
            float rad   = angle * Mathf.Deg2Rad;
            orbitChildren[i].localPosition = Vector3.Lerp(
                orbitChildren[i].localPosition,
                new Vector3(Mathf.Cos(rad) * orbitRadius,
                            Mathf.Sin(t + i) * 0.25f,
                            Mathf.Sin(rad) * orbitRadius),
                Time.deltaTime * 6f);
        }
    }

    // ─────────────────────────────────────────────────────────────
    // Combate
    // ─────────────────────────────────────────────────────────────

    void TryCombat()
    {
        if (attackTimer > 0f) return;
        if (HorizontalDistance() <= attackRange)
            StartCoroutine(DoAttack());
    }

    IEnumerator DoAttack()
    {
        isAttacking = true;
        attackTimer = attackCooldown;

        Vector3 targetPos = playerTransform.position + Vector3.up * 0.5f;
        float elapsed = 0f;

        while (elapsed < attackDuration)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector3.MoveTowards(
                transform.position, targetPos, moveSpeed * 2f * Time.deltaTime);
            yield return null;
        }

        if (Vector3.Distance(transform.position, playerTransform.position) <= 1.5f)
        {
            PlayerHealth ph = playerTransform.GetComponent<PlayerHealth>();
            ph?.TakeDamage(contactDamage, gameObject);
            Debug.Log($"[SHARD SWARM] Gen{generation} acertou player! Dano={contactDamage}");
        }

        isAttacking = false;
    }

    // ─────────────────────────────────────────────────────────────
    // Morte e Split
    // ─────────────────────────────────────────────────────────────

    private void OnDeath()
    {
        if (deathExplosionVFX != null)
            Instantiate(deathExplosionVFX, transform.position, Quaternion.identity);

        Collider[] cols = Physics.OverlapSphere(transform.position, deathExplosionRadius);
        foreach (Collider col in cols)
        {
            if (!col.CompareTag("Player")) continue;
            col.GetComponent<PlayerHealth>()?.TakeDamage(deathExplosionDamage, gameObject);
        }

        // Só a geração 0 spawna filhos
        if (generation == 0 && shardSwarmPrefab != null)
        {
            for (int i = 0; i < splitCount; i++)
            {
                float angle = (360f / splitCount) * i * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(
                    Mathf.Cos(angle) * splitSpawnRadius,
                    0f,
                    Mathf.Sin(angle) * splitSpawnRadius);

                GameObject child = Instantiate(shardSwarmPrefab,
                                               transform.position + offset,
                                               Quaternion.identity);

                child.transform.localScale = transform.localScale * childScale;

                ShardSwarm_AI childAI = child.GetComponent<ShardSwarm_AI>();
                if (childAI != null)
                {
                    childAI.generation       = 1;              // não splita ao morrer
                    childAI.shardSwarmPrefab = shardSwarmPrefab;
                    childAI.childScale       = childScale;
                    childAI.splitSpawnRadius = splitSpawnRadius;
                    childAI.moveSpeed        = moveSpeed * 1.3f; // filhos mais rápidos
                    childAI.contactDamage    = Mathf.Max(5, contactDamage / 2);
                }

                DummyHealth childHealth = child.GetComponent<DummyHealth>();
                if (childHealth != null)
                    childHealth.maxHealth = Mathf.Max(1, health.maxHealth / 3);
            }
            Debug.Log($"[SHARD SWARM] Gen0 morreu → {splitCount} bolas pequenas spawnadas.");
        }
        else
        {
            Debug.Log($"[SHARD SWARM] Gen{generation} morreu. Fim.");
        }

        Destroy(gameObject);
    }

    // ─────────────────────────────────────────────────────────────
    // Buff (Crystal Tuner)
    // ─────────────────────────────────────────────────────────────

    public void SetBuff(bool active)
    {
        contactDamage = active
            ? Mathf.RoundToInt(contactDamage * 1.5f)
            : Mathf.RoundToInt(contactDamage / 1.5f);
    }

    // ─────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────

    float HorizontalDistance()
    {
        if (playerTransform == null) return 999f;
        Vector3 a = transform.position;       a.y = 0f;
        Vector3 b = playerTransform.position; b.y = 0f;
        return Vector3.Distance(a, b);
    }

    Vector3 HorizontalDirection()
    {
        if (playerTransform == null) return Vector3.zero;
        Vector3 dir = playerTransform.position - transform.position;
        dir.y = 0f;
        return dir.normalized;
    }

    // ─────────────────────────────────────────────────────────────
    // Gizmos
    // ─────────────────────────────────────────────────────────────

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, activationDistance);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, orbitRadius);

        Gizmos.color = new Color(1f, 0.5f, 0f, 0.35f);
        Gizmos.DrawSphere(transform.position, deathExplosionRadius);

        Gizmos.color = new Color(0.5f, 1f, 0.5f, 0.35f);
        Gizmos.DrawSphere(transform.position, splitSpawnRadius);
    }
}
