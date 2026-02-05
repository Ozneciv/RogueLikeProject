using UnityEngine;

/// <summary>
/// Pickup de Item Característico do Inimigo
/// Cada mob dropa um item único que dará buffs ao player
/// </summary>
public class CharacteristicItemPickup : MonoBehaviour
{
    [Header("Identificação do Item")]
    [Tooltip("ID único do item (ex: spider_silk, golem_core, crystal_shard)")]
    public string itemId = "unknown";
    [Tooltip("Nome de exibição do item")]
    public string itemName = "Item Misterioso";
    [Tooltip("Descrição do item")]
    [TextArea(2, 4)]
    public string itemDescription = "Um item misterioso dropado por um inimigo.";

    [Header("Visual")]
    public float rotateSpeed = 45f;
    public float bobSpeed = 2f;
    public float bobHeight = 0.2f;

    [Header("Coleta")]
    public float pickupDelay = 0.5f;
    public float lifetime = 60f; // Itens duram mais que essência

    private Vector3 startPosition;
    private float spawnTime;
    private bool canBePickedUp = false;

    void Start()
    {
        startPosition = transform.position;
        spawnTime = Time.time;

        if (lifetime > 0)
        {
            Destroy(gameObject, lifetime);
        }
    }

    void Update()
    {
        // Delay antes de poder coletar
        if (!canBePickedUp)
        {
            if (Time.time - spawnTime >= pickupDelay)
            {
                canBePickedUp = true;
            }
        }

        // Rotação e movimento de flutuação
        transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime);
        
        float newY = startPosition.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!canBePickedUp) return;

        if (other.CompareTag("Player"))
        {
            CollectItem(other.gameObject);
        }
    }

    void CollectItem(GameObject player)
    {
        // Tenta encontrar o inventário do player
        PlayerInventory inventory = player.GetComponent<PlayerInventory>();
        
        if (inventory != null)
        {
            inventory.AddItem(itemId, 1);
            Debug.Log("[ITEM] Coletou: " + itemName + " (ID: " + itemId + ")");
        }
        else
        {
            Debug.Log("[ITEM] Coletou: " + itemName + " (PlayerInventory não encontrado, apenas log)");
        }

        // VFX/SFX de coleta aqui se quiser

        Destroy(gameObject);
    }
}
