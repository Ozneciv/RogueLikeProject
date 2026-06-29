using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Controlador de Sala — fusão do sistema de ondas/portas com o sistema de
/// pontos econômicos do GDD (Economy.pdf §1.2).
///
/// SISTEMA DE PONTOS:
///   Budget total da sala: P(n) = 10 + 0,9 × n   (via RunManager)
///   O budget é dividido igualmente entre as ondas sorteadas.
///   Cada onda usa sua cota de pontos para compor um grupo de inimigos,
///   respeitando as regras de classe definidas no GDD.
///
/// Regras de spawn por classe:
///   Mob Menor  → 1 ponto  | máx. 50% dos pontos totais
///   Atirador   → 2 pontos | sempre em pares (4 pts)
///   Tanque     → 4 pontos | máx. 4 por sala (total, não por onda)
///   Elite      → 10 pontos| máx. 1 por sala (total, não por onda)
///
/// FLUXO:
///   1. LevelGenerator chama Initialize(roomIndex)
///   2. Player entra → portas trancam → ondas começam
///   3. Cada onda spawna inimigos pelo sistema de pontos
///   4. Ao limpar todas as ondas, portas destrancam
///
/// SETUP NO UNITY:
///   • Adicione os prefabs nas listas de cada classe
///   • Configure spawnArea (BoxCollider)
///   • Arraste as portas em doors[]
///   • O LevelGenerator preenche roomIndex automaticamente
/// </summary>
public class RoomController : MonoBehaviour
{
    // =====================================================
    // CONFIGURAÇÃO DA SALA
    // =====================================================

    [Header("Sala")]
    public bool isSafeRoom = false;
    [HideInInspector] public bool doorsAreLocked = false;

    [Header("Índice (definido pelo LevelGenerator)")]
    [Tooltip("Número desta sala na sequência da Run (1–32). Não editar manualmente.")]
    public int roomIndex = 1;

    // =====================================================
    // POOLS DE INIMIGOS POR CLASSE (GDD §1.2)
    // =====================================================

    [Header("Mob Menor — 1 ponto | máx. 50% do budget da onda")]
    [Tooltip("Todos os prefabs que podem aparecer como Mob Menor neste bioma.")]
    public List<GameObject> mobMenorPrefabs = new List<GameObject>();

    [Header("Atirador — 2 pontos | sempre em pares (4 pts/par)")]
    [Tooltip("Todos os prefabs que podem aparecer como Atirador neste bioma.")]
    public List<GameObject> atiradorPrefabs = new List<GameObject>();

    [Header("Tanque — 4 pontos | máx. 4 por sala (total)")]
    [Tooltip("Todos os prefabs que podem aparecer como Tanque neste bioma.")]
    public List<GameObject> tanquePrefabs = new List<GameObject>();

    [Header("Elite — 10 pontos | máx. 1 por sala (total)")]
    [Tooltip("Todos os prefabs que podem aparecer como Elite neste bioma.")]
    public List<GameObject> elitePrefabs = new List<GameObject>();

    // =====================================================
    // CONFIGURAÇÃO DE ONDAS
    // =====================================================

    [Header("Ondas")]
    [Range(1, 5)] public int minWaves = 1;
    [Range(1, 5)] public int maxWaves = 3;
    [Tooltip("Intervalo em segundos entre o fim de uma onda e o início da próxima.")]
    public float timeBetweenWaves = 2f;
    [Tooltip("Número mínimo de inimigos garantidos por onda (Mob Menor preenchem o restante).")]
    [Range(1, 20)] public int minEnemiesPerWave = 6;
    [Tooltip("Fracão máxima do budget que pode ser gasta em Mob Menor. 0.7 = 70%.")]
    [Range(0.1f, 1f)] public float mobCapFraction = 0.7f;

    // =====================================================
    // CONFIGURAÇÃO DE SPAWN
    // =====================================================

    [Header("Spawn")]
    [Tooltip("Uma ou mais áreas onde inimigos podem aparecer.\nUse múltiplas para salas irregulares (Y-Shape, Ampulheta, etc.).")]
    public List<BoxCollider> spawnAreas = new List<BoxCollider>();
    [Tooltip("Prefab do indicador visual (círculo/marcador) antes do spawn.")]
    public GameObject spawnIndicatorPrefab;
    [Tooltip("Tempo em segundos entre o indicador aparecer e o inimigo spawnar.")]
    public float spawnDelay = 1.5f;
    [Tooltip("Altura acima do CHÃO da SpawnArea onde inimigos aparecem. 0.1 = rente ao chão.")]
    public float spawnHeightOffset = 0.1f;

    // =====================================================
    // PORTAS E REFERÊNCIAS
    // =====================================================

    [Header("Portas")]
    [Tooltip("GameObjects das portas que trancam durante o combate.")]
    public GameObject[] doors;

    [Header("Debug")]
    public bool showSpawnLog = true;

    // =====================================================
    // ESTADO INTERNO
    // =====================================================

    // Restrições globais (não por onda) — reiniciam apenas ao entrar na sala
    private int eliteSpawnedTotal  = 0;
    private int tanqueSpawnedTotal = 0;

    private int   totalWavesThisRoom = 1;
    private int   currentWave        = 0;
    private int   enemiesPendingSpawn = 0;
    private List<GameObject> activeEnemies = new List<GameObject>();
    private bool  hasTriggered  = false;
    private bool  combatActive  = false;

    private enum EnemyClass { Elite, Tanque, Atirador, MobMenor }

    // =====================================================
    // API PÚBLICA
    // =====================================================

    /// <summary>
    /// Chamado pelo LevelGenerator ao instanciar a sala.
    /// </summary>
    public void Initialize(int index)
    {
        roomIndex = Mathf.Max(1, index);
    }

    // =====================================================
    // UNITY LIFECYCLE
    // =====================================================

    void Start()
    {
        if (isSafeRoom) return;
        totalWavesThisRoom = Random.Range(minWaves, maxWaves + 1);
        UnlockDoors();
    }

    void Update()
    {
        if (hasTriggered && combatActive)
            CheckWaveStatus();
    }

    // Caso o BoxCollider (trigger) esteja no MESMO GameObject que o RoomController
    private void OnTriggerEnter(Collider other)
    {
        OnPlayerEnteredRoom(other);
    }

    /// <summary>
    /// Chamado diretamente pelo OnTriggerEnter (quando o collider está no mesmo GO)
    /// OU pelo RoomTriggerProxy (quando o collider está num filho).
    /// </summary>
    public void OnPlayerEnteredRoom(Collider other)
    {
        if (other.CompareTag("Player") && !isSafeRoom && !hasTriggered)
        {
            hasTriggered = true;

            // Atualiza o RunManager para esta sala
            if (RunManager.instance != null)
                RunManager.instance.SetCurrentRoom(roomIndex);

            StartCoroutine(StartCombatEncounter());
        }
    }

    // =====================================================
    // COMBATE E ONDAS
    // =====================================================

    IEnumerator StartCombatEncounter()
    {
        // Aguarda o player entrar completamente na sala antes de fechar as portas.
        // Sem este delay, a barreira fecha enquanto o player ainda está no vão da entrada.
        yield return new WaitForSeconds(0.8f);

        LockDoors();
        if (showSpawnLog)
            Debug.Log($"[ROOM {roomIndex}] Portas trancadas. {totalWavesThisRoom} ondas programadas.");
        yield return new WaitForSeconds(1f);

        SpawnNextWave();
        combatActive = true;
    }

    void SpawnNextWave()
    {
        currentWave++;

        // O budget total da sala é dividido igualmente entre as ondas.
        // Isso garante curva de dificuldade uniforme independente do número de ondas.
        int totalBudget = RunManager.instance != null
            ? RunManager.instance.GetSpawnBudget(roomIndex)
            : Mathf.RoundToInt(10 + 0.9f * roomIndex);
        int waveBudget = Mathf.Max(4, Mathf.RoundToInt(totalBudget / (float)totalWavesThisRoom));

        if (showSpawnLog)
            Debug.Log($"[ROOM {roomIndex}] ONDA {currentWave}/{totalWavesThisRoom} | Budget: {waveBudget} pts | Mín. inimigos: {minEnemiesPerWave}");

        // Gera a lista de inimigos desta onda pelo algoritmo de pontos
        List<GameObject> waveEnemies = BuildWaveFromPoints(waveBudget);

        // Flag de pendentes para o CheckWaveStatus
        enemiesPendingSpawn += waveEnemies.Count;

        // Spawna cada inimigo com delay e indicador visual
        foreach (GameObject prefab in waveEnemies)
        {
            StartCoroutine(SpawnEnemyWithDelay(prefab));
        }
    }

    /// <summary>
    /// Monta a lista de inimigos de uma onda usando o sistema de pontos do GDD.
    /// Respeita os limites GLOBAIS de Elite (1/sala) e Tanque (4/sala).
    /// </summary>
    List<GameObject> BuildWaveFromPoints(int waveBudget)
    {
        int maxMobPoints = Mathf.FloorToInt(waveBudget * mobCapFraction);
        int remainingPts = waveBudget;
        int mobPointsUsed = 0;

        List<GameObject> result = new List<GameObject>();

        bool addedSomething = true;
        while (remainingPts > 0 && addedSomething)
        {
            addedSomething = false;
            List<EnemyClass> validOptions = new List<EnemyClass>();

            // Elite: máx. 1 POR SALA (limite global, ratio 2:1 com drop base 20)
            if (elitePrefabs.Count   > 0 && remainingPts >= 10 && eliteSpawnedTotal < 1) validOptions.Add(EnemyClass.Elite);
            // Tanque: máx. 4 POR SALA (limite global)
            if (tanquePrefabs.Count  > 0 && remainingPts >=  4 && tanqueSpawnedTotal < 4) validOptions.Add(EnemyClass.Tanque);
            // Atirador: par (4 pts)
            if (atiradorPrefabs.Count > 0 && remainingPts >= 4)                           validOptions.Add(EnemyClass.Atirador);
            // Mob Menor: máx. 50% do budget da onda
            if (mobMenorPrefabs.Count > 0 && remainingPts >= 1 && mobPointsUsed < maxMobPoints) validOptions.Add(EnemyClass.MobMenor);

            if (validOptions.Count == 0) break;

            addedSomething = true;
            EnemyClass chosen = validOptions[Random.Range(0, validOptions.Count)];

            switch (chosen)
            {
                case EnemyClass.Elite:
                    result.Add(GetRandom(elitePrefabs));
                    eliteSpawnedTotal++;
                    remainingPts -= 10;
                    break;

                case EnemyClass.Tanque:
                    result.Add(GetRandom(tanquePrefabs));
                    tanqueSpawnedTotal++;
                    remainingPts -= 4;
                    break;

                case EnemyClass.Atirador:
                    // Par de atiradores (podem ser inimigos diferentes)
                    result.Add(GetRandom(atiradorPrefabs));
                    result.Add(GetRandom(atiradorPrefabs));
                    remainingPts -= 4;
                    break;

                case EnemyClass.MobMenor:
                    result.Add(GetRandom(mobMenorPrefabs));
                    mobPointsUsed++;
                    remainingPts -= 1;
                    break;
            }
        }

        // Garante mínimo de inimigos por onda preenchendo com Mob Menor
        // (sem custo adicional de pontos — apenas quantidade mínima de jogabilidade)
        if (mobMenorPrefabs.Count > 0)
        {
            while (result.Count < minEnemiesPerWave)
                result.Add(GetRandom(mobMenorPrefabs));
        }

        if (showSpawnLog)
            Debug.Log($"[ROOM {roomIndex}] Onda {currentWave}: {result.Count} inimigos gerados (Elite:{eliteSpawnedTotal} Tanque:{tanqueSpawnedTotal})");

        return result;
    }

    IEnumerator SpawnEnemyWithDelay(GameObject prefab)
    {
        if (prefab == null || spawnAreas == null || spawnAreas.Count == 0)
        {
            enemiesPendingSpawn--;
            yield break;
        }

        // Escolhe uma área aleatória da lista para este inimigo
        BoxCollider chosenArea = spawnAreas[Random.Range(0, spawnAreas.Count)];
        Vector3 spawnPos = GetRandomPositionInArea(chosenArea);

        // Spawn indicator visual
        GameObject indicator = null;
        if (spawnIndicatorPrefab != null)
        {
            Vector3 indicatorPos = new Vector3(spawnPos.x, chosenArea.transform.position.y + 0.05f, spawnPos.z);
            indicator = Instantiate(spawnIndicatorPrefab, indicatorPos, Quaternion.identity);
        }

        yield return new WaitForSeconds(spawnDelay);

        if (indicator != null) Destroy(indicator);

        GameObject newEnemy = Instantiate(prefab, spawnPos, prefab.transform.rotation);

        // Garante que o inimigo não caia do mapa (contenção pelo NavMesh global)
        if (newEnemy.GetComponent<NavMeshBoundaryConstraint>() == null)
            newEnemy.AddComponent<NavMeshBoundaryConstraint>();

        // Integração especial: passa os limites de área para a MagicStone (usa a primeira área como referência)
        MagicStone_AI magicStone = newEnemy.GetComponent<MagicStone_AI>();
        if (magicStone != null)
            magicStone.SetRoomBounds(spawnAreas[0]);

        activeEnemies.Add(newEnemy);
        enemiesPendingSpawn--;
    }

    void CheckWaveStatus()
    {
        // Remove referências de inimigos já destruídos
        activeEnemies.RemoveAll(e => e == null);

        if (activeEnemies.Count == 0 && enemiesPendingSpawn == 0)
        {
            if (currentWave < totalWavesThisRoom)
            {
                combatActive = false;
                if (showSpawnLog) Debug.Log($"[ROOM {roomIndex}] Onda {currentWave} vencida! Próxima em {timeBetweenWaves}s...");
                Invoke(nameof(StartNextWaveDelayed), timeBetweenWaves);
            }
            else
            {
                if (showSpawnLog) Debug.Log($"[ROOM {roomIndex}] SALA LIMPA! Destrancando portas.");
                UnlockDoors();
                this.enabled = false;
            }
        }
    }

    void StartNextWaveDelayed()
    {
        SpawnNextWave();
        combatActive = true;
    }

    // =====================================================
    // PORTAS
    // =====================================================

    void LockDoors()
    {
        doorsAreLocked = true;
        foreach (GameObject d in doors) if (d) d.SetActive(true);
    }

    void UnlockDoors()
    {
        doorsAreLocked = false;
        foreach (GameObject d in doors) if (d) d.SetActive(false);
    }

    // =====================================================
    // HELPERS
    // =====================================================

    Vector3 GetRandomPositionInArea(BoxCollider area)
    {
        if (area == null) return transform.position;
        Bounds bounds = area.bounds;
        float x = Random.Range(bounds.min.x, bounds.max.x);
        float z = Random.Range(bounds.min.z, bounds.max.z);
        // Usa bounds.min.y (chão da BoxCollider) como referência base.
        // Isso é correto independente de onde o pivot/root da área estiver.
        float y = bounds.min.y + spawnHeightOffset;
        return new Vector3(x, y, z);
    }

    GameObject GetRandom(List<GameObject> list)
    {
        if (list == null || list.Count == 0) return null;
        return list[Random.Range(0, list.Count)];
    }
}