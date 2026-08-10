using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Aplica dano e repulsão quando o jogador encosta nos espinhos de cristal do Boss.
/// </summary>
public class SpikeDamageDealer : MonoBehaviour
{
    public int damage = 45;
    private HashSet<GameObject> hitPlayers = new HashSet<GameObject>();

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !hitPlayers.Contains(other.gameObject))
        {
            hitPlayers.Add(other.gameObject);

            PlayerHealth ph = other.GetComponent<PlayerHealth>();
            if (ph != null)
            {
                ph.TakeDamage(damage, gameObject);
            }

            Rigidbody rb = other.GetComponent<Rigidbody>();
            if (rb != null && !rb.isKinematic)
            {
                Vector3 push = (other.transform.position - transform.position).normalized;
                push.y = 0.3f;
                rb.AddForce(push * 15f, ForceMode.Impulse);
            }

            Debug.Log($"[SpikeDamageDealer] 💥 Player atingido por espinho de cristal! Dano: {damage}");
        }
    }
}
