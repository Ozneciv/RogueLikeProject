using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CrystalTuner : MonoBehaviour
{
    [Header("Sintonia")]
    public float connectRange = 20f;
    public float disconnectRange = 30f;
    public LayerMask enemyLayer;
    public Color beamColor = Color.magenta;

    [Header("Movimento Inteligente")]
    public float moveSpeed = 4.5f;
    public float idealDistToTarget = 5f;
    public float fleeDistFromPlayer = 8f;
    
    [Header("Voo (Ajustado)")]
    [Tooltip("Altura fixa do chão. 1.5f é ideal para ser acertado por ataques melee.")]
    public float flyHeight = 1.5f; // BAIXAMOS O VALOR PADRÃO
    public float heightCorrectionSpeed = 5.0f; // Aumentamos a velocidade de correção para ele não "quicar"

    private Transform playerTransform;
    private GameObject currentTargetObj;
    private Transform currentTargetCenter;
    private LineRenderer lineRenderer;
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false; 
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.startWidth = 0.15f;
        lineRenderer.endWidth = 0.05f;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = beamColor;
        lineRenderer.endColor = Color.white;
        lineRenderer.positionCount = 2;
        lineRenderer.enabled = false;
    }

    void Update()
    {
        HandleBuffs();
        UpdateBeamVisuals();
    }

    void FixedUpdate()
    {
        HandleMovement();
    }

    void HandleMovement()
    {
        if (playerTransform == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerTransform = p.transform;
        }

        Vector3 finalDirection = Vector3.zero;
        Vector3 myPosFlat = new Vector3(transform.position.x, 0, transform.position.z);

        // 1. FORÇA DE PROTEÇÃO (Fica perto do amigo)
        if (currentTargetObj != null)
        {
            Vector3 targetPosFlat = new Vector3(currentTargetObj.transform.position.x, 0, currentTargetObj.transform.position.z);
            float distToFriend = Vector3.Distance(myPosFlat, targetPosFlat);

            if (distToFriend > idealDistToTarget)
            {
                Vector3 dirToFriend = (targetPosFlat - myPosFlat).normalized;
                finalDirection += dirToFriend * 1.5f; 
            }
        }

        // 2. FORÇA DE MEDO (Foge do Player)
        if (playerTransform != null)
        {
            Vector3 playerPosFlat = new Vector3(playerTransform.position.x, 0, playerTransform.position.z);
            float distToPlayer = Vector3.Distance(myPosFlat, playerPosFlat);

            if (distToPlayer < fleeDistFromPlayer)
            {
                Vector3 dirAway = (myPosFlat - playerPosFlat).normalized;
                finalDirection += dirAway * 3.0f; 
            }
        }

        // Aplica o movimento Horizontal
        Vector3 targetPos = transform.position;
        if (finalDirection != Vector3.zero)
        {
            finalDirection.Normalize();
            targetPos += finalDirection * moveSpeed * Time.fixedDeltaTime;
            
            // Rotação suave
            if (finalDirection.sqrMagnitude > 0.1f)
            {
                Quaternion targetRot = Quaternion.LookRotation(finalDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 5f * Time.fixedDeltaTime);
            }
        }

        // 3. CORREÇÃO DE ALTURA (Trava no eixo Y)
        // Ignora a altura atual e força suavemente para a flyHeight
        // Se o chão for plano (Y=0), ele vai para Y=1.5.
        // Se o chão tiver elevação, você precisaria de um Raycast aqui, mas para chãos planos isso basta.
        float newY = Mathf.Lerp(transform.position.y, flyHeight, heightCorrectionSpeed * Time.fixedDeltaTime);
        targetPos.y = newY;

        rb.MovePosition(targetPos);
    }

    // ... (O RESTO DO SCRIPT: HandleBuffs, FindNewTarget, ConnectToTarget, etc. continua IGUAL) ...
    
    void HandleBuffs()
    {
        if (currentTargetObj != null)
        {
            float dist = Vector3.Distance(transform.position, currentTargetObj.transform.position);
            if (!currentTargetObj.activeSelf || dist > disconnectRange)
            {
                RemoveBuffs(currentTargetObj);
                currentTargetObj = null;
                currentTargetCenter = null;
                lineRenderer.enabled = false;
            }
        }
        else
        {
            FindNewTarget();
        }
    }

    void FindNewTarget()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, connectRange, enemyLayer);
        float closestDist = Mathf.Infinity;
        GameObject bestCandidate = null;
        foreach (Collider hit in hits)
        {
            GameObject candidate = hit.gameObject;
            if (candidate == gameObject) continue; 
            if (candidate.GetComponent<CrystalTuner>() != null) continue; 
            if (candidate.GetComponent<HomingHazard>() != null) continue;
            if (candidate.GetComponent<DummyHealth>() == null) continue;

            float d = Vector3.Distance(transform.position, candidate.transform.position);
            if (d < closestDist)
            {
                closestDist = d;
                bestCandidate = candidate;
            }
        }
        if (bestCandidate != null) ConnectToTarget(bestCandidate);
    }

    void ConnectToTarget(GameObject target)
    {
        currentTargetObj = target;
        Transform centerPoint = target.transform.Find("CenterTarget");
        if (centerPoint != null) currentTargetCenter = centerPoint;
        else currentTargetCenter = target.transform;
        ApplyBuffs(currentTargetObj);
        lineRenderer.enabled = true;
    }

    void UpdateBeamVisuals()
    {
        if (lineRenderer.enabled && currentTargetCenter != null)
        {
            lineRenderer.SetPosition(0, transform.position);
            lineRenderer.SetPosition(1, currentTargetCenter.position); 
        }
    }

    void ApplyBuffs(GameObject target)
    {
        var totem = target.GetComponent<TotemSpawner>();
        if (totem != null) totem.SetBuff(true);
        var stone = target.GetComponent<MagicStone_AI>();
        if (stone != null) stone.SetBuff(true);
        var swarm = target.GetComponent<ShardSwarm_AI>();
        if (swarm != null) swarm.SetBuff(true);
        var health = target.GetComponent<DummyHealth>();
        if (health != null) health.SetBuffedStatus(true);
    }

    void RemoveBuffs(GameObject target)
    {
        if (target == null) return;
        var totem = target.GetComponent<TotemSpawner>();
        if (totem != null) totem.SetBuff(false);
        var stone = target.GetComponent<MagicStone_AI>();
        if (stone != null) stone.SetBuff(false);
        var swarm = target.GetComponent<ShardSwarm_AI>();
        if (swarm != null) swarm.SetBuff(false);
        var health = target.GetComponent<DummyHealth>();
        if (health != null) health.SetBuffedStatus(false);
    }

    void OnDestroy()
    {
        if (currentTargetObj != null) RemoveBuffs(currentTargetObj);
    }
}