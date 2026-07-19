using System;
using UnityEngine;

/// <summary>
/// Hub central de eventos do Boss Cromático.
///
/// COMO USAR (para os colegas):
///   1. Se inscreva no evento desejado no OnEnable():
///        BossEvents.OnPhaseChanged += MeuMetodo;
///   2. Cancele a inscrição no OnDisable():
///        BossEvents.OnPhaseChanged -= MeuMetodo;
///   3. O BossController dispara esses eventos automaticamente.
///      Você NÃO precisa editar este arquivo.
///
/// EVENTOS DISPONÍVEIS:
///   OnPhaseChanged(int fase)         → Mudança de fase (1, 2 ou 3)
///   OnRefractionToggle(bool ativo)   → Boss ficou invisível (true) ou visível (false)
///   OnBossStunned(float duração)     → Boss foi atordoado por N segundos
///   OnBossDefeated()                 → Boss morreu — abrir selo, tocar música, etc.
///   OnBossHealthChanged(float %)     → HP do boss mudou (0.0 a 1.0) — para barra de vida
///   OnBossFightStarted()             → Luta começou — trancar arena, UI, música
/// </summary>
public static class BossEvents
{
    // =====================================================
    // EVENTOS
    // =====================================================

    /// <summary>
    /// Disparado quando o boss muda de fase.
    /// Parâmetro: número da nova fase (1, 2 ou 3).
    /// Usado por: Lucas (Fase 1), Gabriel (Fase 2/3), Serralha (Fase 3), Matheus (UI).
    /// </summary>
    public static event Action<int> OnPhaseChanged;

    /// <summary>
    /// Disparado quando o boss ativa ou desativa a Refração de Luz (invisibilidade).
    /// Parâmetro: true = ficou invisível, false = voltou a ser visível.
    /// Usado por: Gabriel (shader de distorção, partículas de passos).
    /// </summary>
    public static event Action<bool> OnRefractionToggle;

    /// <summary>
    /// Disparado quando o boss é atordoado (stun).
    /// Parâmetro: duração do stun em segundos.
    /// Usado por: Todos (boss para de se mover e fica vulnerável).
    /// </summary>
    public static event Action<float> OnBossStunned;

    /// <summary>
    /// Disparado quando o boss morre.
    /// Usado por: Todos (destruir selo, tocar música de vitória, UI).
    /// </summary>
    public static event Action OnBossDefeated;

    /// <summary>
    /// Disparado sempre que o HP do boss muda.
    /// Parâmetro: porcentagem de vida restante (0.0 a 1.0).
    /// Usado por: Matheus (barra de vida do boss na UI).
    /// </summary>
    public static event Action<float> OnBossHealthChanged;

    /// <summary>
    /// Disparado quando a luta com o boss começa (player cruzou a linha).
    /// Usado por: UI (mostrar barra de HP), Música, câmera.
    /// </summary>
    public static event Action OnBossFightStarted;

    // =====================================================
    // MÉTODOS DE DISPARO (chamados pelo BossController)
    // =====================================================

    /// <summary>Notifica mudança de fase. Chamado pelo BossController.</summary>
    public static void RaisePhaseChanged(int newPhase)
    {
        Debug.Log($"[BossEvents] 🔄 Fase mudou para: {newPhase}");
        OnPhaseChanged?.Invoke(newPhase);
    }

    /// <summary>Notifica toggle de refração. Chamado pelo BossController.</summary>
    public static void RaiseRefractionToggle(bool isInvisible)
    {
        Debug.Log($"[BossEvents] 👁️ Refração: {(isInvisible ? "ATIVADA" : "DESATIVADA")}");
        OnRefractionToggle?.Invoke(isInvisible);
    }

    /// <summary>Notifica stun do boss. Chamado pelo BossController.</summary>
    public static void RaiseBossStunned(float duration)
    {
        Debug.Log($"[BossEvents] ⚡ Boss atordoado por {duration}s!");
        OnBossStunned?.Invoke(duration);
    }

    /// <summary>Notifica morte do boss. Chamado pelo BossController.</summary>
    public static void RaiseBossDefeated()
    {
        Debug.Log("[BossEvents] 💀 Boss derrotado!");
        OnBossDefeated?.Invoke();
    }

    /// <summary>Notifica mudança de HP. Chamado pelo BossController.</summary>
    public static void RaiseBossHealthChanged(float healthPercent)
    {
        OnBossHealthChanged?.Invoke(healthPercent);
    }

    /// <summary>Notifica início da luta. Chamado pelo BossCombatTrigger.</summary>
    public static void RaiseBossFightStarted()
    {
        Debug.Log("[BossEvents] ⚔️ Luta com o Boss iniciada!");
        OnBossFightStarted?.Invoke();
    }

    // =====================================================
    // LIMPEZA
    // =====================================================

    /// <summary>
    /// Remove TODAS as inscrições de todos os eventos.
    /// Chamado automaticamente pelo BossController ao ser destruído,
    /// para evitar referências fantasmas entre cenas.
    /// </summary>
    public static void ClearAll()
    {
        OnPhaseChanged = null;
        OnRefractionToggle = null;
        OnBossStunned = null;
        OnBossDefeated = null;
        OnBossHealthChanged = null;
        OnBossFightStarted = null;
    }
}
