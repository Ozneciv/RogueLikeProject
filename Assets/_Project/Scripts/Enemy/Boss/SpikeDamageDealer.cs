using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Aplica dano consistente quando o jogador encosta ou está sobre os espinhos de cristal do Boss.
///  • Converte automaticamente colisores para isTrigger = true.
///  • Ignora colisão física com o Boss (NUNCA empurra nem levanta o Boss do chão).
///  • Aplica dano imediato se o player já estiver sobre o ponto onde o espinho emergiu.
/// </summary>
public class SpikeDamageDealer : MonoBehaviour
{
    [Header("Configurações de Dano")]
    public int damage = 45;
    [Tooltip("Intervalo mínimo de dano contínuo caso o jogador permaneça em cima dos espinhos.")]
    public float damageTickInterval = 0.5f;

    private Dictionary<GameObject, float> hitCooldowns = new Dictionary<GameObject, float>();
    private Collider spikeCollider;

    void Awake()
    {
        // Garante que TODOS os colisores do espinho sejam Triggers (intangíveis para física sólida)
        Collider[] cols = GetComponentsInChildren<Collider>(true);
        foreach (Collider col in cols)
        {
            if (col != null)
            {
                col.isTrigger = true;
            }
        }

        spikeCollider = GetComponent<Collider>() ?? GetComponentInChildren<Collider>();

        // Ignora colisões físicas com qualquer Boss presente na cena
        BossController boss = Object.FindFirstObjectByType<BossController>();
        if (boss != null && spikeCollider != null)
        {
            Collider[] bossCols = boss.GetComponentsInChildren<Collider>(true);
            foreach (Collider bCol in bossCols)
            {
                if (bCol != null)
                {
                    Physics.IgnoreCollision(spikeCollider, bCol, true);
                }
            }
        }
    }

    void Start()
    {
        // Checagem imediata: se o player já estiver parado em cima do ponto de emergência, toma dano imediatamente!
        CheckImmediateOverlap();
    }

    void Update()
    {
        // Se o player estiver sobre o espinho enquanto ele está ativo
        CheckImmediateOverlap();
    }

    private void CheckImmediateOverlap()
    {
        Vector3 checkCenter = transform.position + Vector3.up * 0.8f;
        Collider[] hits = Physics.OverlapSphere(checkCenter, 1.25f);
        foreach (Collider hit in hits)
        {
            if (hit != null && hit.CompareTag("Player"))
            {
                TryDamagePlayer(hit.gameObject);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            TryDamagePlayer(other.gameObject);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            TryDamagePlayer(other.gameObject);
        }
    }

    private void TryDamagePlayer(GameObject playerGO)
    {
        if (playerGO == null) return;

        float now = Time.time;
        if (hitCooldowns.TryGetValue(playerGO, out float nextHitTime))
        {
            if (now < nextHitTime) return;
        }

        hitCooldowns[playerGO] = now + damageTickInterval;

        PlayerHealth ph = playerGO.GetComponent<PlayerHealth>() ?? playerGO.GetComponentInParent<PlayerHealth>();
        if (ph != null)
        {
            ph.TakeDamage(damage, gameObject);
            Debug.Log($"[SpikeDamageDealer] 💥 Player atingido por espinho de cristal! Dano: {damage}");
        }

        // Leve empurrão para afastar do espinho
        Rigidbody rb = playerGO.GetComponent<Rigidbody>() ?? playerGO.GetComponentInParent<Rigidbody>();
        if (rb != null && !rb.isKinematic)
        {
            Vector3 push = (playerGO.transform.position - transform.position);
            push.y = 0.2f;
            if (push.sqrMagnitude < 0.001f) push = Vector3.up * 0.3f;
            else push.Normalize();

            rb.AddForce(push * 6.0f, ForceMode.Impulse);
        }
    }
}
