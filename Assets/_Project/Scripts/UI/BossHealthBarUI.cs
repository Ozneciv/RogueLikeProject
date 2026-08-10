using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gerencia a barra de vida do Boss no Canvas.
/// 
/// COMPORTAMENTO:
///   • 1 Barra de Vida vermelha (Fill Image) que diminui de 100% até 0% conforme o Boss toma dano.
///   • 1 Moldura (Frame Image) que troca de Sprite automaticamente conforme a Fase (Fase 1, 2 ou 3).
/// </summary>
public class BossHealthBarUI : MonoBehaviour
{
    [Header("1 Barra de Vida Única (Vermelha)")]
    [Tooltip("A imagem vermelha da vida que vai diminuir (Image Type = Filled, Fill Method = Horizontal).")]
    public Image fillImage;

    [Header("Molduras por Fase")]
    [Tooltip("A Image no Canvas que vai exibir a moldura em volta da barra.")]
    public Image frameImage;

    [Tooltip("Sprite da moldura para a Fase 1 (Casulo)")]
    public Sprite framePhase1;

    [Tooltip("Sprite da moldura para a Fase 2 (Cristal)")]
    public Sprite framePhase2;

    [Tooltip("Sprite da moldura para a Fase 3 (Raízes)")]
    public Sprite framePhase3;

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
        ResetBar();
    }

    void OnFightStarted()
    {
        gameObject.SetActive(true);
        currentPhase = 1;
        UpdateFrame(1);
        ResetBar();
    }

    void OnHealthChanged(float hpPercent)
    {
        if (fillImage != null)
        {
            fillImage.fillAmount = Mathf.Clamp01(hpPercent);
        }
    }

    void OnPhaseChanged(int newPhase)
    {
        currentPhase = newPhase;
        UpdateFrame(newPhase);
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
        Invoke(nameof(Hide), 1.5f);
    }

    void ResetBar()
    {
        if (fillImage != null) fillImage.fillAmount = 1f;
    }

    void Hide()
    {
        gameObject.SetActive(false);
    }
}
