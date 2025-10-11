using UnityEngine;

public class Instakill : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // Verifica se o objeto que entrou no trigger tem a tag "Player"
        if (other.CompareTag("Player"))
        {
            // Pega o componente PlayerM do objeto do jogador
            PlayerM player = other.GetComponent<PlayerM>();

            // Se o componente foi encontrado, chama a função Die()
            if (player != null)
            {
                player.Die();

                // --- MUDANÇA AQUI ---
                // Depois de matar o jogador, destrói o objeto da caveira.
                Destroy(gameObject);
            }
        }
    }
}