using UnityEngine;

/// <summary>
/// Campo de Debuff de Velocidade de Bismuto (Neon Speed Drain Zone).
/// Parâmetros de Slow, Duração, Raio e Cores 100% Editáveis no Inspector da Unity.
/// </summary>
public class BismuthCrystalField : MonoBehaviour
{
    [Header("1. Parâmetros de Redução de Velocidade (Slow Debuff)")]
    [Tooltip("Redução de velocidade quando o player tem buff de speed ativo (ex: 0.20 = 20% de slow)")]
    [Range(0.05f, 0.90f)]
    public float baseSlowPercent = 0.20f;

    [Tooltip("Redução de velocidade quando o player NÃO tem buff de speed ativo (ex: 0.40 = 40% de slow)")]
    [Range(0.05f, 0.90f)]
    public float enhancedSlowPercent = 0.40f;

    [Tooltip("Tempo em segundos que o player precisa permanecer na área para ter seu buff de velocidade roubado")]
    public float stealBuffTime = 3.0f;

    [Header("2. Parâmetros do Campo no Chão")]
    [Tooltip("Duração em segundos antes de a área de debuff desaparecer do chão")]
    public float fieldDuration = 3.5f;

    [Tooltip("Raio da área de debuff no chão (em metros)")]
    public float fieldRadius = 4.0f;

    [Header("3. Aparência e Cores (VFX no Inspector)")]
    [Tooltip("Cor do anel rúnico externo e da onda concentrica de drenagem")]
    public Color ringColor = new Color(0.45f, 0.15f, 0.65f, 0.25f);

    [Tooltip("Cor do disco holográfico translúcido no solo")]
    public Color floorDiscColor = new Color(0.35f, 0.10f, 0.50f, 0.12f);

    [Tooltip("Espessura da linha do anel rúnico néon no solo")]
    public float ringLineWidth = 0.12f;

    [Header("Referências")]
    [HideInInspector] public Geobionte_AI ownerBismutado;

    // Estado interno
    private bool playerInside = false;
    private float playerTimeInside = 0f;
    private bool hasStolen = false;
    private PlayerDebuffs playerDebuffs;
    private float lifeTimer = 0f;

    // Visual VFX
    private LineRenderer outerRingLine;
    private LineRenderer innerWaveLine;
    private MeshRenderer floorDiscRenderer;
    private Material ringMaterial;
    private Material discMaterial;

    void Start()
    {
        SetupCollider();
        CreateFloorDisc();
        CreateNeonRings();
    }

    void SetupCollider()
    {
        SphereCollider col = GetComponent<SphereCollider>();
        if (col == null) col = gameObject.AddComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = fieldRadius;
    }

    /// <summary>
    /// Cria o disco holográfico translúcido no solo com as cores do Inspector.
    /// </summary>
    void CreateFloorDisc()
    {
        GameObject discObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        discObj.name = "HolographicFloorDisc";
        discObj.transform.SetParent(transform, false);
        discObj.transform.localPosition = new Vector3(0, 0.02f, 0);
        discObj.transform.localScale = new Vector3(fieldRadius * 2f, 0.01f, fieldRadius * 2f);

        // Remove o collider do cilindro
        Collider col = discObj.GetComponent<Collider>();
        if (col != null) Destroy(col);

        floorDiscRenderer = discObj.GetComponent<MeshRenderer>();
        if (floorDiscRenderer != null)
        {
            Shader particleShader = Shader.Find("Particles/Standard Unlit") ?? Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/Lit");
            if (particleShader != null)
            {
                discMaterial = new Material(particleShader);
                discMaterial.SetColor("_Color", floorDiscColor);
                if (discMaterial.HasProperty("_BaseColor")) discMaterial.SetColor("_BaseColor", floorDiscColor);
                if (discMaterial.HasProperty("_EmissionColor")) discMaterial.SetColor("_EmissionColor", floorDiscColor * 0.8f);
                discMaterial.EnableKeyword("_EMISSION");
                floorDiscRenderer.material = discMaterial;
            }
        }
    }

    /// <summary>
    /// Cria o anel néon externo e a onda de energia concentrica interna com as cores do Inspector.
    /// </summary>
    void CreateNeonRings()
    {
        Shader particleShader = Shader.Find("Particles/Standard Unlit") ?? Shader.Find("Sprites/Default") ?? Shader.Find("Mobile/Particles/Additive");
        if (particleShader != null)
        {
            ringMaterial = new Material(particleShader);
            ringMaterial.SetColor("_Color", ringColor);
            if (ringMaterial.HasProperty("_EmissionColor")) ringMaterial.SetColor("_EmissionColor", ringColor * 1.0f);
        }

        // 1. Anel Externo Discreto
        GameObject outerRingObj = new GameObject("OuterNeonRing");
        outerRingObj.transform.SetParent(transform, false);
        outerRingObj.transform.localPosition = new Vector3(0, 0.05f, 0);

        outerRingLine = outerRingObj.AddComponent<LineRenderer>();
        outerRingLine.startWidth = ringLineWidth;
        outerRingLine.endWidth = ringLineWidth;
        outerRingLine.useWorldSpace = false;
        outerRingLine.loop = true;
        SetupCirclePositions(outerRingLine, fieldRadius, 45);
        if (ringMaterial != null) outerRingLine.material = ringMaterial;

        Gradient outerGradient = new Gradient();
        outerGradient.SetKeys(
            new GradientColorKey[] { 
                new GradientColorKey(ringColor, 0.0f),
                new GradientColorKey(ringColor * 0.8f, 0.5f),
                new GradientColorKey(ringColor * 0.6f, 1.0f) 
            },
            new GradientAlphaKey[] { new GradientAlphaKey(ringColor.a, 0.0f), new GradientAlphaKey(ringColor.a * 0.2f, 1.0f) }
        );
        outerRingLine.colorGradient = outerGradient;

        // 2. Onda Interna de Drenagem
        GameObject innerWaveObj = new GameObject("InnerDrainWave");
        innerWaveObj.transform.SetParent(transform, false);
        innerWaveObj.transform.localPosition = new Vector3(0, 0.06f, 0);

        innerWaveLine = innerWaveObj.AddComponent<LineRenderer>();
        innerWaveLine.startWidth = ringLineWidth * 0.7f;
        innerWaveLine.endWidth = ringLineWidth * 0.7f;
        innerWaveLine.useWorldSpace = false;
        innerWaveLine.loop = true;
        SetupCirclePositions(innerWaveLine, fieldRadius * 0.5f, 35);
        if (ringMaterial != null) innerWaveLine.material = ringMaterial;
        innerWaveLine.colorGradient = outerGradient;
    }

    private void SetupCirclePositions(LineRenderer lr, float radius, int segments)
    {
        lr.positionCount = segments;
        for (int i = 0; i < segments; i++)
        {
            float angle = (i / (float)segments) * Mathf.PI * 2f;
            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;
            lr.SetPosition(i, new Vector3(x, 0, z));
        }
    }

    void Update()
    {
        // Timer de vida do campo
        if (fieldDuration > 0f)
        {
            lifeTimer += Time.deltaTime;
            if (lifeTimer >= fieldDuration)
            {
                DestroyField();
                return;
            }
        }

        // Conta tempo que o player está dentro
        if (playerInside && playerDebuffs != null && !hasStolen)
        {
            playerTimeInside += Time.deltaTime;
            if (playerTimeInside >= stealBuffTime)
            {
                AttemptStealBuff();
            }
        }

        // Animação de Drenagem e Pulsação Holográfica
        UpdateHolographicPulse();
    }

    void UpdateHolographicPulse()
    {
        float time = Time.time;
        float pulse = Mathf.Sin(time * 3.5f) * 0.35f + 1.15f;

        // Anima a onda interna encolhendo em direção ao centro para simular drenagem de velocidade
        float waveRadiusPct = (time * 0.5f) % 1.0f;
        float currentWaveRadius = Mathf.Lerp(fieldRadius * 0.95f, 0.2f, waveRadiusPct);
        if (innerWaveLine != null)
        {
            SetupCirclePositions(innerWaveLine, currentWaveRadius, 35);
        }

        // Brilho pulsante no anel e disco usando as cores do Inspector
        if (ringMaterial != null)
        {
            Color neonPulse = ringColor * pulse;
            ringMaterial.SetColor("_Color", neonPulse);
            if (ringMaterial.HasProperty("_EmissionColor")) ringMaterial.SetColor("_EmissionColor", neonPulse * 1.5f);
        }

        if (discMaterial != null)
        {
            Color discPulse = new Color(floorDiscColor.r, floorDiscColor.g, floorDiscColor.b, floorDiscColor.a * (pulse * 0.8f));
            discMaterial.SetColor("_Color", discPulse);
            if (discMaterial.HasProperty("_BaseColor")) discMaterial.SetColor("_BaseColor", discPulse);
        }
    }

    private void AttemptStealBuff()
    {
        if (playerDebuffs == null || hasStolen) return;

        bool stole = playerDebuffs.TryStealSpeedBuff(ownerBismutado != null ? ownerBismutado.gameObject : gameObject);
        if (stole)
        {
            hasStolen = true;
            if (ownerBismutado != null)
            {
                ownerBismutado.OnStoleSpeedBuff();
            }
            Debug.Log("💎 [BISMUTH FIELD] Roubou um buff de velocidade do player!");
        }
    }

    private void DestroyField()
    {
        if (ringMaterial != null) Destroy(ringMaterial);
        if (discMaterial != null) Destroy(discMaterial);
        Destroy(gameObject);
    }

    // ==================== TRIGGER ====================

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (ownerBismutado != null && other.gameObject == ownerBismutado.gameObject) return;

        playerDebuffs = other.GetComponent<PlayerDebuffs>() ?? other.gameObject.AddComponent<PlayerDebuffs>();

        playerInside = true;
        playerTimeInside = 0f;

        bool playerHasSpeedBuff = playerDebuffs.HasSpeedBuff();
        float slowAmount = playerHasSpeedBuff ? baseSlowPercent : enhancedSlowPercent;
        playerDebuffs.ApplySlow(slowAmount);

        if (ownerBismutado != null)
        {
            ownerBismutado.OnPlayerTrappedInCrystalField();
        }

        Debug.Log($"💎 [BISMUTH FIELD] Player entrou na zona de velocidade! Slow: {slowAmount * 100}%. Bismutado notificado!");
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playerInside = false;
        playerTimeInside = 0f;

        if (playerDebuffs != null)
        {
            playerDebuffs.RemoveSlow();
        }

        Debug.Log("💎 [BISMUTH FIELD] Player saiu da zona! Slow removido.");
    }
}
