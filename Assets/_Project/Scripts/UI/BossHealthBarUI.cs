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

    [Header("Nome Dinâmico do Chefe na UI")]
    [Tooltip("Texto (TMPro) para o nome/título dinâmico do Boss.")]
    public TMPro.TextMeshProUGUI bossNameText;

    [Tooltip("Nome do Boss na Fase 1")]
    public string namePhase1 = "ORC CROMÁTICO — O GUARDIÃO CRISTALINO";

    [Tooltip("Nome do Boss na Fase 2")]
    public string namePhase2 = "ORC CROMÁTICO — FORMA REFRATADA";

    [Tooltip("Nome do Boss na Fase 3")]
    public string namePhase3 = "ORC CROMÁTICO — CORRUPÇÃO ÁCIDA";

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

        AutoDetectComponents();
    }

    private void AutoDetectComponents()
    {
        if (fillImage == null || frameImage == null)
        {
            Image[] imgs = GetComponentsInChildren<Image>(true);
            foreach (var img in imgs)
            {
                string n = img.name.ToLower();
                if (fillImage == null && n.Contains("fill")) fillImage = img;
                if (frameImage == null && n.Contains("frame")) frameImage = img;
            }
        }

        if (bossNameText == null)
        {
            bossNameText = GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
        }

        if (fillImage != null)
        {
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        }
    }

    void OnEnable()
    {
        AutoDetectComponents();
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
        AutoDetectComponents();
        UpdateFrame(1);
        UpdateColor(1);
        ResetBar();
        SetBarVisible(false); // Começa oculta até o combate iniciar
    }

    void Update()
    {
        // Procura BossController na cena atual caso tenha trocado de sala ou recarregado
        if (bossController == null)
        {
            bossController = FindFirstObjectByType<BossController>();
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

    public void OnFightStarted()
    {
        isFightActive = true;
        AutoDetectComponents();
        currentPhase = (bossController != null && bossController.CurrentPhase > 0) ? bossController.CurrentPhase : 1;
        UpdateFrame(currentPhase);
        UpdateColor(currentPhase);
        UpdateName(currentPhase);
        ResetBar();
        SetBarVisible(true);
    }

    public void OnHealthChanged(float hpPercent)
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

    public void OnPhaseChanged(int newPhase)
    {
        currentPhase = newPhase;
        AutoDetectComponents();
        UpdateFrame(newPhase);
        UpdateColor(newPhase);
        UpdateName(newPhase);
        if (fillImage != null) fillImage.fillAmount = 1.0f; // Reseta a barra para 100% ao entrar na nova fase!
        if (!isFightActive) SetBarVisible(true);
    }

    void UpdateName(int phase)
    {
        if (bossNameText == null)
        {
            bossNameText = GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
        }

        if (bossNameText != null)
        {
            switch (phase)
            {
                case 1: bossNameText.text = namePhase1; break;
                case 2: bossNameText.text = namePhase2; break;
                case 3: bossNameText.text = namePhase3; break;
            }
        }
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

    public void OnBossDefeated()
    {
        if (fillImage != null) fillImage.fillAmount = 0f;
        Invoke(nameof(HideWithDelay), 2.5f);
    }

    void ResetBar()
    {
        if (fillImage != null) fillImage.fillAmount = 1f;
    }

    public void SetBarVisible(bool visible)
    {
        AutoDetectComponents();

        if (containerPanel != null)
        {
            containerPanel.SetActive(visible);
        }
        else
        {
            if (fillImage != null) fillImage.gameObject.SetActive(visible);
            if (frameImage != null) frameImage.gameObject.SetActive(visible);
            if (bossNameText != null) bossNameText.gameObject.SetActive(visible);
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
