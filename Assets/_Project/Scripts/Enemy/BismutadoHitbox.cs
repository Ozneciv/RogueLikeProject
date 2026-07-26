using UnityEngine;

/// <summary>
/// Componente de Hitbox Físico acoplado ao BoxCollider do Arm_Forearm.
/// Ativado automaticamente durante a varredura do Cruzado do Bismutado.
/// </summary>
public class BismutadoHitbox : MonoBehaviour
{
    [Header("Configurações de Dano")]
    public float damage = 25f;
    public float knockbackForce = 12f;

    private Collider col;
    private Rigidbody rb;
    private bool hasHitThisAttack = false;

    void Awake()
    {
        col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
            col.enabled = false; // Desativado fora do ataque
        }

        // Adiciona Rigidbody kinematic para garantir que a Unity dispare o OnTriggerEnter 100% das vezes
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }

    public void EnableHitbox()
    {
        hasHitThisAttack = false;
        if (col == null) col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true; // Garante 100% que o BoxCollider seja um Trigger (para não arremessar o player)
            col.enabled = true;
        }
    }

    public void DisableHitbox()
    {
        if (col == null) col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
            col.enabled = false;
        }
        hasHitThisAttack = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasHitThisAttack) return;

        // Tenta encontrar o PlayerHealth no colisor ou nos pais
        PlayerHealth health = other.GetComponentInParent<PlayerHealth>() ?? other.GetComponent<PlayerHealth>();
        if (health != null)
        {
            // Aplica dano no Player com suporte ao flash vermelho e feedback da UI
            health.TakeDamage(Mathf.RoundToInt(damage), transform.root.gameObject);
            hasHitThisAttack = true;

            // Aplica empurrão (Knockback) horizontal físico no Player (sem jogar para cima)
            Rigidbody playerRb = other.GetComponentInParent<Rigidbody>() ?? other.GetComponent<Rigidbody>();
            if (playerRb != null && !playerRb.isKinematic)
            {
                Vector3 knockDir = (other.transform.position - transform.position).normalized;
                knockDir.y = 0.0f; // Empurrão plano no chão (sem lançar para o ar!)
                playerRb.AddForce(knockDir * knockbackForce, ForceMode.Impulse);
            }

            Debug.Log($"⚔️ [BISMUTADO HITBOX] CRUZADO ACERTOU O PLAYER! Dano causado: {damage}");
        }
    }
}
