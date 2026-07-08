using UnityEngine;

public class PlayerSkinReferences : MonoBehaviour
{
    [Tooltip("The Animator component for this skin")]
    public Animator animator;

    [Tooltip("The Right Hand bone transform of the armature (for attaching weapons)")]
    public Transform rightHand;

    [Tooltip("The Hand Hitbox Collider on the right hand (for attacks)")]
    public Collider handHitbox;
}
