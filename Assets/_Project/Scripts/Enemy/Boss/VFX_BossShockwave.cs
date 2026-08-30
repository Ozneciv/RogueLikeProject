using UnityEngine;

/// <summary>
/// Componente de Efeito Visual (VFX) de Onda de Choque de Impacto no Solo.
/// Usado para os ataques Pisada (Stomp) e Salto Esmagador (Jump Attack) do Boss.
/// Expande dinamicamente anéis de energia cristalina com brilho HDR e desvanece suavemente.
/// </summary>
public class VFX_BossShockwave : MonoBehaviour
{
    [Header("Configurações do Anel de Impacto")]
    public float raioMaximo = 6.0f;
    public float duracaoExpansao = 0.4f;
    public float tempoVidaTotal = 1.2f;
    public Color corInicial = new Color(0f, 0.9f, 1f, 0.9f); // Ciano Cristalino HDR
    public Color corFinal = new Color(0.8f, 0.1f, 1f, 0.0f);   // Magenta Neon Fade

    private LineRenderer lineRenderer;
    private Light impactLight;
    private float elapsed = 0f;

    /// <summary>
    /// Gera o VFX de Onda de Choque proceduralmente no mundo sem depender de prefabs quebrados!
    /// </summary>
    public static GameObject CriarEfeitoOndaDeChoque(Vector3 pos, float raioMax, Color corInic, Color corFin, float duracao = 0.4f, float tempoVida = 1.2f)
    {
        GameObject go = new GameObject("VFX_Shockwave_Dynamic");
        go.transform.position = pos;

        VFX_BossShockwave shock = go.AddComponent<VFX_BossShockwave>();
        shock.raioMaximo = raioMax;
        shock.duracaoExpansao = duracao;
        shock.tempoVidaTotal = tempoVida;
        shock.corInicial = corInic;
        shock.corFinal = corFin;

        return go;
    }

    void Awake()
    {
        GarantirLineRenderer();
    }

    void Start()
    {
        Destroy(gameObject, tempoVidaTotal);
    }

    private void GarantirLineRenderer()
    {
        if (lineRenderer == null)
            lineRenderer = GetComponent<LineRenderer>();

        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
            lineRenderer.positionCount = 36;
            lineRenderer.useWorldSpace = true;
            lineRenderer.startWidth = 0.35f;
            lineRenderer.endWidth = 0.35f;

            Material mat = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.material = mat;
        }

        if (impactLight == null)
        {
            impactLight = GetComponentInChildren<Light>();
            if (impactLight == null)
            {
                GameObject lightObj = new GameObject("ImpactLight");
                lightObj.transform.SetParent(transform, false);
                impactLight = lightObj.AddComponent<Light>();
                impactLight.type = LightType.Point;
                impactLight.color = corInicial;
                impactLight.range = 8.0f;
                impactLight.intensity = 5.0f;
            }
        }
    }

    void Update()
    {
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / duracaoExpansao);

        float curve = Mathf.Sin(t * Mathf.PI * 0.5f);
        float raioAtual = Mathf.Lerp(0.5f, raioMaximo, curve);

        if (lineRenderer != null)
        {
            float width = Mathf.Lerp(0.5f, 0.05f, t);
            lineRenderer.startWidth = width;
            lineRenderer.endWidth = width;

            Color colorCurrent = Color.Lerp(corInicial, corFinal, t);
            lineRenderer.startColor = colorCurrent;
            lineRenderer.endColor = colorCurrent;

            for (int i = 0; i < 36; i++)
            {
                float angle = (i / 35f) * Mathf.PI * 2f;
                Vector3 pos = transform.position + new Vector3(Mathf.Cos(angle) * raioAtual, 0.05f, Mathf.Sin(angle) * raioAtual);
                lineRenderer.SetPosition(i, pos);
            }
        }

        if (impactLight != null)
        {
            impactLight.intensity = Mathf.Lerp(6.0f, 0f, t);
        }
    }
}
