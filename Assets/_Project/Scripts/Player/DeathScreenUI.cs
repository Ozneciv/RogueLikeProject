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
    public Button btnRestart;
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

        if (btnRestart != null)
        {
            btnRestart.onClick.RemoveAllListeners();
            btnRestart.onClick.AddListener(OnRestartClicked);
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
            txtTitle.text = "<color=#ff2233><b>VOCÊ MORREU</b></color>";
        }

        if (txtStatsSummary != null && RunStatsManager.Instance != null)
        {
            RunStatsManager s = RunStatsManager.Instance;
            string timeStr = s.FormatTime(s.survivalTimer);
            string dmgDealtStr = s.FormatNumber(s.totalDamageDealt);
            string dmgTakenStr = s.FormatNumber(s.totalDamageTaken);

            txtStatsSummary.text =
                $"<size=22><color=#ffd700><b>✦ ESTATÍSTICAS DA SUA RUN ✦</b></color></size>\n\n" +
                $"<color=#88ccff>⏱️ <b>Tempo Sobrevivido:</b></color> <color=#ffffff>{timeStr}</color>\n" +
                $"<color=#ffaa44>⚔️ <b>Dano Total Causado:</b></color> <color=#ffffff>{dmgDealtStr}</color>\n" +
                $"<color=#ff4455>💀 <b>Inimigos Derrotados:</b></color> <color=#ffffff>{s.totalMobsKilled}</color>\n" +
                $"<color=#00ff99>💎 <b>Essências Coletadas:</b></color> <color=#ffffff>{s.totalEssenceCollected}</color>\n" +
                $"<color=#ffcc00>🪙 <b>Essências Gastas:</b></color> <color=#ffffff>{s.totalEssenceSpent}</color>\n" +
                $"<color=#ff6666>🩸 <b>Dano Recebido:</b></color> <color=#ffffff>{dmgTakenStr}</color>\n" +
                $"<color=#cc88ff>💀 <b>Local da Morte:</b></color> <color=#ffffff>{s.deathStage}</color>";
        }

        Time.timeScale = 0f; // Pausa o tempo do jogo enquanto a tela de morte está ativa
    }

    private void OnRestartClicked()
    {
        Time.timeScale = 1f;
        if (RunStatsManager.Instance != null)
        {
            RunStatsManager.Instance.ResetStats();
        }
        if (RunManager.instance != null)
        {
            RunManager.instance.StartNewRun();
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
        else
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
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

                RectTransform panelRt = panelGo.AddComponent<RectTransform>();
                panelRt.anchorMin = Vector2.zero;
                panelRt.anchorMax = Vector2.one;
                panelRt.offsetMin = Vector2.zero;
                panelRt.offsetMax = Vector2.zero;

                Image bg = panelGo.AddComponent<Image>();
                bg.color = new Color(0.02f, 0.02f, 0.04f, 0.92f); // Fundo escuro fosco

                // Título
                GameObject titleGo = new GameObject("txtTitle");
                titleGo.transform.SetParent(panelGo.transform, false);
                RectTransform titleRt = titleGo.AddComponent<RectTransform>();
                titleRt.anchorMin = new Vector2(0.1f, 0.80f);
                titleRt.anchorMax = new Vector2(0.9f, 0.95f);
                txtTitle = titleGo.AddComponent<TextMeshProUGUI>();
                txtTitle.fontSize = 42f;
                txtTitle.fontStyle = FontStyles.Bold;
                txtTitle.alignment = TextAlignmentOptions.Center;

                // Resumo de Stats
                GameObject statsGo = new GameObject("txtStatsSummary");
                statsGo.transform.SetParent(panelGo.transform, false);
                RectTransform statsRt = statsGo.AddComponent<RectTransform>();
                statsRt.anchorMin = new Vector2(0.15f, 0.25f);
                statsRt.anchorMax = new Vector2(0.85f, 0.78f);
                txtStatsSummary = statsGo.AddComponent<TextMeshProUGUI>();
                txtStatsSummary.fontSize = 20f;
                txtStatsSummary.lineSpacing = 12f;
                txtStatsSummary.alignment = TextAlignmentOptions.Center;

                // Botão Reiniciar
                GameObject btnRestGo = CreateButton(panelGo.transform, "btnRestart", "🔄 REINICIAR RUN", new Vector2(-120f, -220f));
                btnRestart = btnRestGo.GetComponent<Button>();

                // Botão Voltar Base
                GameObject btnBaseGo = CreateButton(panelGo.transform, "btnReturnBase", "🏰 VOLTAR À BASE", new Vector2(120f, -220f));
                btnReturnBase = btnBaseGo.GetComponent<Button>();

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
        rt.sizeDelta = new Vector2(210f, 50f);

        Image bg = btnGo.AddComponent<Image>();
        bg.color = new Color(0.15f, 0.15f, 0.22f, 1f);

        Button b = btnGo.AddComponent<Button>();

        GameObject txtGo = new GameObject("Text");
        txtGo.transform.SetParent(btnGo.transform, false);
        RectTransform txtRt = txtGo.AddComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;

        TextMeshProUGUI txt = txtGo.AddComponent<TextMeshProUGUI>();
        txt.text = $"<b>{label}</b>";
        txt.fontSize = 18f;
        txt.color = Color.white;
        txt.alignment = TextAlignmentOptions.Center;

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
