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
    private Renderer enemyRenderer;

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

    [Header("Opacidade do Inimigo")]
    [Range(0f, 1f)] public float dashOpacity = 0.15f; 
    public float fadeSpeed = 12f;     
    
    [Header("Efeito do Rastro Fantasma")]
    [Tooltip("Material transparente usado para as cópias do rastro")]
    public Material trailMaterial;
    [Tooltip("Tempo em segundos que cada fantasma fica no chão antes de sumir por completo")]
    public float ghostLifetime = 0.4f;
    [Tooltip("Intervalo de tempo entre a criação de um fantasma e outro durante o dash")]
    public float ghostSpawnInterval = 0.03f;
    [Range(0f, 1f)] public float ghostInitialAlpha = 0.5f;

    [Header("Dano")]
    public int dashDamage = 15;

    private enum State { Idle, Chasing, Dashing, Resting }
    private State currentState = State.Idle;
    private Color targetColor;
    private bool isEmittingTrail = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        health = GetComponent<DummyHealth>();
        enemyRenderer = GetComponentInChildren<Renderer>();

        rb.useGravity = false;
        
    
        rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
            playerRb = player.GetComponent<Rigidbody>();
        }

        if (enemyRenderer != null)
            targetColor = enemyRenderer.material.color;

        if (trailMaterial == null && enemyRenderer != null)
            trailMaterial = enemyRenderer.material;
    }

    void Update()
    {
        if (playerTransform == null || (health != null && health.CurrentHealth <= 0)) return;

        HandleVisualFade();

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
            currentState = State.Chasing;
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

        for (int i = 0; i < 3; i++)
        {
            if (enemyRenderer != null) targetColor.a = dashOpacity;
            Vector3 targetPredictPos = GetPredictedPlayerPosition();
            
            LookAtPosition(targetPredictPos, 20f);
            yield return new WaitForSeconds(anticipationTime);

            Vector3 dashDirection = (targetPredictPos - transform.position).normalized;
            dashDirection.y = 0;

            isEmittingTrail = true;
            StartCoroutine(GenerateTrailRoutine());

            yield return StartCoroutine(ExecuteSingleDash(dashDirection, targetPredictPos));
            
            isEmittingTrail = false;

            yield return new WaitForSeconds(pauseBetweenDashes);
        }

        yield return StartCoroutine(RetreatRoutine());

        if (enemyRenderer != null) targetColor.a = 1f;

        currentState = State.Resting;
        yield return new WaitForSeconds(endSequenceRest);
        
        currentState = State.Chasing;
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

    IEnumerator GenerateTrailRoutine()
    {
        while (isEmittingTrail)
        {
            GameObject ghostObj = new GameObject("SharpBlur_Ghost");
            GhostTrail ghostScript = ghostObj.AddComponent<GhostTrail>();
            
            ghostScript.Init(transform, ghostLifetime, trailMaterial, ghostInitialAlpha);

            yield return new WaitForSeconds(ghostSpawnInterval);
        }
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

    void HandleVisualFade()
    {
        if (enemyRenderer == null) return;
        Color currentColor = enemyRenderer.material.color;
        enemyRenderer.material.color = Color.Lerp(currentColor, targetColor, fadeSpeed * Time.deltaTime);
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