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
        ProcessHit(other);
    }

    private void OnTriggerStay(Collider other)
    {
        ProcessHit(other);
    }

    private void ProcessHit(Collider other)
    {
        if (other == null) return;

        // Ao colidir ou permanecer na área, verificamos se é um inimigo (DummyHealth ou ShardSwarmHealth)
        DummyHealth enemyHealth = other.GetComponent<DummyHealth>()
                                ?? other.GetComponentInParent<DummyHealth>();

        ShardSwarmHealth swarmHealth = other.GetComponent<ShardSwarmHealth>()
                                    ?? other.GetComponentInParent<ShardSwarmHealth>();

        if (enemyHealth != null || swarmHealth != null)
        {
            if (primaryAttackScript == null)
            {
                primaryAttackScript = GetComponentInParent<PrimaryAttackKnife>()
                                   ?? Object.FindFirstObjectByType<PrimaryAttackKnife>();
            }

            if (primaryAttackScript != null && primaryAttackScript.isHitboxActive)
            {
                primaryAttackScript.RegisterHit(other);
            }
        }
    }
}