using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Gerencia toda a lógica de crafting do jogo.
/// Singleton — persiste entre cenas via DontDestroyOnLoad.
///
/// RESPONSABILIDADES:
///   • Verificar se o jogador tem materiais suficientes (CanCraft)
///   • Consumir materiais da Bolsa Sintética (baseResources no SaveManager)
///   • Entregar o resultado (item ou melhoria equipável)
///   • Disparar eventos para a UI se atualizar
///   • Salvar progresso após cada craft
///
/// DEPENDÊNCIAS:
///   - SaveManager.instance  (leitura/escrita de baseResources)
///   - ItemDatabase.Instance (lookup de ItemData para ícones/nomes)
///   - EquipmentManager      (para entregar melhorias craftadas)
/// </summary>
public class CraftingManager : MonoBehaviour
{
    public static CraftingManager Instance { get; private set; }

    [Header("Receitas Disponíveis")]
    [Tooltip("Arraste todos os ScriptableObjects de CraftingRecipe aqui")]
    public List<CraftingRecipe> allRecipes = new List<CraftingRecipe>();

    // ─── EVENTOS ─────────────────────────────────────────────────────────────

    /// <summary>Disparado após um craft bem-sucedido. Passa a receita craftada.</summary>
    public static event Action<CraftingRecipe> OnCraftCompleted;

    /// <summary>Disparado quando as receitas disponíveis mudam (reload).</summary>
    public static event Action OnRecipesChanged;

    // ─── CICLO DE VIDA ────────────────────────────────────────────────────────

    void Awake()
    {
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
        // Tenta carregar receitas do Resources se a lista estiver vazia
        if (allRecipes.Count == 0)
        {
            CraftingRecipe[] loaded = Resources.LoadAll<CraftingRecipe>("");
            if (loaded.Length > 0)
            {
                allRecipes.AddRange(loaded);
                Debug.Log($"[CRAFTING] {loaded.Length} receitas carregadas do Resources.");
            }
        }

        Debug.Log($"[CRAFTING] CraftingManager inicializado com {allRecipes.Count} receitas.");
    }

    // ─── API PRINCIPAL ────────────────────────────────────────────────────────

    /// <summary>
    /// Verifica se o jogador possui todos os ingredientes necessários
    /// para uma receita na Bolsa Sintética (baseResources).
    /// </summary>
    public bool CanCraft(CraftingRecipe recipe)
    {
        if (recipe == null || SaveManager.instance == null) return false;

        foreach (var ingredient in recipe.ingredients)
        {
            int available = SaveManager.instance.GetBaseResourceCount(ingredient.itemId);
            if (available < ingredient.quantity)
                return false;
        }

        // Se o resultado é Equipment, verifica maxStack
        if (recipe.resultType == CraftingResultType.Equipment && recipe.resultEquipment != null)
        {
            EquipmentData equip = recipe.resultEquipment;
            if (equip.maxStack > 0)
            {
                int owned = SaveManager.instance.GetCraftedEquipmentCount(equip.equipmentId);
                if (owned >= equip.maxStack)
                {
                    Debug.Log($"[CRAFTING] Limite de stack atingido para {equip.equipmentName} ({owned}/{equip.maxStack})");
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Executa o craft: consome materiais, entrega resultado, salva e dispara eventos.
    /// Retorna true se o craft foi bem-sucedido.
    /// </summary>
    public bool Craft(CraftingRecipe recipe)
    {
        if (!CanCraft(recipe))
        {
            Debug.LogWarning($"[CRAFTING] Não é possível craftar: {recipe?.recipeName ?? "null"}");
            return false;
        }

        // 1. Consome ingredientes da Bolsa Sintética
        foreach (var ingredient in recipe.ingredients)
        {
            bool removed = SaveManager.instance.RemoveResourceFromBase(ingredient.itemId, ingredient.quantity);
            if (!removed)
            {
                Debug.LogError($"[CRAFTING] Erro ao remover ingrediente {ingredient.itemId}! Craft abortado.");
                return false;
            }
        }

        // 2. Entrega o resultado
        switch (recipe.resultType)
        {
            case CraftingResultType.Item:
                // Adiciona o item resultado à Bolsa Sintética
                if (!string.IsNullOrEmpty(recipe.resultItemId))
                {
                    SaveManager.instance.AddResourceToBase(recipe.resultItemId, 1);
                    Debug.Log($"[CRAFTING] Item craftado: {recipe.resultItemId}");
                }
                break;

            case CraftingResultType.Equipment:
                // Registra a melhoria craftada no save
                if (recipe.resultEquipment != null)
                {
                    SaveManager.instance.AddCraftedEquipment(recipe.resultEquipment.equipmentId);
                    Debug.Log($"[CRAFTING] Melhoria craftada: {recipe.resultEquipment.equipmentName}");
                }
                break;
        }

        // 3. Salva progresso imediatamente
        SaveManager.instance.SavePersistentData();

        // 4. Dispara evento
        OnCraftCompleted?.Invoke(recipe);

        Debug.Log($"[CRAFTING] ✓ Craft concluído: {recipe.recipeName}");
        return true;
    }

    /// <summary>
    /// Retorna a quantidade disponível de um ingrediente na Bolsa Sintética.
    /// Útil para a UI mostrar "3/5" ao lado de cada ingrediente.
    /// </summary>
    public int GetAvailableAmount(string itemId)
    {
        if (SaveManager.instance == null) return 0;
        return SaveManager.instance.GetBaseResourceCount(itemId);
    }

    /// <summary>
    /// Retorna todas as receitas cadastradas.
    /// </summary>
    public List<CraftingRecipe> GetAllRecipes()
    {
        return allRecipes;
    }
}
