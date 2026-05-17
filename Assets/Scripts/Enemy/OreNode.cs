using UnityEngine;

/// <summary>
/// Nó de minério fixo no mapa.
/// O Geobionte busca esses objetos para se fundir e se transformar no Bismutado.
/// Coloque este script em um GameObject com um Collider (para detecção de proximidade).
/// </summary>
public class OreNode : MonoBehaviour
{
    [Header("Configuração")]
    [Tooltip("Nome do minério (para identificação e expansão futura)")]
    public string oreName = "Crystal";

    [Tooltip("Se true, este minério já foi consumido por um Geobionte")]
    [HideInInspector] public bool isConsumed = false;

    [Header("Visual Placeholder")]
    [Tooltip("Cor do brilho emissivo do minério")]
    public Color emissionColor = new Color(0.5f, 0.8f, 1f, 1f); // Azul cristalino
    [Tooltip("Intensidade do brilho")]
    public float glowIntensity = 2f;

    private Renderer oreRenderer;
    private Material oreMaterial;

    void Start()
    {
        SetupVisual();
    }

    /// <summary>
    /// Configura o visual placeholder do minério (esfera brilhante).
    /// </summary>
    void SetupVisual()
    {
        oreRenderer = GetComponentInChildren<Renderer>();
        if (oreRenderer != null)
        {
            // Cria material emissivo para o minério
            // URP usa "Universal Render Pipeline/Lit", fallback para "Standard"
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");

            oreMaterial = new Material(shader);
            oreMaterial.color = emissionColor;
            oreMaterial.EnableKeyword("_EMISSION");
            oreMaterial.SetColor("_EmissionColor", emissionColor * glowIntensity);

            // URP usa _BaseColor ao invés de _Color
            if (oreMaterial.HasProperty("_BaseColor"))
                oreMaterial.SetColor("_BaseColor", emissionColor);

            oreRenderer.material = oreMaterial;
        }
    }

    /// <summary>
    /// Chamado pelo Geobionte quando ele absorve este minério.
    /// Marca como consumido e desativa visualmente.
    /// </summary>
    public void Consume()
    {
        if (isConsumed) return;

        isConsumed = true;
        Debug.Log("[ORE] " + oreName + " consumido por um Geobionte!");

        // Desativa o objeto visual (mas mantém o GameObject para referência)
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Verifica se este minério está disponível para fusão.
    /// </summary>
    public bool IsAvailable()
    {
        return !isConsumed && gameObject.activeSelf;
    }

    void OnDestroy()
    {
        if (oreMaterial != null) Destroy(oreMaterial);
    }

    // Gizmo para visualização no Editor
    void OnDrawGizmos()
    {
        Gizmos.color = isConsumed ? Color.gray : new Color(0.5f, 0.8f, 1f, 0.5f);
        Gizmos.DrawSphere(transform.position, 0.3f);

        if (!isConsumed)
        {
            Gizmos.color = new Color(0.5f, 0.8f, 1f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, 0.5f);
        }
    }
}
