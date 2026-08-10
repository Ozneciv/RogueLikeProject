using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Script de Hitbox da Mão/Braço do Boss Cromático.
/// Ativado temporariamente pelo BossController durante os ataques físicos (Punch / Swipe).
/// </summary>
public class BossHandHitbox : MonoBehaviour
{
    [Header("Configurações da Hitbox")]
    public Collider handCollider;
    public int damage = 35;
    public float knockbackForce = 15f;

    private bool isActive = false;
    private HashSet<GameObject> hitObjects = new HashSet<GameObject>();
    private BossController bossController;

    void Awake()
    {
        if (handCollider == null)
            handCollider = GetComponent<Collider>();

        if (handCollider != null)
        {
            handCollider.isTrigger = true;
            handCollider.enabled = false; // Começa desativada por padrão
        }

        bossController = GetComponentInParent<BossController>();
    }

    /// <summary>
    /// Ativa a hitbox da mão temporariamente por [duration] segundos.
    /// </summary>
    public void EnableHitbox(float duration, int attackDamage = 35, float pushForce = 15f)
    {
        damage = attackDamage;
        knockbackForce = pushForce;
        hitObjects.Clear();

        if (handCollider != null) handCollider.enabled = true;
        isActive = true;

        CancelInvoke(nameof(DisableHitbox));
        Invoke(nameof(DisableHitbox), duration);
    }

    /// <summary>
    /// Desativa a hitbox da mão.
    /// </summary>
    public void DisableHitbox()
    {
        isActive = false;
        if (handCollider != null) handCollider.enabled = false;
        hitObjects.Clear();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isActive) return;
        if (other.CompareTag("Player") && !hitObjects.Contains(other.gameObject))
        {
            hitObjects.Add(other.gameObject);

            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage, gameObject);
            }

            Rigidbody playerRb = other.GetComponent<Rigidbody>();
            if (playerRb != null && !playerRb.isKinematic)
            {
                Vector3 dir = (other.transform.position - transform.position).normalized;
                dir.y = 0.2f;
                playerRb.AddForce(dir * knockbackForce, ForceMode.Impulse);
            }

            if (bossController != null)
            {
                bossController.TriggerCameraShake(0.2f, 0.12f);
            }

            Debug.Log($"[BossHandHitbox] 💥 HITBOX DA MÃO ACERTOU O PLAYER! Dano: {damage}");
        }
    }
}
