using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(DummyHealth))]
public class SharpBlur : MonoBehaviour
{
    private Transform playerTransform;
    private Rigidbody playerRb; 
    private DummyHealth health;
    private Rigidbody rb;
    private Animator anim;

    [Header("Ativação & Perseguição")]
    public float activationDistance = 25f;
    public float attackTriggerDistance = 3f; 
    public float chaseSpeed = 6f;

    [Header("Configurações do Dash")]
    public float dashDistance = 9f;
    public float dashSpeed = 30f;
    public float pauseBetweenDashes = 0.25f;
    public float endSequenceRest = 1.2f;

    [Header("Refinamentos Inteligencia")]
    public float anticipationTime = 0.2f; 
    [Range(0f, 1f)] public float leadPrediction = 0.5f;
    public float retreatDistance = 4f;

    [Header("Dano")]
    public int dashDamage = 15;

    [Header("Efeitos Visuais")]
    [Tooltip("Coloque o Prefab vazio com o script DashHologram aqui")]
    public GameObject hologramPrefab; 
    [Tooltip("Coloque o Material transparente/fantasma aqui")]
    public Material hologramMaterial;
    [Range(0.1f, 1f)] public float hologramAlpha = 0.6f;

    private enum State { Idle, Chasing, Dashing, Resting }
    private State currentState = State.Idle;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        health = GetComponent<DummyHealth>();
        anim = GetComponentInChildren<Animator>();

        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
            playerRb = player.GetComponent<Rigidbody>();
        }
    }

    void Update()
    {
        if (playerTransform == null || (health != null && health.CurrentHealth <= 0)) return;

        switch (currentState)
        {
            case State.Idle: HandleIdle(); break;
            case State.Chasing: HandleChasing(); break;
        }
    }

    void FixedUpdate()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    void HandleIdle()
    {
        if (Vector3.Distance(transform.position, playerTransform.position) <= activationDistance)
        {
            currentState = State.Chasing;
            if (anim != null) anim.SetBool("isChasing", true);
        }
    }

    void HandleChasing()
    {
        float distToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        
        LookAtPosition(playerTransform.position, 10f);

        if (distToPlayer <= attackTriggerDistance)
        {
            StartCoroutine(TripleDashRoutine());
        }
        else
        {
            Vector3 targetPos = Vector3.MoveTowards(transform.position, playerTransform.position, chaseSpeed * Time.deltaTime);
            rb.MovePosition(targetPos);
        }
    }

    IEnumerator TripleDashRoutine()
    {
        currentState = State.Dashing;

        // Desliga a corrida para focar nos ataques do Dash
        if (anim != null) anim.SetBool("isChasing", false);

        for (int i = 0; i < 3; i++)
        {
            Vector3 targetPredictPos = GetPredictedPlayerPosition();
            LookAtPosition(targetPredictPos, 20f);
            
            // Calcula a direção e a posição futura do dash
            Vector3 dashDirection = (targetPredictPos - transform.position).normalized;
            dashDirection.y = 0;
            if (dashDirection == Vector3.zero) dashDirection = transform.forward;

            Vector3 targetBasePos = targetPredictPos;
            targetBasePos.y = transform.position.y; 
            Vector3 targetDashPos = targetBasePos + (dashDirection * (dashDistance / 2f));
            Quaternion dashRot = Quaternion.LookRotation(dashDirection);

            // Instancia o Holograma de Aviso (Telegraph)
            if (hologramPrefab != null && hologramMaterial != null)
            {
                GameObject holo = Instantiate(hologramPrefab);
                DashHologram holoScript = holo.GetComponent<DashHologram>();
                if (holoScript != null)
                {
                    holoScript.Init(transform, targetDashPos, dashRot, anticipationTime, hologramMaterial, hologramAlpha);
                }
            }

            // Inicia a animação de ataque
            if (anim != null) anim.Play("Dash", 0, 0f);
            
            // Aguarda o tempo de preparação (enquanto o holograma aparece)
            yield return new WaitForSeconds(anticipationTime);

            // Executa o dash real até a posição
            yield return StartCoroutine(ExecuteSingleDash(dashDirection, targetPredictPos));

            yield return new WaitForSeconds(pauseBetweenDashes);
        }

        yield return StartCoroutine(RetreatRoutine());

        currentState = State.Resting;
        yield return new WaitForSeconds(endSequenceRest);
        
        // Fim do repouso: volta a perseguir e correr
        currentState = State.Chasing;
        if (anim != null) anim.SetBool("isChasing", true);
    }

    IEnumerator ExecuteSingleDash(Vector3 direction, Vector3 targetBasePos)
    {
        if (direction == Vector3.zero) direction = transform.forward;
        transform.rotation = Quaternion.LookRotation(direction);

        Vector3 startPos = transform.position;
        targetBasePos.y = startPos.y; 

        Vector3 targetDashPos = targetBasePos + (direction * (dashDistance / 2f));
        
        float startTime = Time.time;
        float distance = Vector3.Distance(startPos, targetDashPos);
        float duration = distance / dashSpeed;

        while (Time.time - startTime < duration)
        {
            float t = (Time.time - startTime) / duration;
            rb.MovePosition(Vector3.Lerp(startPos, targetDashPos, t));
            yield return null;
        }

        rb.MovePosition(targetDashPos);
    }

    IEnumerator RetreatRoutine()
    {
        Vector3 retreatDir = -transform.forward; 
        Vector3 startPos = transform.position;
        Vector3 targetRetreatPos = startPos + (retreatDir * retreatDistance);

        float startTime = Time.time;
        float duration = 0.25f; 

        while (Time.time - startTime < duration)
        {
            float t = (Time.time - startTime) / duration;
            rb.MovePosition(Vector3.Lerp(startPos, targetRetreatPos, Mathf.Sin(t * Mathf.PI / 2f)));
            yield return null;
        }
        rb.MovePosition(targetRetreatPos);
    }

    Vector3 GetPredictedPlayerPosition()
    {
        Vector3 playerPos = playerTransform.position;
        if (playerRb != null && leadPrediction > 0)
        {
            float distance = Vector3.Distance(transform.position, playerPos);
            float timeToTarget = distance / dashSpeed;
            playerPos += playerRb.linearVelocity * timeToTarget * leadPrediction; 
        }
        return playerPos;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (currentState == State.Dashing && other.CompareTag("Player"))
            Debug.Log($"[SharpBlur] causou {dashDamage} de dano.");
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (currentState == State.Dashing && collision.gameObject.CompareTag("Player"))
            Debug.Log($"[SharpBlur] causou {dashDamage} de dano.");
    }

    void LookAtPosition(Vector3 target, float rotationSpeed)
    {
        Vector3 dir = (target - transform.position);
        dir.y = 0;
        if (dir.sqrMagnitude > 0.1f)
        {
            Quaternion rot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, rot, rotationSpeed * Time.deltaTime);
        }
    }
}