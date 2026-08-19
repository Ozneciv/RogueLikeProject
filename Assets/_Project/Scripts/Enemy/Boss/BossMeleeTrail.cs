using UnityEngine;

/// <summary>
/// Script de Trail Visual (Rastro de Lâmina/Garra) para os ataques do Boss.
/// Adiciona e gerencia dinamicamente um TrailRenderer suave, etéreo e semi-transparente.
/// </summary>
public class BossMeleeTrail : MonoBehaviour
{
    public TrailRenderer trailRenderer;
    public Color corInicial = new Color(0f, 1f, 0.85f, 0.40f); // Ciano Suave Semi-Transparente
    public Color corFinal = new Color(0.7f, 0.1f, 1f, 0.0f);    // Magenta Ethereal Fade
    public float tempoVidaTrail = 0.30f;
    public float larguraInicial = 0.45f;

    private void Awake()
    {
        GarantirTrailRenderer();
    }

    private void OnEnable()
    {
        DesativarTrail();
    }

    private void Start()
    {
        DesativarTrail();
    }

    private void GarantirTrailRenderer()
    {
        if (trailRenderer == null)
            trailRenderer = GetComponent<TrailRenderer>();

        if (trailRenderer == null)
        {
            trailRenderer = gameObject.AddComponent<TrailRenderer>();
            trailRenderer.time = tempoVidaTrail;
            trailRenderer.startWidth = larguraInicial;
            trailRenderer.endWidth = 0.02f;
            trailRenderer.minVertexDistance = 0.05f;
            trailRenderer.autodestruct = false;

            // Material Unlit/Default com emissão e suporte a transparência
            Material trailMat = new Material(Shader.Find("Sprites/Default"));
            trailRenderer.material = trailMat;

            // Gradiente Etnéreo Suave e Transparente
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { new GradientColorKey(corInicial, 0.0f), new GradientColorKey(corFinal, 1.0f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0.40f, 0.0f), new GradientAlphaKey(0.0f, 1.0f) }
            );
            trailRenderer.colorGradient = gradient;
        }

        trailRenderer.emitting = false; // Começa desligado por padrão
    }

    public void AtivarTrail(float duracao)
    {
        if (trailRenderer == null) GarantirTrailRenderer();
        if (trailRenderer != null)
        {
            trailRenderer.Clear();
            trailRenderer.emitting = true;
            CancelInvoke(nameof(DesativarTrail));
            Invoke(nameof(DesativarTrail), duracao);
        }
    }

    public void DesativarTrail()
    {
        if (trailRenderer != null)
        {
            trailRenderer.emitting = false;
            trailRenderer.Clear();
        }
    }
}
