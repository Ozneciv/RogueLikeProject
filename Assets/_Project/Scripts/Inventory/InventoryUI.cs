using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Controlador principal da UI do Inventário.
/// Cria e gerencia o painel visual de grade (estilo Minecraft).
/// Inclui input handler (Tab para abrir/fechar) e gerenciamento de cursor.
/// 
/// PERSISTÊNCIA:
///   Singleton com DontDestroyOnLoad — cria seu próprio Canvas persistente
///   e reconecta ao PlayerInventory automaticamente ao trocar de cena.
/// 
/// EXTENSÃO: Para alterar aparência, modifique as constantes de configuração.
/// </summary>
public class InventoryUI : MonoBehaviour
{
    // Singleton
    public static InventoryUI Instance { get; private set; }
    [Header("Input")]
    [Tooltip("Tecla para abrir/fechar o inventário")]
    public KeyCode toggleKey = KeyCode.Tab;

    [Header("Layout")]
    [Tooltip("Número de colunas na grade")]
    public int columns = 5;
    [Tooltip("Tamanho de cada slot em pixels")]
    public float slotSize = 72f;
    [Tooltip("Espaçamento entre slots")]
    public float slotSpacing = 6f;

    // Referências internas
    private PlayerInventory playerInventory;
    private Canvas inventoryCanvas;
    private GameObject canvasObject; // Canvas próprio (DontDestroyOnLoad)
    private GameObject panelObject;
    private RectTransform panelRect;
    private TextMeshProUGUI headerText;
    private InventoryTooltip tooltip;
    private List<InventorySlotUI> slots = new List<InventorySlotUI>();
    private ScrollRect scrollRect;
    private RectTransform contentRect;
    private RectTransform scrollViewRect;
    private Scrollbar verticalScrollbar;

    // Estado
    private bool isOpen = false;
    private bool uiBuilt = false;

    // Cores do painel
    private static readonly Color PANEL_BG = new Color(0.06f, 0.06f, 0.10f, 0.92f);
    private static readonly Color PANEL_BORDER = new Color(0.35f, 0.30f, 0.55f, 0.7f);
    private static readonly Color HEADER_COLOR = new Color(0.85f, 0.80f, 0.95f, 1f);
    private static readonly Color HEADER_ACCENT = new Color(0.6f, 0.45f, 0.90f, 1f);

    // Máximo de linhas visíveis sem scroll
    private const int MAX_VISIBLE_ROWS = 2;

    void Awake()
    {
        // Singleton: se já existe um InventoryUI, destroi este duplicado
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        // Conecta ao PlayerInventory
        ConnectToPlayerInventory();

        // Cria toda a UI (Canvas próprio, persistente)
        CreateInventoryUI();

        // Começa fechado
        panelObject.SetActive(false);
        isOpen = false;
        uiBuilt = true;
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnDestroy()
    {
        if (playerInventory != null)
        {
            playerInventory.onInventoryChanged.RemoveListener(RefreshUI);
        }

        // Limpa Canvas persistente se este é o singleton sendo destruído
        if (Instance == this)
        {
            Instance = null;
            if (canvasObject != null) Destroy(canvasObject);
        }
    }

    /// <summary>
    /// Chamado automaticamente quando uma nova cena carrega.
    /// Reconecta ao PlayerInventory (que é DontDestroyOnLoad) para manter o sistema funcionando.
    /// </summary>
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!uiBuilt) return; // Start() ainda não rodou

        ConnectToPlayerInventory();

        // Garante que o inventário esteja fechado ao trocar de cena
        if (isOpen)
        {
            isOpen = false;
            if (panelObject != null) panelObject.SetActive(false);
        }

        Debug.Log("[INVENTORY UI] Reconectado após carregar cena: " + scene.name);
    }

    /// <summary>
    /// Encontra e conecta ao PlayerInventory na cena.
    /// Remove listener antigo e adiciona novo para evitar duplicação.
    /// </summary>
    void ConnectToPlayerInventory()
    {
        // Remove listener antigo se existir
        if (playerInventory != null)
        {
            playerInventory.onInventoryChanged.RemoveListener(RefreshUI);
        }

        // Encontra o inventário do Player (pode ser DontDestroyOnLoad)
        playerInventory = FindObjectOfType<PlayerInventory>();

        if (playerInventory == null)
        {
            Debug.LogWarning("[INVENTORY UI] PlayerInventory não encontrado na cena!");
        }
        else
        {
            // Se inscreve para mudanças no inventário
            playerInventory.onInventoryChanged.AddListener(RefreshUI);
            Debug.Log("[INVENTORY UI] Conectado ao PlayerInventory.");
        }
    }

    void Update()
    {
        // Toggle com a tecla configurada
        if (Input.GetKeyDown(toggleKey))
        {
            if (isOpen)
                CloseInventory();
            else
                OpenInventory();
        }

        // ESC para fechar se estiver aberto
        if (isOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseInventory();
        }
    }

    /// <summary>
    /// Abre o painel do inventário
    /// </summary>
    public void OpenInventory()
    {
        if (isOpen) return;

        // Reconexão de segurança: se o PlayerInventory sumiu, tenta encontrar de novo
        if (playerInventory == null)
        {
            ConnectToPlayerInventory();
        }

        isOpen = true;
        panelObject.SetActive(true);

        // Mostra cursor para interagir com a UI
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        RefreshUI();

        Debug.Log("[INVENTORY UI] Inventário aberto");
    }

    /// <summary>
    /// Fecha o painel do inventário
    /// </summary>
    public void CloseInventory()
    {
        if (!isOpen) return;

        isOpen = false;
        panelObject.SetActive(false);

        // Esconde cursor e trava para gameplay
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        if (tooltip != null)
            tooltip.Hide();

        // Segue o líder: Se o fechar sumiu o inventário, esconde a tela de Fusão junto.
        InfusionUI telaDeUpgrades = Object.FindFirstObjectByType<InfusionUI>(FindObjectsInactive.Include);
        if (telaDeUpgrades != null) 
        {
            telaDeUpgrades.ClosePanel();
        }

        Debug.Log("[INVENTORY UI] Inventário fechado");
    }

    /// <summary>
    /// Retorna se o inventário está aberto
    /// </summary>
    public bool IsOpen()
    {
        return isOpen;
    }

    /// <summary>
    /// Atualiza todos os slots visuais com os dados atuais do inventário
    /// </summary>
    public void RefreshUI()
    {
        if (playerInventory == null) return;

        int maxSlots = playerInventory.MaxSlots;

        // Atualiza header
        headerText.text = "INVENTÁRIO  <color=#" + ColorUtility.ToHtmlStringRGB(HEADER_ACCENT) + ">"
            + playerInventory.OccupiedSlots + "/" + maxSlots + "</color>";

        // Recria slots se a quantidade mudou (ex: IncreaseMaxSlots)
        if (slots.Count != maxSlots)
        {
            RebuildSlots(maxSlots);
        }

        // Preenche os slots com dados do inventário
        Dictionary<string, int> items = playerInventory.GetAllItems();
        int slotIndex = 0;

        foreach (var kvp in items)
        {
            if (slotIndex >= slots.Count) break;

            ItemData itemData = null;
            if (ItemDatabase.Instance != null)
            {
                itemData = ItemDatabase.Instance.GetItemData(kvp.Key);
            }

            slots[slotIndex].SetItem(kvp.Key, kvp.Value, itemData);
            slotIndex++;
        }

        // Slots restantes ficam vazios
        for (int i = slotIndex; i < slots.Count; i++)
        {
            slots[i].SetEmpty();
        }
    }

    // ============================================================
    //  CRIAÇÃO DA UI
    // ============================================================

    void CreateInventoryUI()
    {
        // Cria Canvas próprio persistente (como EconomyHUD faz)
        canvasObject = new GameObject("InventoryUI_Canvas");
        inventoryCanvas = canvasObject.AddComponent<Canvas>();
        inventoryCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        inventoryCanvas.sortingOrder = 100;
        canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObject.AddComponent<GraphicRaycaster>();
        DontDestroyOnLoad(canvasObject);

        // Garante EventSystem
        if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        int uiLayer = gameObject.layer;
        int maxSlots = playerInventory != null ? playerInventory.MaxSlots : 10;
        int rows = Mathf.CeilToInt((float)maxSlots / columns);

        // Calcula tamanho do painel
        float gridWidth = columns * (slotSize + slotSpacing) - slotSpacing;
        float gridHeight = rows * (slotSize + slotSpacing) - slotSpacing;
        float panelPadding = 20f;
        float headerHeight = 44f;
        float totalWidth = gridWidth + panelPadding * 2;
        float totalHeight = gridHeight + panelPadding * 2 + headerHeight;

        // === Painel Principal ===
        panelObject = new GameObject("InventoryPanel");
        panelObject.transform.SetParent(inventoryCanvas.transform, false);
        panelObject.layer = uiLayer;

        panelRect = panelObject.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(totalWidth, totalHeight);
        panelRect.anchoredPosition = Vector2.zero;

        // Background do painel
        panelObject.AddComponent<CanvasRenderer>();
        Image panelBg = panelObject.AddComponent<Image>();
        panelBg.color = PANEL_BG;
        panelBg.raycastTarget = true;

        // === Borda do Painel ===
        GameObject panelBorderObj = new GameObject("PanelBorder");
        panelBorderObj.transform.SetParent(panelObject.transform, false);
        panelBorderObj.layer = uiLayer;

        RectTransform panelBorderRect = panelBorderObj.AddComponent<RectTransform>();
        panelBorderRect.anchorMin = Vector2.zero;
        panelBorderRect.anchorMax = Vector2.one;
        panelBorderRect.sizeDelta = new Vector2(4f, 4f);
        panelBorderRect.anchoredPosition = Vector2.zero;

        panelBorderObj.AddComponent<CanvasRenderer>();
        Image panelBorderImg = panelBorderObj.AddComponent<Image>();
        panelBorderImg.color = PANEL_BORDER;
        panelBorderImg.raycastTarget = false;
        panelBorderImg.type = Image.Type.Sliced;
        panelBorderImg.fillCenter = false;
        panelBorderObj.transform.SetAsFirstSibling();

        // === Acento Superior (barra fina no topo) ===
        GameObject accentObj = new GameObject("TopAccent");
        accentObj.transform.SetParent(panelObject.transform, false);
        accentObj.layer = uiLayer;

        RectTransform accentRect = accentObj.AddComponent<RectTransform>();
        accentRect.anchorMin = new Vector2(0.1f, 1f);
        accentRect.anchorMax = new Vector2(0.9f, 1f);
        accentRect.pivot = new Vector2(0.5f, 1f);
        accentRect.anchoredPosition = new Vector2(0f, 0f);
        accentRect.sizeDelta = new Vector2(0f, 3f);

        accentObj.AddComponent<CanvasRenderer>();
        Image accentImg = accentObj.AddComponent<Image>();
        accentImg.color = HEADER_ACCENT;
        accentImg.raycastTarget = false;

        // === Header ===
        GameObject headerObj = new GameObject("Header");
        headerObj.transform.SetParent(panelObject.transform, false);
        headerObj.layer = uiLayer;

        RectTransform headerRect = headerObj.AddComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0f, 1f);
        headerRect.anchorMax = new Vector2(1f, 1f);
        headerRect.pivot = new Vector2(0.5f, 1f);
        headerRect.anchoredPosition = new Vector2(0f, -8f);
        headerRect.sizeDelta = new Vector2(-panelPadding * 2, headerHeight);

        headerObj.AddComponent<CanvasRenderer>();
        headerText = headerObj.AddComponent<TextMeshProUGUI>();
        headerText.fontSize = 18f;
        headerText.fontStyle = FontStyles.Bold;
        headerText.color = HEADER_COLOR;
        headerText.alignment = TextAlignmentOptions.Center;
        headerText.raycastTarget = false;

        int occupiedSlots = playerInventory != null ? playerInventory.OccupiedSlots : 0;
        headerText.text = "INVENTÁRIO  <color=#" + ColorUtility.ToHtmlStringRGB(HEADER_ACCENT) + ">"
            + occupiedSlots + "/" + maxSlots + "</color>";

        // === ScrollView container ===
        float maxVisibleGridHeight = MAX_VISIBLE_ROWS * (slotSize + slotSpacing) - slotSpacing;
        float scrollViewHeight = Mathf.Min(gridHeight, maxVisibleGridHeight);

        // Recalcula tamanho do painel baseado na área visível
        totalHeight = scrollViewHeight + panelPadding * 2 + headerHeight;
        panelRect.sizeDelta = new Vector2(totalWidth, totalHeight);

        // ScrollView
        GameObject scrollViewObj = new GameObject("ScrollView");
        scrollViewObj.transform.SetParent(panelObject.transform, false);
        scrollViewObj.layer = uiLayer;

        scrollViewRect = scrollViewObj.AddComponent<RectTransform>();
        scrollViewRect.anchorMin = new Vector2(0.5f, 0.5f);
        scrollViewRect.anchorMax = new Vector2(0.5f, 0.5f);
        scrollViewRect.pivot = new Vector2(0.5f, 0.5f);
        scrollViewRect.sizeDelta = new Vector2(gridWidth, scrollViewHeight);
        scrollViewRect.anchoredPosition = new Vector2(0f, -headerHeight / 2f);

        scrollViewObj.AddComponent<CanvasRenderer>();
        Image scrollViewMask = scrollViewObj.AddComponent<Image>();
        scrollViewMask.color = new Color(0, 0, 0, 0.01f); // quase invisível, necessário pro mask
        scrollViewMask.raycastTarget = true;
        scrollViewObj.AddComponent<Mask>().showMaskGraphic = false;

        // Content (o grid real que rola)
        GameObject gridObj = new GameObject("SlotGrid");
        gridObj.transform.SetParent(scrollViewObj.transform, false);
        gridObj.layer = uiLayer;

        contentRect = gridObj.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0.5f, 1f);
        contentRect.anchorMax = new Vector2(0.5f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.sizeDelta = new Vector2(gridWidth, gridHeight);
        contentRect.anchoredPosition = Vector2.zero;

        GridLayoutGroup gridLayout = gridObj.AddComponent<GridLayoutGroup>();
        gridLayout.cellSize = new Vector2(slotSize, slotSize);
        gridLayout.spacing = new Vector2(slotSpacing, slotSpacing);
        gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
        gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
        gridLayout.childAlignment = TextAnchor.UpperLeft;
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = columns;

        ContentSizeFitter fitter = gridObj.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // ScrollRect
        scrollRect = scrollViewObj.AddComponent<ScrollRect>();
        scrollRect.content = contentRect;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 20f;
        scrollRect.viewport = scrollViewRect;

        // === Scrollbar Vertical ===
        bool needsScroll = rows > MAX_VISIBLE_ROWS;
        CreateScrollbar(panelObject.transform, uiLayer, scrollViewHeight, headerHeight);
        scrollRect.verticalScrollbar = verticalScrollbar;
        scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
        if (verticalScrollbar != null)
        {
            verticalScrollbar.gameObject.SetActive(needsScroll);
        }

        // === Tooltip (criado uma vez, reutilizado) ===
        GameObject tooltipObj = new GameObject("InventoryTooltip");
        tooltipObj.transform.SetParent(inventoryCanvas.transform, false);
        tooltipObj.layer = uiLayer;
        tooltip = tooltipObj.AddComponent<InventoryTooltip>();
        tooltip.Initialize(inventoryCanvas);
        // Tooltip deve ficar acima de tudo
        tooltipObj.transform.SetAsLastSibling();

        // === Cria os Slots ===
        BuildSlots(gridObj.transform, maxSlots);

        // === Botão de Fechar (X) ===
        CreateCloseButton(panelObject.transform, uiLayer);

        Debug.Log("[INVENTORY UI] UI criada com " + maxSlots + " slots (" + columns + "x" + rows + ")");
    }

    void BuildSlots(Transform gridParent, int count)
    {
        slots.Clear();

        for (int i = 0; i < count; i++)
        {
            GameObject slotObj = new GameObject("Slot_" + i);
            slotObj.transform.SetParent(gridParent, false);
            slotObj.layer = gameObject.layer;

            InventorySlotUI slot = slotObj.AddComponent<InventorySlotUI>();
            slot.Initialize(tooltip, slotSize);

            slots.Add(slot);
        }
    }

    void RebuildSlots(int newCount)
    {
        // Destrói slots antigos
        foreach (var slot in slots)
        {
            if (slot != null)
                Destroy(slot.gameObject);
        }
        slots.Clear();

        // Encontra o grid container (agora dentro do ScrollView)
        Transform scrollViewTransform = panelObject.transform.Find("ScrollView");
        if (scrollViewTransform == null)
        {
            Debug.LogError("[INVENTORY UI] ScrollView não encontrado para rebuild!");
            return;
        }

        Transform gridTransform = scrollViewTransform.Find("SlotGrid");
        if (gridTransform == null)
        {
            Debug.LogError("[INVENTORY UI] SlotGrid não encontrado para rebuild!");
            return;
        }

        // Recalcula tamanhos
        int rows = Mathf.CeilToInt((float)newCount / columns);
        float gridWidth = columns * (slotSize + slotSpacing) - slotSpacing;
        float gridHeight = rows * (slotSize + slotSpacing) - slotSpacing;
        float panelPadding = 20f;
        float headerHeight = 44f;
        float maxVisibleGridHeight = MAX_VISIBLE_ROWS * (slotSize + slotSpacing) - slotSpacing;
        float scrollViewHeight = Mathf.Min(gridHeight, maxVisibleGridHeight);

        // Atualiza tamanho do painel (mantém tamanho máximo fixo)
        float totalWidth = gridWidth + panelPadding * 2;
        float totalHeight = scrollViewHeight + panelPadding * 2 + headerHeight;
        panelRect.sizeDelta = new Vector2(totalWidth, totalHeight);

        // Atualiza tamanho do scroll view
        if (scrollViewRect != null)
        {
            scrollViewRect.sizeDelta = new Vector2(gridWidth, scrollViewHeight);
        }

        // Atualiza tamanho do content (grid real)
        if (contentRect != null)
        {
            contentRect.sizeDelta = new Vector2(gridWidth, gridHeight);
        }

        // Mostra/esconde scrollbar
        bool needsScroll = rows > MAX_VISIBLE_ROWS;
        if (verticalScrollbar != null)
        {
            verticalScrollbar.gameObject.SetActive(needsScroll);
        }

        // Cria novos slots
        BuildSlots(gridTransform, newCount);

        Debug.Log("[INVENTORY UI] Slots reconstruídos: " + newCount + " (" + columns + "x" + rows + ")");
    }

    void CreateScrollbar(Transform parent, int layer, float scrollViewHeight, float headerHeight)
    {
        float scrollbarWidth = 6f;
        float panelPadding = 20f;

        // Container da scrollbar
        GameObject scrollbarObj = new GameObject("VerticalScrollbar");
        scrollbarObj.transform.SetParent(parent, false);
        scrollbarObj.layer = layer;

        RectTransform scrollbarRect = scrollbarObj.AddComponent<RectTransform>();
        scrollbarRect.anchorMin = new Vector2(1f, 0.5f);
        scrollbarRect.anchorMax = new Vector2(1f, 0.5f);
        scrollbarRect.pivot = new Vector2(1f, 0.5f);
        scrollbarRect.sizeDelta = new Vector2(scrollbarWidth, scrollViewHeight);
        scrollbarRect.anchoredPosition = new Vector2(-panelPadding / 2f, -headerHeight / 2f);

        scrollbarObj.AddComponent<CanvasRenderer>();
        Image scrollbarBg = scrollbarObj.AddComponent<Image>();
        scrollbarBg.color = new Color(0.15f, 0.12f, 0.22f, 0.5f);
        scrollbarBg.raycastTarget = true;

        // Sliding Area
        GameObject slidingArea = new GameObject("SlidingArea");
        slidingArea.transform.SetParent(scrollbarObj.transform, false);
        slidingArea.layer = layer;

        RectTransform slidingRect = slidingArea.AddComponent<RectTransform>();
        slidingRect.anchorMin = Vector2.zero;
        slidingRect.anchorMax = Vector2.one;
        slidingRect.sizeDelta = Vector2.zero;
        slidingRect.anchoredPosition = Vector2.zero;

        // Handle (a barrinha que se move)
        GameObject handle = new GameObject("Handle");
        handle.transform.SetParent(slidingArea.transform, false);
        handle.layer = layer;

        RectTransform handleRect = handle.AddComponent<RectTransform>();
        handleRect.anchorMin = Vector2.zero;
        handleRect.anchorMax = new Vector2(1f, 0.3f);
        handleRect.sizeDelta = Vector2.zero;
        handleRect.anchoredPosition = Vector2.zero;

        handle.AddComponent<CanvasRenderer>();
        Image handleImg = handle.AddComponent<Image>();
        handleImg.color = new Color(0.6f, 0.45f, 0.90f, 0.6f);
        handleImg.raycastTarget = true;

        // Componente Scrollbar
        verticalScrollbar = scrollbarObj.AddComponent<Scrollbar>();
        verticalScrollbar.handleRect = handleRect;
        verticalScrollbar.targetGraphic = handleImg;
        verticalScrollbar.direction = Scrollbar.Direction.BottomToTop;

        // Cores do handle no hover
        ColorBlock colors = verticalScrollbar.colors;
        colors.normalColor = new Color(0.6f, 0.45f, 0.90f, 0.6f);
        colors.highlightedColor = new Color(0.7f, 0.55f, 1f, 0.8f);
        colors.pressedColor = new Color(0.5f, 0.35f, 0.80f, 0.9f);
        verticalScrollbar.colors = colors;
    }

    void CreateCloseButton(Transform parent, int layer)
    {
        GameObject btnObj = new GameObject("CloseButton");
        btnObj.transform.SetParent(parent, false);
        btnObj.layer = layer;

        RectTransform btnRect = btnObj.AddComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(1f, 1f);
        btnRect.anchorMax = new Vector2(1f, 1f);
        btnRect.pivot = new Vector2(1f, 1f);
        btnRect.anchoredPosition = new Vector2(-8f, -8f);
        btnRect.sizeDelta = new Vector2(28f, 28f);

        btnObj.AddComponent<CanvasRenderer>();
        Image btnBg = btnObj.AddComponent<Image>();
        btnBg.color = new Color(0.8f, 0.2f, 0.2f, 0.7f);
        btnBg.raycastTarget = true;

        Button btn = btnObj.AddComponent<Button>();
        btn.onClick.AddListener(CloseInventory);

        // Cores do botão no hover
        ColorBlock colors = btn.colors;
        colors.normalColor = new Color(0.8f, 0.2f, 0.2f, 0.7f);
        colors.highlightedColor = new Color(1f, 0.3f, 0.3f, 0.9f);
        colors.pressedColor = new Color(0.6f, 0.1f, 0.1f, 1f);
        btn.colors = colors;

        // Texto "X"
        GameObject xObj = new GameObject("X");
        xObj.transform.SetParent(btnObj.transform, false);
        xObj.layer = layer;

        RectTransform xRect = xObj.AddComponent<RectTransform>();
        xRect.anchorMin = Vector2.zero;
        xRect.anchorMax = Vector2.one;
        xRect.sizeDelta = Vector2.zero;
        xRect.anchoredPosition = Vector2.zero;

        xObj.AddComponent<CanvasRenderer>();
        TextMeshProUGUI xText = xObj.AddComponent<TextMeshProUGUI>();
        xText.text = "X";
        xText.fontSize = 16f;
        xText.color = Color.white;
        xText.alignment = TextAlignmentOptions.Center;
        xText.raycastTarget = false;
    }
}
