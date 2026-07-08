using UnityEngine;

public class Player_WeaponManager : MonoBehaviour
{
    public Transform rightHand;
    public PrimaryAttackKnife attackScript;
    
    [Header("Animação")]
    public Animator playerAnimator; 
    private RuntimeAnimatorController defaultAnimatorController;

    void Start()
    {

        if (playerAnimator != null)
        {
            defaultAnimatorController = playerAnimator.runtimeAnimatorController;
        }
    }

    public void EquipDagger(GameObject weapon)
    {
        WeaponOffset offsetData = weapon.GetComponent<WeaponOffset>();
        
        if (offsetData == null) return;

        weapon.transform.SetParent(rightHand);
        weapon.transform.localPosition = offsetData.equipPosition;
        weapon.transform.localRotation = Quaternion.Euler(offsetData.equipRotation);

        
        if (playerAnimator != null)
        {
            if (offsetData.weaponAnimatorOverride != null)
            {
                
                playerAnimator.runtimeAnimatorController = offsetData.weaponAnimatorOverride;
                Debug.Log("Moveset alterado para: " + weapon.name);
            }
            else
            {

                playerAnimator.runtimeAnimatorController = defaultAnimatorController;
            }
        }
        // -------------------------------------------------------

        Collider weaponCollider = weapon.GetComponent<Collider>();
        if (weaponCollider != null) weaponCollider.enabled = false; 

        if (weapon.GetComponent<Rigidbody>() != null) weapon.GetComponent<Rigidbody>().isKinematic = true;

        ItemFloat floatScript = weapon.GetComponent<ItemFloat>();
        if (floatScript != null) floatScript.enabled = false;

        Dagger_Pickup daggerPickup = weapon.GetComponent<Dagger_Pickup>();
        if (daggerPickup != null) daggerPickup.enabled = false;

        if (attackScript != null && weaponCollider != null)
        {
            if (offsetData.weaponType == WeaponType.Dagger)
                attackScript.EquipDaggerWeapon(weaponCollider);
            else if (offsetData.weaponType == WeaponType.Axe)
                attackScript.EquipAxeWeapon(weaponCollider);
        }
    }
}