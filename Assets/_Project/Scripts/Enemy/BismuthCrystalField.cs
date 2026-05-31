using UnityEngine;

/// <summary>
/// Campo de cristais de bismuto criado pelo Bismutado.
/// Funciona como uma zona no chão que aplica debuffs ao player:
/// - Reduz 20% da velocidade enquanto o player está dentro
/// - Se o player não tem buff de speed, reduz 40% ao invés de 20%
/// - Após 3 segundos dentro do campo, rouba um buff de speed do player
/// 
/// Similar ao DamageZone existente, mas aplica debuff ao invés de dano.
/// </summary>
public class BismuthCrystalField : MonoBehaviour
{
    [Header("Debuff Settings")]
    [Tooltip("Redução de velocidade base (20% = 0.2)")]
    public float baseSlowPercent = 0.2f;

    [Tooltip("Redução de velocidade quando player não tem buff de speed (40% = 0.4)")]
    public float enhancedSlowPercent = 0.4f;

    [Tooltip("Tempo que o player precisa ficar no campo para ter o buff roubado (segundos)")]
    public float stealBuffTime = 3f;

    [Header("Campo")]
    [Tooltip("Duração do campo antes de desaparecer (segundos). 0 = permanente.")]
    public float fieldDuration = 10f;

    [Tooltip("Raio do campo de cristais")]
    public float fieldRadius = 4f;

    [Header("Referências")]
    [Tooltip("O Bismutado que criou este campo")]
    [HideInInspector] public Geobionte_AI ownerBismutado;

    // Estado interno
    private bool playerInside = false;
    private float playerTimeInside = 0f;
    private bool hasStolen = false;          // Já roubou buff neste campo?
    private PlayerDebuffs playerDebuffs;
    private float lifeTimer = 0f;

    // Visual
    private Renderer fieldRenderer;
    private Material fieldMaterial;

    void Start()
    {
        SetupCollider();
        SetupVisual();
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

            // Após stealBuffTime segundos, tenta roubar buff
            if (playerTimeInside >= stealBuffTime)
            {
                AttemptStealBuff();
            }
        }

        // Pulsação visual
        UpdateVisualPulse();
    }

    void SetupCollider()
    {
        // Garante que tem um trigger collider
        SphereCollider col = GetComponent<SphereCollider>();
        if (col == null)
        {
            col = gameObject.AddComponent<SphereCollider>();
        }
        col.isTrigger = true;
        col.radius = fieldRadius;
    }

    void SetupVisual()
    {
        // Cria um visual placeholder: cilindro achatado com emissão
        // (o cristal real será adicionado depois com arte)
        fieldRenderer = GetComponentInChildren<Renderer>();

        if (fieldRenderer == null)
        {
            // Cria placeholder visual
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            visual.transform.SetParent(transform);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localScale = new Vector3(fieldRadius * 2f, 0.05f, fieldRadius * 2f);

            // Remove o collider do cilindro (já temos o SphereCollider trigger)
            Collider visualCol = visual.GetComponent<Collider>();
            if (visualCol != null) Destroy(visualCol);

            fieldRenderer = visual.GetComponent<Renderer>();
        }

        if (fieldRenderer != null)
        {
            // URP usa "Universal Render Pipeline/Lit", fallback para "Standard"
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");

            fieldMaterial = new Material(shader);

            // Cor bismuto: roxo/rosa iridescente
            Color bismuthColor = new Color(0.7f, 0.3f, 0.6f, 0.4f);

            // Configurar transparência (compatível com URP e Built-in)
            if (shader.name.Contains("Universal"))
            {
                // URP: Surface Type = Transparent
                fieldMaterial.SetFloat("_Surface", 1); // 0=Opaque, 1=Transparent
                fieldMaterial.SetFloat("_Blend", 0);   // 0=Alpha, 1=Premultiply, 2=Additive, 3=Multiply
                fieldMaterial.SetFloat("_ZWrite", 0);
                fieldMaterial.SetFloat("_AlphaClip", 0);
                fieldMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                fieldMaterial.DisableKeyword("_ALPHATEST_ON");
                fieldMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

                fieldMaterial.SetColor("_BaseColor", bismuthColor);
            }
            else
            {
                // Built-in pipeline fallback
                fieldMaterial.SetFloat("_Mode", 3);
                fieldMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                fieldMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                fieldMaterial.SetInt("_ZWrite", 0);
                fieldMaterial.EnableKeyword("_ALPHABLEND_ON");
                fieldMaterial.renderQueue = 3000;
            }

            fieldMaterial.color = bismuthColor;
            fieldMaterial.EnableKeyword("_EMISSION");
            fieldMaterial.SetColor("_EmissionColor", bismuthColor * 1.5f);

            fieldRenderer.material = fieldMaterial;
        }
    }

    void UpdateVisualPulse()
    {
        if (fieldMaterial == null) return;

        // Pulsação suave na emissão
        float pulse = Mathf.Sin(Time.time * 3f) * 0.5f + 1f;
        Color bismuthColor = new Color(0.7f, 0.3f, 0.6f, 1f);
        fieldMaterial.SetColor("_EmissionColor", bismuthColor * pulse);
    }

    // ==================== TRIGGER ====================

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playerDebuffs = other.GetComponent<PlayerDebuffs>();
        if (playerDebuffs == null)
        {
            // Tenta adicionar automaticamente se não existir
            playerDebuffs = other.gameObject.AddComponent<PlayerDebuffs>();
        }

        playerInside = true;
        playerTimeInside = 0f;

        // Aplica slow baseado na presença de buffs de speed
        float slowAmount = playerDebuffs.HasSpeedBuff() ? baseSlowPercent : enhancedSlowPercent;
        playerDebuffs.ApplySlow(slowAmount);

        Debug.Log($"[BISMUTH FIELD] Player entrou no campo! Slow: {slowAmount * 100}%");
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playerInside = false;
        playerTimeInside = 0f;

        // Remove o slow ao sair do campo
        if (playerDebuffs != null)
        {
            playerDebuffs.RemoveSlow();
            Debug.Log("[BISMUTH FIELD] Player saiu do campo. Slow removido.");
        }
    }

    // ==================== ROUBO DE BUFF ====================

    void AttemptStealBuff()
    {
        if (hasStolen || playerDebuffs == null) return;

        hasStolen = true;

        // Tenta roubar buff de speed
        bool stole = playerDebuffs.TryStealSpeedBuff(
            ownerBismutado != null ? ownerBismutado.gameObject : gameObject
        );

        if (stole)
        {
            Debug.Log("[BISMUTH FIELD] Buff de velocidade ROUBADO!");

            // Notifica o Bismutado dono para aplicar speed buff nele mesmo
            if (ownerBismutado != null)
            {
                ownerBismutado.OnStoleSpeedBuff();
            }
        }
        else
        {
            // Player não tinha buff → aplica slow mais forte (40%)
            playerDebuffs.RemoveSlow(); // remove o 20% atual
            playerDebuffs.ApplySlow(enhancedSlowPercent); // aplica 40%
            Debug.Log("[BISMUTH FIELD] Player sem buff! Slow aumentado para 40%!");
        }
    }

    // ==================== DESTRUIÇÃO ====================

    void DestroyField()
    {
        // Remove o slow do player se ele ainda está dentro
        if (playerInside && playerDebuffs != null)
        {
            playerDebuffs.RemoveSlow();
        }

        Debug.Log("[BISMUTH FIELD] Campo expirou.");
        Destroy(gameObject);
    }

    void OnDestroy()
    {
        // Segurança: remove slow se o campo for destruído por qualquer motivo
        if (playerInside && playerDebuffs != null)
        {
            playerDebuffs.RemoveSlow();
        }

        if (fieldMaterial != null) Destroy(fieldMaterial);
    }

    // ==================== GIZMOS ====================

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.7f, 0.3f, 0.6f, 0.2f);
        Gizmos.DrawSphere(transform.position, fieldRadius);

        Gizmos.color = new Color(0.7f, 0.3f, 0.6f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, fieldRadius);
    }
}
