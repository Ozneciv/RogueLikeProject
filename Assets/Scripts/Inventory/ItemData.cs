using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Todos os atributos possíveis no jogo.
/// </summary>
public enum AttributeType
{
    // === OFENSIVOS ===
    BaseDamageMultiplier, 
    AttackSpeedMelee, 
    CritChance, 
    CritMultiplier, 
    Knockback, 
    WeaponRangeMelee, 
    WeaponRangeProjectile, 
    Piercing, 
    BounceChance, 
    BounceCount, 
    MultiShotChance, 
    Spread,
    
    // === DEFENSIVOS E VIDA ===
    MaxHealth, 
    MaxArmor, 
    ArmorRegen, 
    DodgeChance, 
    DamageNegation, 
    Thorns,
    
    // === MOBILIDADE ===
    SpeedMultiplier, 
    DashCooldownMultiplier, 
    DashCounts, 
    DashInvulnerability
}

[System.Serializable]
public class ItemAttributeParam
{
    public AttributeType attributeType;
    [Tooltip("O valor do buff (ex: 0.2 para +20% ou 50 para +50 vida)")]
    public float value;
    [Tooltip("Esse valor deve multiplicar o atributo atual ou ser somado?")]
    public bool isMultiplier = false;
}

/// <summary>
/// ScriptableObject que define os dados visuais e de classificação de um item.
/// Crie assets via: Assets > Create > Inventory > Item Data
/// 
/// EXTENSÃO FUTURA: Adicione campos como buffType, buffValue, craftingRecipe, etc.
/// </summary>
[CreateAssetMenu(fileName = "NewItem", menuName = "Inventory/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("Identificação")]
    [Tooltip("ID único do item (deve ser igual ao itemId do CharacteristicItemPickup)")]
    public string itemId;

    [Tooltip("Nome de exibição do item")]
    public string itemName;

    [TextArea(2, 4)]
    [Tooltip("Descrição do item para tooltip")]
    public string description;

    [Header("Visual")]
    [Tooltip("Ícone do item exibido no inventário")]
    public Sprite icon;

    [Header("Classificação")]
    public ItemTier tier = ItemTier.Common;

    [Tooltip("Nome do inimigo de origem (ex: Spider, Golem)")]
    public string enemySource;

    [Header("Upgrades e Infusão")]
    [Tooltip("Essência gerada se o item for reciclado (descartado).")]
    public int recycleEssenceValue = 10;
    
    [Tooltip("Custo de essência cobrado do jogador para autorizar a Infusão deste item.")]
    public int infusionEssenceCost = 50;

    [Tooltip("Use o '+' para adicionar quantos buffs o item der!")]
    public List<ItemAttributeParam> itemAttributes = new List<ItemAttributeParam>();

    /// <summary>
    /// Retorna a cor associada ao Tier do item
    /// </summary>
    public Color GetTierColor()
    {
        switch (tier)
        {
            case ItemTier.Common:    return new Color(0.85f, 0.85f, 0.85f); // Branco/Cinza claro
            case ItemTier.Uncommon:  return new Color(0.30f, 0.85f, 0.30f); // Verde
            case ItemTier.Rare:      return new Color(0.30f, 0.50f, 1.00f); // Azul
            case ItemTier.Legendary: return new Color(1.00f, 0.84f, 0.00f); // Dourado
            default:                 return Color.white;
        }
    }

    /// <summary>
    /// Retorna o nome localizado do Tier
    /// </summary>
    public string GetTierName()
    {
        switch (tier)
        {
            case ItemTier.Common:    return "Comum";
            case ItemTier.Uncommon:  return "Incomum";
            case ItemTier.Rare:      return "Raro";
            case ItemTier.Legendary: return "Lendário";
            default:                 return "Desconhecido";
        }
    }
}

/// <summary>
/// Tiers de raridade dos itens (GDD 3.7.2)
/// </summary>
public enum ItemTier
{
    Common,     // T1 - Branco
    Uncommon,   // T2 - Verde
    Rare,       // T3 - Azul
    Legendary   // T4 - Dourado
}
