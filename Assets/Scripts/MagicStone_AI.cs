using UnityEngine;
using System.Collections;

// O nome da classe foi alterado para MagicStone_AI
public class MagicStone_AI : MonoBehaviour
{
    [Header("Referências")]
    public Transform playerTransform;
    public GameObject attackMarkerPrefab;
    public GameObject attackBeamPrefab;

    [Header("Comportamento")]
    public float moveSpeed = 3.5f;
    public float idealDistance = 15f;
    public float teleportRange = 4f;

    [Header("Ataque")]
    public float attackInterval = 5f;
    public float attackTelegraphTime = 1.5f;

    [Header("Teleporte")]
    public float teleportCooldown = 30f;
    private float teleportTimer;
    private float attackTimer;

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        playerTransform = GameObject.FindGameObjectWithTag("Player").transform;

        teleportTimer = 0;
        attackTimer = attackInterval / 2;
    }

    void Update()
    {
        if (playerTransform == null) return;

        teleportTimer -= Time.deltaTime;
        attackTimer -= Time.deltaTime;

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        HandleMovement(distanceToPlayer);
        HandleTeleport(distanceToPlayer);
        HandleAttack();
    }

    void HandleMovement(float distance)
    {
        transform.LookAt(2 * transform.position - playerTransform.position);

        if (distance < idealDistance)
        {
            rb.linearVelocity = transform.forward * moveSpeed;
        }
        else
        {
            rb.linearVelocity = Vector3.zero;
        }
    }

    void HandleTeleport(float distance)
    {
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
        Debug.Log("MagicStone teleportou!");
        Vector3 newPosition = -playerTransform.position;
        newPosition.y = transform.position.y;
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