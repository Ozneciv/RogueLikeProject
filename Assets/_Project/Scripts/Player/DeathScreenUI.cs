using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// Tela de Morte ("VOCÊ MORREU") com resumo de todas as 7 estatísticas da run.
/// </summary>
public class DeathScreenUI : MonoBehaviour
{
    public static DeathScreenUI Instance { get; private set; }

    [Header("Painéis & Canvas")]
    public GameObject deathPanel;
    public TextMeshProUGUI txtTitle;
    public TextMeshProUGUI txtStatsSummary;
    public Button btnReturnBase;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoInitialize()
    {
        if (Instance == null)
        {
            GameObject go = new GameObject("DeathScreenUI_AutoInit");
            DontDestroyOnLoad(go);
            go.AddComponent<DeathScreenUI>();
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        EnsureUIReferences();
        if (deathPanel != null)
        {
            deathPanel.SetActive(false);
        }

        if (btnReturnBase != null)
        {
            btnReturnBase.onClick.RemoveAllListeners();
            btnReturnBase.onClick.AddListener(OnReturnBaseClicked);
        }
    }

    public void ShowDeathScreen()
    {
        EnsureUIReferences();
        if (deathPanel != null)
        {
            deathPanel.SetActive(true);
            deathPanel.transform.SetAsLastSibling();
        }

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        if (txtTitle != null)
        {
            txtTitle.text = "<size=46><color=#ff2233><b>VOCÊ MORREU</b></color></size>\n<size=16><color=#ffaa44><i>RESUMO DA RUN</i></color></size>";
        }

        if (txtStatsSummary != null && RunStatsManager.Instance != null)
        {
            RunStatsManager s = RunStatsManager.Instance;
            string timeStr = s.FormatTime(s.survivalTimer);
            string dmgDealtStr = s.FormatNumber(s.totalDamageDealt);
            string dmgTakenStr = s.FormatNumber(s.totalDamageTaken);

            txtStatsSummary.text =
                $"<color=#ffcc00><b>TEMPO SOBREVIDO:</b></color>  <color=#ffffff>{timeStr}</color>\n\n" +
                $"<color=#ffaa44><b>DANO TOTAL CAUSADO:</b></color>  <color=#ffffff>{dmgDealtStr}</color>\n\n" +
                $"<color=#ff4455><b>INIMIGOS DERROTADOS:</b></color>  <color=#ffffff>{s.totalMobsKilled}</color>\n\n" +
                $"<color=#00ff99><b>ESSÊNCIAS COLETADAS:</b></color>  <color=#ffffff>{s.totalEssenceCollected}</color>\n\n" +
                $"<color=#ffcc00><b>ESSÊNCIAS GASTAS:</b></color>  <color=#ffffff>{s.totalEssenceSpent}</color>\n\n" +
                $"<color=#ff6666><b>DANO TOTAL RECEBIDO (RUN):</b></color>  <color=#ffffff>{dmgTakenStr}</color>\n\n" +
                $"<color=#cc88ff><b>LOCAL DA MORTE:</b></color>  <color=#ffffff>{s.deathStage}</color>";
        }

        Time.timeScale = 0f; // Pausa a partida durante o resumo da morte
    }

    private void OnReturnBaseClicked()
    {
        Time.timeScale = 1f;
        if (RunStatsManager.Instance != null)
        {
            RunStatsManager.Instance.ResetStats();
        }
        if (GameManager.instance != null)
        {
            GameManager.instance.ReturnToBase();
        }
        else
        {
            SceneManager.LoadScene("Base");
        }
    }

    private void EnsureUIReferences()
    {
        if (deathPanel == null)
        {
            Canvas parentCanvas = GetComponentInParent<Canvas>();
            if (parentCanvas == null) parentCanvas = FindTargetPlayerCanvas();

            if (parentCanvas != null)
            {
                GameObject panelGo = new GameObject("DeathScreenPanel");
                panelGo.transform.SetParent(parentCanvas.transform, false);

                // Garante que o painel de morte fique no topo ABSOLUTO de todas as UIs da cena
                Canvas topCanvas = panelGo.AddComponent<Canvas>();
                topCanvas.overrideSorting = true;
                topCanvas.sortingOrder = 9999;

                GraphicRaycaster raycaster = panelGo.AddComponent<GraphicRaycaster>();
                raycaster.ignoreReversedGraphics = true;

                RectTransform panelRt = panelGo.GetComponent<RectTransform>();
                panelRt.anchorMin = Vector2.zero;
                panelRt.anchorMax = Vector2.one;
                panelRt.offsetMin = Vector2.zero;
                panelRt.offsetMax = Vector2.zero;

                Image bg = panelGo.AddComponent<Image>();
                bg.color = new Color(0.03f, 0.03f, 0.06f, 0.95f); // Fundo escuro fosco
                bg.raycastTarget = true; // Bloqueia o jogo atrás mas aceita cliques para a UI de Morte

                // Card Container Central
                GameObject cardGo = new GameObject("DeathCard");
                cardGo.transform.SetParent(panelGo.transform, false);

                RectTransform cardRt = cardGo.AddComponent<RectTransform>();
                cardRt.anchorMin = new Vector2(0.5f, 0.5f);
                cardRt.anchorMax = new Vector2(0.5f, 0.5f);
                cardRt.pivot = new Vector2(0.5f, 0.5f);
                cardRt.anchoredPosition = Vector2.zero;
                cardRt.sizeDelta = new Vector2(560f, 620f);

                Image cardBg = cardGo.AddComponent<Image>();
                cardBg.color = new Color(0.06f, 0.06f, 0.10f, 0.92f);
                cardBg.raycastTarget = false; // NÃO bloqueia o botão

                Shadow cardShadow = cardGo.AddComponent<Shadow>();
                cardShadow.effectColor = new Color(0f, 0f, 0f, 0.7f);
                cardShadow.effectDistance = new Vector2(5f, -5f);

                // Título
                GameObject titleGo = new GameObject("txtTitle");
                titleGo.transform.SetParent(cardGo.transform, false);
                RectTransform titleRt = titleGo.AddComponent<RectTransform>();
                titleRt.anchorMin = new Vector2(0.05f, 0.82f);
                titleRt.anchorMax = new Vector2(0.95f, 0.96f);
                titleRt.offsetMin = Vector2.zero;
                titleRt.offsetMax = Vector2.zero;
                txtTitle = titleGo.AddComponent<TextMeshProUGUI>();
                txtTitle.alignment = TextAlignmentOptions.Center;
                txtTitle.raycastTarget = false; // NÃO bloqueia raycasts

                // Resumo de Stats
                GameObject statsGo = new GameObject("txtStatsSummary");
                statsGo.transform.SetParent(cardGo.transform, false);
                RectTransform statsRt = statsGo.AddComponent<RectTransform>();
                statsRt.anchorMin = new Vector2(0.08f, 0.22f);
                statsRt.anchorMax = new Vector2(0.92f, 0.80f);
                statsRt.offsetMin = Vector2.zero;
                statsRt.offsetMax = Vector2.zero;
                txtStatsSummary = statsGo.AddComponent<TextMeshProUGUI>();
                txtStatsSummary.fontSize = 17f;
                txtStatsSummary.alignment = TextAlignmentOptions.Center;
                txtStatsSummary.raycastTarget = false; // NÃO bloqueia o botão de baixo

                // ÚNICO BOTÃO: Voltar à Base
                GameObject btnBaseGo = CreateButton(cardGo.transform, "btnReturnBase", "VOLTAR À BASE", new Vector2(0f, -230f));
                btnReturnBase = btnBaseGo.GetComponent<Button>();
                btnReturnBase.onClick.RemoveAllListeners();
                btnReturnBase.onClick.AddListener(OnReturnBaseClicked);

                deathPanel = panelGo;
            }
        }
    }

    private GameObject CreateButton(Transform parent, string name, string label, Vector2 pos)
    {
        GameObject btnGo = new GameObject(name);
        btnGo.transform.SetParent(parent, false);

        RectTransform rt = btnGo.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(240f, 50f);

        Image bg = btnGo.AddComponent<Image>();
        bg.color = new Color(0.20f, 0.20f, 0.28f, 1f);
        bg.raycastTarget = true; // O botão precisa receber o clique!

        Button b = btnGo.AddComponent<Button>();
        b.interactable = true;

        GameObject txtGo = new GameObject("Text");
        txtGo.transform.SetParent(btnGo.transform, false);
        RectTransform txtRt = txtGo.AddComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = Vector2.zero;
        txtRt.offsetMax = Vector2.zero;

        TextMeshProUGUI txt = txtGo.AddComponent<TextMeshProUGUI>();
        txt.text = $"<size=18><color=#ffffff><b>{label}</b></color></size>";
        txt.alignment = TextAlignmentOptions.Center;
        txt.raycastTarget = false; // O texto dentro do botão não intercepta o clique do próprio botão!

        return btnGo;
    }

    private static Canvas FindTargetPlayerCanvas()
    {
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach (var c in canvases)
        {
            if (c != null && c.gameObject.activeInHierarchy)
            {
                if (c.name.Contains("Player Canvas") || c.name.Contains("PlayerCanvas") || c.name.Contains("HUD"))
                {
                    return c;
                }
            }
        }
        foreach (var c in canvases)
        {
            if (c != null && c.gameObject.activeInHierarchy && c.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                return c;
            }
        }
        return canvases.Length > 0 ? canvases[0] : null;
    }
}
