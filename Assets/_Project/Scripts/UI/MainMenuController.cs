using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Controlador Fail-Proof dos 4 Botões do Menu Principal (JOGAR, OPÇÕES, CRÉDITOS, SAIR).
/// Garante que os cliques funcionem independentemente de nomes de objetos ou inspetores desconfigurados.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [Header("Configuração de Navegação")]
    [Tooltip("Nome da cena inicial da gameplay")]
    public string playSceneName = "Base";

    [Header("Referências Diretas de Botões (Opcional)")]
    public Button playButton;
    public Button optionsButton;
    public Button creditsButton;
    public Button quitButton;

    [Header("Painéis de UI (Opcional)")]
    public GameObject mainButtonsPanel;
    public GameObject optionsPanel;
    public GameObject creditsPanel;

    private void Awake()
    {
        AutoConnectButtons();
    }

    private void Start()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        Time.timeScale = 1f;

        AutoConnectButtons();
        EnsurePanelsExist();
        ShowMainPanel();
    }

    public void AutoConnectButtons()
    {
        // 1. Se os botões foram arrastados diretamente no Inspector, conecta-os
        if (playButton != null) { playButton.onClick.RemoveAllListeners(); playButton.onClick.AddListener(OnPlayClicked); }
        if (optionsButton != null) { optionsButton.onClick.RemoveAllListeners(); optionsButton.onClick.AddListener(OnOptionsClicked); }
        if (creditsButton != null) { creditsButton.onClick.RemoveAllListeners(); creditsButton.onClick.AddListener(OnCreditsClicked); }
        if (quitButton != null) { quitButton.onClick.RemoveAllListeners(); quitButton.onClick.AddListener(OnQuitClicked); }

        // 2. Procura todos os botões na cena
        Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Debug.Log($"[MAIN MENU CONTROLLER] Encontrados {buttons.Length} botões na cena.");

        for (int i = 0; i < buttons.Length; i++)
        {
            Button b = buttons[i];
            if (b == null) continue;

            string n = b.gameObject.name.ToLower();
            TextMeshProUGUI tmp = b.GetComponentInChildren<TextMeshProUGUI>(true);
            Text legacyTxt = b.GetComponentInChildren<Text>(true);
            string rawText = tmp != null ? tmp.text : (legacyTxt != null ? legacyTxt.text : "");
            string t = System.Text.RegularExpressions.Regex.Replace(rawText, "<.*?>", "").Trim().ToLower();

            // Desativa outros ouvintes conflitantes antigos
            b.onClick.RemoveAllListeners();

            if (n.Contains("play") || n.Contains("jogar") || t.Contains("jogar") || t.Contains("play") || i == 0)
            {
                b.onClick.AddListener(OnPlayClicked);
                Debug.Log($"[MAIN MENU CONTROLLER] Botão '{b.name}' ({t}) conectado a -> OnPlayClicked");
            }
            else if (n.Contains("option") || n.Contains("opcao") || n.Contains("opções") || t.Contains("opções") || t.Contains("opcoes") || t.Contains("options") || i == 1)
            {
                b.onClick.AddListener(OnOptionsClicked);
                Debug.Log($"[MAIN MENU CONTROLLER] Botão '{b.name}' ({t}) conectado a -> OnOptionsClicked");
            }
            else if (n.Contains("credit") || n.Contains("crédito") || n.Contains("creditos") || t.Contains("créditos") || t.Contains("creditos") || i == 2)
            {
                b.onClick.AddListener(OnCreditsClicked);
                Debug.Log($"[MAIN MENU CONTROLLER] Botão '{b.name}' ({t}) conectado a -> OnCreditsClicked");
            }
            else if (n.Contains("quit") || n.Contains("exit") || n.Contains("sair") || t.Contains("sair") || t.Contains("quit") || i == 3)
            {
                b.onClick.AddListener(OnQuitClicked);
                Debug.Log($"[MAIN MENU CONTROLLER] Botão '{b.name}' ({t}) conectado a -> OnQuitClicked");
            }
        }
    }

    // ═══════════════════════════════════════
    //  AÇÕES DOS 4 BOTÕES PRINCIPAIS
    // ═══════════════════════════════════════

    /// <summary>
    /// 1. JOGAR -> Carrega a cena da Base!
    /// </summary>
    public void OnPlayClicked()
    {
        Debug.LogWarning(">>> [MAIN MENU] BOTÃO JOGAR CLICADO! CARREGANDO CENA 'Base'... <<<");
        Time.timeScale = 1f;

        try
        {
            SceneManager.LoadScene(playSceneName);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[MAIN MENU ERROR] Erro ao carregar cena '{playSceneName}': {ex.Message}. Tentando carregar por índice 1...");
            SceneManager.LoadScene(1);
        }
    }

    /// <summary>
    /// 2. OPÇÕES -> Abre o painel de opções
    /// </summary>
    public void OnOptionsClicked()
    {
        Debug.LogWarning(">>> [MAIN MENU] BOTÃO OPÇÕES CLICADO! <<<");
        if (mainButtonsPanel != null) mainButtonsPanel.SetActive(false);
        if (creditsPanel != null) creditsPanel.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(true);
    }

    /// <summary>
    /// 3. CRÉDITOS -> Abre o painel de créditos
    /// </summary>
    public void OnCreditsClicked()
    {
        Debug.LogWarning(">>> [MAIN MENU] BOTÃO CRÉDITOS CLICADO! <<<");
        if (mainButtonsPanel != null) mainButtonsPanel.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(false);
        if (creditsPanel != null) creditsPanel.SetActive(true);
    }

    /// <summary>
    /// 4. SAIR -> Fecha o jogo
    /// </summary>
    public void OnQuitClicked()
    {
        Debug.LogWarning(">>> [MAIN MENU] BOTÃO SAIR CLICADO! <<<");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    /// <summary>
    /// VOLTAR -> Retorna ao painel principal de botões
    /// </summary>
    public void ShowMainPanel()
    {
        if (mainButtonsPanel != null) mainButtonsPanel.SetActive(true);
        if (optionsPanel != null) optionsPanel.SetActive(false);
        if (creditsPanel != null) creditsPanel.SetActive(false);
    }

    // ═══════════════════════════════════════
    //  CRIAÇÃO AUTOMÁTICA DE PAINÉIS SE NULOS
    // ═══════════════════════════════════════

    private void EnsurePanelsExist()
    {
        Canvas targetCanvas = GetComponentInParent<Canvas>();
        if (targetCanvas == null) targetCanvas = Object.FindFirstObjectByType<Canvas>();
        if (targetCanvas == null) return;

        if (mainButtonsPanel == null)
        {
            Transform container = targetCanvas.transform.Find("RightPanel/ButtonsContainer");
            if (container == null) container = targetCanvas.transform.Find("RightPanel");
            if (container != null) mainButtonsPanel = container.gameObject;
        }

        if (optionsPanel == null)
        {
            optionsPanel = CreateOptionsSubPanel(targetCanvas.transform);
        }

        if (creditsPanel == null)
        {
            creditsPanel = CreateCreditsSubPanel(targetCanvas.transform);
        }
    }

    private GameObject CreateOptionsSubPanel(Transform parent)
    {
        GameObject panelObj = new GameObject("OptionsPanel_AutoGenerated");
        panelObj.transform.SetParent(parent, false);

        RectTransform rt = panelObj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image bg = panelObj.AddComponent<Image>();
        bg.color = new Color(0.04f, 0.04f, 0.08f, 0.96f);

        // Título OPÇÕES
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(panelObj.transform, false);
        RectTransform titleRt = titleObj.AddComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0.1f, 0.82f);
        titleRt.anchorMax = new Vector2(0.9f, 0.95f);
        TextMeshProUGUI titleTxt = titleObj.AddComponent<TextMeshProUGUI>();
        titleTxt.text = "<size=36><color=#ffffff><b>OPÇÕES</b></color></size>";
        titleTxt.alignment = TextAlignmentOptions.Center;

        // Conteúdo
        GameObject bodyObj = new GameObject("Body");
        bodyObj.transform.SetParent(panelObj.transform, false);
        RectTransform bodyRt = bodyObj.AddComponent<RectTransform>();
        bodyRt.anchorMin = new Vector2(0.15f, 0.25f);
        bodyRt.anchorMax = new Vector2(0.85f, 0.78f);
        TextMeshProUGUI bodyTxt = bodyObj.AddComponent<TextMeshProUGUI>();
        bodyTxt.text =
            "<color=#ffcc00><b>ÁUDIO:</b></color>\n" +
            "Volume Geral: <color=#00ff99>100%</color>\n" +
            "Música & Efeitos: <color=#00ff99>100%</color>\n\n" +
            "<color=#ffcc00><b>VÍDEO:</b></color>\n" +
            "Modo: <color=#ffffff>Tela Cheia</color>\n" +
            "Qualidade: <color=#ffffff>Alta</color>";
        bodyTxt.fontSize = 20f;
        bodyTxt.alignment = TextAlignmentOptions.Center;

        // Botão VOLTAR
        CreateBackButton(panelObj.transform, () => ShowMainPanel());

        panelObj.SetActive(false);
        return panelObj;
    }

    private GameObject CreateCreditsSubPanel(Transform parent)
    {
        GameObject panelObj = new GameObject("CreditsPanel_AutoGenerated");
        panelObj.transform.SetParent(parent, false);

        RectTransform rt = panelObj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image bg = panelObj.AddComponent<Image>();
        bg.color = new Color(0.04f, 0.04f, 0.08f, 0.96f);

        // Título CRÉDITOS
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(panelObj.transform, false);
        RectTransform titleRt = titleObj.AddComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0.1f, 0.82f);
        titleRt.anchorMax = new Vector2(0.9f, 0.95f);
        TextMeshProUGUI titleTxt = titleObj.AddComponent<TextMeshProUGUI>();
        titleTxt.text = "<size=36><color=#ffffff><b>CRÉDITOS</b></color></size>";
        titleTxt.alignment = TextAlignmentOptions.Center;

        // Conteúdo
        GameObject bodyObj = new GameObject("Body");
        bodyObj.transform.SetParent(panelObj.transform, false);
        RectTransform bodyRt = bodyObj.AddComponent<RectTransform>();
        bodyRt.anchorMin = new Vector2(0.15f, 0.25f);
        bodyRt.anchorMax = new Vector2(0.85f, 0.78f);
        TextMeshProUGUI bodyTxt = bodyObj.AddComponent<TextMeshProUGUI>();
        bodyTxt.text =
            "<size=22><color=#ffaa00><b>LEAVING PLEROTUS</b></color></size>\n" +
            "<size=14><color=#88ccff>Desenvolvido por EPTA Entertainment</color></size>\n\n" +
            "<color=#ffffff>Design, Programação & Arte 2D</color>\n" +
            "<color=#ffd700>Equipe EPTA</color>\n\n" +
            "<size=12><color=#888888>Versão v1.0 — Todos os direitos reservados.</color></size>";
        bodyTxt.alignment = TextAlignmentOptions.Center;

        // Botão VOLTAR
        CreateBackButton(panelObj.transform, () => ShowMainPanel());

        panelObj.SetActive(false);
        return panelObj;
    }

    private void CreateBackButton(Transform parent, UnityEngine.Events.UnityAction onClick)
    {
        GameObject btnGo = new GameObject("Btn_Voltar");
        btnGo.transform.SetParent(parent, false);

        RectTransform rt = btnGo.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.12f);
        rt.anchorMax = new Vector2(0.5f, 0.12f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(220f, 48f);

        Image bg = btnGo.AddComponent<Image>();
        bg.color = new Color(0.20f, 0.20f, 0.28f, 1f);

        Button b = btnGo.AddComponent<Button>();
        b.interactable = true;
        b.onClick.AddListener(onClick);

        GameObject txtGo = new GameObject("Text");
        txtGo.transform.SetParent(btnGo.transform, false);
        RectTransform txtRt = txtGo.AddComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;

        TextMeshProUGUI txt = txtGo.AddComponent<TextMeshProUGUI>();
        txt.text = "<size=18><color=#ffffff><b>VOLTAR</b></color></size>";
        txt.alignment = TextAlignmentOptions.Center;
    }
}
