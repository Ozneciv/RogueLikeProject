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

    [System.Serializable]
    public class LootPoolItem
    {
        public GameObject itemPrefab;
        [Tooltip("Peso de chance para dropar. Maior = mais comum. Menor = mais raro.")]
        public float weight = 10f;
    }

    [Header("Item Característico (Roleta)")]
    [Tooltip("Chance do inimigo dropar ALGUM item da lista (0-1)")]
    [Range(0f, 1f)]
    public float globalDropChance = 0.3f;
    [Tooltip("Quantidade de itens dropados se der drop (da opção sorteada)")]
    public int itemAmount = 1;
    [Tooltip("Lista de possíveis drops (T1, T2, T3...). Apenas 1 será sorteado.")]
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

        // --- SISTEMA DE PACTOS DO JOGADOR ---
        bool doubleLoot = false;
        PlayerHealth playerHealth = FindFirstObjectByType<PlayerHealth>();
        if (playerHealth != null && playerHealth.hasDoubleLoot)
        {
            doubleLoot = true;
            Debug.Log("[ENEMY DROPS] A Ganância ativada! Drop em dobro!");
        }
        // ------------------------------------

        // 1. Sempre spawna essência com inflação por sala
        // Fórmula GDD §1.1: E(n) = d × (1 + α × n)  onde α = 0,05
        if (essencePrefab != null)
        {
            // Aplica o multiplicador de sala (d é o essenceAmount base)
            float roomMultiplier = RunManager.instance != null
                ? RunManager.instance.GetEssenceMultiplier()
                : 1f;

            int scaledBase  = Mathf.RoundToInt(essenceAmount * roomMultiplier);
            int finalEssence = scaledBase + Random.Range(-essenceVariation, essenceVariation + 1);
            finalEssence = Mathf.Max(1, finalEssence);

            if (doubleLoot) finalEssence *= 2; // Dobra a essência!

            GameObject essence = SpawnDrop(essencePrefab, basePosition);
            
            // Configura a quantidade de essência
            EssencePickup essenceScript = essence.GetComponent<EssencePickup>();
            if (essenceScript != null)
            {
                essenceScript.essenceValue = finalEssence;
            }

            Debug.Log($"[ENEMY DROPS] Dropou {finalEssence} de essência! (base:{essenceAmount} × mult:{roomMultiplier:F2} | sala {RunManager.instance?.currentRoomNumber})");
        }
        else
        {
            Debug.LogWarning("[ENEMY DROPS] essencePrefab está NULL! Configure no Inspector.");
        }

        // 2. Chance de dropar ALGUM item característico (Roleta com Pesos)
        if (lootPool != null && lootPool.Count > 0 && Random.value <= globalDropChance)
        {
            List<LootPoolItem> filteredPool = new List<LootPoolItem>();
            Geobionte_AI geobionteAI = GetComponent<Geobionte_AI>();
            if (geobionteAI != null)
            {
                bool isSentinel = geobionteAI.IsSentinel;
                foreach (var loot in lootPool)
                {
                    if (loot.itemPrefab == null) continue;
                    string nameLower = loot.itemPrefab.name.ToLower();
                    if (isSentinel && nameLower.Contains("sentinel"))
                    {
                        filteredPool.Add(loot);
                    }
                    else if (!isSentinel && !nameLower.Contains("sentinel"))
                    {
                        filteredPool.Add(loot);
                    }
                }
            }
            else
            {
                filteredPool = lootPool;
            }

            float totalWeight = 0f;
            foreach (var loot in filteredPool)
            {
                if (loot.itemPrefab != null && loot.weight > 0f)
                {
                    totalWeight += loot.weight;
                }
            }

            if (totalWeight > 0f)
            {
                float randomVal = Random.Range(0f, totalWeight);
                float currentSum = 0f;
                GameObject selectedPrefab = null;

                foreach (var loot in filteredPool)
                {
                    if (loot.itemPrefab == null || loot.weight <= 0f) continue;
                    
                    currentSum += loot.weight;
                    if (randomVal <= currentSum)
                    {
                        selectedPrefab = loot.itemPrefab;
                        break;
                    }
                }

                if (selectedPrefab != null)
                {
                    int finalItemAmount = doubleLoot ? itemAmount * 2 : itemAmount; // Dobra itens!

                    for (int i = 0; i < finalItemAmount; i++)
                    {
                        Vector3 offset = Random.insideUnitSphere * spawnRadius;
                        offset.y = 0;
                        
                        SpawnDrop(selectedPrefab, basePosition + offset);
                    }

                    Debug.Log("[ENEMY DROPS] Dropou item da roleta: " + selectedPrefab.name);
                }
            }
        }
        else if (lootPool == null || lootPool.Count == 0)
        {
            Debug.LogWarning("[ENEMY DROPS] lootPool está vazio! Adicione opções no Inspector.");
        }
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
