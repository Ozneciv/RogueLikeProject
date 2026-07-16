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
    [Tooltip("Offset vertical do ponto de atração (0 = pés, 1.2 = peito)")]
    public float attractYOffset = 1.2f;
    [Tooltip("Velocidade de rotação visual")]
    public float rotateSpeed = 90f;

    [Header("Flutuação")]
    [Tooltip("Altura da flutuação (pra cima e pra baixo)")]
    public float bobHeight = 0.15f;
    [Tooltip("Velocidade da flutuação")]
    public float bobSpeed = 2f;

    [Header("Configurações")]
    [Tooltip("Tempo antes de poder ser coletada (evita coleta instantânea)")]
    public float pickupDelay = 0.3f;
    [Tooltip("Tempo de vida antes de desaparecer (0 = infinito)")]
    public float lifetime = 30f;

    private Transform playerTransform;
    private bool canBePickedUp = false;
    private float spawnTime;
    private float baseY;
    private bool isBeingAttracted = false;

    void Start()
    {
        spawnTime = Time.time;
        baseY = transform.position.y;

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

        // Checa se o player está perto pra atrair
        if (!isBeingAttracted && playerTransform != null)
        {
            float currentAttractDistance = attractDistance;
            PlayerAttributesDefensive defStats = playerTransform.GetComponent<PlayerAttributesDefensive>();
            if (defStats != null)
            {
                currentAttractDistance *= defStats.magnetRangeMultiplier;
            }

            float dist = Vector3.Distance(transform.position, playerTransform.position);
            if (dist < currentAttractDistance)
            {
                isBeingAttracted = true;
            }
        }

        if (isBeingAttracted && playerTransform != null)
        {
            // Atração magnética - voa direto pro peito do player
            Vector3 targetPos = playerTransform.position + Vector3.up * attractYOffset;
            transform.position = Vector3.MoveTowards(transform.position, targetPos, attractSpeed * Time.deltaTime);
        }
        else
        {
            // Flutuação (bobbing) - só quando está parada no chão
            float newY = baseY + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
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
        // Tenta encontrar o componente de essência do player no objeto e nos pais
        PlayerEssence playerEssence = player.GetComponentInParent<PlayerEssence>();
        
        // Se ainda não achar (ex: o script está num objeto irmão do colisor), procura globalmente
        if (playerEssence == null)
        {
            playerEssence = FindObjectOfType<PlayerEssence>();
        }
        
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
