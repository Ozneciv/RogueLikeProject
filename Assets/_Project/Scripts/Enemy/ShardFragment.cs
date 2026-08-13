using UnityEngine;

/// <summary>
/// Script para cada fragmento individual do Shard Swarm
/// Detecta colisões e passa o dano para o DummyHealth do controlador principal
/// </summary>
public class ShardFragment : MonoBehaviour
{
    [Header("Referência")]
    [Tooltip("Referência ao DummyHealth do Swarm (preenchido automaticamente se for filho)")]
    public DummyHealth swarmHealth;

    [Header("Visual")]
    public TrailRenderer trail;
    public Color normalColor = Color.cyan;
    public Color hitColor = Color.white;
    public float hitFlashDuration = 0.1f;

    private Renderer fragmentRenderer;
    private Color originalColor;

    void Start()
    {
        // Tenta encontrar o DummyHealth no pai
        if (swarmHealth == null)
        {
            swarmHealth = GetComponentInParent<DummyHealth>();
        }

        // Pega o renderer para efeito de flash
        fragmentRenderer = GetComponent<Renderer>();
        if (fragmentRenderer != null)
        {
            originalColor = fragmentRenderer.material.color;
        }

        // Configura trail se existir
        if (trail == null)
        {
            trail = GetComponent<TrailRenderer>();
        }
    }

    /// <summary>
    /// Chamado quando o fragmento recebe dano (pelo sistema de armas do player)
    /// </summary>
    public void TakeDamage(int damage)
    {
        if (swarmHealth != null)
        {
            swarmHealth.TakeDamage(damage);
            StartCoroutine(FlashHit());
            Debug.Log("[SHARD FRAGMENT] Dano recebido: " + damage);
        }
        else
        {
            Debug.LogWarning("[SHARD FRAGMENT] Nenhum DummyHealth encontrado no pai!");
        }
    }

    System.Collections.IEnumerator FlashHit()
    {
        if (fragmentRenderer != null)
        {
            fragmentRenderer.material.color = hitColor;
            yield return new WaitForSeconds(hitFlashDuration);
            if (fragmentRenderer != null && gameObject.activeSelf)
            {
                fragmentRenderer.material.color = originalColor;
            }
        }
    }

    // Para compatibilidade com o sistema de dano existente (WeaponHitbox)
    void OnTriggerEnter(Collider other)
    {
        // Se for atingido por hitbox de arma do player (checa layer ou nome com segurança sem estourar exceção de tag inexistente)
        int playerAttackLayer = LayerMask.NameToLayer("PlayerAttack");
        if ((playerAttackLayer != -1 && other.gameObject.layer == playerAttackLayer) || other.name.Contains("Weapon") || other.name.Contains("Attack"))
        {
            // O dano será registrado pelo WeaponHitbox através do DummyHealth
            // Este trigger é apenas backup
        }
    }
}
