using UnityEngine;

/// <summary>
/// VFX para a Essência - Orbe Eetéreo Dourado / Amarelo Vibrante
/// Possui núcleo com profundidade de cor (HDR Gold), aura de brilho quente e sistema de partículas flutuantes.
/// </summary>
public class EssenceVFX : MonoBehaviour
{
    [Header("Cores da Essência (Dourado / Amarelo)")]
    public Color coreColor = new Color(1.0f, 0.88f, 0.15f, 1f); // Amarelo Dourado Vibrante
    public Color glowColor = new Color(1.0f, 0.65f, 0.05f, 0.75f); // Âmbar Quente Profundo

    [Header("Glow e Intensidade")]
    public float glowIntensity = 3.2f;
    public float pulseSpeed = 2.2f;
    public float pulseAmount = 0.35f;
    public float glowRadius = 1.6f;

    [Header("Partículas Flutuantes")]
    public bool enableParticles = true;
    public int particleCountPerSecond = 20;

    private Light glowLight;
    private Material coreMaterial;
    private Material glowMaterial;
    private GameObject glowSphere;
    private ParticleSystem particleComp;
    private ParticleSystem collectBurstComp;

    private float baseIntensity;
    private float baseScale;

    void Start()
    {
        SetupCoreMaterial();
        SetupGlowSphere();
        SetupGlowLight();
        if (enableParticles)
        {
            SetupParticleSystem();
        }
        baseScale = transform.localScale.x;
    }

    void Update()
    {
        float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
        float fastPulse = 1f + Mathf.Sin(Time.time * pulseSpeed * 2.2f) * (pulseAmount * 0.4f);

        // Pulso na luz
        if (glowLight != null)
        {
            glowLight.color = coreColor;
            glowLight.intensity = baseIntensity * pulse;
            glowLight.range = glowRadius * pulse;
        }

        // Pulso na esfera de glow externo
        if (glowSphere != null)
        {
            float glowScale = glowRadius * pulse * 0.75f;
            glowSphere.transform.localScale = new Vector3(glowScale, glowScale, glowScale);

            if (glowMaterial != null)
            {
                float alpha = 0.25f + (pulse - 1f) * 0.35f;
                glowMaterial.color = new Color(glowColor.r, glowColor.g, glowColor.b, alpha);
            }
        }

        // Pulso na emissão do núcleo (Profundidade de Cor HDR)
        if (coreMaterial != null)
        {
            float emissionMult = 1.2f + fastPulse * 0.8f;
            Color hdrColor = coreColor * emissionMult * glowIntensity;
            coreMaterial.SetColor("_EmissionColor", hdrColor);
            coreMaterial.color = new Color(coreColor.r, coreColor.g, coreColor.b, 0.95f);
        }
    }

    void SetupCoreMaterial()
    {
        Renderer rend = GetComponent<Renderer>();
        if (rend != null)
        {
            // Tenta encontrar shader URP Unlit/Lit ou Standard
            Shader targetShader = Shader.Find("Universal Render Pipeline/Lit");
            if (targetShader == null) targetShader = Shader.Find("Universal Render Pipeline/Unlit");
            if (targetShader == null) targetShader = Shader.Find("Standard");
            if (targetShader == null) targetShader = Shader.Find("Sprites/Default");

            coreMaterial = new Material(targetShader);
            coreMaterial.color = coreColor;
            coreMaterial.EnableKeyword("_EMISSION");
            coreMaterial.SetColor("_EmissionColor", coreColor * glowIntensity * 2.5f);

            // Suporte a transparência e modo de renderização emissivo
            if (coreMaterial.HasProperty("_Mode"))
            {
                coreMaterial.SetFloat("_Mode", 3);
            }
            coreMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            coreMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
            coreMaterial.SetInt("_ZWrite", 1);
            coreMaterial.EnableKeyword("_ALPHABLEND_ON");
            coreMaterial.renderQueue = 3000;

            rend.material = coreMaterial;
        }
    }

    void SetupGlowSphere()
    {
        // Esfera maior translúcida para criar a atmosfera ao redor da orbe
        glowSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        glowSphere.name = "EssenceGlowAtmosphere";
        glowSphere.transform.SetParent(transform);
        glowSphere.transform.localPosition = Vector3.zero;
        glowSphere.transform.localScale = new Vector3(glowRadius, glowRadius, glowRadius);

        Collider col = glowSphere.GetComponent<Collider>();
        if (col != null) Destroy(col);

        Renderer rend = glowSphere.GetComponent<Renderer>();
        if (rend != null)
        {
            Shader blendShader = Shader.Find("Sprites/Default");
            if (blendShader == null) blendShader = Shader.Find("Mobile/Particles/Additive");
            
            glowMaterial = new Material(blendShader);
            glowMaterial.color = new Color(glowColor.r, glowColor.g, glowColor.b, 0.35f);
            rend.material = glowMaterial;
        }
    }

    void SetupGlowLight()
    {
        GameObject lightObj = new GameObject("EssenceLight");
        lightObj.transform.SetParent(transform);
        lightObj.transform.localPosition = Vector3.zero;

        glowLight = lightObj.AddComponent<Light>();
        glowLight.type = LightType.Point;
        glowLight.color = coreColor;
        glowLight.intensity = glowIntensity;
        glowLight.range = glowRadius * 2.5f;
        glowLight.shadows = LightShadows.None;

        baseIntensity = glowIntensity;
        SetupTrail();
    }

    private TrailRenderer trailComp;

    private void SetupTrail()
    {
        GameObject trailObj = new GameObject("EssenceTrail");
        trailObj.transform.SetParent(transform);
        trailObj.transform.localPosition = Vector3.zero;

        trailComp = trailObj.AddComponent<TrailRenderer>();
        trailComp.time = 0.4f;
        trailComp.startWidth = 0.3f;
        trailComp.endWidth = 0.02f;
        
        Shader trailShader = Shader.Find("Sprites/Default");
        if (trailShader == null) trailShader = Shader.Find("Mobile/Particles/Additive");
        
        trailComp.material = new Material(trailShader);
        trailComp.startColor = new Color(coreColor.r, coreColor.g, coreColor.b, 0.9f);
        trailComp.endColor = new Color(glowColor.r, glowColor.g, glowColor.b, 0.0f);
        trailComp.emitting = false;
    }

    private void SetupParticleSystem()
    {
        GameObject pObj = new GameObject("EssenceSparkleParticles");
        pObj.transform.SetParent(transform);
        pObj.transform.localPosition = Vector3.zero;

        particleComp = pObj.AddComponent<ParticleSystem>();
        ParticleSystemRenderer pRend = pObj.GetComponent<ParticleSystemRenderer>();

        Shader particleShader = Shader.Find("Mobile/Particles/Additive");
        if (particleShader == null) particleShader = Shader.Find("Sprites/Default");
        pRend.material = new Material(particleShader);

        // Módulo Main
        var main = particleComp.main;
        main.duration = 1f;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.3f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 1.2f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.22f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1.0f, 0.95f, 0.4f, 1f),
            new Color(1.0f, 0.65f, 0.1f, 1f)
        );
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        // Módulo Emission
        var emission = particleComp.emission;
        emission.rateOverTime = particleCountPerSecond;

        // Módulo Shape (Esfera ao redor da orbe)
        var shape = particleComp.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.35f;

        // Módulo Velocity Over Lifetime (Subida flutuante)
        var vel = particleComp.velocityOverLifetime;
        vel.enabled = true;
        vel.y = new ParticleSystem.MinMaxCurve(0.4f, 1.0f);

        // Módulo Noise (Efeito orgânico de dança/órbita)
        var noise = particleComp.noise;
        noise.enabled = true;
        noise.strength = 0.4f;
        noise.frequency = 1.2f;

        // Módulo Color Over Lifetime (Fade out)
        var col = particleComp.colorOverLifetime;
        col.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(coreColor, 0.0f), new GradientColorKey(glowColor, 0.7f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(0.8f, 0.5f), new GradientAlphaKey(0.0f, 1.0f) }
        );
        col.color = grad;

        // Módulo Size Over Lifetime (Encolhe levemente ao subir)
        var size = particleComp.sizeOverLifetime;
        size.enabled = true;
        AnimationCurve curve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0f));
        size.size = new ParticleSystem.MinMaxCurve(1f, curve);

        particleComp.Play();
    }

    public void StartFlyingTrail()
    {
        if (trailComp != null)
        {
            trailComp.emitting = true;
        }
    }

    /// <summary>
    /// Efeito de coleta - Explosão de brilho dourado ao encostar no player
    /// </summary>
    public void PlayCollectEffect()
    {
        // Flash de luz amarela no momento da coleta
        if (glowLight != null)
        {
            glowLight.intensity = glowIntensity * 6f;
            glowLight.transform.SetParent(null);
            Destroy(glowLight.gameObject, 0.3f);
        }

        // Explosão de partículas de coleta
        GameObject burstObj = new GameObject("EssenceCollectBurst");
        burstObj.transform.position = transform.position;

        ParticleSystem burst = burstObj.AddComponent<ParticleSystem>();
        ParticleSystemRenderer bRend = burstObj.GetComponent<ParticleSystemRenderer>();

        Shader particleShader = Shader.Find("Mobile/Particles/Additive");
        if (particleShader == null) particleShader = Shader.Find("Sprites/Default");
        bRend.material = new Material(particleShader);

        var main = burst.main;
        main.duration = 0.4f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.6f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 7f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.3f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.95f, 0.5f, 1f), new Color(1f, 0.7f, 0.1f, 1f));

        var emission = burst.emission;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 30) });

        var shape = burst.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.2f;

        burst.Play();
        Destroy(burstObj, 0.7f);

        // Expande e desaparece o glow
        if (glowSphere != null)
        {
            glowSphere.transform.SetParent(null);
            glowSphere.transform.localScale *= 2.5f;
            Destroy(glowSphere, 0.18f);
        }

        if (trailComp != null)
        {
            trailComp.transform.SetParent(null);
            Destroy(trailComp.gameObject, 0.4f);
        }
    }

    void OnDestroy()
    {
        if (coreMaterial != null) Destroy(coreMaterial);
        if (glowMaterial != null) Destroy(glowMaterial);
    }
}
