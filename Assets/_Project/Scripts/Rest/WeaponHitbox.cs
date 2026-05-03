using UnityEngine;

public class WeaponHitbox : MonoBehaviour
{
    // Vamos precisar de uma referência ao script principal de ataque para saber quanto dano causar.
    public PrimaryAttackKnife primaryAttackScript;

    private void Start()
    {
        // --- CORREÇÃO AUTOMÁTICA ---
        // Se você esqueceu de arrastar no Inspector, o código tenta achar no Pai (o Player)
        if (primaryAttackScript == null)
        {
            primaryAttackScript = GetComponentInParent<PrimaryAttackKnife>();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Ao colidir com algo, primeiro verificamos se é um inimigo.
        // Busca no próprio collider primeiro; se não achar, busca no pai
        // (necessário para inimigos com fragmentos filhos, como o ShardSwarm).
        DummyHealth enemyHealth = other.GetComponent<DummyHealth>()
                                ?? other.GetComponentInParent<DummyHealth>();

        if (enemyHealth != null)
        {
            if (primaryAttackScript != null)
            {
                // Passa o collider original (do fragmento ou do inimigo) — RegisterHit
                // também faz o fallback GetComponentInParent para DummyHealth e Rigidbody.
                primaryAttackScript.RegisterHit(other);
            }
            else
            {
                Debug.LogError("ERRO CRÍTICO: O WeaponHitbox não encontrou o script PrimaryAttackKnife no Player!");
            }
        }
    }
}