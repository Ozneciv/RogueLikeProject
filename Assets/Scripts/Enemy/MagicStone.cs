using UnityEngine;
using System.Collections;

public class MagicStone_AI : MonoBehaviour
{
    [Header("Referências")]
    public Transform playerTransform;
    public GameObject attackMarkerPrefab;
    public GameObject attackBeamPrefab;

    [Header("Ativação")]
    public float activationDistance = 25f;
    private bool isAwake = false;

    [Header("Comportamento de Movimento")]
    public float moveSpeed = 4f;
    public float minOrbitDistance = 12f;
    public float maxOrbitDistance = 15f;
    public int orbitDirection = 1;

    [Header("Comportamento de Flutuação")]
    public float floatHeight = 0.2f;
    public float floatSpeed = 1f;
    private float startY;

    [Header("Ataque")]
    public float attackInterval = 5f;
    public float attackTelegraphTime = 1.5f;

    [Header("Teleporte")]
    public float teleportCooldown = 30f;
    public float teleportRange = 4f;
    // --- NOVAS VARIÁVEIS AQUI ---
    [Tooltip("A distância MÍNIMA para a qual a pedra vai se teleportar, a partir do jogador.")]
    public float minTeleportDistance = 15f;
    [Tooltip("A distância MÁXIMA para a qual a pedra vai se teleportar, a partir do jogador.")]
    public float maxTeleportDistance = 20f;
    
    // A variável 'teleportDistanceFactor' foi removida por ser obsoleta.

    private float teleportTimer;
    private float attackTimer;
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
        startY = transform.position.y;

        if (Random.value > 0.5f)
        {
            orbitDirection = -1;
        }
    }

    void Update()
    {
        if (playerTransform == null) return;

        HandleFloating();

        if (!isAwake)
        {
            if (Vector3.Distance(transform.position, playerTransform.position) < activationDistance)
            {
                WakeUp();
            }
        }
        else
        {
            teleportTimer -= Time.deltaTime;
            attackTimer -= Time.deltaTime;

            HandleMovement();
            HandleTeleport();
            HandleAttack();
        }
    }
    
    void WakeUp()
    {
        isAwake = true;
        Debug.Log(gameObject.name + " foi ativado!");
        teleportTimer = 0;
        attackTimer = attackInterval / 2;
    }

    void HandleMovement()
    {
        Vector3 playerPositionOnPlane = new Vector3(playerTransform.position.x, transform.position.y, playerTransform.position.z);
        Vector3 directionToPlayer = (playerPositionOnPlane - transform.position).normalized;
        float distance = Vector3.Distance(playerPositionOnPlane, transform.position);
        
        Vector3 orbitDirectionVector = Vector3.Cross(directionToPlayer, Vector3.up) * orbitDirection;
        Vector3 finalMoveDirection = Vector3.zero;

        if (distance < minOrbitDistance)
        {
            finalMoveDirection = (-directionToPlayer + orbitDirectionVector).normalized;
        }
        else if (distance > maxOrbitDistance)
        {
            finalMoveDirection = (directionToPlayer + orbitDirectionVector).normalized;
        }
        else
        {
            finalMoveDirection = orbitDirectionVector;
        }
        
        rb.linearVelocity = new Vector3(finalMoveDirection.x * moveSpeed, rb.linearVelocity.y, finalMoveDirection.z * moveSpeed);
    }
    
    void HandleFloating()
    {
        float newY = startY + Mathf.Sin(Time.time * floatSpeed) * floatHeight;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }

    void HandleTeleport()
    {
        float distance = Vector3.Distance(transform.position, playerTransform.position);
        if (distance < teleportRange && teleportTimer <= 0)
        {
            Teleport();
        }
    }

    void HandleAttack()
    {
        if (attackTimer <= 0)
        {
            StartCoroutine(SkybeamAttack());
            attackTimer = attackInterval;
        }
    }

    // --- LÓGICA DO TELEPORTE MODIFICADA ---
    void Teleport()
    {
        Debug.Log("MagicStone teleportou!");

        // 1. Pega uma direção 2D aleatória (um ponto na borda de um círculo).
        Vector2 randomDirection = Random.insideUnitCircle.normalized;
        // 2. Pega uma distância aleatória entre o mínimo e o máximo definidos.
        float randomDistance = Random.Range(minTeleportDistance, maxTeleportDistance);

        // 3. Calcula o "deslocamento" a partir da posição do jogador.
        Vector3 offset = new Vector3(randomDirection.x, 0, randomDirection.y) * randomDistance;
        
        // 4. A nova posição é a posição ATUAL do jogador + o deslocamento.
        Vector3 newPosition = playerTransform.position + offset;
        newPosition.y = transform.position.y; // Mantém a mesma altura.

        transform.position = newPosition;
        teleportTimer = teleportCooldown;
    }

    IEnumerator SkybeamAttack()
    {
        Vector3 targetPosition = new Vector3(playerTransform.position.x, 0.01f, playerTransform.position.z);
        GameObject marker = Instantiate(attackMarkerPrefab, targetPosition, Quaternion.Euler(90, 0, 0));
        yield return new WaitForSeconds(attackTelegraphTime);
        Destroy(marker);
        Instantiate(attackBeamPrefab, targetPosition, Quaternion.identity);
    }
}