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

    [Header("Efeito Pop (Juice de Spawn)")]
    public bool enablePopArc = true;
    private Vector3 popVelocity;
    private bool isPopping = true;
    private float popTime = 0.35f;
    private float currentAttractSpeed;
    private Vector3 initialScale;

    void Start()
    {
        spawnTime = Time.time;
        baseY = transform.position.y;
        initialScale = transform.localScale;
        currentAttractSpeed = attractSpeed;

        // Configura impulso de explosão física no nascimento (Pop Arc)
        if (enablePopArc)
        {
            Vector2 randomCircle = Random.insideUnitCircle.normalized * Random.Range(1.8f, 3.2f);
            popVelocity = new Vector3(randomCircle.x, Random.Range(3.5f, 5.0f), randomCircle.y);
        }
        else
        {
            isPopping = false;
        }

        // Encontra o player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }

        if (lifetime > 0)
        {
            Destroy(gameObject, lifetime);
        }
    }

    void Update()
    {
        // 1. Fase de Pop (Arco de nascimento no ar)
        if (isPopping)
        {
            popVelocity.y -= 12f * Time.deltaTime; // Gravidade do arco
            transform.position += popVelocity * Time.deltaTime;

            if (Time.time - spawnTime >= popTime || (popVelocity.y < 0 && transform.position.y <= baseY + 0.2f))
            {
                isPopping = false;
                baseY = transform.position.y;
            }
            return;
        }

        // Delay antes de poder coletar
        if (!canBePickedUp)
        {
            if (Time.time - spawnTime >= pickupDelay)
            {
                canBePickedUp = true;
            }
        }

        // Rotação suave no ar em múltiplos eixos
        transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.World);
        transform.Rotate(Vector3.right, (rotateSpeed * 0.4f) * Time.deltaTime, Space.Self);

        // Checa se o player está perto pra atrair
        if (!isBeingAttracted && playerTransform != null && canBePickedUp)
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

                EssenceVFX vfx = GetComponent<EssenceVFX>();
                if (vfx != null)
                {
                    vfx.StartFlyingTrail();
                }
            }
        }

        // 2. Fase de Atração Magnética Fluida e Acelerada
        if (isBeingAttracted && playerTransform != null)
        {
            currentAttractSpeed = Mathf.Min(currentAttractSpeed + 28f * Time.deltaTime, 26f);
            Vector3 targetPos = playerTransform.position + Vector3.up * attractYOffset;

            // Movimento curvado e acelerado
            transform.position = Vector3.MoveTowards(transform.position, targetPos, currentAttractSpeed * Time.deltaTime);

            float distToChest = Vector3.Distance(transform.position, targetPos);

            // Encolhe suavemente ao se aproximar do peito para dar efeito de absorção física
            if (distToChest < 1.2f)
            {
                float tScale = Mathf.Clamp01(distToChest / 1.2f);
                transform.localScale = Vector3.Lerp(Vector3.zero, initialScale, tScale);
            }

            // Absorve ao encostar
            if (distToChest < 0.35f)
            {
                CollectEssence(playerTransform.gameObject);
            }
        }
        else
        {
            // Flutuação mística (bobbing)
            float newY = baseY + Mathf.Sin((Time.time - spawnTime) * bobSpeed) * bobHeight;
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
            playerEssence = Object.FindFirstObjectByType<PlayerEssence>();
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
