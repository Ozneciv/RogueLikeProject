using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawna FogZones em posicoes aleatorias dentro da arena do boss.
///
/// SETUP:
///   1. Adicione este script em qualquer GameObject da arena (ex: ArenaManager)
///   2. Arraste o prefab de FogZone no campo Fog Zone Prefab
///   3. Ajuste Arena Center (centro da arena) e Arena Radius (raio)
///   4. Configure Spawn Count e Activate On Phase
/// </summary>
public class FogZoneSpawner : MonoBehaviour
{
    public enum ActivationMode { BossPhase, OnStart, Manual }

    [Header("Prefab")]
    [Tooltip("Prefab da FogZone com BoxCollider, FogZone script e Particle System.")]
    public GameObject fogZonePrefab;

    [Header("Spawn")]
    [Tooltip("Quantas FogZones spawnar.")]
    [Range(1, 10)]
    public int spawnCount = 3;

    [Tooltip("Transform do centro da arena (arraste o Arena_Floor ou Arena_Center aqui).")]
    public Transform arenaCenter;

    [Tooltip("Raio da area de spawn dentro da arena.")]
    public float arenaRadius = 20f;

    [Tooltip("Distancia minima entre duas FogZones spawnadas.")]
    public float minDistanceBetween = 3f;

    [Header("Ativacao")]
    [Tooltip("BossPhase: ativa via BossEvents. OnStart: ativa ao habilitar o GameObject. Manual: só via Activate().")]
    public ActivationMode activationMode = ActivationMode.BossPhase;

    [Tooltip("0 = spawna ao iniciar a luta. 1/2/3 = spawna ao entrar nessa fase. Ignorado fora do modo BossPhase.")]
    [Range(0, 3)]
    public int activateOnPhase = 3;

    [Header("Debug")]
    public bool showDebugLog = true;

    // ── Estado interno ───────────────────────────────────────────────────────
    private readonly List<GameObject> spawnedZones = new List<GameObject>();

    // ── Eventos ──────────────────────────────────────────────────────────────
    void Start()
    {
        if (activationMode == ActivationMode.OnStart)
            Activate();
    }

    void OnEnable()
    {
        if (activationMode != ActivationMode.BossPhase) return;
        BossEvents.OnPhaseChanged     += OnPhaseChanged;
        BossEvents.OnBossFightStarted += OnFightStarted;
        BossEvents.OnBossDefeated     += OnBossDefeated;
    }

    void OnDisable()
    {
        if (activationMode != ActivationMode.BossPhase) return;
        BossEvents.OnPhaseChanged     -= OnPhaseChanged;
        BossEvents.OnBossFightStarted -= OnFightStarted;
        BossEvents.OnBossDefeated     -= OnBossDefeated;
    }

    // ── Handlers ─────────────────────────────────────────────────────────────
    private void OnFightStarted()
    {
        if (activateOnPhase == 0)
            SpawnZones();
    }

    private void OnPhaseChanged(int newPhase)
    {
        if (activateOnPhase > 0 && newPhase == activateOnPhase)
            SpawnZones();
    }

    private void OnBossDefeated()
    {
        Deactivate();
    }

    // ── API Pública ───────────────────────────────────────────────────────────
    public void Activate()
    {
        SpawnZones();
    }

    public void Deactivate()
    {
        foreach (GameObject zone in spawnedZones)
            if (zone != null) Destroy(zone);

        spawnedZones.Clear();
    }

    // ── Spawn ─────────────────────────────────────────────────────────────────
    private void SpawnZones()
    {
        if (spawnedZones.Count > 0) return; // ja spawnado
        if (fogZonePrefab == null)
        {
            Debug.LogWarning("[FogZoneSpawner] ⚠️ Fog Zone Prefab não atribuído!");
            return;
        }
        if (arenaCenter == null)
        {
            Debug.LogWarning("[FogZoneSpawner] ⚠️ Arena Center não atribuído! Arraste o Arena_Floor no campo.");
            return;
        }

        List<Vector3> usedPositions = new List<Vector3>();
        int spawned = 0;
        // Rotação aleatória no anel de setores a cada run para não repetir padrão
        float angleOffset = Random.Range(0f, 360f);

        for (int i = 0; i < spawnCount; i++)
        {
            Vector3 pos = TryGetRandomPosition(usedPositions, i, angleOffset);
            if (pos == Vector3.negativeInfinity)
            {
                Debug.LogWarning("[FogZoneSpawner] Não conseguiu encontrar posição válida para FogZone " + i);
                continue;
            }

            GameObject zone = Instantiate(fogZonePrefab, pos, Quaternion.identity);
            zone.name = "FogZone_Spawned_" + i;
            zone.GetComponent<FogZone>()?.Activate();
            spawnedZones.Add(zone);
            usedPositions.Add(pos);
            spawned++;
        }

        if (showDebugLog)
        {
            Debug.Log($"[FogZoneSpawner] {spawned} FogZones spawnadas | modo: {activationMode} | fase: {activateOnPhase} | Centro: {arenaCenter?.position} | Raio: {arenaRadius}");
            foreach (GameObject z in spawnedZones)
                if (z != null) Debug.Log($"[FogZoneSpawner] -> {z.name} em {z.transform.position}");
        }
    }

    // Divide a arena em setores angulares iguais para evitar clustering
    private Vector3 TryGetRandomPosition(List<Vector3> existing, int sectorIndex, float angleOffset)
    {
        const int maxAttempts = 20;
        Vector3 center = arenaCenter != null ? arenaCenter.position : Vector3.zero;

        float sectorSize = 360f / spawnCount;
        float angleMin   = angleOffset + sectorIndex * sectorSize;
        float angleMax   = angleMin + sectorSize;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            float angle     = Random.Range(angleMin, angleMax) * Mathf.Deg2Rad;
            // mínimo de 60% do raio para evitar spawn perto do centro
            float radius    = Random.Range(arenaRadius * 0.6f, arenaRadius * 0.95f);
            Vector3 candidate = center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);

            bool tooClose = false;
            foreach (Vector3 p in existing)
            {
                if (Vector3.Distance(candidate, p) < minDistanceBetween)
                {
                    tooClose = true;
                    break;
                }
            }

            if (!tooClose) return candidate;
        }

        return Vector3.negativeInfinity;
    }

    // ── Gizmos (editor visual) ────────────────────────────────────────────────
#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Vector3 center = arenaCenter != null ? arenaCenter.position : transform.position;
        Gizmos.color = new Color(0.2f, 0.8f, 0.2f, 0.3f);
        Gizmos.DrawSphere(center, arenaRadius);
        Gizmos.color = new Color(0.2f, 0.8f, 0.2f, 1f);
        Gizmos.DrawWireSphere(center, arenaRadius);
    }
#endif
}
