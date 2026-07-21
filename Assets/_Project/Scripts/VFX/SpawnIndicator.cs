using UnityEngine;

/// <summary>
/// Indicador visual premium de spawn de inimigos.
/// Apresenta múltiplos anéis concêntricos (de mira, de carga e ondulado de energia)
/// que pulsam, giram em direções opostas, e aceleram junto com partículas espiraladas
/// em direção ao momento do spawn (pico/flash).
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class SpawnIndicator : MonoBehaviour
{
    [Header("Visual Geral")]
    [Tooltip("Raio final do círculo indicador.")]
    public float radius = 1.2f;
    [Tooltip("Quantidade de segmentos para suavidade do círculo.")]
    public int segments = 80;
    [Tooltip("Duração total do indicador antes de sumir.")]
    public float duration = 1.5f;

    [Header("Cores (HDR / Glow)")]
    [Tooltip("Cor base do glow de carregamento.")]
    public Color glowColor = new Color(0.95f, 0.35f, 0.05f, 1f); // Laranja quente
    [Tooltip("Cor de pico de energia e do flash final.")]
    public Color peakColor = new Color(1f, 0.85f, 0.1f, 1f);   // Amarelo/Ciano elétrico

    [Header("Velocidades Dinâmicas")]
    [Tooltip("Velocidade inicial de rotação dos anéis (graus por segundo).")]
    public float rotationSpeed = 30f;
    [Tooltip("Frequência de pulsação de intensidade.")]
    public float pulseSpeed = 4f;

    // --- Componentes de Renderização ---
    private LineRenderer mainRing;   // Anel de contenção principal no raio alvo
    private LineRenderer outerRing;  // Anel de mira que encolhe de fora para dentro
    private LineRenderer wavyRing;   // Anel interno ondulado que mostra acúmulo de energia
    private LineRenderer innerRing;  // Pequeno anel interno rotatório
    private LineRenderer centerBeam; // Pilar de energia vertical central
    private ParticleSystem sparks;
    private ParticleSystem centerGeyser; // Geyser de partículas subindo no centro

    // --- Internos ---
    private float time = 0f;
    private Material sharedMaterial;

    void Awake()
    {
        // Se as cores forem os valores padrão antigos (roxo/ciano), substitui pelos novos quentes
        if (glowColor == new Color(0.55f, 0.1f, 0.9f, 1f))
            glowColor = new Color(0.95f, 0.35f, 0.05f, 1f);
        if (peakColor == new Color(0.15f, 0.85f, 1f, 1f))
            peakColor = new Color(1.0f, 0.85f, 0.1f, 1f);

        // Cria material aditivo compatível com URP e Standard para efeito de Glow/Neon
        Shader additiveShader = Shader.Find("Legacy Shaders/Particles/Additive");
        if (additiveShader == null)
        {
            additiveShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        }
        sharedMaterial = new Material(additiveShader);

        BuildRings();
        BuildParticles();
    }

    void Update()
    {
        time += Time.deltaTime;
        float progress = Mathf.Clamp01(time / duration);

        // Acelera a pulsação e a rotação à medida que se aproxima do final (100% de carga)
        float currentRotationSpeed = rotationSpeed * Mathf.Lerp(1f, 6f, progress);
        float currentPulseSpeed = pulseSpeed * Mathf.Lerp(1f, 3f, progress);
        
        // Pulso oscilante (0 a 1)
        float pulse = (Mathf.Sin(time * currentPulseSpeed) + 1f) * 0.5f;

        // Cor dinâmica interpolada (fica mais ciano/elétrica perto do spawn)
        Color currentColor = Color.Lerp(glowColor, peakColor, progress);
        
        // Efeito de flash extremo nos últimos 10% da duração
        if (progress > 0.9f)
        {
            float flashT = (progress - 0.9f) / 0.1f;
            currentColor = Color.Lerp(currentColor, Color.white, flashT);
        }

        // --- 1. Desenho do Anel de Mira Externo (Encolhe até o raio alvo) ---
        // Nasce 2x maior e converge para o tamanho certo nos primeiros 40% do tempo
        float outerProgress = Mathf.Clamp01(progress / 0.40f);
        float outerR = Mathf.Lerp(radius * 2.0f, radius, outerProgress);
        Color outerColor = currentColor;
        outerColor.a = Mathf.Lerp(0f, 1f, outerProgress) * (1f - progress * 0.3f);
        DrawCircle(outerRing, outerR, 0.05f + pulse * 0.04f, outerColor, time * currentRotationSpeed);

        // --- 2. Desenho do Anel Principal (Fixo no chão) ---
        Color mainColor = currentColor;
        mainColor.a = Mathf.Lerp(0f, 0.9f, progress * 4f); // Surge rápido
        float mainWidth = Mathf.Lerp(0.06f, 0.14f, pulse);
        DrawCircle(mainRing, radius, mainWidth, mainColor, -time * (currentRotationSpeed * 0.3f));

        // --- 3. Desenho do Anel Ondulado (Wavy / Energia instável) ---
        Color wavyColor = currentColor * 0.7f;
        wavyColor.a = progress; // Fica mais evidente com o tempo
        DrawCircle(wavyRing, radius * 0.75f, 0.05f, wavyColor, time * (currentRotationSpeed * 1.5f), true, progress);

        // --- 4. Desenho do Anel Interno (Gira rápido na direção oposta) ---
        Color innerColor = Color.Lerp(glowColor, peakColor, pulse);
        innerColor.a = Mathf.Lerp(0.2f, 0.8f, progress);
        DrawCircle(innerRing, radius * 0.35f, 0.04f, innerColor, -time * (currentRotationSpeed * 2f));

        // --- 5. Desenho do Pilar de Energia Central (Cresce verticalmente e expande) ---
        if (centerBeam != null)
        {
            centerBeam.startColor = currentColor;
            centerBeam.endColor = new Color(currentColor.r, currentColor.g, currentColor.b, 0f);
            
            float beamWidth = Mathf.Lerp(0.01f, radius * 0.45f, Mathf.Pow(progress, 3.5f));
            centerBeam.startWidth = beamWidth;
            centerBeam.endWidth = beamWidth * 0.2f;
            
            centerBeam.SetPosition(0, new Vector3(0f, 0.03f, 0f));
            centerBeam.SetPosition(1, new Vector3(0f, 3.5f * progress, 0f));
        }

        // --- 6. Controle Dinâmico das Partículas ---
        if (sparks != null)
        {
            var emission = sparks.emission;
            // Taxa de faíscas aumenta conforme carrega
            emission.rateOverTime = Mathf.Lerp(15f, 70f, progress);

            var main = sparks.main;
            // Partículas se movem mais rápido no final
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f * Mathf.Lerp(1f, 2.5f, progress));
        }

        if (centerGeyser != null)
        {
            var emission = centerGeyser.emission;
            // Erupção de partículas aumenta exponencialmente conforme chega perto do spawn
            emission.rateOverTime = Mathf.Lerp(25f, 130f, Mathf.Pow(progress, 2.2f));

            var main = centerGeyser.main;
            main.startSpeed = new ParticleSystem.MinMaxCurve(3.0f * Mathf.Lerp(1f, 2.0f, progress));
        }
    }

    // =========================================================
    // INICIALIZAÇÃO DE COMPONENTES
    // =========================================================

    void BuildRings()
    {
        // Anel principal (GameObject raiz com LineRenderer)
        mainRing = GetComponent<LineRenderer>();
        SetupLineRenderer(mainRing);

        // Anel externo (mira)
        outerRing = CreateChildRing("OuterRing");
        // Anel ondulado (carregamento de energia)
        wavyRing = CreateChildRing("WavyRing");
        // Anel interno
        innerRing = CreateChildRing("InnerRing");
        // Pilar central vertical
        centerBeam = CreateChildRing("CenterBeam");
        centerBeam.loop = false;
        centerBeam.positionCount = 2;
    }

    LineRenderer CreateChildRing(string name)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(transform, false);
        child.transform.localPosition = Vector3.zero;
        child.transform.localRotation = Quaternion.identity;
        LineRenderer lr = child.AddComponent<LineRenderer>();
        SetupLineRenderer(lr);
        return lr;
    }

    void SetupLineRenderer(LineRenderer lr)
    {
        lr.loop = true;
        lr.positionCount = segments + 1;
        lr.useWorldSpace = false;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;
        lr.material = sharedMaterial;
    }

    void BuildParticles()
    {
        GameObject psObj = new GameObject("Sparks");
        psObj.transform.SetParent(transform, false);
        psObj.transform.localPosition = new Vector3(0f, 0.05f, 0f);

        sparks = psObj.AddComponent<ParticleSystem>();

        var main = sparks.main;
        main.loop            = true;
        main.startLifetime   = new ParticleSystem.MinMaxCurve(0.4f, 0.9f);
        main.startSpeed      = new ParticleSystem.MinMaxCurve(1.0f, 2.5f);
        main.startSize       = new ParticleSystem.MinMaxCurve(0.04f, 0.12f);
        main.gravityModifier = -0.3f; // Sobem flutuando como brasas
        main.maxParticles    = 100;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = sparks.emission;
        emission.enabled = true;
        emission.rateOverTime = 15f;

        // Emite a partir de uma borda circular fina
        var shape = sparks.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = radius * 0.9f;
        shape.radiusThickness = 0.05f;

        // Comportamento estético das partículas (Cores & Tamanho)
        var sizeOverLifetime = sparks.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 0.1f);
        sizeCurve.AddKey(0.2f, 1f);
        sizeCurve.AddKey(0.8f, 0.8f);
        sizeCurve.AddKey(1f, 0f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        var colorOverLifetime = sparks.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { 
                new GradientColorKey(Color.white, 0f), 
                new GradientColorKey(peakColor, 0.3f), 
                new GradientColorKey(glowColor, 0.7f) 
            },
            new GradientAlphaKey[] { 
                new GradientAlphaKey(1f, 0f), 
                new GradientAlphaKey(0.9f, 0.5f), 
                new GradientAlphaKey(0f, 1f) 
            }
        );
        colorOverLifetime.color = grad;

        // Ruído orgânico tridimensional para simular calor ondulante/espir
        var noise = sparks.noise;
        noise.enabled = true;
        noise.strength = 0.6f;
        noise.frequency = 1.5f;
        noise.scrollSpeed = 1.2f;

        var renderer = psObj.GetComponent<ParticleSystemRenderer>();
        renderer.material = sharedMaterial;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;

        sparks.Play();

        // Geyser Central (fountain of energy/dust shooting up from center)
        GameObject geyserObj = new GameObject("CenterGeyser");
        geyserObj.transform.SetParent(transform, false);
        geyserObj.transform.localPosition = new Vector3(0f, 0.02f, 0f);

        centerGeyser = geyserObj.AddComponent<ParticleSystem>();

        var gMain = centerGeyser.main;
        gMain.loop            = true;
        gMain.startLifetime   = new ParticleSystem.MinMaxCurve(0.35f, 0.75f);
        gMain.startSpeed      = new ParticleSystem.MinMaxCurve(3.0f, 6.0f);
        gMain.startSize       = new ParticleSystem.MinMaxCurve(0.06f, 0.16f);
        gMain.gravityModifier = -0.6f;
        gMain.maxParticles    = 150;
        gMain.simulationSpace = ParticleSystemSimulationSpace.World;

        var gEmission = centerGeyser.emission;
        gEmission.enabled = true;
        gEmission.rateOverTime = 25f;

        var gShape = centerGeyser.shape;
        gShape.enabled = true;
        gShape.shapeType = ParticleSystemShapeType.Circle;
        gShape.radius = radius * 0.15f;
        gShape.radiusThickness = 0.02f;

        var gSize = centerGeyser.sizeOverLifetime;
        gSize.enabled = true;
        AnimationCurve gSizeCurve = new AnimationCurve();
        gSizeCurve.AddKey(0f, 0.2f);
        gSizeCurve.AddKey(0.15f, 1f);
        gSizeCurve.AddKey(0.7f, 0.8f);
        gSizeCurve.AddKey(1f, 0f);
        gSize.size = new ParticleSystem.MinMaxCurve(1f, gSizeCurve);

        var gColor = centerGeyser.colorOverLifetime;
        gColor.enabled = true;
        Gradient gGrad = new Gradient();
        gGrad.SetKeys(
            new GradientColorKey[] { 
                new GradientColorKey(Color.white, 0f), 
                new GradientColorKey(peakColor, 0.25f), 
                new GradientColorKey(glowColor, 0.65f) 
            },
            new GradientAlphaKey[] { 
                new GradientAlphaKey(1f, 0f), 
                new GradientAlphaKey(0.85f, 0.4f), 
                new GradientAlphaKey(0f, 1f) 
            }
        );
        gColor.color = gGrad;

        var gNoise = centerGeyser.noise;
        gNoise.enabled = true;
        gNoise.strength = 0.8f;
        gNoise.frequency = 2.0f;
        gNoise.scrollSpeed = 2.0f;

        var gRenderer = geyserObj.GetComponent<ParticleSystemRenderer>();
        gRenderer.material = sharedMaterial;
        gRenderer.renderMode = ParticleSystemRenderMode.Billboard;

        centerGeyser.Play();
    }

    // =========================================================
    // DESENHO GEOMÉTRICO DOS CÍRCULOS
    // =========================================================

    void DrawCircle(LineRenderer lr, float r, float width, Color color, float rotationAngle, bool isWavy = false, float progress = 0f)
    {
        if (lr == null) return;

        lr.startWidth = width;
        lr.endWidth   = width;
        lr.startColor = color;
        lr.endColor   = color;

        float angleStep = 360f / segments;
        for (int i = 0; i <= segments; i++)
        {
            float angleDeg = (i * angleStep) + rotationAngle;
            float angleRad = angleDeg * Mathf.Deg2Rad;
            
            float currentR = r;
            if (isWavy)
            {
                // A distorção ondulatória aumenta com o progresso do tempo e pulsação
                float waveFrequency = 10f;
                float waveAmplitude = 0.08f * progress;
                currentR += Mathf.Sin(angleRad * waveFrequency + time * 15f) * waveAmplitude;
            }

            float x = Mathf.Cos(angleRad) * currentR;
            float z = Mathf.Sin(angleRad) * currentR;
            
            // Desenha rente ao chão (3cm acima do plano Y para evitar Z-fighting)
            lr.SetPosition(i, new Vector3(x, 0.03f, z));
        }
    }

    private void OnDestroy()
    {
        if (sharedMaterial != null)
        {
            Destroy(sharedMaterial);
        }
    }
}
