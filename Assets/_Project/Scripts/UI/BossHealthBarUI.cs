using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gerencia a barra de vida do Boss no Canvas (Fases 1, 2 e 3).
/// 
/// SUPORTA 4 IMAGENS:
///   • 1 Imagem de preenchimento (Fill Image) — diminui conforme a vida cai
///   • 3 Sprites de moldura (Frame Phase 1, Phase 2, Phase 3) — troca a borda automaticamente por fase
/// </summary>
public class BossHealthBarUI : MonoBehaviour
{
    [Header("Preenchimento da Vida (Fill)")]
    [Tooltip("A imagem da barra de vida que vai diminuir. Deve estar com Image Type = Filled, Fill Method = Horizontal.")]
    public Image fillImage;

    [Header("Molduras por Fase (Frames)")]
    [Tooltip("A Image no Canvas que exibe a moldura da barra de vida.")]
    public Image frameImage;

    [Tooltip("Sprite da moldura para a Fase 1 (Casulo)")]
    public Sprite framePhase1;

    [Tooltip("Sprite da moldura para a Fase 2 (Cristal)")]
    public Sprite framePhase2;

    [Tooltip("Sprite da moldura para a Fase 3 (Raízes)")]
    public Sprite framePhase3;

    [Header("Suporte Opcional: 3 Barras Separadas (se preferir por fase)")]
    public Image phase1Bar;
    public Image phase2Bar;
    public Image phase3Bar;

    [Header("Thresholds de Fase (deve bater com BossPhaseConfig)")]
    public float phase2Threshold = 0.70f;
    public float phase3Threshold = 0.35f;

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
        UpdateFrame(1);
        ResetBars();
    }

    void OnFightStarted()
    {
        gameObject.SetActive(true);
        currentPhase = 1;
        UpdateFrame(1);
        ResetBars();
    }

    void OnHealthChanged(float hpPercent)
    {
        // 1. Atualiza o Fill Único se atribuído
        if (fillImage != null)
        {
            fillImage.fillAmount = Mathf.Clamp01(hpPercent);
        }

        // 2. Atualiza Barras Separadas se atribuídas
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
        UpdateFrame(newPhase);

        if (newPhase == 2 && phase1Bar != null) phase1Bar.fillAmount = 0f;
        if (newPhase == 3 && phase2Bar != null) phase2Bar.fillAmount = 0f;
    }

    void UpdateFrame(int phase)
    {
        if (frameImage == null) return;

        switch (phase)
        {
            case 1:
                if (framePhase1 != null) frameImage.sprite = framePhase1;
                break;
            case 2:
                if (framePhase2 != null) frameImage.sprite = framePhase2;
                break;
            case 3:
                if (framePhase3 != null) frameImage.sprite = framePhase3;
                break;
        }
    }

    void OnBossDefeated()
    {
        if (fillImage != null) fillImage.fillAmount = 0f;
        if (phase3Bar != null) phase3Bar.fillAmount = 0f;
        Invoke(nameof(Hide), 1.5f);
    }

    void ResetBars()
    {
        if (fillImage != null) fillImage.fillAmount = 1f;
        if (phase1Bar != null) phase1Bar.fillAmount = 1f;
        if (phase2Bar != null) phase2Bar.fillAmount = 1f;
        if (phase3Bar != null) phase3Bar.fillAmount = 1f;
    }

    void Hide()
    {
        gameObject.SetActive(false);
    }
}
