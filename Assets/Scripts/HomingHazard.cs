using UnityEngine;

public class HomingHazard : MonoBehaviour
{
    [Tooltip("A velocidade com que a caveira persegue o jogador.")]
    public float moveSpeed = 2.5f;

    private Transform playerTransform;

    void Start()
    {
        // Encontra o jogador ao ser criada
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
        else
        {
            Debug.LogError("HomingHazard: Não foi possível encontrar o jogador! Verifique a tag 'Player'.");
            this.enabled = false; // Desativa o script se não encontrar o jogador
        }
    }

    void Update()
    {
        if (playerTransform != null)
        {
            // Calcula a direção para o jogador
            Vector3 direction = (playerTransform.position - transform.position).normalized;

            // Move a caveira naquela direção
            transform.position += direction * moveSpeed * Time.deltaTime;

            // Opcional: Faz a caveira "olhar" para o jogador
            transform.LookAt(playerTransform);
        }
    }
}