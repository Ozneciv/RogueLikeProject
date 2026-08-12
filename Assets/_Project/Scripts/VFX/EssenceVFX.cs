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

        // Pulso na esfera de glow externo (Aura sutil e translúcida amarela)
        if (glowSphere != null)
        {
            float glowScale = (glowRadius * 0.6f) * pulse;
            glowSphere.transform.localScale = new Vector3(glowScale, glowScale, glowScale);

            if (glowMaterial != null)
            {
                float alpha = 0.05f + (pulse - 1f) * 0.05f; // Transparência suave (nunca sólida)
                Color goldAlpha = new Color(1.0f, 0.80f, 0.15f, alpha);
                glowMaterial.color = goldAlpha;
                if (glowMaterial.HasProperty("_BaseColor")) glowMaterial.SetColor("_BaseColor", goldAlpha);
                if (glowMaterial.HasProperty("_Color")) glowMaterial.SetColor("_Color", goldAlpha);
            }
        }

        // Pulso na emissão do núcleo (Profundidade de Cor HDR Dourado)
        if (coreMaterial != null)
        {
            float emissionMult = 1.2f + fastPulse * 0.8f;
            Color hdrColor = coreColor * emissionMult * glowIntensity;
            coreMaterial.SetColor("_EmissionColor", hdrColor);
            coreMaterial.color = new Color(coreColor.r, coreColor.g, coreColor.b, 0.95f);
        }
    }

    private Material CreateYellowMaterial(bool transparent)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit")
                     ?? Shader.Find("Universal Render Pipeline/Lit")
                     ?? Shader.Find("Particles/Standard Unlit")
                     ?? Shader.Find("Unlit/Color")
                     ?? Shader.Find("Sprites/Default")
                     ?? Shader.Find("Standard");

        Material mat = new Material(shader);
        Color yellow = transparent ? new Color(1.0f, 0.80f, 0.15f, 0.08f) : new Color(1.0f, 0.85f, 0.10f, 1.0f);

        mat.color = yellow;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", yellow);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", yellow);

        if (!transparent)
        {
            mat.EnableKeyword("_EMISSION");
            if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", yellow * glowIntensity * 2.5f);
        }
        else
        {
            if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f); // Transparent
            if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 1f); // Additive
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
            mat.SetInt("_ZWrite", 0);
            mat.renderQueue = 3000;
        }

        return mat;
    }

    void SetupCoreMaterial()
    {
        Renderer rend = GetComponent<Renderer>();
        if (rend != null)
        {
            coreMaterial = CreateYellowMaterial(false);
            rend.material = coreMaterial;
        }
    }

    void SetupGlowSphere()
    {
        glowSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        glowSphere.name = "EssenceGlowAtmosphere";
        glowSphere.transform.SetParent(transform);
        glowSphere.transform.localPosition = Vector3.zero;
        glowSphere.transform.localScale = new Vector3(glowRadius * 0.6f, glowRadius * 0.6f, glowRadius * 0.6f);

        Collider col = glowSphere.GetComponent<Collider>();
        if (col != null) Destroy(col);

        Renderer gRend = glowSphere.GetComponent<Renderer>();
        if (gRend != null)
        {
            glowMaterial = CreateYellowMaterial(true);
            gRend.material = glowMaterial;
            gRend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            gRend.receiveShadows = false;
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

        trailComp.material = CreateYellowMaterial(true);
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
        particleComp.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ParticleSystemRenderer pRend = pObj.GetComponent<ParticleSystemRenderer>();
        pRend.material = CreateYellowMaterial(true);

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

        // Módulo Velocity Over Lifetime (Subida flutuante - eixos uniformizados)
        var vel = particleComp.velocityOverLifetime;
        vel.enabled = true;
        vel.x = new ParticleSystem.MinMaxCurve(0f, 0f);
        vel.y = new ParticleSystem.MinMaxCurve(0.4f, 1.0f);
        vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);

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
        burst.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ParticleSystemRenderer bRend = burstObj.GetComponent<ParticleSystemRenderer>();
        bRend.material = CreateYellowMaterial(true);

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
