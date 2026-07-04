using UnityEngine;

/// <summary>
/// Coletável que aumenta o espaço máximo do inventário do player.
/// NÃO vai para o inventário — apenas expande a capacidade de slots.
/// 
/// Uso: Coloque num GameObject com Collider (isTrigger = true).
///      Quando o player encosta, o inventário ganha +slotsToAdd slots.
/// </summary>
public class StorageUpgradePickup : MonoBehaviour
{
    [Header("Upgrade")]
    [Tooltip("Quantos slots extras esse pickup adiciona ao inventário")]
    public int slotsToAdd = 1;

    [Header("Visual")]
    [Tooltip("Velocidade de rotação visual")]
    public float rotateSpeed = 60f;
    [Tooltip("Velocidade da flutuação")]
    public float bobSpeed = 2f;
    [Tooltip("Altura da flutuação")]
    public float bobHeight = 0.2f;

    [Header("Coleta")]
    [Tooltip("Tempo antes de poder ser coletado")]
    public float pickupDelay = 0.5f;
    [Tooltip("Tempo de vida antes de desaparecer (0 = infinito)")]
    public float lifetime = 60f;

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

        // Rotação e flutuação visual
        transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime);

        float newY = startPosition.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!canBePickedUp) return;

        if (other.CompareTag("Player"))
        {
            CollectUpgrade(other.gameObject);
        }
    }

    void CollectUpgrade(GameObject player)
    {
        // Encontra o inventário do player
        PlayerInventory inventory = player.GetComponent<PlayerInventory>();

        if (inventory == null)
        {
            inventory = player.GetComponentInParent<PlayerInventory>();
        }

        if (inventory == null)
        {
            inventory = FindObjectOfType<PlayerInventory>();
        }

        if (inventory == null)
        {
            Debug.LogWarning("[STORAGE UPGRADE] PlayerInventory não encontrado!");
            return;
        }

        // Aumenta a capacidade do inventário
        int oldMax = inventory.MaxSlots;
        inventory.IncreaseMaxSlots(slotsToAdd);
        Debug.Log("[STORAGE UPGRADE] Inventário expandido! " + oldMax + " -> " + inventory.MaxSlots + " slots");

        // VFX/SFX de coleta aqui se quiser

        Destroy(gameObject);
    }
}
