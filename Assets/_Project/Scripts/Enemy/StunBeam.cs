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
        // Cria o VFX elaborado do stun
        GameObject vfxObj = new GameObject("GolemStunVFX");
        vfxObj.transform.position = transform.position;
        
        GolemStunVFX vfx = vfxObj.AddComponent<GolemStunVFX>();
        vfx.SetRadius(radius);
        vfx.stunColor = beamColor;
        
        // Autodestruction do StunBeam (VFX se destrói sozinho)
        Destroy(gameObject, 0.1f);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
