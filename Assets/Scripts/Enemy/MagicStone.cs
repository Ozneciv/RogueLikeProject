using UnityEngine;
using System.Collections;

public class MagicStone_AI : MonoBehaviour
{
    [Header("Referências")]
    public Transform playerTransform;
    public GameObject attackMarkerPrefab;
    public GameObject attackBeamPrefab;

    // --- NOVA VARIÁVEL ---
    [HideInInspector]
    public BoxCollider roomBounds; // A sala vai preencher isso automaticamente

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
    public float minTeleportDistance = 15f;
    public float maxTeleportDistance = 20f;

    private float originalAttackInterval;
    private float originalMoveSpeed;
    private bool isBuffed = false;
    private float teleportTimer;
    private float attackTimer;
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        // Tenta achar o player se não foi atribuído
        if(playerTransform == null)
        {
             GameObject p = GameObject.FindGameObjectWithTag("Player");
             if(p != null) playerTransform = p.transform;
        }
        
        startY = transform.position.y;

        if (Random.value > 0.5f) orbitDirection = -1;
    }

    void Update()
    {
        if (playerTransform == null) return;

        // Trava de rotação visual (olha sempre para o player, mas em pé)
        Vector3 lookPos = playerTransform.position;
        lookPos.y = transform.position.y;
        transform.LookAt(lookPos);

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
    
    // Função chamada pelo RoomController para definir os limites
    public void SetRoomBounds(BoxCollider bounds)
    {
        roomBounds = bounds;
    }


    public void SetBuff(bool active)
    {
        if (active && !isBuffed)
        {
            isBuffed = true;
            originalAttackInterval = attackInterval;
            originalMoveSpeed = moveSpeed;
            
            attackInterval /= 2f; // Ataca 2x mais rápido
            moveSpeed *= 1.5f;    // Move 50% mais rápido
            
            // Se quiser aumentar o dano do raio, precisaria passar isso para o prefab do raio,
            // mas a velocidade de ataque já aumenta o DPS drasticamente.
        }
        else if (!active && isBuffed)
        {
            isBuffed = false;
            attackInterval = originalAttackInterval;
            moveSpeed = originalMoveSpeed;
        }
    }
    void WakeUp()
    {
        isAwake = true;
        teleportTimer = 0;
        attackTimer = attackInterval / 2;
    }

    void HandleMovement()
    {
        // (Lógica de movimento orbital continua igual...)
        Vector3 playerPositionOnPlane = new Vector3(playerTransform.position.x, transform.position.y, playerTransform.position.z);
        Vector3 directionToPlayer = (playerPositionOnPlane - transform.position).normalized;
        float distance = Vector3.Distance(playerPositionOnPlane, transform.position);
        
        Vector3 orbitDirectionVector = Vector3.Cross(directionToPlayer, Vector3.up) * orbitDirection;
        Vector3 finalMoveDirection = Vector3.zero;

        if (distance < minOrbitDistance)
            finalMoveDirection = (-directionToPlayer + orbitDirectionVector).normalized;
        else if (distance > maxOrbitDistance)
            finalMoveDirection = (directionToPlayer + orbitDirectionVector).normalized;
        else
            finalMoveDirection = orbitDirectionVector;
        
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

    void Teleport()
    {
        // 1. Calcula a posição de destino desejada (como antes)
        Vector2 randomDirection = Random.insideUnitCircle.normalized;
        float randomDistance = Random.Range(minTeleportDistance, maxTeleportDistance);
        Vector3 offset = new Vector3(randomDirection.x, 0, randomDirection.y) * randomDistance;
        Vector3 targetPosition = playerTransform.position + offset;
        targetPosition.y = transform.position.y; // Mantém altura

        // --- CORREÇÃO DE LIMITES (CLAMP) ---
        // Se a sala definiu limites, usamos eles para impedir que a pedra saia
        if (roomBounds != null)
        {
            // ClosestPoint retorna o ponto dentro do collider mais próximo do alvo.
            // Se o alvo já estiver dentro, retorna o alvo. Se estiver fora, retorna a borda.
            targetPosition = roomBounds.ClosestPoint(targetPosition);
            
            // Pequeno ajuste para não ficar literalmente dentro da parede
            Vector3 directionToCenter = (roomBounds.bounds.center - targetPosition).normalized;
            targetPosition += directionToCenter * 1.0f; 
            
            // Garante a altura correta novamente após o Clamp
            targetPosition.y = transform.position.y; 
        }

        transform.position = targetPosition;
        teleportTimer = teleportCooldown;
        
        Debug.Log("MagicStone teleportou (dentro dos limites)!");
    }

    IEnumerator SkybeamAttack()
    {
        Vector3 targetPosition = new Vector3(playerTransform.position.x, 0.01f, playerTransform.position.z);
        GameObject marker = Instantiate(attackMarkerPrefab, targetPosition, Quaternion.Euler(90, 0, 0));
        yield return new WaitForSeconds(attackTelegraphTime);
        Destroy(marker);
        // Criar o raio e definir o owner para thorns
        GameObject beam = Instantiate(attackBeamPrefab, targetPosition, Quaternion.identity);
        AttackBeam beamScript = beam.GetComponent<AttackBeam>();
        if (beamScript != null)
        {
            beamScript.owner = gameObject; // Define a Magic Stone como dona do raio
        }
    }
}