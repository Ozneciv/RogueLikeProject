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
        Debug.Log($"[Player_WeaponManager] EquipDagger chamado para o objeto: {weapon.name}");
        WeaponOffset offsetData = weapon.GetComponent<WeaponOffset>();
        
        if (offsetData == null)
        {
            Debug.LogWarning($"[Player_WeaponManager] WeaponOffset ausente em '{weapon.name}'! Criando um componente temporário com valores padrão para possibilitar o equip.");
            offsetData = weapon.AddComponent<WeaponOffset>();
            offsetData.weaponType = WeaponType.Dagger;
            offsetData.equipPosition = Vector3.zero;
            offsetData.equipRotation = Vector3.zero;
        }

        if (rightHand == null)
        {
            Debug.LogWarning("[Player_WeaponManager] Mão Direita (rightHand) está nula no script. Tentando encontrar osso 'RightHand' nos filhos...");
            // Procura osso nos filhos
            Transform foundHand = transform.Find("RightHand") ?? transform.Find("Hand_R") ?? transform.Find("Hand.R");
            if (foundHand != null)
            {
                rightHand = foundHand;
                Debug.Log($"[Player_WeaponManager] Mão Direita encontrada dinamicamente: {rightHand.name}");
            }
        }

        weapon.transform.SetParent(rightHand);
        weapon.transform.localPosition = offsetData.equipPosition;
        weapon.transform.localRotation = Quaternion.Euler(offsetData.equipRotation);
        Debug.Log($"[Player_WeaponManager] Objeto {weapon.name} acoplado à mão: {(rightHand != null ? rightHand.name : "null")}");

        
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

        // Procura colisor na arma ou em seus filhos
        Collider weaponCollider = weapon.GetComponent<Collider>() ?? weapon.GetComponentInChildren<Collider>();
        if (weaponCollider != null) 
        {
            weaponCollider.enabled = false; 

            // Garante que o colisor tenha o componente WeaponHitbox para aplicar dano
            WeaponHitbox hitboxScript = weaponCollider.GetComponent<WeaponHitbox>();
            if (hitboxScript == null)
            {
                hitboxScript = weaponCollider.gameObject.AddComponent<WeaponHitbox>();
                Debug.Log($"[Player_WeaponManager] WeaponHitbox adicionado dinamicamente ao colisor de '{weapon.name}'");
            }
            hitboxScript.primaryAttackScript = attackScript; // Garante referência atualizada no Player
        } 

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