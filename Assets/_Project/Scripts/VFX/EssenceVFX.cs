using UnityEngine;

/// <summary>
/// VFX para a Essência - Orbe Ethereal de Alma
/// Glow suave pulsante ao redor da esfera
/// </summary>
public class EssenceVFX : MonoBehaviour
{
    [Header("Cores da Alma")]
    public Color coreColor = new Color(0.4f, 0.9f, 1f, 1f); // Cyan claro
    public Color glowColor = new Color(0.2f, 0.6f, 0.9f, 0.6f); // Azul suave

    [Header("Glow")]
    public float glowIntensity = 2f;
    public float pulseSpeed = 1.5f;
    public float pulseAmount = 0.4f;
    public float glowRadius = 1.5f;

    private Light glowLight;
    private Material coreMaterial;
    private Material glowMaterial;
    private GameObject glowSphere;
    private float baseIntensity;
    private float baseScale;

    void Start()
    {
        SetupCoreMaterial();
        SetupGlowSphere();
        SetupGlowLight();
        baseScale = transform.localScale.x;
    }

    void Update()
    {
        float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
        float fastPulse = 1f + Mathf.Sin(Time.time * pulseSpeed * 2f) * (pulseAmount * 0.3f);

        // Pulso na luz
        if (glowLight != null)
        {
            glowLight.intensity = baseIntensity * pulse;
            glowLight.range = glowRadius * pulse;
        }

        // Pulso no glow sphere
        if (glowSphere != null)
        {
            float glowScale = glowRadius * pulse;
            glowSphere.transform.localScale = new Vector3(glowScale, glowScale, glowScale);

            // Fade do glow baseado no pulso
            if (glowMaterial != null)
            {
                float alpha = 0.15f + (pulse - 1f) * 0.3f;
                Color c = glowColor;
                glowMaterial.color = new Color(c.r, c.g, c.b, alpha);
            }
        }

        // Pulso na emissão do core
        if (coreMaterial != null)
        {
            float emission = 0.5f + fastPulse * 0.5f;
            coreMaterial.SetColor("_EmissionColor", coreColor * emission * glowIntensity);
        }
    }

    void SetupCoreMaterial()
    {
        Renderer rend = GetComponent<Renderer>();
        if (rend != null)
        {
            // Material brilhante para o core
            coreMaterial = new Material(Shader.Find("Standard"));
            coreMaterial.color = coreColor;
            coreMaterial.EnableKeyword("_EMISSION");
            coreMaterial.SetColor("_EmissionColor", coreColor * glowIntensity);
            
            // Leve transparência
            coreMaterial.SetFloat("_Mode", 3);
            coreMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            coreMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            coreMaterial.SetInt("_ZWrite", 1);
            coreMaterial.EnableKeyword("_ALPHABLEND_ON");
            coreMaterial.renderQueue = 3000;
            coreMaterial.color = new Color(coreColor.r, coreColor.g, coreColor.b, 0.9f);

            rend.material = coreMaterial;
        }
    }

    void SetupGlowSphere()
    {
        // Esfera maior e translúcida ao redor
        glowSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        glowSphere.name = "EssenceGlow";
        glowSphere.transform.SetParent(transform);
        glowSphere.transform.localPosition = Vector3.zero;
        glowSphere.transform.localScale = new Vector3(glowRadius, glowRadius, glowRadius);

        // Remove collider
        Collider col = glowSphere.GetComponent<Collider>();
        if (col != null) Destroy(col);

        // Material translúcido ethereal
        Renderer rend = glowSphere.GetComponent<Renderer>();
        if (rend != null)
        {
            glowMaterial = new Material(Shader.Find("Sprites/Default"));
            glowMaterial.color = new Color(glowColor.r, glowColor.g, glowColor.b, 0.2f);
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
        glowLight.range = glowRadius;
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
        trailComp.time = 0.35f;
        trailComp.startWidth = 0.25f;
        trailComp.endWidth = 0.02f;
        trailComp.material = new Material(Shader.Find("Sprites/Default"));
        trailComp.startColor = new Color(coreColor.r, coreColor.g, coreColor.b, 0.85f);
        trailComp.endColor = new Color(coreColor.r, coreColor.g, coreColor.b, 0.0f);
        trailComp.emitting = false;
    }

    public void StartFlyingTrail()
    {
        if (trailComp != null)
        {
            trailComp.emitting = true;
        }
    }

    /// <summary>
    /// Efeito de coleta - flash e desaparece
    /// </summary>
    public void PlayCollectEffect()
    {
        // Flash de luz no momento da coleta
        if (glowLight != null)
        {
            glowLight.intensity = glowIntensity * 5f;
            glowLight.transform.SetParent(null);
            Destroy(glowLight.gameObject, 0.25f);
        }

        // Expande e desaparece o glow
        if (glowSphere != null)
        {
            glowSphere.transform.SetParent(null);
            glowSphere.transform.localScale *= 2.2f;
            Destroy(glowSphere, 0.15f);
        }

        if (trailComp != null)
        {
            trailComp.transform.SetParent(null);
            Destroy(trailComp.gameObject, 0.35f);
        }
    }

    void OnDestroy()
    {
        if (coreMaterial != null) Destroy(coreMaterial);
        if (glowMaterial != null) Destroy(glowMaterial);
    }
}
