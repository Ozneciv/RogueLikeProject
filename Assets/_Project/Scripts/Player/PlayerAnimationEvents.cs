using UnityEngine;

public class PlayerAnimationEvents : MonoBehaviour
{
    // Esta função vai estar no objeto FILHO (astronauta), junto com o Animator
    public void HandleReviveCompletion()
    {
        // Procura o script PlayerHealth no objeto PAI e chama a função dele
        PlayerHealth healthScript = GetComponentInParent<PlayerHealth>();
        
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