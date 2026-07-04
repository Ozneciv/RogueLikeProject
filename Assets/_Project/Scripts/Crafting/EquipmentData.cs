using UnityEngine;

/// <summary>
/// Tipos de efeito que uma melhoria pode aplicar.
/// Extensível para novos tipos de melhoria no futuro.
/// </summary>
public enum EquipmentEffectType
{
    InventorySlotExpansion,  // Aumenta maxSlots do inventário de run
    MaxHealthBoost,          // Aumenta vida máxima
    MaxArmorBoost,           // Aumenta armadura máxima
    SpeedBoost,              // Aumenta velocidade
    DamageBoost,             // Aumenta dano base
    CritChanceBoost,         // Aumenta chance de crítico
    ArmorRegenBoost          // Aumenta regeneração de armadura
}

/// <summary>
/// ScriptableObject que define uma melhoria equipável.
/// Crie assets via: Assets > Create > Crafting > Equipment Data
///
/// Melhorias são produzidas pelo sistema de Crafting e podem ser
/// equipadas/desequipadas pelo jogador na Mesa de Trabalho.
///
/// EXEMPLO:
///   "Expansão de Inventário Nv.1"
///   Efeito: InventorySlotExpansion, valor: 5
///   → Ao equipar, inventário ganha +5 slots
///   → Ao desequipar, perde os 5 slots extras
/// </summary>
[CreateAssetMenu(fileName = "NewEquipment", menuName = "Crafting/Equipment Data")]
public class EquipmentData : ScriptableObject
{
    [Header("Identificação")]
    [Tooltip("ID único da melhoria (usado no save)")]
    public string equipmentId;

    [Tooltip("Nome de exibição")]
    public string equipmentName;

    [TextArea(2, 4)]
    [Tooltip("Descrição da melhoria")]
    public string description;

    [Tooltip("Ícone exibido na UI")]
    public Sprite icon;

    [Header("Efeito")]
    [Tooltip("Tipo de efeito aplicado ao equipar")]
    public EquipmentEffectType effectType = EquipmentEffectType.InventorySlotExpansion;

    [Tooltip("Valor do efeito (ex: 5 para +5 slots, 20 para +20 HP)")]
    public float effectValue = 5f;

    [Header("Limites")]
    [Tooltip("Quantas cópias desta melhoria o jogador pode possuir (0 = ilimitado)")]
    public int maxStack = 0;
}
