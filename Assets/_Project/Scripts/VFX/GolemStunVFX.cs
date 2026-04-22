using UnityEngine;

/// <summary>
/// VFX para o Stun do Golem
/// Cria um efeito visual de onda de choque com partículas e luz
/// </summary>
public class GolemStunVFX : MonoBehaviour
{
    [Header("Cores")]
    public Color stunColor = new Color(0.2f, 0.8f, 1f, 1f); // Cyan
    public Color flashColor = new Color(1f, 1f, 1f, 1f); // Branco

    [Header("Ring Shockwave")]
    public float expandSpeed = 15f;
    public float maxRadius = 5f;
    public float ringWidth = 0.5f;

    [Header("Pillar Beam")]
    public float pillarHeight = 8f;
    public float pillarDuration = 0.4f;

    [Header("Partículas")]
    public int particleCount = 30;

    private ParticleSystem particles;
    private Light flashLight;
    private GameObject ringObject;
    private GameObject pillarObject;
    private float currentRadius = 0f;
    private float lifetime = 0f;

    void Start()
    {
        CreateFlashLight();
        CreateRingShockwave();
        CreatePillarBeam();
        CreateParticles();
    }

    void Update()
    {
        lifetime += Time.deltaTime;

        // Expande o ring
        if (ringObject != null && currentRadius < maxRadius)
        {
            currentRadius += expandSpeed * Time.deltaTime;
            ringObject.transform.localScale = new Vector3(currentRadius * 2, 0.1f, currentRadius * 2);

            // Fade out
            Renderer rend = ringObject.GetComponent<Renderer>();
            if (rend != null)
            {
                float alpha = 1f - (currentRadius / maxRadius);
                Color c = rend.material.color;
                rend.material.color = new Color(c.r, c.g, c.b, alpha * 0.6f);
            }
        }

        // Fade do flash
        if (flashLight != null)
        {
            flashLight.intensity = Mathf.Lerp(flashLight.intensity, 0, Time.deltaTime * 8f);
        }

        // Encolhe o pillar
        if (pillarObject != null)
        {
            float t = lifetime / pillarDuration;
            if (t < 1f)
            {
                float scaleY = Mathf.Lerp(pillarHeight, 0, t);
                pillarObject.transform.localScale = new Vector3(maxRadius * 0.3f, scaleY, maxRadius * 0.3f);
            }
            else
            {
                Destroy(pillarObject);
            }
        }

        // Autodestruction
        if (lifetime > 1f)
        {
            Destroy(gameObject);
        }
    }

    void CreateFlashLight()
    {
        GameObject lightObj = new GameObject("StunFlash");
        lightObj.transform.SetParent(transform);
        lightObj.transform.localPosition = Vector3.up * 2f;

        flashLight = lightObj.AddComponent<Light>();
        flashLight.type = LightType.Point;
        flashLight.color = stunColor;
        flashLight.intensity = 8f;
        flashLight.range = maxRadius * 2f;
        flashLight.shadows = LightShadows.None;
    }

    void CreateRingShockwave()
    {
        // Cria cilindro achatado para o ring
        ringObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ringObject.name = "StunRing";
        ringObject.transform.SetParent(transform);
        ringObject.transform.localPosition = Vector3.up * 0.1f;
        ringObject.transform.localScale = new Vector3(0, 0.1f, 0);

        // Remove collider
        Collider col = ringObject.GetComponent<Collider>();
        if (col != null) Destroy(col);

        // Material translúcido
        Renderer rend = ringObject.GetComponent<Renderer>();
        if (rend != null)
        {
            Material mat = new Material(Shader.Find("Sprites/Default"));
            mat.color = new Color(stunColor.r, stunColor.g, stunColor.b, 0.6f);
            rend.material = mat;
        }

        Destroy(ringObject, 0.8f);
    }

    void CreatePillarBeam()
    {
        // Cilindro vertical (beam de luz)
        pillarObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pillarObject.name = "StunPillar";
        pillarObject.transform.SetParent(transform);
        pillarObject.transform.localPosition = Vector3.up * (pillarHeight / 2f);
        pillarObject.transform.localScale = new Vector3(maxRadius * 0.3f, pillarHeight, maxRadius * 0.3f);

        // Remove collider
        Collider col = pillarObject.GetComponent<Collider>();
        if (col != null) Destroy(col);

        // Material brilhante
        Renderer rend = pillarObject.GetComponent<Renderer>();
        if (rend != null)
        {
            Material mat = new Material(Shader.Find("Sprites/Default"));
            mat.color = new Color(stunColor.r, stunColor.g, stunColor.b, 0.5f);
            rend.material = mat;
        }
    }

    void CreateParticles()
    {
        GameObject particleObj = new GameObject("StunParticles");
        particleObj.transform.SetParent(transform);
        particleObj.transform.localPosition = Vector3.zero;

        particles = particleObj.AddComponent<ParticleSystem>();

        // Config principal
        var main = particles.main;
        main.startColor = stunColor;
        main.startSize = 0.2f;
        main.startSpeed = 8f;
        main.startLifetime = 0.5f;
        main.maxParticles = particleCount;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.loop = false;

        // Emissão burst
        var emission = particles.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] {
            new ParticleSystem.Burst(0f, particleCount)
        });

        // Forma (esfera expandindo)
        var shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.5f;

        // Velocidade radial (para fora)
        var velocityOverLifetime = particles.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.radial = 5f;

        // Cor fade
        var colorOverLifetime = particles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(flashColor, 0f),
                new GradientColorKey(stunColor, 0.3f),
                new GradientColorKey(stunColor, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 0.5f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = gradient;

        // Material
        var particleRenderer = particleObj.GetComponent<ParticleSystemRenderer>();
        particleRenderer.material = new Material(Shader.Find("Particles/Standard Unlit"));
        particleRenderer.material.color = stunColor;

        Destroy(particleObj, 1f);
    }

    /// <summary>
    /// Configura o raio do VFX para combinar com o stun
    /// </summary>
    public void SetRadius(float radius)
    {
        maxRadius = radius;
    }
}
