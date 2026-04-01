using UnityEngine;
using System.Collections;

/// <summary>
/// Sistema de VFX PREMIUM para a Ultimate do jogador.
/// Múltiplas camadas de efeitos visuais procedurais de alta qualidade.
/// </summary>
public class UltimateVFXPremium : MonoBehaviour
{
    [Header("VFX Colors")]
    public Color primaryColor = new Color(0.2f, 0.8f, 1f, 1f); // Azul elétrico
    public Color secondaryColor = new Color(1f, 0.4f, 0f, 1f); // Laranja energia
    public Color accentColor = new Color(1f, 1f, 0.3f, 1f); // Amarelo brilhante
    
    [Header("VFX Intensity")]
    [Range(1f, 10f)]
    public float glowIntensity = 5.0f;
    
    [Range(0.5f, 3f)]
    public float particleScale = 1.0f;
    
    public float duration = 5f;
    
    [Header("Screen Effects")]
    public bool enableScreenShake = true;
    public float shakeIntensity = 0.3f;
    
    // Particle Systems
    private ParticleSystem spiralParticles;
    private ParticleSystem energyRings;
    private ParticleSystem burstExplosion;
    private ParticleSystem groundImpact;
    private ParticleSystem lightningBolts;
    private ParticleSystem floatingOrbs;
    
    // Lights
    private Light mainLight;
    private Light pulseLight;
    
    // Estado
    private float timer = 0f;
    private bool isActive = false;
    private Camera mainCamera;
    
    void Start()
    {
        mainCamera = Camera.main;
        
        CreateSpiralParticles();
        CreateEnergyRings();
        CreateBurstExplosion();
        CreateGroundImpact();
        CreateLightningBolts();
        CreateFloatingOrbs();
        CreateLights();
        
        StopAllEffects();
    }
    
    void Update()
    {
        if (isActive)
        {
            timer += Time.deltaTime;
            
            // Animar luzes
            AnimateLights();
            
            if (timer >= duration)
            {
                StopEffect();
            }
        }
    }
    
    public void PlayEffect()
    {
        isActive = true;
        timer = 0f;
        
        // Ativar todos os sistemas
        if (spiralParticles != null) spiralParticles.Play();
        if (energyRings != null) energyRings.Play();
        if (burstExplosion != null) burstExplosion.Play();
        if (groundImpact != null) groundImpact.Play();
        if (lightningBolts != null) lightningBolts.Play();
        if (floatingOrbs != null) floatingOrbs.Play();
        
        if (mainLight != null) mainLight.enabled = true;
        if (pulseLight != null) pulseLight.enabled = true;
        
        // Screen shake
        if (enableScreenShake && mainCamera != null)
        {
            StartCoroutine(ScreenShake());
        }
        
        Debug.Log("💥 Ultimate VFX PREMIUM ativado!");
    }
    
    public void StopEffect()
    {
        isActive = false;
        StopAllEffects();
    }
    
    void StopAllEffects()
    {
        if (spiralParticles != null) spiralParticles.Stop();
        if (energyRings != null) energyRings.Stop();
        if (burstExplosion != null) burstExplosion.Stop();
        if (groundImpact != null) groundImpact.Stop();
        if (lightningBolts != null) lightningBolts.Stop();
        if (floatingOrbs != null) floatingOrbs.Stop();
        
        if (mainLight != null) mainLight.enabled = false;
        if (pulseLight != null) pulseLight.enabled = false;
    }
    
    void AnimateLights()
    {
        float time = Time.time;
        
        if (mainLight != null)
        {
            // Pulso rápido
            float pulse = Mathf.Sin(time * 8f) * 0.4f + 0.6f;
            mainLight.intensity = glowIntensity * pulse;
            
            // Mudança de cor sutil
            mainLight.color = Color.Lerp(primaryColor, accentColor, Mathf.Sin(time * 3f) * 0.5f + 0.5f);
        }
        
        if (pulseLight != null)
        {
            // Pulso lento e suave
            float pulse = Mathf.Sin(time * 4f) * 0.5f + 0.5f;
            pulseLight.intensity = glowIntensity * 0.7f * pulse;
        }
    }
    
    IEnumerator ScreenShake()
    {
        Vector3 originalPos = mainCamera.transform.localPosition;
        float elapsed = 0f;
        float shakeDuration = 0.3f;
        
        while (elapsed < shakeDuration)
        {
            float x = Random.Range(-1f, 1f) * shakeIntensity;
            float y = Random.Range(-1f, 1f) * shakeIntensity;
            
            mainCamera.transform.localPosition = originalPos + new Vector3(x, y, 0);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        mainCamera.transform.localPosition = originalPos;
    }
    
    void CreateSpiralParticles()
    {
        GameObject obj = new GameObject("SpiralParticles");
        obj.transform.SetParent(transform);
        obj.transform.localPosition = Vector3.zero;
        
        spiralParticles = obj.AddComponent<ParticleSystem>();
        spiralParticles.Stop(true); // Para antes de configurar para evitar erro de duration
        var main = spiralParticles.main;
        main.duration = duration;
        main.loop = true;
        main.startLifetime = 2.5f;
        main.startSpeed = 0f;
        main.startSize = 0.4f * particleScale;
        main.startColor = new ParticleSystem.MinMaxGradient(primaryColor, accentColor);
        main.maxParticles = 100;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        
        var emission = spiralParticles.emission;
        emission.rateOverTime = 40f;
        
        var shape = spiralParticles.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.1f;
        
        // Velocidade ao longo do tempo (espiral)
        var velocityOverLifetime = spiralParticles.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.space = ParticleSystemSimulationSpace.Local;
        velocityOverLifetime.orbitalY = 10f;
        velocityOverLifetime.radial = 3f;
        velocityOverLifetime.speedModifier = 2f;
        
        var colorOverLifetime = spiralParticles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { 
                new GradientColorKey(primaryColor, 0f),
                new GradientColorKey(accentColor, 0.5f),
                new GradientColorKey(secondaryColor, 1f)
            },
            new GradientAlphaKey[] { 
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(1f, 0.2f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = gradient;
        
        var sizeOverLifetime = spiralParticles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 0f);
        sizeCurve.AddKey(0.3f, 1.5f);
        sizeCurve.AddKey(1f, 0f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);
        
        SetupRenderer(obj, primaryColor);
    }
    
    void CreateEnergyRings()
    {
        GameObject obj = new GameObject("EnergyRings");
        obj.transform.SetParent(transform);
        obj.transform.localPosition = Vector3.zero;
        
        energyRings = obj.AddComponent<ParticleSystem>();
        energyRings.Stop(true);
        var main = energyRings.main;
        main.duration = 1f;
        main.loop = true;
        main.startLifetime = 1.5f;
        main.startSpeed = 0f;
        main.startSize = 0.3f * particleScale;
        main.startColor = accentColor;
        main.maxParticles = 200;
        
        var emission = energyRings.emission;
        emission.rateOverTime = 0f;
        emission.SetBurst(0, new ParticleSystem.Burst(0f, 30));
        
        var shape = energyRings.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.5f;
        shape.radiusThickness = 0f;
        
        var velocityOverLifetime = energyRings.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.radial = 8f;
        
        var sizeOverLifetime = energyRings.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 0.5f);
        sizeCurve.AddKey(0.5f, 2f);
        sizeCurve.AddKey(1f, 0f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);
        
        var colorOverLifetime = energyRings.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { 
                new GradientColorKey(accentColor, 0f),
                new GradientColorKey(primaryColor, 1f)
            },
            new GradientAlphaKey[] { 
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = gradient;
        
        SetupRenderer(obj, accentColor);
    }
    
    void CreateBurstExplosion()
    {
        GameObject obj = new GameObject("BurstExplosion");
        obj.transform.SetParent(transform);
        obj.transform.localPosition = Vector3.zero;
        
        burstExplosion = obj.AddComponent<ParticleSystem>();
        burstExplosion.Stop(true);
        var main = burstExplosion.main;
        main.duration = 1f;
        main.loop = false;
        main.startLifetime = 1.2f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(10f, 20f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.5f * particleScale, 1.2f * particleScale);
        main.startColor = new ParticleSystem.MinMaxGradient(primaryColor, secondaryColor);
        main.maxParticles = 150;
        
        var emission = burstExplosion.emission;
        emission.rateOverTime = 0f;
        emission.SetBurst(0, new ParticleSystem.Burst(0f, 100));
        
        var shape = burstExplosion.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.2f;
        
        var colorOverLifetime = burstExplosion.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { 
                new GradientColorKey(accentColor, 0f),
                new GradientColorKey(primaryColor, 0.5f),
                new GradientColorKey(secondaryColor, 1f)
            },
            new GradientAlphaKey[] { 
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0.6f, 0.5f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = gradient;
        
        var sizeOverLifetime = burstExplosion.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 1f);
        sizeCurve.AddKey(1f, 0f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);
        
        SetupRenderer(obj, primaryColor);
    }
    
    void CreateGroundImpact()
    {
        GameObject obj = new GameObject("GroundImpact");
        obj.transform.SetParent(transform);
        obj.transform.localPosition = new Vector3(0, -0.5f, 0);
        
        groundImpact = obj.AddComponent<ParticleSystem>();
        groundImpact.Stop(true);
        var main = groundImpact.main;
        main.duration = 1f;
        main.loop = false;
        main.startLifetime = 0.8f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(5f, 10f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.3f * particleScale, 0.8f * particleScale);
        main.startColor = new ParticleSystem.MinMaxGradient(secondaryColor, accentColor);
        main.maxParticles = 50;
        
        var emission = groundImpact.emission;
        emission.rateOverTime = 0f;
        emission.SetBurst(0, new ParticleSystem.Burst(0f, 40));
        
        var shape = groundImpact.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 1f;
        shape.radiusThickness = 0f;
        
        var velocityOverLifetime = groundImpact.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.y = -5f;
        
        var colorOverLifetime = groundImpact.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { 
                new GradientColorKey(accentColor, 0f),
                new GradientColorKey(secondaryColor, 1f)
            },
            new GradientAlphaKey[] { 
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = gradient;
        
        SetupRenderer(obj, secondaryColor);
    }
    
    void CreateLightningBolts()
    {
        GameObject obj = new GameObject("LightningBolts");
        obj.transform.SetParent(transform);
        obj.transform.localPosition = Vector3.up * 2f;
        
        lightningBolts = obj.AddComponent<ParticleSystem>();
        lightningBolts.Stop(true);
        var main = lightningBolts.main;
        main.duration = duration;
        main.loop = true;
        main.startLifetime = 0.3f;
        main.startSpeed = 15f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.1f * particleScale, 0.3f * particleScale);
        main.startColor = accentColor;
        main.maxParticles = 50;
        
        var emission = lightningBolts.emission;
        emission.rateOverTime = 20f;
        
        var shape = lightningBolts.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 25f;
        shape.radius = 0.1f;
        
        var colorOverLifetime = lightningBolts.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { 
                new GradientColorKey(accentColor, 0f),
                new GradientColorKey(primaryColor, 1f)
            },
            new GradientAlphaKey[] { 
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = gradient;
        
        SetupRenderer(obj, accentColor);
    }
    
    void CreateFloatingOrbs()
    {
        GameObject obj = new GameObject("FloatingOrbs");
        obj.transform.SetParent(transform);
        obj.transform.localPosition = Vector3.zero;
        
        floatingOrbs = obj.AddComponent<ParticleSystem>();
        floatingOrbs.Stop(true);
        var main = floatingOrbs.main;
        main.duration = duration;
        main.loop = true;
        main.startLifetime = 3f;
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.3f * particleScale, 0.6f * particleScale);
        main.startColor = new ParticleSystem.MinMaxGradient(primaryColor, accentColor);
        main.maxParticles = 30;
        
        var emission = floatingOrbs.emission;
        emission.rateOverTime = 10f;
        
        var shape = floatingOrbs.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 2f;
        
        var velocityOverLifetime = floatingOrbs.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.y = 1f;
        velocityOverLifetime.orbitalY = 2f;
        
        var colorOverLifetime = floatingOrbs.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { 
                new GradientColorKey(primaryColor, 0f),
                new GradientColorKey(accentColor, 0.5f),
                new GradientColorKey(primaryColor, 1f)
            },
            new GradientAlphaKey[] { 
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(1f, 0.3f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = gradient;
        
        var sizeOverLifetime = floatingOrbs.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 0f);
        sizeCurve.AddKey(0.5f, 1f);
        sizeCurve.AddKey(1f, 0f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);
        
        SetupRenderer(obj, primaryColor);
    }
    
    void CreateLights()
    {
        // Luz principal
        GameObject lightObj1 = new GameObject("MainLight");
        lightObj1.transform.SetParent(transform);
        lightObj1.transform.localPosition = Vector3.up;
        
        mainLight = lightObj1.AddComponent<Light>();
        mainLight.type = LightType.Point;
        mainLight.color = primaryColor;
        mainLight.intensity = glowIntensity;
        mainLight.range = 15f;
        mainLight.shadows = LightShadows.None;
        
        // Luz de pulso
        GameObject lightObj2 = new GameObject("PulseLight");
        lightObj2.transform.SetParent(transform);
        lightObj2.transform.localPosition = Vector3.zero;
        
        pulseLight = lightObj2.AddComponent<Light>();
        pulseLight.type = LightType.Point;
        pulseLight.color = accentColor;
        pulseLight.intensity = glowIntensity * 0.7f;
        pulseLight.range = 20f;
        pulseLight.shadows = LightShadows.None;
    }
    
    void SetupRenderer(GameObject obj, Color color)
    {
        var renderer = obj.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = new Material(Shader.Find("Particles/Standard Unlit"));
        renderer.material.SetColor("_Color", color);
        renderer.material.EnableKeyword("_EMISSION");
        renderer.material.SetColor("_EmissionColor", color * glowIntensity);
    }
}
