using UnityEngine;

public class PlayerAnimationEvents : MonoBehaviour
{
    // Referência para o script que realmente contém a lógica de saúde
    public PlayerHealth playerHealth;

    // Esta é a função que o Animator vai chamar
    public void OnReviveAnimationComplete()
    {
        // Se a referência ao PlayerHealth existir, ele repassa o comando
        if (playerHealth != null)
        {
            // Pede ao PlayerHealth para finalizar a sequência de renascimento
            playerHealth.HandleReviveCompletion();
        }
        else
        {
            Debug.LogError("Referência ao PlayerHealth não está configurada no PlayerAnimationEvents!");
        }
    }
}