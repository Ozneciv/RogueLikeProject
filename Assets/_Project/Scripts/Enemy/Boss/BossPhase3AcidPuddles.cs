using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Fase 3 do Boss CromÃ¡tico â€” Sistema de PoÃ§as Ãcidas.
///
/// SETUP NA CENA:
///   1. Coloque este componente em qualquer GameObject da arena (ex: ArenaManager, FogZoneSpawner)
///   2. Arraste o prefab da poÃ§a no campo "Acid Puddle Prefab"
///   3. Configure Arena Center e Arena Radius (igual ao FogZoneSpawner)
///   4. Ativa automaticamente na Fase 3 via BossEvents
/// </summary>
public class BossPhase3AcidPuddles : MonoBehaviour
{
    [Header("Prefab")]
    public GameObject acidPuddlePrefab;

    [Header("Spawn")]
    [Tooltip("Intervalo mÃ­nimo entre ciclos de spawn.")]
    public float spawnIntervalMin = 3f;
    [Tooltip("Intervalo mÃ¡ximo entre ciclos de spawn.")]
    public float spawnIntervalMax = 6f;
    [Tooltip("Quantas poÃ§as aleatÃ³rias spawnar por ciclo (alÃ©m da que cai sob o player).")]
    [Range(0, 5)]
    public int randomPuddlesPerCycle = 1;

    [Header("Arena")]
    [Tooltip("Centro da arena â€” igual ao usado no FogZoneSpawner.")]
    public Transform arenaCenter;
    [Tooltip("Raio da Ã¡rea de spawn aleatÃ³rio.")]
    public float arenaRadius = 15f;
    [Tooltip("DistÃ¢ncia mÃ­nima entre poÃ§as spawnadas no mesmo ciclo.")]
    public float minDistanceBetween = 3f;

    [Header("Limite")]
    [Range(1, 20)]
    public int maxSimultaneousPuddles = 6;

    [Header("Debug")]
    public bool showDebugLog = true;

    // â”€â”€ Estado interno â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    private Transform playerTransform;
    private BossController bossController;
    private Coroutine spawnCoroutine;
    private bool phase3Active = false;

    // â”€â”€ Editor: auto-atribui prefab para contornar override de prefab aninhado â”€â”€
#if UNITY_EDITOR
    void OnValidate()
    {
        if (acidPuddlePrefab != null) return;
        string[] guids = UnityEditor.AssetDatabase.FindAssets("Acid Puddle VFX t:Prefab");
        if (guids.Length > 0)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
            acidPuddlePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
            UnityEditor.EditorUtility.SetDirty(this);
        }
    }
#endif

    void OnEnable()
    {
        BossEvents.OnPhaseChanged += OnPhaseChanged;
        BossEvents.OnBossDefeated += OnBossDefeated;
    }

    void OnDisable()
    {
        BossEvents.OnPhaseChanged -= OnPhaseChanged;
        BossEvents.OnBossDefeated -= OnBossDefeated;
        StopSpawn();
    }

    private void OnPhaseChanged(int newPhase)
    {
        if (newPhase == 3) Activate();
    }

    private void OnBossDefeated()
    {
        StopSpawn();
        AcidPuddle.ResetStaticCounters();
    }

    private void Activate()
    {
        if (phase3Active) return;
        phase3Active = true;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) playerTransform = player.transform;

        bossController = FindFirstObjectByType<BossController>();

        if (acidPuddlePrefab == null)
            Debug.LogError("[Phase3AcidPuddles] âŒ Acid Puddle Prefab nÃ£o atribuÃ­do!");

        spawnCoroutine = StartCoroutine(SpawnRoutine());

        if (showDebugLog) Debug.Log("[Phase3AcidPuddles] ðŸ§ª Ativado na Fase 3.");
    }

    private void StopSpawn()
    {
        phase3Active = false;
        if (spawnCoroutine != null) { StopCoroutine(spawnCoroutine); spawnCoroutine = null; }
    }

    private IEnumerator SpawnRoutine()
    {
        yield return new WaitForSeconds(spawnIntervalMin);

        while (phase3Active)
        {
            if (!IsBossPaused())
                SpawnCycle();

            yield return new WaitForSeconds(Random.Range(spawnIntervalMin, spawnIntervalMax));
        }
    }

    // Spawna 1 sob o player + randomPuddlesPerCycle aleatÃ³rias na arena
    private void SpawnCycle()
    {
        if (acidPuddlePrefab == null) return;

        List<Vector3> used = new List<Vector3>();

        // 1 sob o player
        Vector3 playerPos = playerTransform != null ? playerTransform.position : transform.position;
        TrySpawn(playerPos, used);

        // N aleatÃ³rias espalhadas na arena
        Vector3 center = arenaCenter != null ? arenaCenter.position : transform.position;
        float angleOffset = Random.Range(0f, 360f);

        for (int i = 0; i < randomPuddlesPerCycle; i++)
        {
            float angle  = (angleOffset + i * (360f / (randomPuddlesPerCycle + 1))) * Mathf.Deg2Rad;
            float radius = Random.Range(arenaRadius * 0.5f, arenaRadius * 0.95f);
            Vector3 pos  = center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);

            TrySpawn(pos, used);
        }
    }

    private void TrySpawn(Vector3 position, List<Vector3> used)
    {
        if (AcidPuddle.ActiveCount >= maxSimultaneousPuddles)
        {
            if (showDebugLog) Debug.Log($"[Phase3AcidPuddles] Cap atingido ({maxSimultaneousPuddles}). Spawn ignorado.");
            return;
        }

        foreach (Vector3 p in used)
            if (Vector3.Distance(position, p) < minDistanceBetween) return;

        Instantiate(acidPuddlePrefab, position, Quaternion.identity);
        used.Add(position);

        if (showDebugLog) Debug.Log($"[Phase3AcidPuddles] ðŸŸ¢ PoÃ§a em {position} | Ativas: {AcidPuddle.ActiveCount + 1}/{maxSimultaneousPuddles}");
    }

    private bool IsBossPaused() =>
        bossController != null && (bossController.IsStunned || bossController.IsDead);

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Vector3 center = arenaCenter != null ? arenaCenter.position : transform.position;
        Gizmos.color = new Color(0.2f, 0.8f, 0.2f, 0.2f);
        Gizmos.DrawSphere(center, arenaRadius);
        Gizmos.color = new Color(0.2f, 0.8f, 0.2f, 1f);
        Gizmos.DrawWireSphere(center, arenaRadius);
    }
#endif
}

/// <summary>
/// Fase 3 do Boss CromÃ¡tico â€” Moveset da Floresta TÃ³xica.
///
/// RESPONSABILIDADES:
///   â€¢ Spawn aleatÃ³rio de poÃ§as Ã¡cidas sob o jogador em intervalos randomizados
///   â€¢ Acid Spit: o boss cospe um projÃ©til Ã¡cido pela boca em direÃ§Ã£o ao jogador
///   â€¢ Controla o limite mÃ¡ximo de poÃ§as simultÃ¢neas na arena
///   â€¢ Usa AttackCooldownMultiplier do BossPhase3Modifiers automaticamente (se presente)
///
/// SETUP NO UNITY:
///   1. Adicione este componente no mesmo GameObject do BossController
///   2. Crie um Prefab de poÃ§a: GameObject vazio â†’ CapsuleCollider (isTrigger=true) + AcidPuddle.cs
///   3. Arraste o prefab no campo "acidPuddlePrefab"
///   4. (Cuspida) Reutilize o projÃ©til jÃ¡ existente: crie um Prefab com Rigidbody + Collider (isTrigger)
///      + CrystalSpikeProjectile.cs (dÃª a ele um visual/material Ã¡cido) e arraste em "acidSpitProjectilePrefab"
///   5. BossController Ã© encontrado automaticamente (ou arraste manualmente)
///
/// DEPENDÃŠNCIAS:
///   â€¢ BossPhase3Modifiers (opcional, no mesmo GameObject) â€” aplica -50% ao cooldown dos ataques
///   â€¢ AcidPuddle.cs â€” script do prefab da poÃ§a
///   â€¢ CrystalSpikeProjectile.cs â€” projÃ©til jÃ¡ existente, reutilizado pela cuspida de Ã¡cido
/// </summary>
