using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gerencia a barra de vida do Boss no Canvas.
/// 
/// COMPORTAMENTO DE PERSISTÊNCIA E MAPA:
///   • Pode ser instanciado pelo BossController automaticamente ou colocado na cena.
///   • Suporta DontDestroyOnLoad para não sumir ao trocar de salas/portas.
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

    [Header("Cores do Preenchimento da Vida por Fase")]
    public Color colorPhase1 = new Color(0.2f, 0.55f, 1.0f);   // Azul (Fase 1 - Casulo)
    public Color colorPhase2 = new Color(0.95f, 0.15f, 0.15f);  // Vermelho (Fase 2 - Cristal)
    public Color colorPhase3 = new Color(0.2f, 0.9f, 0.35f);   // Verde (Fase 3 - Raízes)

    [Header("Controle de Visibilidade e Persistência")]
    [Tooltip("Se verdadeiro, o Canvas da barra de vida sobrevive a trocas de cena/portas.")]
    public bool dontDestroyOnLoad = true;

    [Tooltip("Opcional: Painel/Container filho com os visuais.")]
    public GameObject containerPanel;

    [Tooltip("Distância (em metros) do Boss para ativar a barra de vida automaticamente.")]
    public float detectionDistance = 25f;

    private int currentPhase = 1;
    private bool isFightActive = false;
    private Transform playerTransform;
    private BossController bossController;

    private static BossHealthBarUI instance;

    void Awake()
    {
        // Singleton simples para evitar duplicatas ao carregar novas cenas
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;

        if (dontDestroyOnLoad)
        {
            DontDestroyOnLoad(gameObject);
        }
    }

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
        UpdateColor(1);
        ResetBar();
        SetBarVisible(false); // Começa oculta até chegar perto do boss
    }

    void Update()
    {
        // Procura BossController na cena atual caso tenha trocado de sala ou recarregado
        if (bossController == null)
        {
            bossController = FindObjectOfType<BossController>();
            if (bossController != null && bossController.IsFighting)
            {
                OnFightStarted();
                OnHealthChanged(bossController.HealthPercent);
            }
        }

        if (playerTransform == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerTransform = p.transform;
        }

        // Se o boss estiver em combate, garante que a barra está visível
        if (bossController != null)
        {
            if (bossController.IsFighting && !isFightActive)
            {
                OnFightStarted();
                OnHealthChanged(bossController.HealthPercent);
                return;
            }

            // Checa aproximação do player ao boss
            if (!isFightActive && playerTransform != null && bossController.CurrentState == BossController.BossState.Idle)
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
        currentPhase = (bossController != null && bossController.CurrentPhase > 0) ? bossController.CurrentPhase : 1;
        UpdateFrame(currentPhase);
        UpdateColor(currentPhase);
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
        UpdateColor(newPhase);
        if (fillImage != null) fillImage.fillAmount = 1.0f; // Reseta a barra para 100% ao entrar na nova fase!
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

    void UpdateColor(int phase)
    {
        if (fillImage == null) return;

        switch (phase)
        {
            case 1: fillImage.color = colorPhase1; break; // Azul
            case 2: fillImage.color = colorPhase2; break; // Vermelho
            case 3: fillImage.color = colorPhase3; break; // Verde
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

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }
}
