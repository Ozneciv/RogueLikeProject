using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Interface de usuário do sistema de Crafting.
/// Singleton — criado programaticamente (mesmo padrão do InventoryUI.cs).
///
/// LAYOUT:
///   ┌─────────────────────────────────────────────────┐
///   │  MESA DE TRABALHO                          [X]  │
///   ├────────────────────┬────────────────────────────┤
///   │  Lista de Receitas │  Detalhes da Receita       │
///   │  ┌──────────────┐  │  Nome: Expansão de Inv.    │
///   │  │ Receita 1  ✓ │  │  Ingredientes:             │
///   │  ├──────────────┤  │    5/5 Shard Splinter      │
///   │  │ Receita 2  ✗ │  │    3/3 Magic Dust          │
///   │  ├──────────────┤  │  Resultado:                │
///   │  │ Receita 3  ✗ │  │    +5 Slots de Inventário  │
///   │  └──────────────┘  │  [   CRAFTAR   ]           │
///   ├────────────────────┴────────────────────────────┤
///   │  MELHORIAS CRAFTADAS                            │
///   │  ┌────────────┐ ┌────────────┐                  │
///   │  │ Melhoria 1 │ │ Melhoria 2 │                  │
///   │  │ [Equipar]  │ │[Desequipar]│                  │
///   │  └────────────┘ └────────────┘                  │
///   └─────────────────────────────────────────────────┘
///
/// DEPENDÊNCIAS:
///   - CraftingManager.Instance  (lógica de craft)
///   - EquipmentManager.Instance (equipar/desequipar)
///   - SaveManager.instance      (leitura de baseResources)
///   - ItemDatabase.Instance     (ícones e nomes dos itens)
/// </summary>
public class CraftingUI : MonoBehaviour
{
    public static CraftingUI Instance { get; private set; }

    // Referências internas
    private Canvas craftingCanvas;
    private GameObject canvasObject;
    private GameObject panelObject;
    private RectTransform panelRect;

    // Seção de receitas
    private Transform recipesContainer;
    private List<RecipeSlotUI> recipeSlots = new List<RecipeSlotUI>();

    // Seção de detalhes
    private TextMeshProUGUI detailNameText;
    private TextMeshProUGUI detailDescText;
    private TextMeshProUGUI ingredientsText;
    private TextMeshProUGUI resultText;
    private Button craftButton;
    private TextMeshProUGUI craftButtonText;

    // Seção de equipamentos
    private Transform equipmentContainer;
    private List<GameObject> equipmentSlotObjects = new List<GameObject>();

    // Estado
    private bool isOpen = false;
    private bool uiBuilt = false;
    private CraftingRecipe selectedRecipe;

    public TMP_FontAsset customFont;

    // Cores do painel (tema escuro/roxo consistente com InventoryUI)
    private static readonly Color PANEL_BG = new Color(0.02f, 0.02f, 0.04f, 0.92f); // frosted glass background, slightly more opaque and darker
    private static readonly Color PANEL_BORDER = new Color(1f, 1f, 1f, 0.12f); // light reflection border
    private static readonly Color HEADER_COLOR = new Color(0.85f, 0.80f, 0.95f, 1f);
    private static readonly Color ACCENT = new Color(0.6f, 0.45f, 0.90f, 1f);
    private static readonly Color SECTION_BG = new Color(0.04f, 0.04f, 0.07f, 0.90f); // frosted glass sub-section background, slightly darker
    private static readonly Color BTN_CRAFT_ENABLED = new Color(0.25f, 0.70f, 0.30f, 1f);
    private static readonly Color BTN_CRAFT_DISABLED = new Color(0.3f, 0.3f, 0.3f, 0.6f);
    private static readonly Color BTN_EQUIP = new Color(0.3f, 0.55f, 0.85f, 1f);
    private static readonly Color BTN_UNEQUIP = new Color(0.7f, 0.35f, 0.35f, 1f);

    // Dimensões
    private const float PANEL_WIDTH = 800f; // Aumentado de 700f
    private const float PANEL_HEIGHT = 600f; // Aumentado de 520f
    private const float RECIPE_SLOT_HEIGHT = 58f; // Aumentado de 52f para melhor espaçamento de fontes maiores
    private const float EQUIPMENT_SLOT_SIZE = 110f;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Carrega Oswald Bold SDF como fallback consistente se não definida no Inspector
        if (customFont == null)
        {
            customFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/Oswald Bold SDF");
        }
    }

    void Start()
    {
        CreateCraftingUI();
        panelObject.SetActive(false);
        uiBuilt = true;
    }

    void OnEnable()
    {
        CraftingManager.OnCraftCompleted += OnCraftCompleted;
        SaveManager.OnBaseResourcesChanged += RefreshUI;
        EquipmentManager.OnEquipmentStateChanged += RefreshEquipmentSection;
        SaveManager.OnEquipmentChanged += RefreshEquipmentSection;
    }

    void OnDisable()
    {
        CraftingManager.OnCraftCompleted -= OnCraftCompleted;
        SaveManager.OnBaseResourcesChanged -= RefreshUI;
        EquipmentManager.OnEquipmentStateChanged -= RefreshEquipmentSection;
        SaveManager.OnEquipmentChanged -= RefreshEquipmentSection;
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
            if (canvasObject != null) Destroy(canvasObject);
        }
    }

    void Update()
    {
        if (isOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseCrafting();
        }

        // Atalho T para realizar Craft se a UI estiver aberta e a receita for craftável
        if (isOpen && Input.GetKeyDown(KeyCode.T))
        {
            if (selectedRecipe != null && CraftingManager.Instance != null)
            {
                if (CraftingManager.Instance.CanCraft(selectedRecipe))
                {
                    OnCraftButtonClicked();
                }
            }
        }

        // CHEAT DEBUG: Pressionando P com a UI aberta adiciona todos os recursos para testes rápidos de craft!
        if (isOpen && Input.GetKeyDown(KeyCode.P))
        {
            if (ItemDatabase.Instance != null && SaveManager.instance != null)
            {
                foreach (var item in ItemDatabase.Instance.allItems)
                {
                    if (item != null && item.returnsToBase)
                    {
                        SaveManager.instance.AddResourceToBase(item.itemId, 20);
                    }
                }
                RefreshUI();
                Debug.Log("[DEBUG CHEAT] Adicionados 20 de cada recurso de base!");
            }
        }
    }

    // ─── API PÚBLICA ─────────────────────────────────────────────────────────

    /// <summary>Abre a tela de crafting.</summary>
    public void OpenCrafting()
    {
        if (isOpen) return;
        isOpen = true;
        panelObject.SetActive(true);
        selectedRecipe = null;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        RefreshUI();
        Debug.Log("[CRAFTING] UI aberta");
    }

    /// <summary>Fecha a tela de crafting.</summary>
    public void CloseCrafting()
    {
        if (!isOpen) return;
        isOpen = false;
        panelObject.SetActive(false);

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        Debug.Log("[CRAFTING] UI fechada");
    }

    /// <summary>Retorna se a UI está aberta.</summary>
    public bool IsOpen() => isOpen;

    // ─── REFRESH ─────────────────────────────────────────────────────────────

    private void RefreshUI()
    {
        if (!uiBuilt || CraftingManager.Instance == null) return;

        RefreshRecipeList();
        RefreshDetails();
        RefreshEquipmentSection();
    }

    private void RefreshRecipeList()
    {
        List<CraftingRecipe> recipes = CraftingManager.Instance.GetAllRecipes();

        // Limpa slots antigos
        foreach (var slot in recipeSlots)
        {
            if (slot != null) Destroy(slot.gameObject);
        }
        recipeSlots.Clear();

        // Cria novos slots
        foreach (var recipe in recipes)
        {
            GameObject slotObj = new GameObject("RecipeSlot_" + recipe.recipeId);
            slotObj.transform.SetParent(recipesContainer, false);

            RecipeSlotUI slot = slotObj.AddComponent<RecipeSlotUI>();
            slot.Initialize(OnRecipeSelected, 230f, RECIPE_SLOT_HEIGHT);

            bool canCraft = CraftingManager.Instance.CanCraft(recipe);
            slot.SetRecipe(recipe, canCraft);
            slot.SetSelected(selectedRecipe == recipe);

            recipeSlots.Add(slot);
        }
    }

    private void RefreshDetails()
    {
        if (selectedRecipe == null)
        {
            detailNameText.text = "Selecione uma receita";
            detailDescText.text = "";
            ingredientsText.text = "";
            resultText.text = "";
            craftButton.interactable = false;
            craftButtonText.text = "CRAFTAR";
            craftButton.GetComponent<Image>().color = BTN_CRAFT_DISABLED;
            return;
        }

        detailNameText.text = selectedRecipe.recipeName;
        detailDescText.text = selectedRecipe.description;

        // Ingredientes
        string ingText = "<b>Ingredientes:</b>\n";
        foreach (var ing in selectedRecipe.ingredients)
        {
            int available = CraftingManager.Instance.GetAvailableAmount(ing.itemId);
            string itemName = ing.itemId;

            // Tenta pegar nome bonito do ItemDatabase
            if (ItemDatabase.Instance != null)
            {
                ItemData itemData = ItemDatabase.Instance.GetItemData(ing.itemId);
                if (itemData != null)
                    itemName = itemData.itemName;
            }

            string color = available >= ing.quantity ? "#4AE04A" : "#E04A4A";
            ingText += $"  <color={color}>{available}/{ing.quantity}</color> {itemName}\n";
        }
        ingredientsText.text = ingText;

        // Resultado
        switch (selectedRecipe.resultType)
        {
            case CraftingResultType.Equipment:
                if (selectedRecipe.resultEquipment != null)
                {
                    var eq = selectedRecipe.resultEquipment;
                    resultText.text = $"<b>Resultado:</b>\n  {eq.equipmentName}\n  <color=#9B7FD4>{GetEffectDescription(eq)}</color>";
                }
                break;
            case CraftingResultType.Item:
                string resultName = selectedRecipe.resultItemId;
                if (ItemDatabase.Instance != null)
                {
                    ItemData rd = ItemDatabase.Instance.GetItemData(selectedRecipe.resultItemId);
                    if (rd != null) resultName = rd.itemName;
                }
                resultText.text = $"<b>Resultado:</b>\n  {resultName}";
                break;
        }

        // Botão de craft
        bool canCraft = CraftingManager.Instance.CanCraft(selectedRecipe);
        craftButton.interactable = canCraft;
        craftButton.GetComponent<Image>().color = canCraft ? BTN_CRAFT_ENABLED : BTN_CRAFT_DISABLED;
        craftButtonText.text = canCraft ? "CRAFTAR [T]" : "MATERIAIS INSUFICIENTES";
    }

    private void RefreshEquipmentSection()
    {
        if (!uiBuilt || EquipmentManager.Instance == null) return;

        // Limpa slots antigos
        foreach (var obj in equipmentSlotObjects)
        {
            if (obj != null) Destroy(obj);
        }
        equipmentSlotObjects.Clear();

        // Cria slots para cada equipamento craftado
        List<EquipmentData> owned = EquipmentManager.Instance.GetOwnedEquipment();

        foreach (var equip in owned)
        {
            GameObject slotObj = CreateEquipmentSlot(equip);
            slotObj.transform.SetParent(equipmentContainer, false);
            equipmentSlotObjects.Add(slotObj);
        }
    }

    // ─── CALLBACKS ───────────────────────────────────────────────────────────

    private void OnRecipeSelected(CraftingRecipe recipe)
    {
        selectedRecipe = recipe;

        // Atualiza seleção visual
        foreach (var slot in recipeSlots)
        {
            slot.SetSelected(false);
        }

        // Encontra e marca o slot selecionado
        if (CraftingManager.Instance != null)
        {
            var recipes = CraftingManager.Instance.GetAllRecipes();
            for (int i = 0; i < recipes.Count && i < recipeSlots.Count; i++)
            {
                if (recipes[i] == recipe)
                {
                    recipeSlots[i].SetSelected(true);
                    break;
                }
            }
        }

        RefreshDetails();
    }

    private void OnCraftButtonClicked()
    {
        if (selectedRecipe == null || CraftingManager.Instance == null) return;

        if (CraftingManager.Instance.Craft(selectedRecipe))
        {
            Debug.Log($"[CRAFTING UI] Craft realizado: {selectedRecipe.recipeName}");
        }
    }

    private void OnCraftCompleted(CraftingRecipe recipe)
    {
        RefreshUI();
    }

    // ─── HELPERS ─────────────────────────────────────────────────────────────

    private string GetEffectDescription(EquipmentData equip)
    {
        switch (equip.effectType)
        {
            case EquipmentEffectType.InventorySlotExpansion:
                return $"+{equip.effectValue:0} Slots de Inventário";
            case EquipmentEffectType.MaxHealthBoost:
                return $"+{equip.effectValue:0} Vida Máxima";
            case EquipmentEffectType.MaxArmorBoost:
                return $"+{equip.effectValue:0} Armadura Máxima";
            case EquipmentEffectType.SpeedBoost:
                return $"×{equip.effectValue:F1} Velocidade";
            case EquipmentEffectType.DamageBoost:
                return $"×{equip.effectValue:F1} Dano Base";
            case EquipmentEffectType.CritChanceBoost:
                return $"+{equip.effectValue:0}% Chance Crítico";
            case EquipmentEffectType.ArmorRegenBoost:
                return $"+{equip.effectValue:F1} Regen. Armadura";
            default:
                return equip.effectValue.ToString();
        }
    }

    // ─── CRIAÇÃO DA UI ───────────────────────────────────────────────────────

    private void CreateCraftingUI()
    {
        // Canvas próprio persistente
        canvasObject = new GameObject("CraftingUI_Canvas");
        craftingCanvas = canvasObject.AddComponent<Canvas>();
        craftingCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        craftingCanvas.sortingOrder = 110; // Acima do inventário
        var scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasObject.AddComponent<GraphicRaycaster>();
        DontDestroyOnLoad(canvasObject);

        // Painel principal
        panelObject = new GameObject("CraftingPanel");
        panelObject.transform.SetParent(craftingCanvas.transform, false);

        panelRect = panelObject.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(PANEL_WIDTH, PANEL_HEIGHT);

        Image panelBg = panelObject.AddComponent<Image>();
        panelBg.color = PANEL_BG;
        panelBg.raycastTarget = true;

        // Efeito de sombra (Drop Shadow) para dar profundidade de vidro suspenso
        Shadow panelShadow = panelObject.AddComponent<Shadow>();
        panelShadow.effectColor = new Color(0f, 0f, 0f, 0.45f);
        panelShadow.effectDistance = new Vector2(6f, -6f);

        // Acento reflexivo superior de vidro (Glass Highlight)
        GameObject glassHighlightObj = new GameObject("GlassHighlight");
        glassHighlightObj.transform.SetParent(panelObject.transform, false);
        glassHighlightObj.layer = panelObject.layer;

        RectTransform glassHighlightRect = glassHighlightObj.AddComponent<RectTransform>();
        glassHighlightRect.anchorMin = new Vector2(0f, 1f);
        glassHighlightRect.anchorMax = new Vector2(1f, 1f);
        glassHighlightRect.pivot = new Vector2(0.5f, 1f);
        glassHighlightRect.anchoredPosition = new Vector2(0f, -2f);
        glassHighlightRect.sizeDelta = new Vector2(-4f, 2f);

        glassHighlightObj.AddComponent<CanvasRenderer>();
        Image glassHighlightImg = glassHighlightObj.AddComponent<Image>();
        glassHighlightImg.color = new Color(1f, 1f, 1f, 0.12f); // reflexo superior fino
        glassHighlightImg.raycastTarget = false;

        // Borda do painel
        CreateBorder(panelObject.transform);

        // Acento superior
        CreateTopAccent(panelObject.transform);

        // Header
        CreateHeader(panelObject.transform);

        // Área de conteúdo dividida em duas colunas
        CreateRecipeListSection(panelObject.transform);
        CreateDetailSection(panelObject.transform);

        // Seção de equipamentos (abaixo)
        CreateEquipmentSection(panelObject.transform);

        // Botão de fechar
        CreateCloseButton(panelObject.transform);

        Debug.Log("[CRAFTING] UI criada.");
    }

    private void CreateBorder(Transform parent)
    {
        GameObject borderObj = new GameObject("PanelBorder");
        borderObj.transform.SetParent(parent, false);
        RectTransform r = borderObj.AddComponent<RectTransform>();
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.sizeDelta = new Vector2(4f, 4f);
        borderObj.AddComponent<CanvasRenderer>();
        Image img = borderObj.AddComponent<Image>();
        img.color = PANEL_BORDER;
        img.type = Image.Type.Sliced;
        img.fillCenter = false;
        img.raycastTarget = false;
    }

    private void CreateTopAccent(Transform parent)
    {
        GameObject obj = new GameObject("TopAccent");
        obj.transform.SetParent(parent, false);
        RectTransform r = obj.AddComponent<RectTransform>();
        r.anchorMin = new Vector2(0.05f, 1f);
        r.anchorMax = new Vector2(0.95f, 1f);
        r.pivot = new Vector2(0.5f, 1f);
        r.sizeDelta = new Vector2(0f, 3f);
        obj.AddComponent<CanvasRenderer>();
        Image img = obj.AddComponent<Image>();
        img.color = ACCENT;
        img.raycastTarget = false;
    }

    private void CreateHeader(Transform parent)
    {
        GameObject obj = new GameObject("Header");
        obj.transform.SetParent(parent, false);
        RectTransform r = obj.AddComponent<RectTransform>();
        r.anchorMin = new Vector2(0f, 1f);
        r.anchorMax = new Vector2(1f, 1f);
        r.pivot = new Vector2(0.5f, 1f);
        r.anchoredPosition = new Vector2(0f, -6f);
        r.sizeDelta = new Vector2(-40f, 36f);
        obj.AddComponent<CanvasRenderer>();
        TextMeshProUGUI txt = obj.AddComponent<TextMeshProUGUI>();
        txt.text = "MESA DE TRABALHO";
        txt.fontSize = 22f; // Aumentado de 18f
        txt.fontStyle = FontStyles.Bold;
        txt.color = HEADER_COLOR;
        txt.alignment = TextAlignmentOptions.Center;
        txt.raycastTarget = false;
        if (customFont != null) txt.font = customFont;
    }

    private void CreateRecipeListSection(Transform parent)
    {
        // Container da lista de receitas (lado esquerdo)
        GameObject listPanel = new GameObject("RecipeListPanel");
        listPanel.transform.SetParent(parent, false);
        RectTransform r = listPanel.AddComponent<RectTransform>();
        r.anchorMin = new Vector2(0f, 0.28f);
        r.anchorMax = new Vector2(0.38f, 0.92f);
        r.offsetMin = new Vector2(12f, 0f);
        r.offsetMax = new Vector2(0f, -8f);

        listPanel.AddComponent<CanvasRenderer>();
        Image bg = listPanel.AddComponent<Image>();
        bg.color = SECTION_BG;
        bg.raycastTarget = true;

        // Label
        GameObject labelObj = new GameObject("RecipesLabel");
        labelObj.transform.SetParent(listPanel.transform, false);
        RectTransform lr = labelObj.AddComponent<RectTransform>();
        lr.anchorMin = new Vector2(0f, 1f);
        lr.anchorMax = new Vector2(1f, 1f);
        lr.pivot = new Vector2(0.5f, 1f);
        lr.sizeDelta = new Vector2(0f, 22f);
        lr.anchoredPosition = new Vector2(0f, -2f);
        labelObj.AddComponent<CanvasRenderer>();
        TextMeshProUGUI labelText = labelObj.AddComponent<TextMeshProUGUI>();
        labelText.text = "RECEITAS";
        labelText.fontSize = 14f; // Aumentado de 11f
        labelText.fontStyle = FontStyles.Bold;
        labelText.color = ACCENT;
        labelText.alignment = TextAlignmentOptions.Center;
        labelText.raycastTarget = false;
        if (customFont != null) labelText.font = customFont;

        // ScrollView para as receitas
        GameObject scrollObj = new GameObject("RecipeScroll");
        scrollObj.transform.SetParent(listPanel.transform, false);
        RectTransform sr = scrollObj.AddComponent<RectTransform>();
        sr.anchorMin = new Vector2(0f, 0f);
        sr.anchorMax = new Vector2(1f, 1f);
        sr.offsetMin = new Vector2(4f, 4f);
        sr.offsetMax = new Vector2(-4f, -26f);

        scrollObj.AddComponent<CanvasRenderer>();
        Image scrollMask = scrollObj.AddComponent<Image>();
        scrollMask.color = new Color(0, 0, 0, 0.01f);
        scrollObj.AddComponent<Mask>().showMaskGraphic = false;

        // Content
        GameObject contentObj = new GameObject("RecipeContent");
        contentObj.transform.SetParent(scrollObj.transform, false);
        RectTransform cr = contentObj.AddComponent<RectTransform>();
        cr.anchorMin = new Vector2(0f, 1f);
        cr.anchorMax = new Vector2(1f, 1f);
        cr.pivot = new Vector2(0.5f, 1f);
        cr.sizeDelta = new Vector2(0f, 0f);

        VerticalLayoutGroup vlg = contentObj.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 4f;
        vlg.padding = new RectOffset(2, 2, 2, 2);
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlHeight = false;
        vlg.childControlWidth = true;

        ContentSizeFitter fitter = contentObj.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // ScrollRect
        ScrollRect scroll = scrollObj.AddComponent<ScrollRect>();
        scroll.content = cr;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 20f;

        recipesContainer = contentObj.transform;
    }

    private void CreateDetailSection(Transform parent)
    {
        // Container dos detalhes (lado direito)
        GameObject detailPanel = new GameObject("DetailPanel");
        detailPanel.transform.SetParent(parent, false);
        RectTransform r = detailPanel.AddComponent<RectTransform>();
        r.anchorMin = new Vector2(0.40f, 0.28f);
        r.anchorMax = new Vector2(1f, 0.92f);
        r.offsetMin = new Vector2(0f, 0f);
        r.offsetMax = new Vector2(-12f, -8f);

        detailPanel.AddComponent<CanvasRenderer>();
        Image bg = detailPanel.AddComponent<Image>();
        bg.color = SECTION_BG;
        bg.raycastTarget = false;

        // Nome da receita
        detailNameText = CreateTextElement(detailPanel.transform, "DetailName",
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -10f), new Vector2(-16f, 30f), 20f, FontStyles.Bold, HEADER_COLOR);

        // Descrição
        detailDescText = CreateTextElement(detailPanel.transform, "DetailDesc",
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -48f), new Vector2(-16f, 36f), 14f, FontStyles.Italic,
            new Color(0.7f, 0.68f, 0.78f, 1f));

        // Ingredientes
        ingredientsText = CreateTextElement(detailPanel.transform, "Ingredients",
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -96f), new Vector2(-16f, 130f), 15f, FontStyles.Normal,
            new Color(0.85f, 0.83f, 0.92f, 1f));
        ingredientsText.richText = true;

        // Resultado
        resultText = CreateTextElement(detailPanel.transform, "Result",
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -240f), new Vector2(-16f, 60f), 15f, FontStyles.Normal,
            new Color(0.85f, 0.83f, 0.92f, 1f));
        resultText.richText = true;

        // Botão de Craftar
        GameObject btnObj = new GameObject("CraftButton");
        btnObj.transform.SetParent(detailPanel.transform, false);
        RectTransform br = btnObj.AddComponent<RectTransform>();
        br.anchorMin = new Vector2(0.1f, 0f);
        br.anchorMax = new Vector2(0.9f, 0f);
        br.pivot = new Vector2(0.5f, 0f);
        br.anchoredPosition = new Vector2(0f, 12f);
        br.sizeDelta = new Vector2(0f, 38f);

        btnObj.AddComponent<CanvasRenderer>();
        Image btnBg = btnObj.AddComponent<Image>();
        btnBg.color = BTN_CRAFT_DISABLED;
        btnBg.raycastTarget = true;

        craftButton = btnObj.AddComponent<Button>();
        craftButton.onClick.AddListener(OnCraftButtonClicked);
        craftButton.interactable = false;

        // Texto do botão
        GameObject btnTextObj = new GameObject("CraftBtnText");
        btnTextObj.transform.SetParent(btnObj.transform, false);
        RectTransform btr = btnTextObj.AddComponent<RectTransform>();
        btr.anchorMin = Vector2.zero;
        btr.anchorMax = Vector2.one;
        btr.sizeDelta = Vector2.zero;
        btnTextObj.AddComponent<CanvasRenderer>();
        craftButtonText = btnTextObj.AddComponent<TextMeshProUGUI>();
        craftButtonText.text = "CRAFTAR";
        craftButtonText.fontSize = 17f; // Aumentado de 14f
        craftButtonText.fontStyle = FontStyles.Bold;
        craftButtonText.color = Color.white;
        craftButtonText.alignment = TextAlignmentOptions.Center;
        craftButtonText.raycastTarget = false;
        if (customFont != null) craftButtonText.font = customFont;

        // Inicializa com "selecione uma receita"
        detailNameText.text = "Selecione uma receita";
        detailDescText.text = "";
        ingredientsText.text = "";
        resultText.text = "";
    }

    private void CreateEquipmentSection(Transform parent)
    {
        // Seção de melhorias (parte inferior)
        GameObject equipPanel = new GameObject("EquipmentPanel");
        equipPanel.transform.SetParent(parent, false);
        RectTransform r = equipPanel.AddComponent<RectTransform>();
        r.anchorMin = new Vector2(0f, 0f);
        r.anchorMax = new Vector2(1f, 0.26f);
        r.offsetMin = new Vector2(12f, 8f);
        r.offsetMax = new Vector2(-12f, -2f);

        equipPanel.AddComponent<CanvasRenderer>();
        Image bg = equipPanel.AddComponent<Image>();
        bg.color = SECTION_BG;
        bg.raycastTarget = false;

        // Label
        GameObject labelObj = new GameObject("EquipmentLabel");
        labelObj.transform.SetParent(equipPanel.transform, false);
        RectTransform lr = labelObj.AddComponent<RectTransform>();
        lr.anchorMin = new Vector2(0f, 1f);
        lr.anchorMax = new Vector2(1f, 1f);
        lr.pivot = new Vector2(0.5f, 1f);
        lr.sizeDelta = new Vector2(0f, 20f);
        lr.anchoredPosition = new Vector2(0f, -2f);
        labelObj.AddComponent<CanvasRenderer>();
        TextMeshProUGUI label = labelObj.AddComponent<TextMeshProUGUI>();
        label.text = "MELHORIAS CRAFTADAS";
        label.fontSize = 14f; // Aumentado de 11f
        label.fontStyle = FontStyles.Bold;
        label.color = ACCENT;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        if (customFont != null) label.font = customFont;

        // ScrollView horizontal para equipamentos
        GameObject scrollObj = new GameObject("EquipScroll");
        scrollObj.transform.SetParent(equipPanel.transform, false);
        RectTransform sr = scrollObj.AddComponent<RectTransform>();
        sr.anchorMin = new Vector2(0f, 0f);
        sr.anchorMax = new Vector2(1f, 1f);
        sr.offsetMin = new Vector2(4f, 4f);
        sr.offsetMax = new Vector2(-4f, -24f);

        scrollObj.AddComponent<CanvasRenderer>();
        Image scrollMask = scrollObj.AddComponent<Image>();
        scrollMask.color = new Color(0, 0, 0, 0.01f);
        scrollObj.AddComponent<Mask>().showMaskGraphic = false;

        GameObject contentObj = new GameObject("EquipContent");
        contentObj.transform.SetParent(scrollObj.transform, false);
        RectTransform cr = contentObj.AddComponent<RectTransform>();
        cr.anchorMin = new Vector2(0f, 0f);
        cr.anchorMax = new Vector2(0f, 1f);
        cr.pivot = new Vector2(0f, 0.5f);
        cr.sizeDelta = new Vector2(0f, 0f);

        HorizontalLayoutGroup hlg = contentObj.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 8f;
        hlg.padding = new RectOffset(4, 4, 4, 4);
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;
        hlg.childControlWidth = false;

        ContentSizeFitter fitter = contentObj.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect scroll = scrollObj.AddComponent<ScrollRect>();
        scroll.content = cr;
        scroll.horizontal = true;
        scroll.vertical = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;

        equipmentContainer = contentObj.transform;
    }

    private GameObject CreateEquipmentSlot(EquipmentData equip)
    {
        bool isEquipped = EquipmentManager.Instance.IsEquipped(equip.equipmentId);

        GameObject slotObj = new GameObject("EquipSlot_" + equip.equipmentId);
        RectTransform r = slotObj.AddComponent<RectTransform>();
        r.sizeDelta = new Vector2(EQUIPMENT_SLOT_SIZE, 0f);

        slotObj.AddComponent<CanvasRenderer>();
        Image bg = slotObj.AddComponent<Image>();
        bg.color = new Color(0.12f, 0.12f, 0.18f, 0.95f);
        bg.raycastTarget = false;

        // Nome
        GameObject nameObj = new GameObject("Name");
        nameObj.transform.SetParent(slotObj.transform, false);
        RectTransform nr = nameObj.AddComponent<RectTransform>();
        nr.anchorMin = new Vector2(0f, 0.55f);
        nr.anchorMax = new Vector2(1f, 0.95f);
        nr.sizeDelta = Vector2.zero;
        nr.offsetMin = new Vector2(4f, 0f);
        nr.offsetMax = new Vector2(-4f, 0f);
        nameObj.AddComponent<CanvasRenderer>();
        TextMeshProUGUI nameText = nameObj.AddComponent<TextMeshProUGUI>();
        nameText.text = equip.equipmentName;
        nameText.fontSize = 13f; // Aumentado de 10f
        nameText.fontStyle = FontStyles.Bold;
        nameText.color = new Color(0.9f, 0.88f, 0.95f);
        nameText.alignment = TextAlignmentOptions.Center;
        nameText.raycastTarget = false;
        nameText.enableWordWrapping = true;
        if (customFont != null) nameText.font = customFont;

        // Efeito
        GameObject effectObj = new GameObject("Effect");
        effectObj.transform.SetParent(slotObj.transform, false);
        RectTransform er = effectObj.AddComponent<RectTransform>();
        er.anchorMin = new Vector2(0f, 0.35f);
        er.anchorMax = new Vector2(1f, 0.58f);
        er.sizeDelta = Vector2.zero;
        er.offsetMin = new Vector2(4f, 0f);
        er.offsetMax = new Vector2(-4f, 0f);
        effectObj.AddComponent<CanvasRenderer>();
        TextMeshProUGUI effectText = effectObj.AddComponent<TextMeshProUGUI>();
        effectText.text = GetEffectDescription(equip);
        effectText.fontSize = 11f; // Aumentado de 9f
        effectText.color = new Color(0.6f, 0.85f, 0.6f);
        effectText.alignment = TextAlignmentOptions.Center;
        effectText.raycastTarget = false;
        if (customFont != null) effectText.font = customFont;

        // Botão Equipar/Desequipar
        GameObject btnObj = new GameObject("EquipBtn");
        btnObj.transform.SetParent(slotObj.transform, false);
        RectTransform br = btnObj.AddComponent<RectTransform>();
        br.anchorMin = new Vector2(0.1f, 0.05f);
        br.anchorMax = new Vector2(0.9f, 0.32f);
        br.sizeDelta = Vector2.zero;

        btnObj.AddComponent<CanvasRenderer>();
        Image btnBg = btnObj.AddComponent<Image>();
        btnBg.color = isEquipped ? BTN_UNEQUIP : BTN_EQUIP;
        btnBg.raycastTarget = true;

        Button btn = btnObj.AddComponent<Button>();
        string eqId = equip.equipmentId; // Captura para o closure
        btn.onClick.AddListener(() =>
        {
            if (EquipmentManager.Instance == null) return;
            if (EquipmentManager.Instance.IsEquipped(eqId))
                EquipmentManager.Instance.Unequip(eqId);
            else
                EquipmentManager.Instance.Equip(eqId);
        });

        // Texto do botão
        GameObject btnTextObj = new GameObject("BtnText");
        btnTextObj.transform.SetParent(btnObj.transform, false);
        RectTransform btr = btnTextObj.AddComponent<RectTransform>();
        btr.anchorMin = Vector2.zero;
        btr.anchorMax = Vector2.one;
        btr.sizeDelta = Vector2.zero;
        btnTextObj.AddComponent<CanvasRenderer>();
        TextMeshProUGUI btnText = btnTextObj.AddComponent<TextMeshProUGUI>();
        btnText.text = isEquipped ? "DESEQUIPAR" : "EQUIPAR";
        btnText.fontSize = 12f; // Aumentado de 10f
        btnText.fontStyle = FontStyles.Bold;
        btnText.color = Color.white;
        btnText.alignment = TextAlignmentOptions.Center;
        btnText.raycastTarget = false;
        if (customFont != null) btnText.font = customFont;

        return slotObj;
    }

    private void CreateCloseButton(Transform parent)
    {
        GameObject btnObj = new GameObject("CloseButton");
        btnObj.transform.SetParent(parent, false);
        RectTransform r = btnObj.AddComponent<RectTransform>();
        r.anchorMin = new Vector2(1f, 1f);
        r.anchorMax = new Vector2(1f, 1f);
        r.pivot = new Vector2(1f, 1f);
        r.anchoredPosition = new Vector2(-8f, -8f);
        r.sizeDelta = new Vector2(28f, 28f);

        btnObj.AddComponent<CanvasRenderer>();
        Image bg = btnObj.AddComponent<Image>();
        bg.color = new Color(0.8f, 0.2f, 0.2f, 0.7f);

        Button btn = btnObj.AddComponent<Button>();
        btn.onClick.AddListener(CloseCrafting);

        ColorBlock colors = btn.colors;
        colors.normalColor = new Color(0.8f, 0.2f, 0.2f, 0.7f);
        colors.highlightedColor = new Color(1f, 0.3f, 0.3f, 0.9f);
        colors.pressedColor = new Color(0.6f, 0.1f, 0.1f, 1f);
        btn.colors = colors;

        GameObject xObj = new GameObject("X");
        xObj.transform.SetParent(btnObj.transform, false);
        RectTransform xr = xObj.AddComponent<RectTransform>();
        xr.anchorMin = Vector2.zero;
        xr.anchorMax = Vector2.one;
        xr.sizeDelta = Vector2.zero;
        xObj.AddComponent<CanvasRenderer>();
        TextMeshProUGUI x = xObj.AddComponent<TextMeshProUGUI>();
        x.text = "X";
        x.fontSize = 18f; // Aumentado de 16f
        x.color = Color.white;
        x.alignment = TextAlignmentOptions.Center;
        x.raycastTarget = false;
        if (customFont != null) x.font = customFont;
    }

    private TextMeshProUGUI CreateTextElement(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
        Vector2 anchoredPos, Vector2 sizeDelta,
        float fontSize, FontStyles style, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        RectTransform r = obj.AddComponent<RectTransform>();
        r.anchorMin = anchorMin;
        r.anchorMax = anchorMax;
        r.pivot = pivot;
        r.anchoredPosition = anchoredPos;
        r.sizeDelta = sizeDelta;
        r.offsetMin = new Vector2(12f, r.offsetMin.y);
        r.offsetMax = new Vector2(-12f, r.offsetMax.y);
        obj.AddComponent<CanvasRenderer>();
        TextMeshProUGUI txt = obj.AddComponent<TextMeshProUGUI>();
        txt.fontSize = fontSize;
        txt.fontStyle = style;
        txt.color = color;
        txt.alignment = TextAlignmentOptions.TopLeft;
        txt.raycastTarget = false;
        txt.enableWordWrapping = true;
        if (customFont != null) txt.font = customFont;
        return txt;
    }
}
