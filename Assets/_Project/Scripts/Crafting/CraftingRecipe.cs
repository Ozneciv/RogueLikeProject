using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Ingrediente de uma receita: referência ao item e quantidade necessária.
/// </summary>
[System.Serializable]
public class CraftingIngredient
{
    [Tooltip("ID do item necessário (deve existir no ItemDatabase)")]
    public string itemId;

    [Tooltip("Quantidade necessária deste item")]
    public int quantity = 1;
}

/// <summary>
/// Tipo de resultado que a receita produz.
/// </summary>
public enum CraftingResultType
{
    Item,       // Produz um item comum (volta pro inventário/bolsa)
    Equipment   // Produz uma melhoria equipável
}

/// <summary>
/// ScriptableObject que define uma receita de crafting.
/// Crie assets via: Assets > Create > Crafting > Recipe
///
/// EXEMPLO:
///   Receita "Expansão de Inventário"
///   Ingredientes: 5x shard_splinter_t1 + 3x magic_dust_t1
///   Resultado: EquipmentData de expansão de slots
/// </summary>
[CreateAssetMenu(fileName = "NewRecipe", menuName = "Crafting/Recipe")]
public class CraftingRecipe : ScriptableObject
{
    [Header("Identificação")]
    [Tooltip("ID único da receita")]
    public string recipeId;

    [Tooltip("Nome de exibição da receita")]
    public string recipeName;

    [TextArea(2, 4)]
    [Tooltip("Descrição da receita para a UI")]
    public string description;

    [Tooltip("Ícone da receita exibido na UI de crafting")]
    public Sprite icon;

    [Header("Ingredientes")]
    [Tooltip("Lista de materiais necessários para craftar")]
    public List<CraftingIngredient> ingredients = new List<CraftingIngredient>();

    [Header("Resultado")]
    [Tooltip("Tipo de resultado: Item comum ou Equipment (melhoria equipável)")]
    public CraftingResultType resultType = CraftingResultType.Equipment;

    [Tooltip("Se resultType == Item, o ID do item produzido")]
    public string resultItemId;

    [Tooltip("Se resultType == Equipment, a melhoria produzida")]
    public EquipmentData resultEquipment;
}
