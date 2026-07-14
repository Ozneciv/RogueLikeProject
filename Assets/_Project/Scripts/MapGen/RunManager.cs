using UnityEngine;

/// <summary>
/// Gerencia o estado global de uma Run (partida).
/// Rastreia o número da sala atual (n) para aplicar as fórmulas de
/// progressão econômica definidas no GDD (Economy.pdf).
///
/// Fórmula de Drops:  E(n) = d × (1 + α × n)   onde α = 0,05
/// Fórmula de Spawn:  P(n) = 10 + 0,9 × n
///
/// O LevelGenerator notifica este manager ao criar salas.
/// O RunManager persiste entre cenas (DontDestroyOnLoad).
/// </summary>
public class RunManager : MonoBehaviour
{
    public static RunManager instance;

    [Header("Estado da Run")]
    [Tooltip("Número da sala atual no contexto global da run (1–32).")]
    public int currentRoomNumber = 1;

    [Header("Parâmetros de Drops (GDD §1.1)")]
    [Tooltip("Coeficiente de inflação de drops por sala (α). GDD usa 0,05 → +5% por sala.")]
    public float dropInflationAlpha = 0.05f;

    [Header("Parâmetros de Spawn (GDD §1.2)")]
    [Tooltip("Pontos base de spawn na sala 1.")]
    public float spawnPointsBase = 10f;
    [Tooltip("Incremento de pontos de spawn por sala.")]
    public float spawnPointsGrowth = 0.9f;

    [Header("Progressão de Rounds")]
    [Tooltip("Total de rounds por run. O ÚLTIMO round é sempre Boss Fight.")]
    public int totalLevels = 4;

    [Tooltip("Número de salas principais geradas em cada round normal (índice 0 = Round 1, 1 = Round 2, etc.).\nO boss round não usa este array.")]
    public int[] roomsPerLevel = new int[] { 5, 7, 9 };

    /// <summary>Round atual da run (1 = primeiro nível, totalLevels = boss). Persiste entre cenas via DontDestroyOnLoad.</summary>
    [HideInInspector] public int currentLevel = 1;

    // ==================== GEOBIONTE — PROGRESSO MULTI-FASE ====================

    /// <summary>
    /// Quantas vezes o Geobionte já foi derrotado como Bismutado durante esta run (0–3).
    /// Ao atingir fusionsToSentinel (3), o Geobionte evolui para Sentinela.
    /// Persiste entre cenas via DontDestroyOnLoad.
    /// </summary>
    [HideInInspector] public int geobionteDefeatCount = 0;

    /// <summary>
    /// Se o Geobionte já absorveu um cristal NESTA fase (round).
    /// Impede absorção dupla na mesma fase. Resetado ao avançar de fase.
    /// </summary>
    [HideInInspector] public bool geobionteAbsorbedThisLevel = false;

    // =====================================================

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // =====================================================
    // API PÚBLICA
    // =====================================================

    /// <summary>
    /// Inicia uma nova Run: reseta sala E round.
    /// Chamado pelo GameManager quando o jogador começa uma partida do zero (ex: saindo da BaseLab).
    /// NÃO chame isso ao avançar entre níveis da mesma run — use AdvanceLevel().
    /// </summary>
    public void StartNewRun()
    {
        currentRoomNumber = 1;
        currentLevel = 1;

        // Reset do progresso do Geobionte
        geobionteDefeatCount = 0;
        geobionteAbsorbedThisLevel = false;

        Debug.Log("[RunManager] 🆕 Nova run iniciada. Sala 1 | Round 1.");
    }

    /// <summary>
    /// True se o round atual é o Boss Fight (último round da run).
    /// </summary>
    public bool isBossRound => currentLevel >= totalLevels;

    /// <summary>
    /// Avança para o próximo round da run.
    /// Chamado pelo GameManager quando o jogador usa a Exit Room.
    /// </summary>
    public void AdvanceLevel()
    {
        currentLevel = Mathf.Min(currentLevel + 1, totalLevels);

        // Permite o Geobionte absorver um novo cristal na próxima fase
        geobionteAbsorbedThisLevel = false;

        Debug.Log($"[RunManager] ▶️ Round {currentLevel}/{totalLevels} | Boss? {isBossRound} | Geobionte derrotas: {geobionteDefeatCount}/3");
    }

    /// <summary>
    /// Retorna o maxMainRooms configurado para o round atual.
    /// Retorna 0 se for Boss Round (o LevelGenerator deve lidar com isso).
    /// </summary>
    public int GetMaxRoomsForCurrentLevel()
    {
        if (isBossRound) return 0;
        int idx = Mathf.Clamp(currentLevel - 1, 0, roomsPerLevel.Length - 1);
        return roomsPerLevel[idx];
    }

    /// <summary>
    /// Atualiza a sala atual (chamado pelo RoomEnemySpawner ao entrar na sala).
    /// </summary>
    public void SetCurrentRoom(int roomNumber)
    {
        currentRoomNumber = Mathf.Max(1, roomNumber);
        Debug.Log($"[RUN MANAGER] Sala atual: {currentRoomNumber}");
    }

    /// <summary>
    /// Multiplicador de essência para a sala n.
    /// E(n) = d × (1 + α × n) → multiplicador = (1 + α × n)
    /// 
    /// Se roomNumber = -1, usa o currentRoomNumber.
    /// </summary>
    public float GetEssenceMultiplier(int roomNumber = -1)
    {
        int n = roomNumber < 0 ? currentRoomNumber : roomNumber;
        return 1f + dropInflationAlpha * n;
    }

    /// <summary>
    /// Orçamento de pontos de spawn para a sala n.
    /// P(n) = 10 + 0,9 × n
    /// </summary>
    public int GetSpawnBudget(int roomNumber)
    {
        return Mathf.RoundToInt(spawnPointsBase + spawnPointsGrowth * roomNumber);
    }
}
