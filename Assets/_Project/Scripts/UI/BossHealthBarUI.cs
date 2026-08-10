using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gerencia a barra de vida do Boss no Canvas (Fases 1, 2 e 3).
/// 
/// SETUP NO UNITY:
///   1. Adicione este script a um GameObject no Canvas (ex: BossHealthBar_Root)
///   2. Arraste as 3 barras de fase (Image com Fill Method = Horizontal) no Inspector
///   3. O script escuta os BossEvents automaticamente
/// </summary>
public class BossHealthBarUI : MonoBehaviour
{
    [Header("Barras de Fase")]
    [Tooltip("Barra da Fase 1 (Casulo) — Image com Fill Method = Horizontal")]
    public Image phase1Bar;

    [Tooltip("Barra da Fase 2 (Cristal) — Image com Fill Method = Horizontal")]
    public Image phase2Bar;

    [Tooltip("Barra da Fase 3 (Raízes) — Image com Fill Method = Horizontal")]
    public Image phase3Bar;

    [Header("Cores por Fase")]
    public Color colorPhase1 = new Color(0.2f, 0.5f, 1.0f);    // Azul
    public Color colorPhase2 = new Color(0.8f, 0.2f, 0.9f);    // Roxo
    public Color colorPhase3 = new Color(0.2f, 0.9f, 0.3f);    // Verde

    [Header("Thresholds de Fase (deve bater com BossPhaseConfig)")]
    [Tooltip("HP% em que a Fase 2 começa. Ex: 0.70 = 70%")]
    public float phase2Threshold = 0.70f;

    [Tooltip("HP% em que a Fase 3 começa. Ex: 0.35 = 35%")]
    public float phase3Threshold = 0.35f;

    // ── Estado interno ───────────────────────────────────────
    private int currentPhase = 1;

    void OnEnable()
    {
        BossEvents.OnBossHealthChanged += OnHealthChanged;
        BossEvents.OnPhaseChanged      += OnPhaseChanged;
        BossEvents.OnBossFightStarted  += OnFightStarted;
        BossEvents.OnBossDefeated      += OnBossDefeated;
    }

    void OnDisable()
    {
        BossEvents.OnBossHealthChanged -= OnHealthChanged;
        BossEvents.OnPhaseChanged      -= OnPhaseChanged;
        BossEvents.OnBossFightStarted  -= OnFightStarted;
        BossEvents.OnBossDefeated      -= OnBossDefeated;
    }

    void Start()
    {
        // Aplica as cores iniciais
        if (phase1Bar != null) phase1Bar.color = colorPhase1;
        if (phase2Bar != null) phase2Bar.color = colorPhase2;
        if (phase3Bar != null) phase3Bar.color = colorPhase3;

        ResetBars();
    }

    void OnFightStarted()
    {
        gameObject.SetActive(true);
        currentPhase = 1;
        ResetBars();
    }

    void OnHealthChanged(float hpPercent)
    {
        // hpPercent: HP total do boss de 0.0 a 1.0
        if (phase1Bar != null)
        {
            float fill1 = Mathf.InverseLerp(phase2Threshold, 1.0f, hpPercent);
            phase1Bar.fillAmount = Mathf.Clamp01(fill1);
        }

        if (phase2Bar != null)
        {
            float fill2 = Mathf.InverseLerp(phase3Threshold, phase2Threshold, hpPercent);
            phase2Bar.fillAmount = Mathf.Clamp01(fill2);
        }

        if (phase3Bar != null)
        {
            float fill3 = Mathf.InverseLerp(0.0f, phase3Threshold, hpPercent);
            phase3Bar.fillAmount = Mathf.Clamp01(fill3);
        }
    }

    void OnPhaseChanged(int newPhase)
    {
        currentPhase = newPhase;

        if (newPhase == 2 && phase1Bar != null) phase1Bar.fillAmount = 0f;
        if (newPhase == 3 && phase2Bar != null) phase2Bar.fillAmount = 0f;
    }

    void OnBossDefeated()
    {
        if (phase3Bar != null) phase3Bar.fillAmount = 0f;
        Invoke(nameof(Hide), 1.5f);
    }

    void ResetBars()
    {
        if (phase1Bar != null) phase1Bar.fillAmount = 1f;
        if (phase2Bar != null) phase2Bar.fillAmount = 1f;
        if (phase3Bar != null) phase3Bar.fillAmount = 1f;
    }

    void Hide()
    {
        gameObject.SetActive(false);
    }
}
