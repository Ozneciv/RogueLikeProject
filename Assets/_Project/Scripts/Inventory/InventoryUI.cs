using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Controlador principal da UI do Inventário.
/// Cria e gerencia o painel visual de grade (estilo Minecraft).
/// Inclui input handler (Tab para abrir/fechar) e gerenciamento de cursor.
/// 
/// SETUP:
/// 1. Adicione a um GameObject com Canvas (ou filho de Canvas)
/// 2. O script encontra automaticamente o PlayerInventory na cena
/// 3. Cria toda a UI por código
/// 
/// EXTENSÃO: Para alterar aparência, modifique as constantes de configuração.
/// </summary>
public class InventoryUI : MonoBehaviour
{
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
    private GameObject panelObject;
    private RectTransform panelRect;
    private TextMeshProUGUI headerText;
    private InventoryTooltip tooltip;
    private List<InventorySlotUI> slots = new List<InventorySlotUI>();

    // Estado
    private bool isOpen = false;

    // Cores do painel
    private static readonly Color PANEL_BG = new Color(0.06f, 0.06f, 0.10f, 0.92f);
    private static readonly Color PANEL_BORDER = new Color(0.35f, 0.30f, 0.55f, 0.7f);
    private static readonly Color HEADER_COLOR = new Color(0.85f, 0.80f, 0.95f, 1f);
    private static readonly Color HEADER_ACCENT = new Color(0.6f, 0.45f, 0.90f, 1f);

    void Start()
    {
        // Encontra o inventário do Player
        playerInventory = FindObjectOfType<PlayerInventory>();

        if (playerInventory == null)
        {
            Debug.LogWarning("[INVENTORY UI] PlayerInventory não encontrado na cena!");
        }
        else
        {
            // Se inscreve para mudanças no inventário
            playerInventory.onInventoryChanged.AddListener(RefreshUI);
        }

        // Cria toda a UI
        CreateInventoryUI();

        // Começa fechado
        panelObject.SetActive(false);
        isOpen = false;
    }

    void OnDestroy()
    {
        if (playerInventory != null)
        {
            playerInventory.onInventoryChanged.RemoveListener(RefreshUI);
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
        // Garante que tem um Canvas
        inventoryCanvas = GetComponentInParent<Canvas>();
        if (inventoryCanvas == null)
        {
            inventoryCanvas = gameObject.AddComponent<Canvas>();
            inventoryCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            inventoryCanvas.sortingOrder = 100;
            gameObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            gameObject.AddComponent<GraphicRaycaster>();
        }

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

        // === Container da Grade ===
        GameObject gridObj = new GameObject("SlotGrid");
        gridObj.transform.SetParent(panelObject.transform, false);
        gridObj.layer = uiLayer;

        RectTransform gridRect = gridObj.AddComponent<RectTransform>();
        gridRect.anchorMin = new Vector2(0.5f, 0.5f);
        gridRect.anchorMax = new Vector2(0.5f, 0.5f);
        gridRect.pivot = new Vector2(0.5f, 0.5f);
        gridRect.sizeDelta = new Vector2(gridWidth, gridHeight);
        gridRect.anchoredPosition = new Vector2(0f, -headerHeight / 2f);

        GridLayoutGroup gridLayout = gridObj.AddComponent<GridLayoutGroup>();
        gridLayout.cellSize = new Vector2(slotSize, slotSize);
        gridLayout.spacing = new Vector2(slotSpacing, slotSpacing);
        gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
        gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
        gridLayout.childAlignment = TextAnchor.UpperLeft;
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = columns;

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

        // Encontra o grid container
        Transform gridTransform = panelObject.transform.Find("SlotGrid");
        if (gridTransform == null)
        {
            Debug.LogError("[INVENTORY UI] SlotGrid não encontrado para rebuild!");
            return;
        }

        // Recalcula tamanho do painel
        int rows = Mathf.CeilToInt((float)newCount / columns);
        float gridWidth = columns * (slotSize + slotSpacing) - slotSpacing;
        float gridHeight = rows * (slotSize + slotSpacing) - slotSpacing;
        float panelPadding = 20f;
        float headerHeight = 44f;

        // Atualiza tamanho do painel
        panelRect.sizeDelta = new Vector2(
            gridWidth + panelPadding * 2,
            gridHeight + panelPadding * 2 + headerHeight
        );

        // Atualiza tamanho do grid
        RectTransform gridRect = gridTransform as RectTransform;
        gridRect.sizeDelta = new Vector2(gridWidth, gridHeight);

        // Cria novos slots
        BuildSlots(gridTransform, newCount);

        Debug.Log("[INVENTORY UI] Slots reconstruídos: " + newCount + " (" + columns + "x" + rows + ")");
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
