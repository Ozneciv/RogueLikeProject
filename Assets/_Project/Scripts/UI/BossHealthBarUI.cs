using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Barra de vida do Boss Cromatico - HUD no topo da tela.
/// </summary>
public class BossHealthBarUI : MonoBehaviour
{
    public static BossHealthBarUI Instance { get; private set; }

    [Header("Layout")]
    public float barWidth  = 720f;
    public float barHeight = 26f;
    public float topOffset = 28f;

    [Header("Visual")]
    [Tooltip("Cor de preenchimento da barra de HP.")]
    public Color barColor = new Color(0.85f, 0.15f, 0.15f, 1f);

    private BossController boss;
    private GameObject rootPanel;
    private RectTransform fillRect;   // anchorMax.x controla o preenchimento
    private Image fillImage;
    private TextMeshProUGUI phaseLabel;
    private TextMeshProUGUI hpLabel;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoInitialize()
    {
        if (Instance == null)
        {
            GameObject go = new GameObject("BossHealthBarUI_AutoInit");
            DontDestroyOnLoad(go);
            go.AddComponent<BossHealthBarUI>();
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        if (boss == null)
            boss = FindFirstObjectByType<BossController>();

        bool shouldShow = boss != null && boss.IsFighting && !boss.IsDead;

        if (!shouldShow)
        {
            if (rootPanel != null && rootPanel.activeSelf)
                rootPanel.SetActive(false);
            return;
        }

        if (rootPanel == null) BuildUI();
        if (rootPanel == null) return;

        if (!rootPanel.activeSelf) rootPanel.SetActive(true);

        UpdateBar();
    }

    private void UpdateBar()
    {
        // Muda a ancora direita do fill — confiavel sem precisar de sprite
        if (fillRect != null)
            fillRect.anchorMax = new Vector2(boss.HealthPercent, 1f);

        if (phaseLabel != null)
        {
            int p = Mathf.Clamp(boss.CurrentPhase, 1, 3);
            phaseLabel.text = string.Format("BOSS CROMATICO  -  <b>FASE {0}/3</b>", p);
        }

        if (hpLabel != null && boss.phaseConfig != null)
        {
            int maxHP     = boss.phaseConfig.maxHealth;
            int currentHP = Mathf.RoundToInt(boss.HealthPercent * maxHP);
            hpLabel.text  = string.Format("{0} / {1}", currentHP, maxHP);
        }
    }

    private void BuildUI()
    {
        Canvas canvas = FindTargetPlayerCanvas();
        if (canvas == null) return;

        // Painel raiz
        rootPanel = new GameObject("BossHealthBarPanel");
        rootPanel.transform.SetParent(canvas.transform, false);
        RectTransform rootRt = rootPanel.AddComponent<RectTransform>();
        rootRt.anchorMin = new Vector2(0.5f, 1f);
        rootRt.anchorMax = new Vector2(0.5f, 1f);
        rootRt.pivot     = new Vector2(0.5f, 1f);
        rootRt.anchoredPosition = new Vector2(0f, -topOffset);
        rootRt.sizeDelta = new Vector2(barWidth, barHeight + 26f);

        // Label de fase
        GameObject labelGo = new GameObject("PhaseLabel");
        labelGo.transform.SetParent(rootPanel.transform, false);
        RectTransform labelRt = labelGo.AddComponent<RectTransform>();
        labelRt.anchorMin = new Vector2(0f, 1f);
        labelRt.anchorMax = new Vector2(1f, 1f);
        labelRt.pivot     = new Vector2(0.5f, 1f);
        labelRt.anchoredPosition = Vector2.zero;
        labelRt.sizeDelta = new Vector2(0f, 22f);
        phaseLabel = labelGo.AddComponent<TextMeshProUGUI>();
        phaseLabel.fontSize   = 16f;
        phaseLabel.fontStyle  = FontStyles.Bold;
        phaseLabel.alignment  = TextAlignmentOptions.Center;
        phaseLabel.color      = Color.white;
        phaseLabel.text       = "BOSS CROMATICO  -  <b>FASE 1/3</b>";
        Shadow labelShadow = labelGo.AddComponent<Shadow>();
        labelShadow.effectColor    = new Color(0f, 0f, 0f, 0.8f);
        labelShadow.effectDistance = new Vector2(2f, -2f);

        // Container da barra
        GameObject barGo = new GameObject("Bar");
        barGo.transform.SetParent(rootPanel.transform, false);
        RectTransform barRt = barGo.AddComponent<RectTransform>();
        barRt.anchorMin = new Vector2(0.5f, 1f);
        barRt.anchorMax = new Vector2(0.5f, 1f);
        barRt.pivot     = new Vector2(0.5f, 1f);
        barRt.anchoredPosition = new Vector2(0f, -24f);
        barRt.sizeDelta = new Vector2(barWidth, barHeight);

        // Fundo escuro
        Image bg = barGo.AddComponent<Image>();
        bg.color = new Color(0.15f, 0.05f, 0.05f, 0.9f);
        Outline barOutline = barGo.AddComponent<Outline>();
        barOutline.effectColor    = new Color(0f, 0f, 0f, 0.9f);
        barOutline.effectDistance = new Vector2(2f, -2f);

        // Fill — ancora direita muda proporcionalmente ao HP
        GameObject fillGo = new GameObject("Fill");
        fillGo.transform.SetParent(barGo.transform, false);
        fillRect = fillGo.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;   // comeca cheio; UpdateBar vai ajustar
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        fillImage = fillGo.AddComponent<Image>();
        fillImage.color = barColor;

        // Texto de HP
        GameObject hpGo = new GameObject("HPLabel");
        hpGo.transform.SetParent(barGo.transform, false);
        RectTransform hpRt = hpGo.AddComponent<RectTransform>();
        hpRt.anchorMin = Vector2.zero;
        hpRt.anchorMax = Vector2.one;
        hpRt.offsetMin = Vector2.zero;
        hpRt.offsetMax = Vector2.zero;
        hpLabel = hpGo.AddComponent<TextMeshProUGUI>();
        hpLabel.fontSize  = 13f;
        hpLabel.fontStyle = FontStyles.Bold;
        hpLabel.alignment = TextAlignmentOptions.Center;
        hpLabel.color     = Color.white;
        Shadow hpShadow = hpGo.AddComponent<Shadow>();
        hpShadow.effectColor    = new Color(0f, 0f, 0f, 1f);
        hpShadow.effectDistance = new Vector2(1f, -1f);
    }

    private static Canvas FindTargetPlayerCanvas()
    {
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach (var c in canvases)
            if (c != null && c.gameObject.activeInHierarchy &&
               (c.name.Contains("Player Canvas") || c.name.Contains("PlayerCanvas") || c.name.Contains("HUD")))
                return c;
        foreach (var c in canvases)
            if (c != null && c.gameObject.activeInHierarchy && c.renderMode == RenderMode.ScreenSpaceOverlay)
                return c;
        return canvases.Length > 0 ? canvases[0] : null;
    }
}
