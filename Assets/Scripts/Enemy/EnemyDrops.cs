using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Componente de Drop para Inimigos
/// Adicione este script a qualquer inimigo com DummyHealth
/// Configura os drops de essência e itens característicos
/// </summary>
public class EnemyDrops : MonoBehaviour
{
    [Header("Essência (Sempre Dropa)")]
    [Tooltip("Prefab da essência que spawna")]
    public GameObject essencePrefab;
    [Tooltip("Quantidade de essência dropada")]
    public int essenceAmount = 10;
    [Tooltip("Variação aleatória (+/-)")]
    public int essenceVariation = 3;

    [Header("Item Característico (Chance)")]
    [Tooltip("Prefab do item específico deste inimigo")]
    public GameObject characteristicItemPrefab;
    [Tooltip("Chance de dropar o item (0-1)")]
    [Range(0f, 1f)]
    public float itemDropChance = 0.3f;
    [Tooltip("Quantidade de itens dropados se der drop")]
    public int itemAmount = 1;

    [System.Serializable]
    public class LootPoolItem
    {
        [Tooltip("Prefab do item possível")]
        public GameObject itemPrefab;
        [Tooltip("Peso no sorteio (maior = mais comum)")]
        public float weight = 1f;
    }

    [Header("Loot Pool por Tier (Opcional)")]
    [Tooltip("Se preencher, usa roleta ponderada entre T1-T4. Se vazio, usa characteristicItemPrefab.")]
    public List<LootPoolItem> lootPool = new List<LootPoolItem>();

    [Header("Spawn Settings")]
    [Tooltip("Altura acima do inimigo para spawnar drops")]
    public float spawnHeight = 0.5f;
    [Tooltip("Raio de dispersão dos drops")]
    public float spawnRadius = 0.5f;
    [Tooltip("Força de impulso para cima")]
    public float launchForce = 3f;

    private DummyHealth health;

    void Start()
    {
        health = GetComponent<DummyHealth>();
        
        if (health == null)
        {
            Debug.LogWarning("[ENEMY DROPS] DummyHealth não encontrado! Este script requer DummyHealth.");
        }
    }

    /// <summary>
    /// Chamado pelo DummyHealth quando o inimigo morre
    /// </summary>
    public void OnDeath()
    {
        SpawnDrops();
    }

    void SpawnDrops()
    {
        Debug.Log("[ENEMY DROPS] SpawnDrops chamado para " + gameObject.name);
        
        Vector3 basePosition = transform.position + Vector3.up * spawnHeight;

        // 1. Sempre spawna essência
        if (essencePrefab != null)
        {
            int finalEssence = essenceAmount + Random.Range(-essenceVariation, essenceVariation + 1);
            finalEssence = Mathf.Max(1, finalEssence);

            GameObject essence = SpawnDrop(essencePrefab, basePosition);
            
            // Configura a quantidade de essência
            EssencePickup essenceScript = essence.GetComponent<EssencePickup>();
            if (essenceScript != null)
            {
                essenceScript.essenceValue = finalEssence;
            }

            Debug.Log("[ENEMY DROPS] Dropou " + finalEssence + " de essência!");
        }
        else
        {
            Debug.LogWarning("[ENEMY DROPS] essencePrefab está NULL! Configure no Inspector.");
        }

        // 2. Chance de dropar item característico
        if (Random.value <= itemDropChance)
        {
            GameObject selectedItem = SelectDropPrefab();
            if (selectedItem == null)
            {
                Debug.LogWarning("[ENEMY DROPS] Nenhum item configurado em lootPool/characteristicItemPrefab.");
                return;
            }

            for (int i = 0; i < itemAmount; i++)
            {
                Vector3 offset = Random.insideUnitSphere * spawnRadius;
                offset.y = 0;
                
                SpawnDrop(selectedItem, basePosition + offset);
            }

            Debug.Log("[ENEMY DROPS] Dropou item característico: " + selectedItem.name);
        }
        else if ((lootPool == null || lootPool.Count == 0) && characteristicItemPrefab == null)
        {
            Debug.LogWarning("[ENEMY DROPS] characteristicItemPrefab e lootPool estão vazios! Configure no Inspector.");
        }
    }

    GameObject SelectDropPrefab()
    {
        // Se houver pool configurado, usa roleta ponderada
        if (lootPool != null && lootPool.Count > 0)
        {
            float totalWeight = 0f;
            for (int i = 0; i < lootPool.Count; i++)
            {
                if (lootPool[i] != null && lootPool[i].itemPrefab != null && lootPool[i].weight > 0f)
                {
                    totalWeight += lootPool[i].weight;
                }
            }

            if (totalWeight > 0f)
            {
                float roll = Random.Range(0f, totalWeight);
                float acc = 0f;

                for (int i = 0; i < lootPool.Count; i++)
                {
                    var entry = lootPool[i];
                    if (entry == null || entry.itemPrefab == null || entry.weight <= 0f) continue;

                    acc += entry.weight;
                    if (roll <= acc)
                    {
                        return entry.itemPrefab;
                    }
                }
            }
        }

        // Fallback para compatibilidade com configuração antiga
        return characteristicItemPrefab;
    }

    GameObject SpawnDrop(GameObject prefab, Vector3 position)
    {
        GameObject drop = Instantiate(prefab, position, Quaternion.identity);

        // Configura rigidbody para não ter drift
        Rigidbody rb = drop.GetComponent<Rigidbody>();
        if (rb != null)
        {
            // Alto drag para parar rápido quando atingir o chão
            rb.linearDamping = 5f;
            rb.angularDamping = 5f;
            
            // Apenas impulso vertical (sem movimento lateral)
            Vector3 upForce = Vector3.up * launchForce;
            rb.AddForce(upForce, ForceMode.Impulse);
        }

        return drop;
    }
}
