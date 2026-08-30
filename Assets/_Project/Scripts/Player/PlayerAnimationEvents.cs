using UnityEngine;

public class PlayerAnimationEvents : MonoBehaviour
{
    private PrimaryAttackKnife attackScript;
    private Animator anim;
    private Rigidbody playerRb;

    private void Awake()
    {
        FindAttackScript();
        anim = GetComponent<Animator>();
        playerRb = GetComponentInParent<Rigidbody>();
    }

    private void OnAnimatorMove()
    {
        if (anim != null && anim.applyRootMotion && playerRb != null)
        {
            // Repassa o deslocamento do Root Motion da animação para a física do Rigidbody pai
            playerRb.MovePosition(playerRb.position + anim.deltaPosition);
            playerRb.MoveRotation(playerRb.rotation * anim.deltaRotation);
        }
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
                healthScript = playerObj.GetComponent<PlayerHealth>() ?? playerObj.GetComponentInChildren<PlayerHealth>();
            }
        }
        if (healthScript == null)
        {
            healthScript = Object.FindFirstObjectByType<PlayerHealth>();
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

    /// <summary>
    /// Evento de animação para o impacto da onda de choque do Machado (Suporta maiúsculas e minúsculas).
    /// </summary>
    public void OnAxeSlam() => OnAxeSlamImpact();
    public void onaxeslam() => OnAxeSlamImpact();
    public void onaxeslamimpact() => OnAxeSlamImpact();

    public void OnAxeSlamImpact()
    {
        Debug.Log("[PlayerAnimationEvents] Repassando evento 'OnAxeSlamImpact' para o Ultimate_Axe...");
        Ultimate_Axe axeUlt = GetComponentInParent<Ultimate_Axe>() ?? GetComponent<Ultimate_Axe>();
        if (axeUlt == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) axeUlt = player.GetComponentInChildren<Ultimate_Axe>() ?? player.GetComponent<Ultimate_Axe>();
        }

        if (axeUlt != null)
        {
            axeUlt.TriggerAxeSlamImpact();
        }
        else
        {
            Debug.LogError("[PlayerAnimationEvents] ERRO: Ultimate_Axe não encontrado no Player!");
        }
    }

    /// <summary>
    /// Evento de animação para o fim da sequência do Ultimate (retorno ao Idle).
    /// </summary>
    public void EndUltimateSequence()
    {
        PlayerUltimate ultManager = GetComponentInParent<PlayerUltimate>() ?? GetComponent<PlayerUltimate>();
        if (ultManager == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) ultManager = player.GetComponentInChildren<PlayerUltimate>();
        }

        if (ultManager != null)
        {
            ultManager.EndUltimateSequence();
        }
    }
}