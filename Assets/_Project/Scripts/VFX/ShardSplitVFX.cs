using UnityEngine;

/// <summary>
/// VFX de Split do ShardSwarm — Explosão de fragmentos de cristal
/// Uso: Crie um GameObject vazio, adicione este script, salve como Prefab.
/// Arraste o prefab no campo "Split VFX" do ShardSwarm_AI.
/// Auto-destrói após executar.
/// </summary>
public class ShardSplitVFX : MonoBehaviour
{
    [Header("Configuração")]
    public Color primaryColor = new Color(0.4f, 0.8f, 1f, 1f);   // Ciano cristal
    public Color secondaryColor = new Color(0.7f, 0.3f, 1f, 1f);  // Roxo energia
    public float duration = 1.2f;
    public float radius = 2f;

    void Start()
    {
        CreateBurstParticles();
        CreateShockwave();
        CreateSparkles();

        Destroy(gameObject, duration + 0.5f);
    }

    void CreateBurstParticles()
    {
        GameObject burstObj = new GameObject("Burst");
        burstObj.transform.SetParent(transform);
        burstObj.transform.localPosition = Vector3.zero;

        ParticleSystem ps = burstObj.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.duration = 0.1f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 1.0f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 8f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.3f);
        main.startColor = new ParticleSystem.MinMaxGradient(primaryColor, secondaryColor);
        main.gravityModifier = 0.5f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 30;

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] {
            new ParticleSystem.Burst(0f, 20, 30)
        });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.3f;

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve(
            new Keyframe(0, 1),
            new Keyframe(0.7f, 0.5f),
            new Keyframe(1, 0)
        );
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(primaryColor, 0f),
                new GradientColorKey(secondaryColor, 0.5f),
                new GradientColorKey(Color.white, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0.8f, 0.5f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(grad);

        // Material simples
        var renderer = burstObj.GetComponent<ParticleSystemRenderer>();
        renderer.material = GetParticleMaterial();
    }

    void CreateShockwave()
    {
        GameObject waveObj = new GameObject("Shockwave");
        waveObj.transform.SetParent(transform);
        waveObj.transform.localPosition = Vector3.zero;

        ParticleSystem ps = waveObj.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.duration = 0.1f;
        main.loop = false;
        main.startLifetime = 0.4f;
        main.startSpeed = 0f;
        main.startSize = 0.5f;
        main.startColor = new Color(primaryColor.r, primaryColor.g, primaryColor.b, 0.5f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0, Mathf.PI * 2f);
        main.maxParticles = 1;

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] {
            new ParticleSystem.Burst(0f, 1)
        });

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve(
            new Keyframe(0, 0.5f),
            new Keyframe(1, radius * 3f)
        );
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(primaryColor, 0f), new GradientColorKey(primaryColor, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0.6f, 0f), new GradientAlphaKey(0f, 1f) }
        );
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(grad);

        var renderer = waveObj.GetComponent<ParticleSystemRenderer>();
        renderer.material = GetParticleMaterial();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
    }

    void CreateSparkles()
    {
        GameObject sparkObj = new GameObject("Sparkles");
        sparkObj.transform.SetParent(transform);
        sparkObj.transform.localPosition = Vector3.zero;

        ParticleSystem ps = sparkObj.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.duration = 0.3f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.8f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1f, 4f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.08f);
        main.startColor = Color.white;
        main.maxParticles = 15;

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] {
            new ParticleSystem.Burst(0.05f, 10, 15)
        });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.5f;

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(primaryColor, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
        );
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(grad);

        var renderer = sparkObj.GetComponent<ParticleSystemRenderer>();
        renderer.material = GetParticleMaterial();
    }

    Material GetParticleMaterial()
    {
        // Usa shader builtin de partículas
        Shader shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null) shader = Shader.Find("Legacy Shaders/Particles/Additive");
        if (shader == null) shader = Shader.Find("Sprites/Default");

        Material mat = new Material(shader);
        mat.SetFloat("_Mode", 1); // Additive
        mat.color = Color.white;
        return mat;
    }
}
