using UnityEngine;

public class WeaponHitbox : MonoBehaviour
{
    // Vamos precisar de uma referência ao script principal de ataque para saber quanto dano causar.
    public PrimaryAttackKnife primaryAttackScript;

    private void OnTriggerEnter(Collider other)
    {
        // Ao colidir com algo, primeiro verificamos se é um inimigo (procurando pelo script de vida)
        DummyHealth enemyHealth = other.GetComponent<DummyHealth>();
        if (enemyHealth != null)
        {
            // Se for um inimigo, pedimos ao script principal para tentar causar dano nele.
            primaryAttackScript.RegisterHit(other);
        }
    }
}