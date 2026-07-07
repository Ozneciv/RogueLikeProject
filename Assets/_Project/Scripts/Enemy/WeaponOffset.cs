using UnityEngine;

public enum WeaponType { Dagger, Sword, Axe }

public class WeaponOffset : MonoBehaviour
{
    [Header("Identificação")]
    public WeaponType weaponType;

    [Header("Configurações de Encaixe na Mão")]
    public Vector3 equipPosition;
    public Vector3 equipRotation;

    [Header("Animações (Moveset)")]

    public AnimatorOverrideController weaponAnimatorOverride; 
}