using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gerencia a barra de vida do Boss no Canvas.
/// 
/// COMPORTAMENTO DE ATIVAÇÃO:
///   • Começa oculta.
///   • Quando o jogador se aproxima do Boss (distância de detecção) ou quando a luta inicia, a barra aparece automaticamente na tela.
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

    [Header("Controle de Visibilidade")]
    [Tooltip("Opcional: Painel/Container filho com os visuais. Se nulo, usará este próprio GameObject.")]
    public GameObject containerPanel;

    [Tooltip("Distância (em metros) do Boss para ativar a barra de vida automaticamente caso a luta não tenha iniciado.")]
    public float detectionDistance = 25f;

    private int currentPhase = 1;
    private bool isFightActive = false;
    private Transform playerTransform;
    private BossController bossController;

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
        SetBarVisible(false); // Começa oculta até chegar perto do boss
    }

    void Update()
    {
        if (isFightActive) return;

        // Procura referências se não tiver
        if (bossController == null) bossController = FindObjectOfType<BossController>();
        if (playerTransform == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerTransform = p.transform;
        }

        // Se o boss já começou a luta (ou mudou de fase/tomou dano), ativa a barra
        if (bossController != null)
        {
            if (bossController.CurrentState != BossState.Idle && bossController.CurrentState != BossState.Dead)
            {
                OnFightStarted();
                return;
            }

            // Checa aproximação do player ao boss
            if (playerTransform != null)
            {
                float dist = Vector3.Distance(bossController.transform.position, playerTransform.position);
                if (dist <= detectionDistance)
                {
                    bossController.StartFight();
                }
            }
        }
    }

    void OnFightStarted()
    {
        isFightActive = true;
        currentPhase = 1;
        UpdateFrame(1);
        ResetBar();
        SetBarVisible(true);
    }

    void OnHealthChanged(float hpPercent)
    {
        if (!isFightActive && hpPercent < 1.0f)
        {
            OnFightStarted();
        }

        if (fillImage != null)
        {
            fillImage.fillAmount = Mathf.Clamp01(hpPercent);
        }
    }

    void OnPhaseChanged(int newPhase)
    {
        currentPhase = newPhase;
        UpdateFrame(newPhase);
        if (!isFightActive) SetBarVisible(true);
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
        Invoke(nameof(HideWithDelay), 1.5f);
    }

    void ResetBar()
    {
        if (fillImage != null) fillImage.fillAmount = 1f;
    }

    void SetBarVisible(bool visible)
    {
        if (containerPanel != null)
        {
            containerPanel.SetActive(visible);
        }
        else
        {
            if (fillImage != null) fillImage.gameObject.SetActive(visible);
            if (frameImage != null) frameImage.gameObject.SetActive(visible);
        }
    }

    void HideWithDelay()
    {
        isFightActive = false;
        SetBarVisible(false);
    }
}
