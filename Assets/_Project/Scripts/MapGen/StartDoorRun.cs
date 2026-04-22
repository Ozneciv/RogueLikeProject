using UnityEngine;

public class StartRunDoor : MonoBehaviour
{
    private bool isLoading = false; // Impede múltiplos triggers

    private void OnTriggerEnter(Collider other)
    {
        // Se o jogador (com a tag "Player") tocar o trigger
        if (other.CompareTag("Player") && !isLoading)
        {
            isLoading = true;
            Debug.Log("Jogador tocou a porta! Iniciando a run...");

            // Desativa o collider da porta para evitar problemas
            GetComponent<Collider>().enabled = false; 

            // --- A MUDANÇA ESTÁ AQUI ---
            // Em vez de usar uma variável do Inspector, chamamos diretamente
            // a instância "imortal" e "única" do GameManager.
            if (GameManager.instance != null)
            {
                GameManager.instance.LoadGameLevel();
            }
            else
            {
                Debug.LogError("GameManager.instance não encontrado! O jogo não pode começar.");
            }
        }
    }
}