using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody))]
public class CrystalTuner : MonoBehaviour
{
    [Header("Sintonia")]
    public float connectRange = 20f;
    public float disconnectRange = 30f;
    public LayerMask enemyLayer;
    public Color beamColor = Color.magenta;

    [Header("Múltiplos Alvos")]
    [Tooltip("Máximo de inimigos buffados simultaneamente")]
    public int maxTargets = 3;

    [Header("Movimento Inteligente")]
    public float moveSpeed = 4.5f;
    public float idealDistToTarget = 5f;
    public float fleeDistFromPlayer = 8f;

    [Header("Voo")]
    [Tooltip("Altura fixa do chão.")]
    public float flyHeight = 1.5f;
    public float heightCorrectionSpeed = 5.0f;

    // --- Privados ---
    private Transform playerTransform;
    private Transform beamOrigin;
    private Rigidbody rb;
    private float beamPulseTimer = 0f;
    private const int BEAM_SEGMENTS = 8;

    // Lista de alvos ativos
    private List<TargetData> targets = new List<TargetData>();

    private struct TargetData
    {
        public GameObject obj;
        public Transform center;
        public Renderer renderer;
        public LineRenderer beam;
    }

    private bool registradoNoBestiario = false;

    // ──────────────────────────────────────────────
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        Transform center = transform.Find("Center");
        beamOrigin = (center != null) ? center : transform;
        if (center == null)
            Debug.LogWarning("[CrystalTuner] Filho 'Center' não encontrado. Usando o pivô.");
    }

    void Update()
    {
        HandleBuffs();
        UpdateAllBeams();

        // Registro no bestiário por proximidade
        if (!registradoNoBestiario && playerTransform != null)
        {
            if (Vector3.Distance(transform.position, playerTransform.position) < fleeDistFromPlayer * 2f)
            {
                registradoNoBestiario = true;
                EnemyIdentity id = GetComponent<EnemyIdentity>() ?? GetComponentInChildren<EnemyIdentity>() ?? GetComponentInParent<EnemyIdentity>();
                Debug.Log("[CRYSTAL] EnemyIdentity: " + (id != null ? id.nomeInimigo : "NULL") + " | BestiarioManager: " + (BestiarioManager.instancia != null));
                if (id != null && BestiarioManager.instancia != null)
                    BestiarioManager.instancia.Registrar(id);
            }
        }
    }

    void FixedUpdate()
    {
        HandleMovement();
    }

    // ──────────────────────────────────────────────
    void HandleMovement()
    {
        if (playerTransform == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerTransform = p.transform;
        }

        Vector3 finalDirection = Vector3.zero;
        Vector3 myPosFlat = new Vector3(transform.position.x, 0, transform.position.z);

        // Força de proteção: fica perto do primeiro alvo
        if (targets.Count > 0 && targets[0].obj != null)
        {
            Vector3 targetPosFlat = new Vector3(targets[0].obj.transform.position.x, 0, targets[0].obj.transform.position.z);
            if (Vector3.Distance(myPosFlat, targetPosFlat) > idealDistToTarget)
                finalDirection += (targetPosFlat - myPosFlat).normalized * 1.5f;
        }

        // Força de medo: foge do player
        if (playerTransform != null)
        {
            Vector3 playerPosFlat = new Vector3(playerTransform.position.x, 0, playerTransform.position.z);
            if (Vector3.Distance(myPosFlat, playerPosFlat) < fleeDistFromPlayer)
                finalDirection += (myPosFlat - playerPosFlat).normalized * 3.0f;
        }

        Vector3 targetPos = transform.position;
        if (finalDirection != Vector3.zero)
        {
            finalDirection.Normalize();
            targetPos += finalDirection * moveSpeed * Time.fixedDeltaTime;

            if (finalDirection.sqrMagnitude > 0.1f)
            {
                Quaternion targetRot = Quaternion.LookRotation(finalDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 5f * Time.fixedDeltaTime);
            }
        }

        float newY = Mathf.Lerp(transform.position.y, flyHeight, heightCorrectionSpeed * Time.fixedDeltaTime);
        targetPos.y = newY;
        rb.MovePosition(targetPos);
    }

    // ──────────────────────────────────────────────
    void HandleBuffs()
    {
        // 1. Remove alvos inválidos (mortos ou fora de range)
        for (int i = targets.Count - 1; i >= 0; i--)
        {
            var td = targets[i];
            if (td.obj == null || !td.obj.activeSelf ||
                Vector3.Distance(transform.position, td.obj.transform.position) > disconnectRange)
            {
                RemoveBuffs(td.obj);
                if (td.beam != null) Destroy(td.beam.gameObject);
                targets.RemoveAt(i);
            }
        }

        // 2. Tenta preencher slots vazios
        if (targets.Count < maxTargets)
            FindNewTargets();
    }

    void FindNewTargets()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, connectRange, enemyLayer);

        // Ordena por distância
        System.Array.Sort(hits, (a, b) =>
            Vector3.Distance(transform.position, a.transform.position)
            .CompareTo(Vector3.Distance(transform.position, b.transform.position)));

        foreach (Collider hit in hits)
        {
            if (targets.Count >= maxTargets) break;

            GameObject candidate = hit.gameObject;
            if (candidate == gameObject) continue;
            if (candidate.GetComponent<CrystalTuner>() != null) continue;
            if (candidate.GetComponent<HomingHazard>() != null) continue;
            if (candidate.GetComponent<DummyHealth>() == null && candidate.GetComponent<ShardSwarmHealth>() == null) continue;

            // Já é alvo?
            bool alreadyTargeted = false;
            foreach (var td in targets)
                if (td.obj == candidate) { alreadyTargeted = true; break; }
            if (alreadyTargeted) continue;

            ConnectToTarget(candidate);
        }
    }

    void ConnectToTarget(GameObject target)
    {
        Transform centerPoint = target.transform.Find("CenterTarget");
        Renderer rend = target.GetComponentInChildren<Renderer>();

        LineRenderer beam = CreateBeam();
        targets.Add(new TargetData { obj = target, center = centerPoint, renderer = rend, beam = beam });
        ApplyBuffs(target);
    }

    LineRenderer CreateBeam()
    {
        GameObject beamObj = new GameObject("Beam");
        beamObj.transform.SetParent(transform);

        LineRenderer lr = beamObj.AddComponent<LineRenderer>();
        lr.positionCount = BEAM_SEGMENTS + 2;
        lr.startWidth = 0.12f;
        lr.endWidth = 0.04f;
        lr.numCapVertices = 4;
        lr.useWorldSpace = true;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;

        Shader sh = Shader.Find("Sprites/Default") ?? Shader.Find("UI/Default");
        Material mat = new Material(sh);
        mat.color = Color.white;
        lr.material = mat;

        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(beamColor,   0.0f),
                new GradientColorKey(Color.white,  0.5f),
                new GradientColorKey(beamColor,   1.0f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.0f, 0.0f),
                new GradientAlphaKey(1.0f, 0.1f),
                new GradientAlphaKey(1.0f, 0.9f),
                new GradientAlphaKey(0.0f, 1.0f)
            }
        );
        lr.colorGradient = grad;
        return lr;
    }

    // ──────────────────────────────────────────────
    void UpdateAllBeams()
    {
        beamPulseTimer += Time.deltaTime * 10f;
        Vector3 origin = (beamOrigin != null) ? beamOrigin.position : transform.position;

        for (int i = 0; i < targets.Count; i++)
        {
            var td = targets[i];
            if (td.beam == null || td.obj == null) continue;

            Vector3 targetPos = td.obj.transform.position;
            if (td.center != null)
                targetPos = td.center.position;
            else if (td.renderer != null)
                targetPos = td.renderer.bounds.center;

            UpdateBeam(td.beam, origin, targetPos, i);
        }
    }

    void UpdateBeam(LineRenderer lr, Vector3 origin, Vector3 targetPos, int index)
    {
        float totalDist = Vector3.Distance(origin, targetPos);

        float pulse = 0.06f + Mathf.Abs(Mathf.Sin(beamPulseTimer + index * 1.2f)) * 0.06f;
        lr.startWidth = pulse;
        lr.endWidth = pulse * 0.4f;

        Vector3 dir = targetPos - origin;
        Vector3 perp = (dir.sqrMagnitude > 0.001f)
            ? Vector3.Cross(dir.normalized, Vector3.up)
            : Vector3.right;

        int total = BEAM_SEGMENTS + 2;
        for (int i = 0; i < total; i++)
        {
            float t = (float)i / (total - 1);
            Vector3 pt = Vector3.Lerp(origin, targetPos, t);

            if (i > 0 && i < total - 1)
            {
                float amp = Mathf.Sin(t * Mathf.PI) * totalDist * 0.07f;
                float noiseX = (Mathf.PerlinNoise(t * 4f + beamPulseTimer + index * 3f, 0.5f) - 0.5f) * 2f;
                float noiseY = (Mathf.PerlinNoise(0.5f, t * 4f + beamPulseTimer + index * 5f + 7f) - 0.5f) * 2f;
                pt += perp * noiseX * amp;
                pt += Vector3.up * noiseY * amp * 0.4f;
            }

            lr.SetPosition(i, pt);
        }
    }

    // ──────────────────────────────────────────────
    void ApplyBuffs(GameObject target)
    {
        if (target == null) return;
        target.GetComponent<TotemSpawner>()?.SetBuff(true);
        target.GetComponent<MagicStone_AI>()?.SetBuff(true);
        target.GetComponent<ShardSwarm_AI>()?.SetBuff(true);
        target.GetComponent<GoblinAI_Transform>()?.SetBuff(true);
        target.GetComponent<DummyHealth>()?.SetBuffedStatus(true);
        target.GetComponent<ShardSwarmHealth>()?.SetBuffedStatus(true);
        target.GetComponent<CrystalWatcher_AI>()?.SetBuff(true);
        target.GetComponent<Cristalus_AI>()?.SetBuff(true);
        target.GetComponent<Geobionte_AI>()?.SetBuff(true);
    }

    void RemoveBuffs(GameObject target)
    {
        if (target == null) return;
        target.GetComponent<TotemSpawner>()?.SetBuff(false);
        target.GetComponent<MagicStone_AI>()?.SetBuff(false);
        target.GetComponent<ShardSwarm_AI>()?.SetBuff(false);
        target.GetComponent<GoblinAI_Transform>()?.SetBuff(false);
        target.GetComponent<DummyHealth>()?.SetBuffedStatus(false);
        target.GetComponent<ShardSwarmHealth>()?.SetBuffedStatus(false);
        target.GetComponent<CrystalWatcher_AI>()?.SetBuff(false);
        target.GetComponent<Cristalus_AI>()?.SetBuff(false);
        target.GetComponent<Geobionte_AI>()?.SetBuff(false);
    }

    void OnDestroy()
    {
        foreach (var td in targets)
            RemoveBuffs(td.obj);
    }
}