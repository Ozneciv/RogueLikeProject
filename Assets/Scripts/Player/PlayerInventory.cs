using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Inventário do Player para itens característicos
/// Armazena materiais dropados por inimigos para crafting/buffs futuros
/// </summary>
public class PlayerInventory : MonoBehaviour
{
    [Header("Inventário")]
    [Tooltip("Dicionário de itens coletados (ID -> Quantidade)")]
    private Dictionary<string, int> items = new Dictionary<string, int>();

    [Header("Eventos")]
    public UnityEngine.Events.UnityEvent<string, int> onItemAdded;
    public UnityEngine.Events.UnityEvent<string, int> onItemRemoved;

    /// <summary>
    /// Adiciona um item ao inventário
    /// </summary>
    public void AddItem(string itemId, int amount = 1)
    {
        if (items.ContainsKey(itemId))
        {
            items[itemId] += amount;
        }
        else
        {
            items[itemId] = amount;
        }

        Debug.Log("[INVENTORY] +" + amount + " " + itemId + " | Total: " + items[itemId]);
        onItemAdded?.Invoke(itemId, items[itemId]);
    }

    /// <summary>
    /// Remove um item do inventário
    /// </summary>
    public bool RemoveItem(string itemId, int amount = 1)
    {
        if (!items.ContainsKey(itemId) || items[itemId] < amount)
        {
            Debug.Log("[INVENTORY] Não possui " + amount + "x " + itemId);
            return false;
        }

        items[itemId] -= amount;
        
        if (items[itemId] <= 0)
        {
            items.Remove(itemId);
        }

        Debug.Log("[INVENTORY] -" + amount + " " + itemId);
        onItemRemoved?.Invoke(itemId, GetItemCount(itemId));
        return true;
    }

    /// <summary>
    /// Retorna a quantidade de um item
    /// </summary>
    public int GetItemCount(string itemId)
    {
        return items.ContainsKey(itemId) ? items[itemId] : 0;
    }

    /// <summary>
    /// Verifica se possui determinada quantidade de um item
    /// </summary>
    public bool HasItem(string itemId, int amount = 1)
    {
        return GetItemCount(itemId) >= amount;
    }

    /// <summary>
    /// Retorna todos os itens do inventário
    /// </summary>
    public Dictionary<string, int> GetAllItems()
    {
        return new Dictionary<string, int>(items);
    }

    /// <summary>
    /// Debug: Lista todos os itens no console
    /// </summary>
    public void DebugListItems()
    {
        Debug.Log("=== INVENTÁRIO ===");
        foreach (var item in items)
        {
            Debug.Log(item.Key + ": " + item.Value);
        }
        Debug.Log("==================");
    }
}
