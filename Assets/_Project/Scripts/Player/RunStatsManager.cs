using UnityEngine;
using System;

/// <summary>
/// =================================================================================
/// RASTREADOR DE ESTATÍSTICAS DA RUN & TIMER DO MODO ENDLESS (SPEEDRUN)
/// =================================================================================
/// Desenvolvido para: RogueLikeProject
/// 
/// Métricas Rastradas:
/// 1. Tempo Sobrevivido (Speedrun Timer)
/// 2. Dano Total Causado a Inimigos
/// 3. Dano Total Recebido pelo Jogador
/// 4. Inimigos/Mobs Derrotados
/// 5. Essências Coletadas na Run
/// 6. Essências Gastas (Mercador/Crafting)
/// 7. Fase/Local da Morte
/// =================================================================================
/// </summary>
public class RunStatsManager : MonoBehaviour
{
    public static RunStatsManager Instance { get; private set; }

    [Header("Estado da Run")]
    public bool isRunActive = false;
    public bool isEndlessMode = false;
    public float survivalTimer = 0f;

    [Header("Métricas Acumuladas")]
    public long totalDamageDealt = 0;
    public long totalDamageTaken = 0;
    public int totalMobsKilled = 0;
    public int totalEssenceCollected = 0;
    public int totalEssenceSpent = 0;
    public string deathStage = "Base";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoInitialize()
    {
        if (Instance == null)
        {
            GameObject go = new GameObject("RunStatsManager_AutoInit");
            DontDestroyOnLoad(go);
            go.AddComponent<RunStatsManager>();
        }
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Update()
    {
        if (isRunActive)
        {
            survivalTimer += Time.deltaTime;
        }
    }

    public void StartRunTracking(bool endless = false)
    {
        ResetStats();
        isRunActive = true;
        isEndlessMode = endless;
        survivalTimer = 0f;
        Debug.Log($"⏱️ [RUN STATS] Rastreamento iniciado! Modo Endless: {isEndlessMode}");
    }

    public void StopRunTracking()
    {
        isRunActive = false;
        Debug.Log($"⏱️ [RUN STATS] Rastreamento parado. Tempo final: {FormatTime(survivalTimer)}");
    }

    public void ResetStats()
    {
        survivalTimer = 0f;
        totalDamageDealt = 0;
        totalDamageTaken = 0;
        totalMobsKilled = 0;
        totalEssenceCollected = 0;
        totalEssenceSpent = 0;
        deathStage = "Base";
    }

    public void RecordDamageDealt(long damage)
    {
        if (!isRunActive || damage <= 0) return;
        totalDamageDealt += damage;
    }

    public void RecordDamageTaken(long damage)
    {
        if (!isRunActive || damage <= 0) return;
        totalDamageTaken += damage;
    }

    public void RecordEnemyKilled()
    {
        if (!isRunActive) return;
        totalMobsKilled++;
    }

    public void RecordEssenceCollected(int amount)
    {
        if (!isRunActive || amount <= 0) return;
        totalEssenceCollected += amount;
    }

    public void RecordEssenceSpent(int amount)
    {
        if (!isRunActive || amount <= 0) return;
        totalEssenceSpent += amount;
    }

    public string FormatTime(float timeInSeconds)
    {
        TimeSpan t = TimeSpan.FromSeconds(timeInSeconds);
        if (t.Hours > 0)
        {
            return string.Format("{0:D2}:{1:D2}:{2:D2}", t.Hours, t.Minutes, t.Seconds);
        }
        return string.Format("{0:D2}:{1:D2}", t.Minutes, t.Seconds);
    }

    public string FormatNumber(long number)
    {
        return number.ToString("N0");
    }
}
