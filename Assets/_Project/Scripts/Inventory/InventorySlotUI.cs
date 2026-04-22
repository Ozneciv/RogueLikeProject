using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Componente visual de um slot individual do inventário.
/// Exibe ícone do item, quantidade e borda colorida por tier.
/// Detecta hover do mouse para exibir tooltip.
/// </summary>
public class InventorySlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    // Referências internas (criadas por código)
    private Image backgroundImage;
    private Image borderImage;
    private Image iconImage;
    private TextMeshProUGUI quantityText;

    // Dados do slot
    private string currentItemId;
    private int currentQuantity;
    private ItemData currentItemData;
    private bool isOccupied = false;

    // Referência ao tooltip
    private InventoryTooltip tooltip;

    // Cores
    private static readonly Color SLOT_EMPTY_BG = new Color(0.12f, 0.12f, 0.16f, 0.9f);
    private static readonly Color SLOT_OCCUPIED_BG = new Color(0.18f, 0.18f, 0.24f, 0.95f);
    private static readonly Color SLOT_EMPTY_BORDER = new Color(0.3f, 0.3f, 0.35f, 0.6f);
    private static readonly Color SLOT_HOVER_BG = new Color(0.25f, 0.25f, 0.32f, 0.95f);

    private bool isHovered = false;

    /// <summary>
    /// Inicializa o slot criando todos os elementos visuais
    /// </summary>
    public void Initialize(InventoryTooltip tooltipRef, float slotSize)
    {
        tooltip = tooltipRef;

        int uiLayer = gameObject.layer;

        // === Background do Slot ===
        backgroundImage = gameObject.AddComponent<Image>();
        backgroundImage.color = SLOT_EMPTY_BG;
        backgroundImage.raycastTarget = true;

        RectTransform rect = GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(slotSize, slotSize);

        // === Borda ===
        GameObject borderObj = new GameObject("SlotBorder");
        borderObj.transform.SetParent(transform, false);
        borderObj.layer = uiLayer;

        RectTransform borderRect = borderObj.AddComponent<RectTransform>();
        borderRect.anchorMin = Vector2.zero;
        borderRect.anchorMax = Vector2.one;
        borderRect.sizeDelta = Vector2.zero;
        borderRect.anchoredPosition = Vector2.zero;

        borderObj.AddComponent<CanvasRenderer>();
        borderImage = borderObj.AddComponent<Image>();
        borderImage.color = SLOT_EMPTY_BORDER;
        borderImage.raycastTarget = false;
        // Fazemos a borda ser um outline usando sprite sliced
        borderImage.type = Image.Type.Sliced;
        borderImage.fillCenter = false;
        borderImage.pixelsPerUnitMultiplier = 1f;

        // === Ícone do Item ===
        GameObject iconObj = new GameObject("ItemIcon");
        iconObj.transform.SetParent(transform, false);
        iconObj.layer = uiLayer;

        RectTransform iconRect = iconObj.AddComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.1f, 0.15f);
        iconRect.anchorMax = new Vector2(0.9f, 0.95f);
        iconRect.sizeDelta = Vector2.zero;
        iconRect.anchoredPosition = Vector2.zero;

        iconObj.AddComponent<CanvasRenderer>();
        iconImage = iconObj.AddComponent<Image>();
        iconImage.preserveAspect = true;
        iconImage.raycastTarget = false;
        iconImage.enabled = false; // Escondido até ter item

        // === Texto de Quantidade ===
        GameObject textObj = new GameObject("Quantity");
        textObj.transform.SetParent(transform, false);
        textObj.layer = uiLayer;

        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.0f, 0.0f);
        textRect.anchorMax = new Vector2(1.0f, 0.35f);
        textRect.sizeDelta = Vector2.zero;
        textRect.anchoredPosition = Vector2.zero;

        textObj.AddComponent<CanvasRenderer>();
        quantityText = textObj.AddComponent<TextMeshProUGUI>();
        quantityText.fontSize = slotSize * 0.22f;
        quantityText.color = new Color(0.9f, 0.9f, 0.9f, 0.95f);
        quantityText.alignment = TextAlignmentOptions.BottomRight;
        quantityText.fontStyle = FontStyles.Bold;
        quantityText.raycastTarget = false;
        quantityText.margin = new Vector4(0, 0, 4, 2);
        quantityText.enabled = false;

        SetEmpty();
    }

    /// <summary>
    /// Atualiza o slot com dados de um item
    /// </summary>
    public void SetItem(string itemId, int quantity, ItemData itemData)
    {
        currentItemId = itemId;
        currentQuantity = quantity;
        currentItemData = itemData;
        isOccupied = true;

        // Background mais claro quando ocupado
        if (!isHovered)
            backgroundImage.color = SLOT_OCCUPIED_BG;

        // Ícone
        if (itemData != null && itemData.icon != null)
        {
            iconImage.sprite = itemData.icon;
            iconImage.enabled = true;
        }
        else
        {
            // Fallback: mostra slot ocupado sem ícone (item sem sprite configurado)
            iconImage.enabled = false;
        }

        // Quantidade
        if (quantity > 1)
        {
            quantityText.text = "x" + quantity;
            quantityText.enabled = true;
        }
        else
        {
            quantityText.text = "";
            quantityText.enabled = false;
        }

        // Borda com cor do Tier
        if (itemData != null)
        {
            Color tierColor = itemData.GetTierColor();
            borderImage.color = new Color(tierColor.r, tierColor.g, tierColor.b, 0.8f);
        }
        else
        {
            borderImage.color = SLOT_EMPTY_BORDER;
        }
    }

    /// <summary>
    /// Limpa o slot (slot vazio)
    /// </summary>
    public void SetEmpty()
    {
        currentItemId = null;
        currentQuantity = 0;
        currentItemData = null;
        isOccupied = false;

        if (!isHovered)
            backgroundImage.color = SLOT_EMPTY_BG;

        iconImage.enabled = false;
        quantityText.enabled = false;
        borderImage.color = SLOT_EMPTY_BORDER;
    }

    // === Hover Events para Tooltip ===

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;

        if (isOccupied)
        {
            backgroundImage.color = SLOT_HOVER_BG;

            if (tooltip != null && currentItemData != null)
            {
                tooltip.Show(currentItemData, currentQuantity);
            }
        }
        else
        {
            backgroundImage.color = new Color(SLOT_EMPTY_BG.r + 0.05f, SLOT_EMPTY_BG.g + 0.05f, SLOT_EMPTY_BG.b + 0.05f, SLOT_EMPTY_BG.a);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;

        backgroundImage.color = isOccupied ? SLOT_OCCUPIED_BG : SLOT_EMPTY_BG;

        if (tooltip != null)
        {
            tooltip.Hide();
        }
    }

    // === Click Event para enviar o item para o Upgrade (Infusão) ===

    public void OnPointerClick(PointerEventData eventData)
    {
        // Só faz algo se fomos clicados e se tem um item aqui dentro
        if (isOccupied && !string.IsNullOrEmpty(currentItemId))
        {
            // Tenta procurar na cena atual se existe a Tela de Infusão (Upgrades)
            // (Isso permite que o código nunca quebre mesmo se vc não tiver a tela instalada)
            InfusionUI telaDeUpgrades = Object.FindFirstObjectByType<InfusionUI>(FindObjectsInactive.Include);
            
            if (telaDeUpgrades != null)
            {
                // Manda abrir o painel imediatamente (já que você achou o outro botão inútil!)
                telaDeUpgrades.OpenPanel();
                
                // Manda o item pra lá
                telaDeUpgrades.SelectItem(currentItemId);
                Debug.Log($"[SLOT CLICADO] Item de ID: {currentItemId} mandou abrir a janela de Upgrades e foi pra Bigorna central.");
            }
        }
    }
}
