using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Interface de usuário do sistema de Crafting.
/// Singleton — deve ser colocado em um GameObject na cena da Base.
///
/// ARQUITETURA:
///   Este script NÃO cria elementos visuais por código.
///   Todas as referências de UI são expostas via [SerializeField] para que
///   o artista monte o layout no Canvas do Unity e arraste os elementos aqui.
///
///   Os únicos GameObjects criados em runtime são os slots de receita
///   (RecipeSlotUI), pois eles são dinâmicos e se auto-constroem.
///
/// LAYOUT ESPERADO NO CANVAS:
///   ┌─────────────────────────────────────────────────┐
///   │  MESA DE TRABALHO                          [X]  │
///   ├────────────────────┬────────────────────────────┤
///   │  Lista de Receitas │  Detalhes da Receita       │
///   │  (recipesContainer)│  Nome / Ingredientes       │
///   │                    │  Resultado                 │
///   │                    │  [   CRAFTAR   ]           │
///   ├────────────────────┴────────────────────────────┤
///   │  MELHORIAS CRAFTADAS (equipmentContainer)       │
///   │  [Melhoria 1] [Melhoria 2] ...                 │
///   └─────────────────────────────────────────────────┘
///
/// SETUP NO EDITOR:
///   1. Monte o layout acima no Canvas da Unity.
///   2. Arraste cada elemento para o campo correspondente no Inspector.
///   3. O script cuida de toda a lógica automaticamente.
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

    // ─── REFERÊNCIAS DO EDITOR (arrastar no Inspector) ──────────────────────

    [Header("Painel Principal")]
    [Tooltip("O GameObject raiz do painel de crafting (será ativado/desativado)")]
    [SerializeField] private GameObject panelObject;

    [Header("Lista de Receitas")]
    [Tooltip("Transform pai onde os slots de receita serão instanciados")]
    [SerializeField] private Transform recipesContainer;

    [Header("Painel de Detalhes")]
    [Tooltip("Texto que exibe o nome da receita selecionada")]
    [SerializeField] private TextMeshProUGUI detailNameText;

    [Tooltip("Texto que exibe a descrição da receita selecionada")]
    [SerializeField] private TextMeshProUGUI detailDescText;

    [Tooltip("Texto que exibe a lista de ingredientes (com quantidades)")]
    [SerializeField] private TextMeshProUGUI ingredientsText;

    [Tooltip("Texto que exibe o resultado da receita")]
    [SerializeField] private TextMeshProUGUI resultText;

    [Header("Botão de Craftar")]
    [Tooltip("Botão que executa o craft da receita selecionada")]
    [SerializeField] private Button craftButton;

    [Tooltip("Texto dentro do botão de craft (ex: 'CRAFTAR' ou 'MATERIAIS INSUFICIENTES')")]
    [SerializeField] private TextMeshProUGUI craftButtonText;

    [Header("Seção de Equipamentos")]
    [Tooltip("Transform pai onde os slots de melhorias craftadas serão instanciados")]
    [SerializeField] private Transform equipmentContainer;

    [Header("Botão de Fechar")]
    [Tooltip("Botão X para fechar a tela de crafting")]
    [SerializeField] private Button closeButton;

    [Header("Configurações dos Slots de Receita")]
    [Tooltip("Largura de cada slot de receita na lista")]
    [SerializeField] private float recipeSlotWidth = 230f;

    [Tooltip("Altura de cada slot de receita na lista")]
    [SerializeField] private float recipeSlotHeight = 52f;

    // ─── ESTADO INTERNO ─────────────────────────────────────────────────────

    private bool isOpen = false;
    private CraftingRecipe selectedRecipe;
    private List<RecipeSlotUI> recipeSlots = new List<RecipeSlotUI>();
    private List<GameObject> equipmentSlotObjects = new List<GameObject>();

    // ─── CORES (usadas apenas nos slots dinâmicos de equipamento) ────────────

    private static readonly Color BTN_EQUIP = new Color(0.3f, 0.55f, 0.85f, 1f);
    private static readonly Color BTN_UNEQUIP = new Color(0.7f, 0.35f, 0.35f, 1f);
    private static readonly Color BTN_CRAFT_ENABLED = new Color(0.25f, 0.70f, 0.30f, 1f);
    private static readonly Color BTN_CRAFT_DISABLED = new Color(0.3f, 0.3f, 0.3f, 0.6f);

    // ─── CICLO DE VIDA ──────────────────────────────────────────────────────

    void Awake()
    {
        // Singleton
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
        // Garante que o painel comece fechado
        if (panelObject != null)
            panelObject.SetActive(false);

        // Vincula o botão de craftar
        if (craftButton != null)
            craftButton.onClick.AddListener(OnCraftButtonClicked);

        // Vincula o botão de fechar
        if (closeButton != null)
            closeButton.onClick.AddListener(CloseCrafting);

        // Estado inicial do painel de detalhes
        ClearDetails();
    }

    void OnEnable()
    {
        // Inscreve nos eventos para atualizar a UI em tempo real
        CraftingManager.OnCraftCompleted += OnCraftCompleted;
        SaveManager.OnBaseResourcesChanged += RefreshUI;
        EquipmentManager.OnEquipmentStateChanged += RefreshEquipmentSection;
        SaveManager.OnEquipmentChanged += RefreshEquipmentSection;
    }

    void OnDisable()
    {
        // Remove inscrições para evitar memory leaks
        CraftingManager.OnCraftCompleted -= OnCraftCompleted;
        SaveManager.OnBaseResourcesChanged -= RefreshUI;
        EquipmentManager.OnEquipmentStateChanged -= RefreshEquipmentSection;
        SaveManager.OnEquipmentChanged -= RefreshEquipmentSection;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Update()
    {
        // Permite fechar com Escape
        if (isOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseCrafting();
        }
    }

    // ─── API PÚBLICA (chamada pelo CraftingTableInteraction) ────────────────

    /// <summary>Abre a tela de crafting.</summary>
    public void OpenCrafting()
    {
        if (isOpen) return;
        isOpen = true;

        if (panelObject != null)
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

        if (panelObject != null)
            panelObject.SetActive(false);

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        Debug.Log("[CRAFTING] UI fechada");
    }

    /// <summary>Retorna se a UI está aberta.</summary>
    public bool IsOpen() => isOpen;

    // ─── REFRESH GERAL ──────────────────────────────────────────────────────

    /// <summary>
    /// Atualiza todas as seções da UI.
    /// Chamado ao abrir e quando os recursos da bolsa mudam.
    /// </summary>
    private void RefreshUI()
    {
        if (CraftingManager.Instance == null) return;

        RefreshRecipeList();
        RefreshDetails();
        RefreshEquipmentSection();
    }

    // ─── LISTA DE RECEITAS (Grade Dinâmica) ─────────────────────────────────

    /// <summary>
    /// Limpa e recria os slots de receita no recipesContainer.
    /// Cada slot é um GameObject vazio com RecipeSlotUI anexado.
    /// </summary>
    private void RefreshRecipeList()
    {
        if (recipesContainer == null || CraftingManager.Instance == null) return;

        List<CraftingRecipe> recipes = CraftingManager.Instance.GetAllRecipes();

        // Limpa slots antigos
        foreach (var slot in recipeSlots)
        {
            if (slot != null) Destroy(slot.gameObject);
        }
        recipeSlots.Clear();

        // Cria um slot para cada receita
        foreach (var recipe in recipes)
        {
            // Cria um GameObject vazio com RectTransform
            GameObject slotObj = new GameObject("RecipeSlot_" + recipe.recipeId);
            slotObj.transform.SetParent(recipesContainer, false);

            // Anexa o componente RecipeSlotUI (ele constrói seus próprios visuais)
            RecipeSlotUI slot = slotObj.AddComponent<RecipeSlotUI>();

            // Inicializa com callback de seleção e dimensões
            slot.Initialize(OnRecipeSelected, recipeSlotWidth, recipeSlotHeight);

            // Define os dados da receita e se pode ser craftada
            bool canCraft = CraftingManager.Instance.CanCraft(recipe);
            slot.SetRecipe(recipe, canCraft);

            // Marca o slot selecionado (se houver)
            slot.SetSelected(selectedRecipe == recipe);

            recipeSlots.Add(slot);
        }
    }

    // ─── PAINEL DE DETALHES ─────────────────────────────────────────────────

    /// <summary>
    /// Atualiza o painel central com os detalhes da receita selecionada.
    /// </summary>
    private void RefreshDetails()
    {
        if (selectedRecipe == null)
        {
            ClearDetails();
            return;
        }

        // Nome e descrição
        if (detailNameText != null)
            detailNameText.text = selectedRecipe.recipeName;

        if (detailDescText != null)
            detailDescText.text = selectedRecipe.description;

        // Ingredientes — mostra quantidade disponível vs necessária
        if (ingredientsText != null)
        {
            string ingText = "<b>Ingredientes:</b>\n";

            foreach (var ing in selectedRecipe.ingredients)
            {
                int available = CraftingManager.Instance.GetAvailableAmount(ing.itemId);

                // Tenta pegar o nome bonito do ItemDatabase
                string itemName = ing.itemId;
                if (ItemDatabase.Instance != null)
                {
                    ItemData itemData = ItemDatabase.Instance.GetItemData(ing.itemId);
                    if (itemData != null)
                        itemName = itemData.itemName;
                }

                // Verde se tem material suficiente, vermelho se não
                string color = available >= ing.quantity ? "#4AE04A" : "#E04A4A";
                ingText += $"  <color={color}>{available}/{ing.quantity}</color> {itemName}\n";
            }

            ingredientsText.text = ingText;
        }

        // Resultado
        if (resultText != null)
        {
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
        }

        // Botão de craft — habilita ou desabilita
        if (craftButton != null)
        {
            bool canCraft = CraftingManager.Instance.CanCraft(selectedRecipe);
            craftButton.interactable = canCraft;

            Image btnImage = craftButton.GetComponent<Image>();
            if (btnImage != null)
                btnImage.color = canCraft ? BTN_CRAFT_ENABLED : BTN_CRAFT_DISABLED;

            if (craftButtonText != null)
                craftButtonText.text = canCraft ? "CRAFTAR" : "MATERIAIS INSUFICIENTES";
        }
    }

    /// <summary>
    /// Limpa o painel de detalhes quando nenhuma receita está selecionada.
    /// </summary>
    private void ClearDetails()
    {
        if (detailNameText != null) detailNameText.text = "Selecione uma receita";
        if (detailDescText != null) detailDescText.text = "";
        if (ingredientsText != null) ingredientsText.text = "";
        if (resultText != null) resultText.text = "";

        if (craftButton != null)
        {
            craftButton.interactable = false;

            Image btnImage = craftButton.GetComponent<Image>();
            if (btnImage != null)
                btnImage.color = BTN_CRAFT_DISABLED;
        }

        if (craftButtonText != null)
            craftButtonText.text = "CRAFTAR";
    }

    // ─── SEÇÃO DE EQUIPAMENTOS ──────────────────────────────────────────────

    /// <summary>
    /// Atualiza a seção de melhorias craftadas (equipar/desequipar).
    /// </summary>
    private void RefreshEquipmentSection()
    {
        if (equipmentContainer == null || EquipmentManager.Instance == null) return;

        // Limpa slots antigos
        foreach (var obj in equipmentSlotObjects)
        {
            if (obj != null) Destroy(obj);
        }
        equipmentSlotObjects.Clear();

        // Cria slots para cada equipamento que o jogador possui
        List<EquipmentData> owned = EquipmentManager.Instance.GetOwnedEquipment();

        foreach (var equip in owned)
        {
            GameObject slotObj = CreateEquipmentSlot(equip);
            slotObj.transform.SetParent(equipmentContainer, false);
            equipmentSlotObjects.Add(slotObj);
        }
    }

    // ─── CALLBACKS ──────────────────────────────────────────────────────────

    /// <summary>
    /// Chamado quando o jogador clica em um slot de receita.
    /// Atualiza a seleção visual e o painel de detalhes.
    /// </summary>
    private void OnRecipeSelected(CraftingRecipe recipe)
    {
        selectedRecipe = recipe;

        // Desmarca todos os slots
        foreach (var slot in recipeSlots)
        {
            slot.SetSelected(false);
        }

        // Marca o slot da receita selecionada
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

    /// <summary>
    /// Chamado pelo botão "CRAFTAR".
    /// Executa o craft da receita atualmente selecionada.
    /// </summary>
    private void OnCraftButtonClicked()
    {
        if (selectedRecipe == null || CraftingManager.Instance == null) return;

        if (CraftingManager.Instance.Craft(selectedRecipe))
        {
            Debug.Log($"[CRAFTING UI] Craft realizado: {selectedRecipe.recipeName}");
        }
    }

    /// <summary>
    /// Chamado pelo evento CraftingManager.OnCraftCompleted.
    /// Atualiza toda a UI após um craft bem-sucedido.
    /// </summary>
    private void OnCraftCompleted(CraftingRecipe recipe)
    {
        RefreshUI();
    }

    // ─── HELPERS ────────────────────────────────────────────────────────────

    /// <summary>
    /// Retorna uma descrição legível do efeito de um equipamento.
    /// </summary>
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

    /// <summary>
    /// Cria um slot visual para uma melhoria craftada na seção de equipamentos.
    /// Este é o único elemento criado via código, pois a quantidade de melhorias é dinâmica.
    /// </summary>
    private GameObject CreateEquipmentSlot(EquipmentData equip)
    {
        bool isEquipped = EquipmentManager.Instance.IsEquipped(equip.equipmentId);

        // Container do slot
        GameObject slotObj = new GameObject("EquipSlot_" + equip.equipmentId);
        RectTransform r = slotObj.AddComponent<RectTransform>();
        r.sizeDelta = new Vector2(110f, 0f);

        slotObj.AddComponent<CanvasRenderer>();
        Image bg = slotObj.AddComponent<Image>();
        bg.color = new Color(0.12f, 0.12f, 0.18f, 0.95f);
        bg.raycastTarget = false;

        // Nome da melhoria
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
        nameText.fontSize = 10f;
        nameText.fontStyle = FontStyles.Bold;
        nameText.color = new Color(0.9f, 0.88f, 0.95f);
        nameText.alignment = TextAlignmentOptions.Center;
        nameText.raycastTarget = false;
        nameText.enableWordWrapping = true;

        // Texto do efeito
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
        effectText.fontSize = 9f;
        effectText.color = new Color(0.6f, 0.85f, 0.6f);
        effectText.alignment = TextAlignmentOptions.Center;
        effectText.raycastTarget = false;

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
        btnText.fontSize = 10f;
        btnText.fontStyle = FontStyles.Bold;
        btnText.color = Color.white;
        btnText.alignment = TextAlignmentOptions.Center;
        btnText.raycastTarget = false;

        return slotObj;
    }
}
