using UnityEngine;

/// <summary>
/// Pickup de Essência - A "moeda XP" do jogo
/// Player coleta ao encostar e ganha pontos de essência
/// </summary>
public class EssencePickup : MonoBehaviour
{
    [Header("Valor")]
    [Tooltip("Quantidade de essência que este pickup dá")]
    public int essenceValue = 10;

    [Header("Movimento")]
    [Tooltip("Velocidade de atração quando perto do player")]
    public float attractSpeed = 8f;
    [Tooltip("Distância para começar a atrair")]
    public float attractDistance = 3f;
    [Tooltip("Velocidade de rotação visual")]
    public float rotateSpeed = 90f;

    [Header("Configurações")]
    [Tooltip("Tempo antes de poder ser coletada (evita coleta instantânea)")]
    public float pickupDelay = 0.3f;
    [Tooltip("Tempo de vida antes de desaparecer (0 = infinito)")]
    public float lifetime = 30f;

    private Transform playerTransform;
    private bool canBePickedUp = false;
    private float spawnTime;

    void Start()
    {
        spawnTime = Time.time;

        // Encontra o player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }

        // Destroi após lifetime se configurado
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
            return;
        }

        // Rotação visual
        transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime);

        // Atração magnética para o player
        if (playerTransform != null)
        {
            float dist = Vector3.Distance(transform.position, playerTransform.position);
            
            if (dist < attractDistance)
            {
                Vector3 direction = (playerTransform.position - transform.position).normalized;
                transform.position += direction * attractSpeed * Time.deltaTime;
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!canBePickedUp) return;

        if (other.CompareTag("Player"))
        {
            CollectEssence(other.gameObject);
        }
    }

    void CollectEssence(GameObject player)
    {
        // Tenta encontrar o componente de essência do player
        PlayerEssence playerEssence = player.GetComponent<PlayerEssence>();
        
        if (playerEssence != null)
        {
            playerEssence.AddEssence(essenceValue);
            Debug.Log("[ESSENCE] Coletou " + essenceValue + " de essência!");
        }
        else
        {
            Debug.Log("[ESSENCE] Coletou " + essenceValue + " de essência! (PlayerEssence não encontrado, apenas log)");
        }

        // VFX de coleta
        EssenceVFX vfx = GetComponent<EssenceVFX>();
        if (vfx != null)
        {
            vfx.PlayCollectEffect();
        }

        Destroy(gameObject);
    }
}
