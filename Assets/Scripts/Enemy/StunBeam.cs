using UnityEngine;

/// <summary>
/// Área de efeito de stun - Usado pelo Golem
/// Aplica stun ao jogador que estiver dentro da área
/// </summary>
public class StunBeam : MonoBehaviour
{
    [Header("Configurações")]
    public float radius = 4f;
    public float stunDuration = 1.5f;

    [Header("VFX Placeholder")]
    public Color beamColor = Color.cyan;
    private bool hasAppliedStun = false;

    /// <summary>
    /// Inicializa o beam com valores do Golem
    /// </summary>
    public void Initialize(float newRadius, float newStunDuration)
    {
        radius = newRadius;
        stunDuration = newStunDuration;
        
        // Aplica o stun imediatamente
        ApplyStunToPlayersInArea();
    }

    void Start()
    {
        // Se não foi inicializado externamente, aplica com valores padrão
        if (!hasAppliedStun)
        {
            ApplyStunToPlayersInArea();
        }

        // Visual placeholder: cria um efeito simples
        CreatePlaceholderVisual();
    }

    void ApplyStunToPlayersInArea()
    {
        hasAppliedStun = true;

        Collider[] hits = Physics.OverlapSphere(transform.position, radius);
        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                PlayerHealth playerHealth = hit.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    playerHealth.ApplyStun(stunDuration);
                    Debug.Log("StunBeam aplicou stun no player por " + stunDuration + "s!");
                }
            }
        }
    }

    void CreatePlaceholderVisual()
    {
        // Cria um cilindro visual como placeholder
        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        visual.transform.SetParent(transform);
        visual.transform.localPosition = Vector3.up * 2.5f;
        visual.transform.localScale = new Vector3(radius * 2, 5f, radius * 2);

        // Remove o collider do visual (apenas visual)
        Collider visualCollider = visual.GetComponent<Collider>();
        if (visualCollider != null) Destroy(visualCollider);

        // Configura material translúcido
        Renderer rend = visual.GetComponent<Renderer>();
        if (rend != null)
        {
            Material mat = new Material(Shader.Find("Sprites/Default"));
            mat.color = new Color(beamColor.r, beamColor.g, beamColor.b, 0.4f);
            rend.material = mat;
        }

        // Autodestruction
        Destroy(visual, 0.5f);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
