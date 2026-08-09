using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public struct PlayerSkinConfig
{
    public string skinID; // e.g. "astronaut", "astro", "goku"
    public string skinName; // UI display name
    public bool isPrefab; // True if instantiated from prefab, false if child GameObject

    [Header("Prefab Settings")]
    public GameObject skinPrefab; // Used if isPrefab is true

    [Header("In-Scene Child Settings")]
    [Tooltip("Lista de objetos locais a serem ativados/desativados para esta skin (ex: Armature + Malha Mesh).")]
    public List<GameObject> existingChildObjects; // Replaces single existingChildObject

    [Header("Offsets (Somente para Prefabs)")]
    public Vector3 localPositionOffset;
    public Vector3 localRotationOffset; // Euler angles
    public Vector3 localScale; // Default (1,1,1)

    [Header("Components to inject")]
    public bool injectModelOffset;
    public bool lockYOffset;
    public bool lockRootRotation;
}

public class PlayerSkinManager : MonoBehaviour
{
    [Header("Skin Configurations")]
    public List<PlayerSkinConfig> skins = new List<PlayerSkinConfig>();
    public string defaultSkinID = "astronaut";

    [Header("Debug")]
    [SerializeField] private string activeSkinID;
    private GameObject spawnedSkinInstance;

    // Cache original transform values for local child objects to prevent sinking/resetting bugs
    private Dictionary<string, List<Vector3>> originalPositions = new Dictionary<string, List<Vector3>>();
    private Dictionary<string, List<Quaternion>> originalRotations = new Dictionary<string, List<Quaternion>>();
    private Dictionary<string, List<Vector3>> originalScales = new Dictionary<string, List<Vector3>>();
    [Header("Controller Setup")]
    [Tooltip("Arraste aqui o Animator Controller principal do jogador (PlayerAnimation) para garantir que todas as skins usem o mesmo cérebro de animações.")]
    public RuntimeAnimatorController mainAnimatorController;

    public string ActiveSkinID => activeSkinID;

    private void Awake()
    {
        // Cache do controller principal caso não tenha sido arrastado no Inspector
        if (mainAnimatorController == null)
        {
            Animator defaultAnim = GetComponentInChildren<Animator>();
            if (defaultAnim != null)
            {
                mainAnimatorController = defaultAnim.runtimeAnimatorController;
                Debug.Log($"[PlayerSkinManager] Cached main RuntimeAnimatorController dynamically: {(mainAnimatorController != null ? mainAnimatorController.name : "null")}");
            }
        }
        else
        {
            Debug.Log($"[PlayerSkinManager] Using pre-assigned RuntimeAnimatorController: {mainAnimatorController.name}");
        }

        // Cache original transform values of all existing child objects
        foreach (var config in skins)
        {
            if (!config.isPrefab && config.existingChildObjects != null)
            {
                var posList = new List<Vector3>();
                var rotList = new List<Quaternion>();
                var scaleList = new List<Vector3>();

                foreach (var obj in config.existingChildObjects)
                {
                    if (obj != null)
                    {
                        posList.Add(obj.transform.localPosition);
                        rotList.Add(obj.transform.localRotation);
                        scaleList.Add(obj.transform.localScale);
                    }
                    else
                    {
                        posList.Add(Vector3.zero);
                        rotList.Add(Quaternion.identity);
                        scaleList.Add(Vector3.one);
                    }
                }

                originalPositions[config.skinID] = posList;
                originalRotations[config.skinID] = rotList;
                originalScales[config.skinID] = scaleList;
            }
        }
    }

    private void Start()
    {
        // If no skin has been loaded yet, load the default skin
        if (string.IsNullOrEmpty(activeSkinID))
        {
            SetSkin(defaultSkinID);
        }
    }

    public void SetSkin(string skinID)
    {
        Debug.Log($"[PlayerSkinManager] Attempting to set skin to: '{skinID}'");

        PlayerSkinConfig selectedConfig = default;
        bool found = false;
        foreach (var config in skins)
        {
            if (config.skinID == skinID)
            {
                selectedConfig = config;
                found = true;
                break;
            }
        }

        if (!found)
        {
            Debug.LogWarning($"[PlayerSkinManager] Skin ID '{skinID}' not found! Falling back to default: '{defaultSkinID}'");
            if (skinID == defaultSkinID) return;
            SetSkin(defaultSkinID);
            return;
        }

        // --- VALIDATION CHECK ---
        bool isValid = true;
        if (selectedConfig.isPrefab)
        {
            if (selectedConfig.skinPrefab == null) isValid = false;
        }
        else
        {
            if (selectedConfig.existingChildObjects == null || selectedConfig.existingChildObjects.Count == 0) isValid = false;
        }

        if (!isValid)
        {
            Debug.LogError($"[PlayerSkinManager] Skin configuration for ID '{skinID}' is invalid (missing prefab reference or in-scene child list is empty)! Falling back to default.");
            if (skinID == defaultSkinID) return;
            SetSkin(defaultSkinID);
            return;
        }

        // --- WEAPON PERSISTENCE PRE-SAVE ---
        // Find if there is an equipped weapon attached to the old right hand bone before we disable/destroy it
        GameObject equippedWeapon = null;
        Player_WeaponManager weaponManager = GetComponentInChildren<Player_WeaponManager>();
        if (weaponManager != null && weaponManager.rightHand != null)
        {
            foreach (Transform child in weaponManager.rightHand)
            {
                // Capture the first child on the hand bone as the weapon
                equippedWeapon = child.gameObject;
                break;
            }
        }

        // 1. Disable all local child skins that are not the selected one
        foreach (var config in skins)
        {
            if (!config.isPrefab && config.existingChildObjects != null)
            {
                foreach (var obj in config.existingChildObjects)
                {
                    if (obj != null)
                    {
                        if (IsScriptHolder(obj))
                        {
                            // Do not deactivate the GameObject containing essential scripts!
                            // Only disable its visual renderers and animator to prevent double-rendering/execution
                            var renderers = obj.GetComponentsInChildren<Renderer>(true);
                            foreach (var r in renderers)
                            {
                                r.enabled = false;
                            }
                            var anim = obj.GetComponent<Animator>();
                            if (anim != null) anim.enabled = false;
                        }
                        else
                        {
                            obj.SetActive(false);
                        }
                    }
                }
            }
        }

        // 2. Destroy previously spawned prefab instance if there is one
        if (spawnedSkinInstance != null)
        {
            Destroy(spawnedSkinInstance);
            spawnedSkinInstance = null;
        }

        // 3. Activate the chosen skin
        GameObject activeModelObj = null;

        if (selectedConfig.isPrefab)
        {
            if (selectedConfig.skinPrefab != null)
            {
                spawnedSkinInstance = Instantiate(selectedConfig.skinPrefab, transform);
                spawnedSkinInstance.name = $"Skin_{selectedConfig.skinID}";
                
                // Apply offsets for prefab
                spawnedSkinInstance.transform.localPosition = selectedConfig.localPositionOffset;
                spawnedSkinInstance.transform.localRotation = Quaternion.Euler(selectedConfig.localRotationOffset);
                if (selectedConfig.localScale != Vector3.zero)
                {
                    spawnedSkinInstance.transform.localScale = selectedConfig.localScale;
                }
                else
                {
                    spawnedSkinInstance.transform.localScale = Vector3.one;
                }

                activeModelObj = spawnedSkinInstance;
            }
            else
            {
                Debug.LogError($"[PlayerSkinManager] Prefab for skin '{skinID}' is null!");
            }
        }
        else
        {
            if (selectedConfig.existingChildObjects != null && selectedConfig.existingChildObjects.Count > 0)
            {
                // Restore original saved transforms for local objects instead of forcing (0,0,0)
                List<Vector3> savedPos = originalPositions.ContainsKey(skinID) ? originalPositions[skinID] : null;
                List<Quaternion> savedRot = originalRotations.ContainsKey(skinID) ? originalRotations[skinID] : null;
                List<Vector3> savedScale = originalScales.ContainsKey(skinID) ? originalScales[skinID] : null;

                for (int i = 0; i < selectedConfig.existingChildObjects.Count; i++)
                {
                    GameObject obj = selectedConfig.existingChildObjects[i];
                    if (obj != null)
                    {
                        if (IsScriptHolder(obj))
                        {
                            // Restore visual renderers and animator
                            var renderers = obj.GetComponentsInChildren<Renderer>(true);
                            foreach (var r in renderers)
                            {
                                r.enabled = true;
                            }
                            var anim = obj.GetComponent<Animator>();
                            if (anim != null) anim.enabled = true;
                        }
                        else
                        {
                            obj.SetActive(true);
                        }
                        
                        // Restore cached values
                        if (savedPos != null && i < savedPos.Count) obj.transform.localPosition = savedPos[i];
                        if (savedRot != null && i < savedRot.Count) obj.transform.localRotation = savedRot[i];
                        if (savedScale != null && i < savedScale.Count) obj.transform.localScale = savedScale[i];
                    }
                }

                // Use the first object as the primary object containing the animator/references
                activeModelObj = selectedConfig.existingChildObjects[0];
            }
            else
            {
                Debug.LogError($"[PlayerSkinManager] Existing child list for skin '{skinID}' is null or empty!");
            }
        }

        if (activeModelObj == null)
        {
            Debug.LogError($"[PlayerSkinManager] Could not activate model for skin '{skinID}'!");
            return;
        }

        // 4. Retrieve/Establish references with robust fallbacks in case inspector references are unassigned
        Animator skinAnimator = null;
        Transform rightHandBone = null;
        Collider handHitboxCollider = null;

        PlayerSkinReferences skinRefs = activeModelObj.GetComponent<PlayerSkinReferences>();
        if (skinRefs != null)
        {
            skinAnimator = skinRefs.animator != null ? skinRefs.animator : activeModelObj.GetComponentInChildren<Animator>();
            rightHandBone = skinRefs.rightHand != null ? skinRefs.rightHand : (FindDeepChild(activeModelObj.transform, "RightHand") ?? FindDeepChild(activeModelObj.transform, "Hand_R") ?? FindDeepChild(activeModelObj.transform, "Hand.R"));
            handHitboxCollider = skinRefs.handHitbox != null ? skinRefs.handHitbox : activeModelObj.GetComponentInChildren<Collider>();
        }
        else
        {
            // Fallback: search components dynamically
            skinAnimator = activeModelObj.GetComponentInChildren<Animator>();
            
            // Search for right hand bone recursively
            rightHandBone = FindDeepChild(activeModelObj.transform, "RightHand") 
                            ?? FindDeepChild(activeModelObj.transform, "Hand_R")
                            ?? FindDeepChild(activeModelObj.transform, "Hand.R");
            
            // Search for hand hitbox
            handHitboxCollider = activeModelObj.GetComponentInChildren<Collider>();
            if (handHitboxCollider != null && handHitboxCollider.gameObject == activeModelObj)
            {
                handHitboxCollider = null;
                var colliders = activeModelObj.GetComponentsInChildren<Collider>();
                foreach (var col in colliders)
                {
                    if (col.gameObject != activeModelObj && col.name.Contains("Hitb"))
                    {
                        handHitboxCollider = col;
                        break;
                    }
                }
            }
        }

        // --- AUTOMATIC RUNTIME CONTROLLER AND EVENT ROUTER SYNC ---
        var animators = activeModelObj.GetComponentsInChildren<Animator>(true);
        if (animators != null && animators.Length > 0)
        {
            skinAnimator = animators[0]; // Primary animator is the first one found

            foreach (var anim in animators)
            {
                // Disable Root Motion to prevent physics conflicts
                anim.applyRootMotion = false;

                // Force main RuntimeAnimatorController (or weapon override) to sync state machine
                RuntimeAnimatorController controllerToApply = mainAnimatorController;
                if (equippedWeapon != null)
                {
                    WeaponOffset offsetData = equippedWeapon.GetComponent<WeaponOffset>();
                    if (offsetData != null && offsetData.weaponAnimatorOverride != null)
                    {
                        controllerToApply = offsetData.weaponAnimatorOverride;
                    }
                }

                if (controllerToApply != null)
                {
                    anim.runtimeAnimatorController = controllerToApply;
                    Debug.Log($"[PlayerSkinManager] Synced Animator Controller '{controllerToApply.name}' onto '{anim.gameObject.name}'");
                }

                // Route animation events safely on the exact GameObject that holds the Animator
                PlayerAnimationEvents animEvents = anim.gameObject.GetComponent<PlayerAnimationEvents>();
                if (animEvents == null)
                {
                    animEvents = anim.gameObject.AddComponent<PlayerAnimationEvents>();
                    Debug.Log($"[PlayerSkinManager] Bound PlayerAnimationEvents router directly to Animator on '{anim.gameObject.name}'");
                }
            }
        }
        else
        {
            Debug.LogWarning($"[PlayerSkinManager] No Animator found on skin '{skinID}'!");
        }

        // --- WEAPON RE-PARENTING ---
        // Re-parent the saved weapon to the newly instantiated or activated right hand bone
        if (weaponManager != null && rightHandBone != null)
        {
            weaponManager.rightHand = rightHandBone;
            if (equippedWeapon != null)
            {
                equippedWeapon.transform.SetParent(rightHandBone);
                
                WeaponOffset offsetData = equippedWeapon.GetComponent<WeaponOffset>();
                if (offsetData != null)
                {
                    equippedWeapon.transform.localPosition = offsetData.equipPosition;
                    equippedWeapon.transform.localRotation = Quaternion.Euler(offsetData.equipRotation);
                }
                else
                {
                    equippedWeapon.transform.localPosition = Vector3.zero;
                    equippedWeapon.transform.localRotation = Quaternion.identity;
                }
                
                equippedWeapon.SetActive(true); // Force weapon active!
                Debug.Log($"[PlayerSkinManager] Re-parented equipped weapon '{equippedWeapon.name}' to new right hand.");
            }
        }

        // 5. Inject components if needed
        if (selectedConfig.injectModelOffset)
        {
            PlayerModelOffset offsetComp = activeModelObj.GetComponent<PlayerModelOffset>();
            if (offsetComp == null)
            {
                offsetComp = activeModelObj.AddComponent<PlayerModelOffset>();
            }
            offsetComp.lockYOffset = selectedConfig.lockYOffset;
            offsetComp.lockRootRotation = selectedConfig.lockRootRotation;
        }

        // Ensure PlayerAnimationEvents is attached to handle revive animation triggers
        if (activeModelObj.GetComponent<PlayerAnimationEvents>() == null)
        {
            activeModelObj.AddComponent<PlayerAnimationEvents>();
        }

        // 6. Bind references on the parent Player scripts
        BindReferences(skinAnimator, rightHandBone, handHitboxCollider, equippedWeapon);

        activeSkinID = skinID;
        Debug.Log($"[PlayerSkinManager] Skin successfully changed to: '{skinID}'");
    }

    private void BindReferences(Animator newAnimator, Transform newRightHand, Collider newHandHitbox, GameObject equippedWeapon)
    {
        // PlayerM
        PlayerM playerMovement = GetComponent<PlayerM>();
        if (playerMovement != null)
        {
            playerMovement.animator = newAnimator;
            Debug.Log("[PlayerSkinManager] Bound Animator to PlayerM");
        }

        // PlayerHealth
        PlayerHealth playerHealth = GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.playerAnimator = newAnimator;
            playerHealth.playerAnimator?.Rebind();
            Debug.Log("[PlayerSkinManager] Bound Animator to PlayerHealth");
        }

        // PrimaryAttackKnife
        PrimaryAttackKnife attackScript = GetComponentInChildren<PrimaryAttackKnife>();
        if (attackScript != null)
        {
            attackScript.animator = newAnimator;
            if (newHandHitbox != null)
            {
                attackScript.handHitbox = newHandHitbox;
            }

            // Only equip default weapon if the player does not have a weapon currently equipped.
            // If they have a weapon, preserve the state and link the new weapon collider.
            if (!attackScript.hasWeapon)
            {
                attackScript.EquipDefaultWeapon();
                Debug.Log("[PlayerSkinManager] Bound default hand hitbox to PrimaryAttackKnife (No weapon)");
            }
            else if (equippedWeapon != null)
            {
                equippedWeapon.SetActive(true); // Force weapon active!
                Collider weaponCollider = equippedWeapon.GetComponentInChildren<Collider>();
                if (weaponCollider != null)
                {
                    // Re-equip the weapon based on its name (case-insensitive) to prevent range floating-point comparison bugs
                    string weaponName = equippedWeapon.name.ToLower();
                    if (weaponName.Contains("dagger") || weaponName.Contains("adaga") || weaponName.Contains("knife"))
                    {
                        attackScript.EquipDaggerWeapon(weaponCollider);
                    }
                    else
                    {
                        attackScript.EquipSwordWeapon(weaponCollider);
                    }
                    Debug.Log($"[PlayerSkinManager] Re-bound equipped weapon collider '{weaponCollider.name}' to PrimaryAttackKnife");
                }
                else
                {
                    Debug.LogWarning($"[PlayerSkinManager] Equipped weapon '{equippedWeapon.name}' has no Collider!");
                }
            }
        }

        // Player_WeaponManager
        Player_WeaponManager weaponManager = GetComponentInChildren<Player_WeaponManager>();
        if (weaponManager != null)
        {
            if (newRightHand != null)
            {
                weaponManager.rightHand = newRightHand;
            }
            if (newAnimator != null)
            {
                weaponManager.playerAnimator = newAnimator;
            }
            Debug.Log("[PlayerSkinManager] Bound RightHand Bone and Animator to Player_WeaponManager");
        }

        // CheatConsole
        CheatConsole cheatConsole = GetComponentInChildren<CheatConsole>();
        if (cheatConsole != null)
        {
            cheatConsole.playerAnimator = newAnimator;
            Debug.Log("[PlayerSkinManager] Bound Animator to CheatConsole");
        }

        // PlayerUltimate
        PlayerUltimate playerUlt = GetComponentInChildren<PlayerUltimate>() ?? GetComponentInParent<PlayerUltimate>();
        if (playerUlt != null)
        {
            playerUlt.RebindReferences();
            Debug.Log("[PlayerSkinManager] Rebound references to PlayerUltimate");
        }

        // PlayerM
        PlayerM playerM = GetComponentInChildren<PlayerM>() ?? GetComponentInParent<PlayerM>();
        if (playerM != null && newAnimator != null)
        {
            playerM.animator = newAnimator;
            Debug.Log("[PlayerSkinManager] Bound Animator to PlayerM");
        }
    }

    private Transform FindDeepChild(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name.Contains(name))
                return child;
            Transform result = FindDeepChild(child, name);
            if (result != null)
                return result;
        }
        return null;
    }

    private bool IsScriptHolder(GameObject obj)
    {
        if (obj == null) return false;
        if (obj == gameObject) return true;
        if (obj.GetComponent<PrimaryAttackKnife>() != null) return true;
        if (obj.GetComponent<Player_WeaponManager>() != null) return true;
        return false;
    }
}
