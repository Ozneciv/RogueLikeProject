using UnityEngine;

public class Player_WeaponManager : MonoBehaviour
{
    public Transform rightHand;
    public Vector3 daggerOffsetPosition;
    public Vector3 daggerOffsetRotation;
    
    public PrimaryAttackKnife attackScript;

    public void EquipDagger(GameObject dagger)
    {
        dagger.transform.SetParent(rightHand);

        dagger.transform.localPosition = daggerOffsetPosition;
        dagger.transform.localRotation = Quaternion.Euler(daggerOffsetRotation);

        dagger.GetComponent<Collider>().enabled = false;
        dagger.GetComponent<Dagger_Pickup>().enabled = false;
        
        if (dagger.GetComponent<Rigidbody>() != null)
        {
            dagger.GetComponent<Rigidbody>().isKinematic = true;
        }

        if (attackScript != null)
        {
            attackScript.EquipWeapon(attackScript.daggerDamage, attackScript.daggerRange);
        }

        // Adiciona uma mensagem ao console para confirmar que a adaga foi equipada
        Debug.Log("Adaga equipada com sucesso! Dano: " + attackScript.currentDamage + ", Alcance: " + attackScript.currentRange);
    }
}