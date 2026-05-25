using UnityEngine;

/// <summary>
/// Indicador visual de spawn de inimigo.
///
/// COMO CRIAR O PREFAB NO UNITY:
///   1. Crie um GameObject vazio na cena → renomeie para "SpawnIndicator".
///   2. Adicione este script (Add Component → SpawnIndicator).
///   3. O script cria automaticamente o LineRenderer e o ParticleSystem no Awake.
///   4. Salve como Prefab em: Assets/_Project/Enviroment/VFX/SpawnIndicator.prefab
///   5. Arraste o prefab para o campo "Spawn Indicator Prefab" do RoomController.
///
/// O RoomController já cuida de instanciar e destruir este objeto no tempo certo.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class SpawnIndicator : MonoBehaviour
{
    [Header("Visual")]
    [Tooltip("Raio do círculo em unidades do mundo.")]
    public float radius = 1.2f;
    [Tooltip("Quantidade de pontos que formam o círculo (mais = mais suave).")]
    public int segments = 64;
    [Tooltip("Cor base do glow. Combina com a paleta do bioma.")]
    public Color glowColor = new Color(0.55f, 0.1f, 0.9f, 1f); // roxo escuro
    [Tooltip("Cor do pico do pulso.")]
    public Color peakColor = new Color(0.85f, 0.3f, 1f, 1f);   // roxo claro vibrante

    [Header("Animação")]
    [Tooltip("Velocidade de pulsação do anel.")]
    public float pulseSpeed = 3.5f;
    [Tooltip("Velocidade de rotação das runas (em graus por segundo).")]
    public float rotationSpeed = 45f;
    [Tooltip("Velocidade de crescimento do raio no início.")]
    public float growSpeed = 2.5f;

    // --- Internos ---
    private LineRenderer ring;
    private LineRenderer innerRing;
    private ParticleSystem sparks;
    private float currentRadius = 0f;
    private float time = 0f;
    private bool ready = false;

    void Awake()
    {
        BuildRings();
        BuildParticles();
    }

    void Update()
    {
        time += Time.deltaTime;

        // --- Cresce até o raio alvo ---
        if (currentRadius < radius)
        {
            currentRadius = Mathf.MoveTowards(currentRadius, radius, growSpeed * Time.deltaTime);
        }
        else if (!ready)
        {
            ready = true;
            if (sparks != null) sparks.Play();
        }

        // --- Pulso de intensidade ---
        float pulse = (Mathf.Sin(time * pulseSpeed) + 1f) * 0.5f; // 0..1
        Color currentColor = Color.Lerp(glowColor, peakColor, pulse);

        // Largura pulsa levemente
        float width = Mathf.Lerp(0.04f, 0.12f, pulse);

        // --- Anel externo ---
        DrawCircle(ring, currentRadius, width, currentColor);

        // --- Anel interno (50% do tamanho, rotação oposta) ---
        float innerR = currentRadius * 0.5f;
        Color innerColor = currentColor;
        innerColor.a *= 0.6f;
        DrawCircle(innerRing, innerR, width * 0.6f, innerColor);

        // --- Rotação do objeto (gira as runas) ---
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
    }

    // =========================================================
    // CONSTRUÇÃO DOS COMPONENTES
    // =========================================================

    void BuildRings()
    {
        // Anel externo (já existe como LineRenderer obrigatório)
        ring = GetComponent<LineRenderer>();
        SetupLineRenderer(ring);

        // Anel interno como filho
        GameObject innerObj = new GameObject("InnerRing");
        innerObj.transform.SetParent(transform, false);
        innerObj.transform.localPosition = Vector3.zero;
        innerRing = innerObj.AddComponent<LineRenderer>();
        SetupLineRenderer(innerRing);
    }

    void SetupLineRenderer(LineRenderer lr)
    {
        lr.loop = true;
        lr.positionCount = segments + 1;
        lr.useWorldSpace = false;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;

        // Shader que emite luz (HDR). Usa o shader built-in do URP.
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
        mat.SetColor("_BaseColor", glowColor);

        // Ativa emissão para o bloom do URP funcionar
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", glowColor * 2f);
        lr.material = mat;

        lr.startWidth = 0.08f;
        lr.endWidth   = 0.08f;
    }

    void BuildParticles()
    {
        GameObject psObj = new GameObject("Sparks");
        psObj.transform.SetParent(transform, false);
        psObj.transform.localPosition = Vector3.zero;

        sparks = psObj.AddComponent<ParticleSystem>();

        var main = sparks.main;
        main.loop            = true;
        main.startLifetime   = new ParticleSystem.MinMaxCurve(0.5f, 1.2f);
        main.startSpeed      = new ParticleSystem.MinMaxCurve(0.8f, 2.0f);
        main.startSize       = new ParticleSystem.MinMaxCurve(0.03f, 0.1f);
        main.startColor      = new ParticleSystem.MinMaxGradient(glowColor, peakColor);
        main.gravityModifier = -0.15f; // partículas sobem levemente
        main.maxParticles    = 40;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = sparks.emission;
        emission.enabled     = true;
        emission.rateOverTime = 20f;

        // Emite a partir do anel (shape circular)
        var shape = sparks.shape;
        shape.enabled    = true;
        shape.shapeType  = ParticleSystemShapeType.Circle;
        shape.radius     = radius;
        shape.radiusThickness = 0.1f; // borda do círculo

        // Renderizador das partículas
        var renderer = psObj.GetComponent<ParticleSystemRenderer>();
        Material pMat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
        pMat.SetColor("_BaseColor", glowColor);
        renderer.material = pMat;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;

        sparks.Stop(); // começa parado, ativa quando o círculo estiver pronto
    }

    // =========================================================
    // DESENHO DO CÍRCULO
    // =========================================================

    void DrawCircle(LineRenderer lr, float r, float width, Color color)
    {
        if (lr == null) return;

        lr.startWidth = width;
        lr.endWidth   = width;
        lr.material.SetColor("_BaseColor", color);
        lr.material.SetColor("_EmissionColor", color * 2.5f);

        for (int i = 0; i <= segments; i++)
        {
            float angle = (float)i / segments * Mathf.PI * 2f;
            float x = Mathf.Cos(angle) * r;
            float z = Mathf.Sin(angle) * r;
            lr.SetPosition(i, new Vector3(x, 0.05f, z)); // 5cm acima do chão
        }
    }
}
