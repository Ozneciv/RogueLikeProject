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

        // Ao colidir ou permanecer na área, verificamos se é um inimigo (DummyHealth ou ShardSwarmHealth) ou um NPC/Escudo (InvulnerableShieldNPC)
        DummyHealth enemyHealth = other.GetComponent<DummyHealth>()
                                ?? other.GetComponentInParent<DummyHealth>();

        ShardSwarmHealth swarmHealth = other.GetComponent<ShardSwarmHealth>()
                                    ?? other.GetComponentInParent<ShardSwarmHealth>();

        InvulnerableShieldNPC invShield = other.GetComponent<InvulnerableShieldNPC>()
                                       ?? other.GetComponentInParent<InvulnerableShieldNPC>();

        MerchantVFX merchantVfx = other.GetComponent<MerchantVFX>()
                               ?? other.GetComponentInParent<MerchantVFX>();

        if (enemyHealth != null || swarmHealth != null || invShield != null || merchantVfx != null)
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