using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Sistema de Respostas Narrativas & Avisos Táticos do Eptinho no Boss (Estilo Hades).
///  • 100% Desacoplado: Escuta BossEvents sem travar Corrotinas ou o loop do Boss.
///  • Sistema de Cooldowns por Tópico: Impede spam de mensagens durante a luta.
///  • Avisos Táticos para Refracção/Invisibilidade, Espinhos do Solo, Sangue Ácido e Impactos.
/// </summary>
public class BossTacticalEptinhoUI : MonoBehaviour
{
    private static BossTacticalEptinhoUI instance;
    public static BossTacticalEptinhoUI Instance => instance;

    public enum CalloutType
    {
        FightStart,
        Phase1Spikes,
        Phase2RefractionInvisibility,
        Phase3AcidPuddles,
        GroundShockwave,
        BossDefeated
    }

    [Header("⏱️ Cooldown de Repetição por Tópico (Segundos)")]
    public float topicCooldown = 10f;

    private Dictionary<CalloutType, float> lastCalloutTimes = new Dictionary<CalloutType, float>();

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    void OnEnable()
    {
        BossEvents.OnBossFightStarted += HandleFightStarted;
        BossEvents.OnPhaseChanged     += HandlePhaseChanged;
        BossEvents.OnBossDefeated     += HandleBossDefeated;
    }

    void OnDisable()
    {
        BossEvents.OnBossFightStarted -= HandleFightStarted;
        BossEvents.OnPhaseChanged     -= HandlePhaseChanged;
        BossEvents.OnBossDefeated     -= HandleBossDefeated;
    }

    private void HandleFightStarted()
    {
        TriggerCallout(CalloutType.FightStart, "Eptinho: Sinto uma presença esmagadora... Cuidado com as patadas e investidas do Boss!");
    }

    private void HandlePhaseChanged(int newPhase)
    {
        if (newPhase == 2)
        {
            TriggerCallout(CalloutType.Phase2RefractionInvisibility, "Eptinho: Alerta! Assinatura energética refratando — use o som dos passos do Boss!");
        }
        else if (newPhase == 3)
        {
            TriggerCallout(CalloutType.Phase3AcidPuddles, "Eptinho: Cuidado! O sangue do Boss está corrompendo o chão com acidez tóxica!");
        }
    }

    private void HandleBossDefeated()
    {
        TriggerCallout(CalloutType.BossDefeated, "Eptinho: Incrível! Conseguimos derrotar o Guardião Cromático!");
    }

    /// <summary>
    /// Dispara um aviso tático do Eptinho no HUD (sem travar NENHUMA corrotina).
    /// </summary>
    public static void TriggerTacticalNotice(CalloutType type, string customMessage = null)
    {
        if (instance != null)
        {
            instance.TriggerCallout(type, customMessage);
        }
        else
        {
            // Fallback direto se o script não estiver instanciado na cena
            DirectFallbackCallout(type, customMessage);
        }
    }

    private void TriggerCallout(CalloutType type, string message = null)
    {
        // Trava de Cooldown por Tópico para evitar repetição incômoda
        if (lastCalloutTimes.TryGetValue(type, out float lastTime))
        {
            if (Time.time - lastTime < topicCooldown) return;
        }

        lastCalloutTimes[type] = Time.time;

        string finalMsg = string.IsNullOrEmpty(message) ? GetDefaultMessage(type) : message;

        // Disparo seguro e não-bloqueante no EptinhoPopupController
        if (EptinhoPopupController.instancia != null)
        {
            if (type == CalloutType.Phase2RefractionInvisibility || type == CalloutType.Phase3AcidPuddles)
            {
                EptinhoPopupController.instancia.MostrarPopupMercador(finalMsg);
            }
            else if (type == CalloutType.BossDefeated)
            {
                EptinhoPopupController.instancia.MostrarPopupAviso(finalMsg);
            }
            else
            {
                EptinhoPopupController.instancia.MostrarPopupAviso(finalMsg);
            }

            Debug.Log($"💬 [EPTINHO TÁTICO - {type}] {finalMsg}");
        }
    }

    private static void DirectFallbackCallout(CalloutType type, string message)
    {
        if (EptinhoPopupController.instancia != null)
        {
            string msg = string.IsNullOrEmpty(message) ? GetDefaultMessage(type) : message;
            EptinhoPopupController.instancia.MostrarPopupAviso(msg);
        }
    }

    private static string GetDefaultMessage(CalloutType type)
    {
        switch (type)
        {
            case CalloutType.FightStart:
                return "Eptinho: Sinto uma presença esmagadora... Cuidado com as patadas e investidas!";
            case CalloutType.Phase1Spikes:
                return "Eptinho: Cuidado! Os espinhos de cristal estão se erguendo do solo!";
            case CalloutType.Phase2RefractionInvisibility:
                return "Eptinho: Alerta! Assinatura energética refratando — use o som dos passos do Boss!";
            case CalloutType.Phase3AcidPuddles:
                return "Eptinho: Cuidado! O sangue do Boss está corrompendo o chão com acidez!";
            case CalloutType.GroundShockwave:
                return "Eptinho: Cuidado com a onda de choque no solo! Pule ou recue!";
            case CalloutType.BossDefeated:
                return "Eptinho: Incrível! Vitória contra a aberração cromática!";
            default:
                return "Eptinho: Fique atento ao combate!";
        }
    }
}
