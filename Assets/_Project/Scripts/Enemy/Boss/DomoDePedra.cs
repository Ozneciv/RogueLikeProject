using UnityEngine;

/// <summary>
/// Domo de Pedra Destrutível que envolve e protege o casulo no final da Fase 1 (Mestre do Solo).
/// Enquanto o domo estiver ativo, o casulo fica protegido contra dano direto.
/// O jogador precisa quebrar a estrutura do Domo para alcançar o casulo novamente.
/// </summary>
[RequireComponent(typeof(DummyHealth))]
public class DomoDePedra : MonoBehaviour
{
    public int vidaDomo = 100;
    public float forcaEmpurraoAoEmergir = 12f;
    public GameObject vfxEmergirPrefab;
    public GameObject vfxQuebrarPrefab;

    private DummyHealth dummyHealth;

    private void Awake()
    {
        dummyHealth = GetComponent<DummyHealth>();
        if (dummyHealth != null)
        {
            dummyHealth.maxHealth = vidaDomo;
            dummyHealth.ResetHealth();
            dummyHealth.onDeathOverride += DestruirDomo;
        }

        Collider col = GetComponent<Collider>() ?? GetComponentInChildren<Collider>();
        if (col != null) col.isTrigger = false;
    }

    public void EmergiraDoSolo(Vector3 posicao)
    {
        transform.position = posicao;
        if (vfxEmergirPrefab != null)
        {
            Instantiate(vfxEmergirPrefab, posicao, Quaternion.identity);
        }

        // Empurra o player para longe ao brotar do chão
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            Rigidbody rb = player.GetComponent<Rigidbody>() ?? player.GetComponentInParent<Rigidbody>();
            if (rb != null)
            {
                Vector3 dir = (player.transform.position - posicao).normalized;
                dir.y = 0.25f;
                rb.linearVelocity = Vector3.zero;
                rb.AddForce(dir * forcaEmpurraoAoEmergir, ForceMode.Impulse);
            }
        }
    }

    private void DestruirDomo()
    {
        if (vfxQuebrarPrefab != null)
        {
            Instantiate(vfxQuebrarPrefab, transform.position + Vector3.up * 1.5f, Quaternion.identity);
        }

        Destroy(gameObject);
    }
}
