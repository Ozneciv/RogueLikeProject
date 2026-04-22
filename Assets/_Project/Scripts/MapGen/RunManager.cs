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
    /// Inicia uma nova Run: reseta o contador de salas.
    /// Chamado pelo GameManager ao carregar uma nova partida.
    /// </summary>
    public void StartNewRun()
    {
        currentRoomNumber = 1;
        Debug.Log("[RUN MANAGER] Nova run iniciada. Sala atual = 1.");
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
