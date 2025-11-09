using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class HomingHazard : MonoBehaviour
{
    [Tooltip("A velocidade com que a caveira persegue o jogador.")]
    public float moveSpeed = 2.5f;

    private Transform playerTransform;
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            // --- MUDANÇA PRINCIPAL AQUI ---
            // Procura por um filho chamado "TorsoTarget" dentro do jogador.
            Transform targetPoint = player.transform.Find("TorsoTarget");

            if (targetPoint != null)
            {
                // Se encontrou, define o TorsoTarget como o alvo.
                playerTransform = targetPoint;
            }
            else
            {
                // Se não encontrou (por segurança), usa o jogador principal como alvo.
                Debug.LogWarning("Não foi encontrado um 'TorsoTarget' no jogador. A caveira mirará nos pés.");
                playerTransform = player.transform;
            }
        }
        else
        {
            Debug.LogError("HomingHazard: Não foi possível encontrar o jogador! Verifique a tag 'Player'.");
            this.enabled = false;
        }
    }

    void FixedUpdate()
    {
        if (playerTransform != null)
        {
            // A lógica de movimento agora usa a posição do TorsoTarget.
            Vector3 targetPosition = new Vector3(playerTransform.position.x, transform.position.y, playerTransform.position.z);
            Vector3 direction = (targetPosition - transform.position).normalized;

            rb.linearVelocity = direction * moveSpeed;
            
            // A lógica de rotação também usa a posição do TorsoTarget.
            transform.LookAt(playerTransform);
        }
    }
}