using UnityEngine;

/// <summary>
/// Sistema de VFX para a Ultimate do jogador.
/// Cria efeitos visuais procedurais sem necessidade de assets externos.
/// </summary>
public class UltimateVFX : MonoBehaviour
{
    [Header("VFX Settings")]
    [Tooltip("Cor principal do efeito")]
    public Color primaryColor = new Color(1f, 0.8f, 0f, 1f); // Dourado
    
    [Tooltip("Cor secundária do efeito")]
    public Color secondaryColor = new Color(1f, 0.3f, 0f, 1f); // Laranja
    
    [Tooltip("Intensidade do brilho")]
    [Range(0f, 5f)]
    public float glowIntensity = 2.0f;
    
    [Tooltip("Duração do efeito")]
    public float duration = 5f;
    
    private ParticleSystem auraParticles;
    private ParticleSystem burstParticles;
    private Light pointLight;
    private float timer = 0f;
    private bool isActive = false;
    
    void Start()
    {
        CreateAuraEffect();
        CreateBurstEffect();
        CreateLight();
        
        // Desativar no início
        if (auraParticles != null) auraParticles.Stop();
        if (burstParticles != null) burstParticles.Stop();
        if (pointLight != null) pointLight.enabled = false;
    }
    
    void Update()
    {
        if (isActive)
        {
            timer += Time.deltaTime;
            
            // Pulsar a luz
            if (pointLight != null)
            {
                float pulse = Mathf.Sin(Time.time * 5f) * 0.3f + 0.7f;
                pointLight.intensity = glowIntensity * pulse;
            }
            
            if (timer >= duration)
            {
                StopEffect();
            }
        }
    }
    
    /// <summary>
    /// Ativa o efeito visual da Ultimate.
    /// </summary>
    public void PlayEffect()
    {
        isActive = true;
        timer = 0f;
        
        if (auraParticles != null)
        {
            auraParticles.Play();
        }
        
        if (burstParticles != null)
        {
            burstParticles.Play();
        }
        
        if (pointLight != null)
        {
            pointLight.enabled = true;
        }
        
        Debug.Log("💥 Ultimate VFX ativado!");
    }
    
    /// <summary>
    /// Para o efeito visual.
    /// </summary>
    public void StopEffect()
    {
        isActive = false;
        
        if (auraParticles != null)
        {
            auraParticles.Stop();
        }
        
        if (burstParticles != null)
        {
            burstParticles.Stop();
        }
        
        if (pointLight != null)
        {
            pointLight.enabled = false;
        }
    }
    
    void CreateAuraEffect()
    {
        GameObject auraObj = new GameObject("UltimateAura");
        auraObj.transform.SetParent(transform);
        auraObj.transform.localPosition = Vector3.zero;
        
        auraParticles = auraObj.AddComponent<ParticleSystem>();
        var main = auraParticles.main;
        main.duration = duration;
        main.loop = true;
        main.startLifetime = 1.5f;
        main.startSpeed = 2f;
        main.startSize = 0.5f;
        main.startColor = new ParticleSystem.MinMaxGradient(primaryColor, secondaryColor);
        main.maxParticles = 50;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        
        // Emissão
        var emission = auraParticles.emission;
        emission.rateOverTime = 30f;
        
        // Shape - Esfera ao redor do player
        var shape = auraParticles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 1.5f;
        shape.radiusThickness = 0.5f;
        
        // Color over lifetime
        var colorOverLifetime = auraParticles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { 
                new GradientColorKey(primaryColor, 0f),
                new GradientColorKey(secondaryColor, 0.5f),
                new GradientColorKey(primaryColor, 1f)
            },
            new GradientAlphaKey[] { 
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0.8f, 0.5f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = gradient;
        
        // Size over lifetime
        var sizeOverLifetime = auraParticles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 0.3f);
        sizeCurve.AddKey(0.5f, 1f);
        sizeCurve.AddKey(1f, 0f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);
        
        // Renderer
        var renderer = auraObj.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = new Material(Shader.Find("Particles/Standard Unlit"));
        renderer.material.SetColor("_Color", primaryColor);
        renderer.material.EnableKeyword("_EMISSION");
        renderer.material.SetColor("_EmissionColor", primaryColor * glowIntensity);
    }
    
    void CreateBurstEffect()
    {
        GameObject burstObj = new GameObject("UltimateBurst");
        burstObj.transform.SetParent(transform);
        burstObj.transform.localPosition = Vector3.zero;
        
        burstParticles = burstObj.AddComponent<ParticleSystem>();
        var main = burstParticles.main;
        main.duration = 1f;
        main.loop = false;
        main.startLifetime = 0.8f;
        main.startSpeed = 8f;
        main.startSize = 0.8f;
        main.startColor = primaryColor;
        main.maxParticles = 100;
        
        // Emissão em burst
        var emission = burstParticles.emission;
        emission.rateOverTime = 0f;
        ParticleSystem.Burst burst = new ParticleSystem.Burst(0f, 50);
        emission.SetBurst(0, burst);
        
        // Shape - Explosão esférica
        var shape = burstParticles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.1f;
        
        // Color over lifetime
        var colorOverLifetime = burstParticles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { 
                new GradientColorKey(primaryColor, 0f),
                new GradientColorKey(secondaryColor, 1f)
            },
            new GradientAlphaKey[] { 
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = gradient;
        
        // Size over lifetime
        var sizeOverLifetime = burstParticles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 1f);
        sizeCurve.AddKey(1f, 0f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);
        
        // Renderer
        var renderer = burstObj.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = new Material(Shader.Find("Particles/Standard Unlit"));
        renderer.material.SetColor("_Color", primaryColor);
        renderer.material.EnableKeyword("_EMISSION");
        renderer.material.SetColor("_EmissionColor", primaryColor * glowIntensity * 2f);
    }
    
    void CreateLight()
    {
        GameObject lightObj = new GameObject("UltimateLight");
        lightObj.transform.SetParent(transform);
        lightObj.transform.localPosition = Vector3.up;
        
        pointLight = lightObj.AddComponent<Light>();
        pointLight.type = LightType.Point;
        pointLight.color = primaryColor;
        pointLight.intensity = glowIntensity;
        pointLight.range = 10f;
        pointLight.shadows = LightShadows.None;
        pointLight.enabled = false;
    }
}
