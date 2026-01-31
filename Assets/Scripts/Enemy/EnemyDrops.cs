using UnityEngine;

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
        if (characteristicItemPrefab != null && Random.value <= itemDropChance)
        {
            for (int i = 0; i < itemAmount; i++)
            {
                Vector3 offset = Random.insideUnitSphere * spawnRadius;
                offset.y = 0;
                
                SpawnDrop(characteristicItemPrefab, basePosition + offset);
            }

            Debug.Log("[ENEMY DROPS] Dropou item característico!");
        }
        else if (characteristicItemPrefab == null)
        {
            Debug.LogWarning("[ENEMY DROPS] characteristicItemPrefab está NULL! Configure no Inspector.");
        }
    }

    GameObject SpawnDrop(GameObject prefab, Vector3 position)
    {
        GameObject drop = Instantiate(prefab, position, Quaternion.identity);

        // Aplica impulso para cima para efeito visual
        Rigidbody rb = drop.GetComponent<Rigidbody>();
        if (rb != null)
        {
            Vector3 randomDir = Random.insideUnitSphere;
            randomDir.y = Mathf.Abs(randomDir.y); // Sempre para cima
            rb.AddForce(randomDir * launchForce, ForceMode.Impulse);
        }

        return drop;
    }
}
