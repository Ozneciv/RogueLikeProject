using UnityEngine;

/// <summary>
/// Efeito visual para a Essência
/// Cria partículas brilhantes e glow automaticamente via código
/// </summary>
public class EssenceVFX : MonoBehaviour
{
    [Header("Cores")]
    public Color essenceColor = new Color(0.5f, 0.8f, 1f, 1f); // Azul claro
    public Color glowColor = new Color(0.3f, 0.6f, 1f, 0.5f);

    [Header("Partículas")]
    public int particleCount = 15;
    public float particleSpeed = 0.5f;
    public float particleSize = 0.1f;

    [Header("Glow")]
    public float glowIntensity = 2f;
    public float pulseSpeed = 2f;
    public float pulseAmount = 0.3f;

    private ParticleSystem particles;
    private Light glowLight;
    private Material essenceMaterial;
    private float baseIntensity;

    void Start()
    {
        SetupMaterial();
        SetupParticles();
        SetupGlow();
    }

    void Update()
    {
        // Efeito de pulso no glow
        if (glowLight != null)
        {
            float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
            glowLight.intensity = baseIntensity * pulse;
        }

        // Pulso no material também
        if (essenceMaterial != null)
        {
            float emission = 0.5f + Mathf.Sin(Time.time * pulseSpeed) * 0.3f;
            essenceMaterial.SetColor("_EmissionColor", essenceColor * emission * glowIntensity);
        }
    }

    void SetupMaterial()
    {
        Renderer rend = GetComponent<Renderer>();
        if (rend != null)
        {
            // Cria material brilhante
            essenceMaterial = new Material(Shader.Find("Standard"));
            essenceMaterial.color = essenceColor;
            essenceMaterial.EnableKeyword("_EMISSION");
            essenceMaterial.SetColor("_EmissionColor", essenceColor * glowIntensity);
            
            // Transparência
            essenceMaterial.SetFloat("_Mode", 3); // Transparent
            essenceMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            essenceMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            essenceMaterial.SetInt("_ZWrite", 0);
            essenceMaterial.DisableKeyword("_ALPHATEST_ON");
            essenceMaterial.EnableKeyword("_ALPHABLEND_ON");
            essenceMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            essenceMaterial.renderQueue = 3000;

            rend.material = essenceMaterial;
        }
    }

    void SetupParticles()
    {
        // Cria GameObject para partículas
        GameObject particleObj = new GameObject("EssenceParticles");
        particleObj.transform.SetParent(transform);
        particleObj.transform.localPosition = Vector3.zero;

        particles = particleObj.AddComponent<ParticleSystem>();

        // Configuração principal
        var main = particles.main;
        main.startColor = essenceColor;
        main.startSize = particleSize;
        main.startSpeed = particleSpeed;
        main.startLifetime = 1.5f;
        main.maxParticles = particleCount * 2;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.loop = true;

        // Emissão
        var emission = particles.emission;
        emission.rateOverTime = particleCount;

        // Forma (esfera ao redor)
        var shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.2f;

        // Cor ao longo do tempo (fade out)
        var colorOverLifetime = particles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(essenceColor, 0f),
                new GradientColorKey(essenceColor, 0.5f),
                new GradientColorKey(Color.white, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0.8f, 0.5f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = gradient;

        // Tamanho ao longo do tempo (diminui)
        var sizeOverLifetime = particles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, 0f);

        // Velocidade (sobe)
        var velocityOverLifetime = particles.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.y = 0.5f;

        // Renderer das partículas
        var particleRenderer = particleObj.GetComponent<ParticleSystemRenderer>();
        particleRenderer.material = new Material(Shader.Find("Particles/Standard Unlit"));
        particleRenderer.material.color = essenceColor;
    }

    void SetupGlow()
    {
        // Adiciona luz pontual para glow
        GameObject lightObj = new GameObject("EssenceGlow");
        lightObj.transform.SetParent(transform);
        lightObj.transform.localPosition = Vector3.zero;

        glowLight = lightObj.AddComponent<Light>();
        glowLight.type = LightType.Point;
        glowLight.color = glowColor;
        glowLight.range = 2f;
        glowLight.intensity = glowIntensity;
        glowLight.shadows = LightShadows.None;

        baseIntensity = glowIntensity;
    }

    /// <summary>
    /// Efeito de coleta (chame antes de destruir)
    /// </summary>
    public void PlayCollectEffect()
    {
        if (particles != null)
        {
            // Burst final de partículas
            var emission = particles.emission;
            emission.rateOverTime = 0;
            particles.Emit(30);

            // Desanexa para não ser destruído com o pai
            particles.transform.SetParent(null);
            
            // Destroi após as partículas terminarem
            var main = particles.main;
            main.loop = false;
            Destroy(particles.gameObject, main.startLifetime.constant + 0.5f);
        }

        // Flash de luz
        if (glowLight != null)
        {
            glowLight.intensity = glowIntensity * 3f;
            glowLight.transform.SetParent(null);
            Destroy(glowLight.gameObject, 0.3f);
        }
    }

    void OnDestroy()
    {
        // Limpa materiais criados
        if (essenceMaterial != null)
        {
            Destroy(essenceMaterial);
        }
    }
}
