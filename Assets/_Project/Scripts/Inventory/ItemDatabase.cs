using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Singleton que registra todos os ItemData do jogo para busca rápida por itemId.
/// Adicione este componente a um GameObject persistente na cena (ex: GameManager).
/// Arraste todos os ScriptableObjects de ItemData para a lista "allItems" no Inspector.
/// </summary>
public class ItemDatabase : MonoBehaviour
{
    public static ItemDatabase Instance { get; private set; }

    [Header("Todos os Itens do Jogo")]
    [Tooltip("Arraste todos os ScriptableObjects de ItemData aqui")]
    public List<ItemData> allItems = new List<ItemData>();

    private Dictionary<string, ItemData> itemLookup = new Dictionary<string, ItemData>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Constrói dicionário para busca rápida por itemId
        itemLookup.Clear();
        foreach (var item in allItems)
        {
            if (item == null) continue;

            if (itemLookup.ContainsKey(item.itemId))
            {
                Debug.LogWarning("[ITEM DATABASE] ID duplicado encontrado: " + item.itemId);
                continue;
            }

            itemLookup[item.itemId] = item;
        }

        Debug.Log("[ITEM DATABASE] " + itemLookup.Count + " itens registrados.");
    }

    /// <summary>
    /// Busca os dados de um item pelo seu ID.
    /// Retorna null se o item não estiver registrado.
    /// </summary>
    public ItemData GetItemData(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return null;
        return itemLookup.TryGetValue(itemId, out var data) ? data : null;
    }

    /// <summary>
    /// Verifica se um item está registrado no banco de dados.
    /// </summary>
    public bool HasItem(string itemId)
    {
        return !string.IsNullOrEmpty(itemId) && itemLookup.ContainsKey(itemId);
    }

    /// <summary>
    /// Retorna todos os itens registrados.
    /// </summary>
    public List<ItemData> GetAllItems()
    {
        return new List<ItemData>(allItems);
    }
}
