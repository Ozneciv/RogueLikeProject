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
        // Ao colidir com algo, primeiro verificamos se é um inimigo
        DummyHealth enemyHealth = other.GetComponent<DummyHealth>();
        
        if (enemyHealth != null)
        {
            // --- PROTEÇÃO CONTRA O ERRO ---
            // Só tentamos registrar o hit se o script principal foi encontrado
            if (primaryAttackScript != null)
            {
                primaryAttackScript.RegisterHit(other);
            }
            else
            {
                Debug.LogError("ERRO CRÍTICO: O WeaponHitbox não encontrou o script PrimaryAttackKnife no Player!");
            }
        }
    }
}