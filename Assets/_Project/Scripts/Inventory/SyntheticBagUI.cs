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

    // ─── Paleta (placeholder — será substituída pelo design definitivo) ───────
    private static readonly Color PANEL_BG     = new Color(0.06f, 0.08f, 0.06f, 0.93f);
    private static readonly Color PANEL_BORDER = new Color(0.30f, 0.60f, 0.30f, 0.75f);
    private static readonly Color HEADER_COLOR = new Color(0.55f, 0.95f, 0.55f, 1.00f);

    // ─────────────────────────────────────────────────────────────────────────
    // Unity callbacks
    // ─────────────────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
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
            headerText.text = $"◆ BOLSA SINTÉTICA  <color=#7FBF7F>{resources.Count}</color> tipo(s)";

        // Destroi slots antigos
        foreach (Transform child in slotGrid.transform)
            Destroy(child.gameObject);
        slots.Clear();

        // Cria um slot por tipo de recurso
        foreach (ItemSaveEntry entry in resources)
        {
            ItemData itemData = ItemDatabase.Instance != null
                ? ItemDatabase.Instance.GetItemData(entry.itemId)
                : null;

            GameObject slotObj = new GameObject("Slot_" + entry.itemId);
            slotObj.transform.SetParent(slotGrid.transform, false);
            slotObj.layer = canvasObject.layer;

            InventorySlotUI slot = slotObj.AddComponent<InventorySlotUI>();
            slot.Initialize(tooltip, slotSize);
            slot.SetItem(entry.itemId, entry.quantity, itemData);
            slots.Add(slot);
        }

        // Redimensiona o grid para caber os slots
        ResizeGrid(resources.Count);

        Debug.Log($"[BAG UI] Atualizada — {resources.Count} recurso(s) exibido(s).");
    }

    private void ResizeGrid(int count)
    {
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

        canvasObject.AddComponent<GraphicRaycaster>();

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
        panelRect.anchoredPosition = Vector2.zero;

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
