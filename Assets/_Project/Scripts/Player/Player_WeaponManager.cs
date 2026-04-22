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

        // Desativa os componentes de quando o item estava no chão
        dagger.GetComponent<Collider>().enabled = false;
        dagger.GetComponent<Dagger_Pickup>().enabled = false;
        
        if (dagger.GetComponent<Rigidbody>() != null)
        {
            dagger.GetComponent<Rigidbody>().isKinematic = true;
        }

        // --- MUDANÇA AQUI ---
        // Procura pelo script de flutuação e o desativa.
        ItemFloat floatScript = dagger.GetComponent<ItemFloat>();
        if (floatScript != null)
        {
            floatScript.enabled = false;
        }
        // --- FIM DA MUDANÇA ---

        if (attackScript != null)
        {
            attackScript.EquipDaggerWeapon(dagger.GetComponent<Collider>());
        }

        Debug.Log("Adaga equipada com sucesso!");
    }
}