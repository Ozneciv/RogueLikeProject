using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// UI da Bolsa Sintética — exibe os recursos persistentes do jogador (returnsToBase = true).
/// Singleton com DontDestroyOnLoad. Toggle com tecla B (configurável).
///
/// FONTE DE DADOS : SaveManager.instance.GetAllBaseResources()
/// EVENTO         : SaveManager.OnBaseResourcesChanged → RefreshUI()
///
/// SETUP NA CENA  : Basta adicionar este script a qualquer GameObject na cena inicial.
///   O Canvas e todos os elementos visuais são criados inteiramente por código.
///   Design visual definitivo será substituído quando o layout chegar.
/// </summary>
public class SyntheticBagUI : MonoBehaviour
{
    // ─── Singleton ────────────────────────────────────────────────────────────
    public static SyntheticBagUI Instance { get; private set; }

    // ─── Inspector ────────────────────────────────────────────────────────────
    [Header("Input")]
    [Tooltip("Tecla para abrir/fechar a Bolsa Sintética")]
    public KeyCode toggleKey = KeyCode.B;

    [Header("Layout")]
    [Tooltip("Número de colunas na grade")]
    public int columns = 5;
    [Tooltip("Tamanho de cada slot em pixels")]
    public float slotSize = 72f;
    [Tooltip("Espaçamento entre slots")]
    public float slotSpacing = 6f;

    [Header("Prefab-Based UI (Optional)")]
    [Tooltip("Ative para usar a interface baseada em Prefabs desenhada na Unity. Se desativado, o inventário será gerado dinamicamente por script (Modo Provisório).")]
    public bool useCustomPrefabUI = false;
    [Tooltip("Painel principal da Bolsa Sintética no Prefab.")]
    public GameObject customPanel;
    [Tooltip("Transform que contém o Grid Layout Group da Bolsa no Prefab.")]
    public Transform customGridParent;
    [Tooltip("Prefab do Slot customizado.")]
    public GameObject customSlotPrefab;
    [Tooltip("Texto do cabeçalho customizado (TMP).")]
    public TextMeshProUGUI customHeaderText;

    // ─── Referências internas ─────────────────────────────────────────────────
    private Canvas      bagCanvas;
    private GameObject  canvasObject;
    private GameObject  panelObject;
    private TextMeshProUGUI headerText;
    private GameObject  slotGrid;
    private InventoryTooltip tooltip;
    private readonly List<InventorySlotUI> slots = new List<InventorySlotUI>();

    // ─── Estado ───────────────────────────────────────────────────────────────
    private bool isOpen   = false;
    private bool uiBuilt  = false;

    // ─── Paleta (unificada com o Inventário de Run) ───────────────────────────
    private static readonly Color PANEL_BG     = new Color(0.06f, 0.06f, 0.10f, 0.92f);
    private static readonly Color PANEL_BORDER = new Color(0.35f, 0.30f, 0.55f, 0.70f);
    private static readonly Color HEADER_COLOR = new Color(0.85f, 0.80f, 0.95f, 1.00f);

    // ─────────────────────────────────────────────────────────────────────────
    // Unity callbacks
    // ─────────────────────────────────────────────────────────────────────────

    void Awake()
    {
        Debug.Log($"[BAG UI Awake] Entrou. name: {gameObject.name} (ID: {gameObject.GetInstanceID()}). Instance atual: {(Instance != null ? Instance.gameObject.name + " (ID: " + Instance.gameObject.GetInstanceID() + ")" : "null")}");

        // Limpa referência estática "suja" caso o objeto da Unity tenha sido destruído
        if (Instance != null && Instance.gameObject == null)
        {
            Debug.Log("[BAG UI Awake] Detectada referência estática a objeto destruído. Limpando Instance...");
            Instance = null;
        }

        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"[BAG UI Awake] AUTO-DESTRUIÇÃO DETECTADA! Destruindo {gameObject.name} (ID: {gameObject.GetInstanceID()}) porque Instance já é {Instance.gameObject.name} (ID: {Instance.gameObject.GetInstanceID()})");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        Debug.Log($"[BAG UI Awake] {gameObject.name} (ID: {gameObject.GetInstanceID()}) registrado com sucesso como Instance.");
    }

    void Start()
    {
        CreateBagUI();
        panelObject.SetActive(false);
        uiBuilt = true;
    }

    void OnEnable()  => SaveManager.OnBaseResourcesChanged += RefreshUI;
    void OnDisable() => SaveManager.OnBaseResourcesChanged -= RefreshUI;

    void OnDestroy()
    {
        SaveManager.OnBaseResourcesChanged -= RefreshUI;
        if (Instance == this)
        {
            Instance = null;
            if (canvasObject != null) Destroy(canvasObject);
        }
    }

    void Update()
    {
        if (!uiBuilt) return;

        if (Input.GetKeyDown(toggleKey))
        {
            Debug.Log($"[BAG UI] Tecla de atalho detectada ({toggleKey}). isOpen atual: {isOpen}.");
            if (isOpen) CloseBag();
            else        OpenBag();
        }

        if (isOpen && Input.GetKeyDown(KeyCode.Escape))
            CloseBag();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // API pública
    // ─────────────────────────────────────────────────────────────────────────

    public void OpenBag()
    {
        if (!uiBuilt || isOpen) return;
        isOpen = true;
        panelObject.SetActive(true);
        RefreshUI();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;

        // Posiciona a Bolsa ao lado direito do painel customizado de Inventário (se houver) para evitar sobreposição
        if (customPanel == null && panelObject != null)
        {
            RectTransform bagRect = panelObject.GetComponent<RectTransform>();
            if (bagRect != null)
            {
                if (InventoryUI.Instance != null && InventoryUI.Instance.useCustomPrefabUI && InventoryUI.Instance.customPanel != null)
                {
                    RectTransform invRect = InventoryUI.Instance.customPanel.GetComponent<RectTransform>();
                    if (invRect != null)
                    {
                        // Posiciona exatamente à direita do painel do inventário + 20px de espaçamento
                        float offset = (invRect.rect.width / 2f) + (bagRect.rect.width / 2f) + 20f;
                        bagRect.anchoredPosition = new Vector2(invRect.anchoredPosition.x + offset, invRect.anchoredPosition.y);
                    }
                }
                else
                {
                    // Fallback para quando o inventário também é por código
                    bagRect.anchoredPosition = new Vector2(220f, -20f);
                }
            }
        }

        // Sincroniza com o Inventário de Run
        if (InventoryUI.Instance != null && !InventoryUI.Instance.IsOpen())
        {
            InventoryUI.Instance.OpenInventory();
        }

        Debug.Log("[BAG UI] Bolsa Sintética aberta.");
    }

    public void CloseBag()
    {
        if (!isOpen) return;
        isOpen = false;
        panelObject.SetActive(false);
        if (tooltip != null) tooltip.Hide();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;

        // Sincroniza com o Inventário de Run
        if (InventoryUI.Instance != null && InventoryUI.Instance.IsOpen())
        {
            InventoryUI.Instance.CloseInventory();
        }

        Debug.Log("[BAG UI] Bolsa Sintética fechada.");
    }

    public bool IsOpen() => isOpen;

    // ─────────────────────────────────────────────────────────────────────────
    // Refresh
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Reconstrói os slots com os dados atuais da Bolsa Sintética.
    /// Chamado automaticamente por SaveManager.OnBaseResourcesChanged.
    /// </summary>
    public void RefreshUI()
    {
        if (!uiBuilt) return;

        List<ItemSaveEntry> resources = SaveManager.instance != null
            ? SaveManager.instance.GetAllBaseResources()
            : new List<ItemSaveEntry>();

        // Atualiza header
        if (headerText != null)
            headerText.text = $"BOLSA SINTÉTICA  <color=#9F8FDF>{resources.Count}</color> tipo(s)";

        Transform gridParent = customGridParent != null ? customGridParent : (slotGrid != null ? slotGrid.transform : null);
        if (gridParent == null) return;

        // Destroi slots antigos
        foreach (Transform child in gridParent)
            Destroy(child.gameObject);
        slots.Clear();

        // Cria um slot por tipo de recurso
        foreach (ItemSaveEntry entry in resources)
        {
            ItemData itemData = ItemDatabase.Instance != null
                ? ItemDatabase.Instance.GetItemData(entry.itemId)
                : null;

            GameObject slotObj;
            if (customSlotPrefab != null)
            {
                slotObj = Instantiate(customSlotPrefab, gridParent);
            }
            else
            {
                slotObj = new GameObject("Slot_" + entry.itemId);
                slotObj.transform.SetParent(gridParent, false);
                slotObj.layer = canvasObject != null ? canvasObject.layer : gameObject.layer;
            }

            InventorySlotUI slot = slotObj.GetComponent<InventorySlotUI>();
            if (slot == null) slot = slotObj.AddComponent<InventorySlotUI>();
            slot.Initialize(tooltip, slotSize);
            slot.SetItem(entry.itemId, entry.quantity, itemData);
            slots.Add(slot);
        }

        // Redimensiona o grid para caber os slots (apenas se for gerado por código)
        if (customPanel == null)
        {
            ResizeGrid(resources.Count);
        }

        Debug.Log($"[BAG UI] Atualizada — {resources.Count} recurso(s) exibido(s).");
    }

    private void ResizeGrid(int count)
    {
        if (slotGrid == null) return;
        RectTransform gridRect = slotGrid.GetComponent<RectTransform>();
        if (gridRect == null) return;

        int rows  = Mathf.Max(1, Mathf.CeilToInt((float)count / columns));
        float w   = columns * (slotSize + slotSpacing) + slotSpacing;
        float h   = rows    * (slotSize + slotSpacing) + slotSpacing;
        gridRect.sizeDelta = new Vector2(w, h);

        // Ajusta painel para conter o grid + header + margens
        if (panelObject != null)
        {
            RectTransform panelRect = panelObject.GetComponent<RectTransform>();
            if (panelRect != null)
                panelRect.sizeDelta = new Vector2(w + 32f, h + 64f);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Construção da UI
    // ─────────────────────────────────────────────────────────────────────────

    private void CreateBagUI()
    {
        /*
         * =================================================================================
         * COMO ATIVAR A INTERFACE DA BOLSA POR PREFAB DEPOIS:
         * 1. No Inspector do prefab 'InventorySystem', marque a caixa 'Use Custom Prefab UI' como TRUE.
         * 2. Arraste manualmente as referências para:
         *    - Custom Panel (o GameObject do seu painel da Bolsa Sintética)
         *    - Custom Grid Parent (o GameObject com o Grid Layout Group onde os slots vão ficar)
         *    - Custom Slot Prefab (o prefab do seu slot individual, ex: InventorySlotPrefab.prefab)
         *    - Custom Header Text (o texto TMP para o título)
         * =================================================================================
         */
        if (useCustomPrefabUI && customPanel != null)
        {
            panelObject = customPanel;
            headerText = customHeaderText;
            uiBuilt = true;
            return;
        }

        // === Canvas persistente ===
        canvasObject = new GameObject("SyntheticBag_Canvas");
        canvasObject.transform.SetParent(transform, false);
        DontDestroyOnLoad(canvasObject);

        bagCanvas = canvasObject.AddComponent<Canvas>();
        bagCanvas.renderMode  = RenderMode.ScreenSpaceOverlay;
        bagCanvas.sortingOrder = 101; // Acima do InventoryUI (100)

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight  = 0.5f;

        // Garante EventSystem
        if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        // === Tooltip (deve ser criado antes dos slots) ===
        GameObject tooltipObj = new GameObject("BagTooltip");
        tooltipObj.transform.SetParent(bagCanvas.transform, false);
        tooltip = tooltipObj.AddComponent<InventoryTooltip>();
        tooltip.Initialize(bagCanvas);

        // === Painel principal ===
        panelObject = new GameObject("BagPanel");
        panelObject.transform.SetParent(bagCanvas.transform, false);

        RectTransform panelRect = panelObject.AddComponent<RectTransform>();
        panelRect.anchorMin       = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax       = new Vector2(0.5f, 0.5f);
        panelRect.pivot           = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = new Vector2(220f, -20f);

        float initW = columns * (slotSize + slotSpacing) + slotSpacing + 32f;
        panelRect.sizeDelta = new Vector2(initW, 300f);

        Image panelBg = panelObject.AddComponent<Image>();
        panelBg.color         = PANEL_BG;
        panelBg.raycastTarget = true;

        // Borda do painel
        GameObject borderObj  = new GameObject("PanelBorder");
        borderObj.transform.SetParent(panelObject.transform, false);
        RectTransform borderR = borderObj.AddComponent<RectTransform>();
        borderR.anchorMin       = Vector2.zero;
        borderR.anchorMax       = Vector2.one;
        borderR.sizeDelta       = new Vector2(4f, 4f);
        borderR.anchoredPosition = Vector2.zero;
        Image borderImg = borderObj.AddComponent<Image>();
        borderImg.color         = PANEL_BORDER;
        borderImg.raycastTarget = false;

        // === Header ===
        GameObject headerObj = new GameObject("BagHeader");
        headerObj.transform.SetParent(panelObject.transform, false);
        RectTransform headerR = headerObj.AddComponent<RectTransform>();
        headerR.anchorMin       = new Vector2(0f, 1f);
        headerR.anchorMax       = new Vector2(1f, 1f);
        headerR.pivot           = new Vector2(0.5f, 1f);
        headerR.anchoredPosition = new Vector2(0f, -10f);
        headerR.sizeDelta       = new Vector2(-16f, 32f);

        headerText = headerObj.AddComponent<TextMeshProUGUI>();
        headerText.text      = "◆ BOLSA SINTÉTICA";
        headerText.fontSize  = 13f;
        headerText.fontStyle = FontStyles.Bold;
        headerText.alignment = TextAlignmentOptions.Center;
        headerText.color     = HEADER_COLOR;
        headerText.raycastTarget = false;

        // === Grade de slots ===
        slotGrid = new GameObject("SlotGrid");
        slotGrid.transform.SetParent(panelObject.transform, false);

        RectTransform gridRect = slotGrid.AddComponent<RectTransform>();
        gridRect.anchorMin       = new Vector2(0.5f, 0.5f);
        gridRect.anchorMax       = new Vector2(0.5f, 0.5f);
        gridRect.pivot           = new Vector2(0.5f, 0.5f);
        gridRect.anchoredPosition = new Vector2(0f, -16f);

        GridLayoutGroup grid = slotGrid.AddComponent<GridLayoutGroup>();
        grid.cellSize        = new Vector2(slotSize, slotSize);
        grid.spacing         = new Vector2(slotSpacing, slotSpacing);
        grid.padding         = new RectOffset(
            (int)slotSpacing, (int)slotSpacing,
            (int)slotSpacing, (int)slotSpacing);
        grid.startCorner     = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis       = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment  = TextAnchor.UpperLeft;
        grid.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = columns;

        // Tamanho inicial do grid (vazio)
        ResizeGrid(0);
    }
}
