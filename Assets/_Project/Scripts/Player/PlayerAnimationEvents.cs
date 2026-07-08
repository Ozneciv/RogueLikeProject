using UnityEngine;

public class PlayerAnimationEvents : MonoBehaviour
{
    private PrimaryAttackKnife attackScript;

    private void Awake()
    {
        FindAttackScript();
    }

    private void FindAttackScript()
    {
        if (attackScript == null)
        {
            attackScript = GetComponentInParent<PrimaryAttackKnife>();
            if (attackScript == null)
            {
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null)
                {
                    attackScript = playerObj.GetComponentInChildren<PrimaryAttackKnife>();
                }
            }
        }
    }

    public void EnableHitbox()
    {
        FindAttackScript();
        if (attackScript != null)
        {
            attackScript.EnableHitbox();
        }
    }

    public void DisableHitbox()
    {
        FindAttackScript();
        if (attackScript != null)
        {
            attackScript.DisableHitbox();
        }
    }

    public void OpenAttackWindow()
    {
        FindAttackScript();
        if (attackScript != null)
        {
            attackScript.OpenAttackWindow();
        }
    }

    // Esta função vai estar no objeto FILHO (astronauta), junto com o Animator
    public void HandleReviveCompletion()
    {
        PlayerHealth healthScript = GetComponentInParent<PlayerHealth>();
        if (healthScript == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                healthScript = playerObj.GetComponent<PlayerHealth>();
            }
        }
        
        if (healthScript != null)
        {
            healthScript.HandleReviveCompletion();
        }
        else
        {
            Debug.LogError("PlayerAnimationEvents: Não encontrei o PlayerHealth no pai!");
        }
    }
}