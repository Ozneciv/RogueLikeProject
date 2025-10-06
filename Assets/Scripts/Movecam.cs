using UnityEngine;

public class MoveCam : MonoBehaviour
{
    public Transform playerTransform;
    public float followSpeed = 5f;
    public Vector3 offset;

    private void LateUpdate()
    {
        if (playerTransform != null)
        {
            // Calcula a posição desejada com base na posição do jogador e o deslocamento
            Vector3 desiredPosition = playerTransform.position + offset;

            // Move a câmera de forma suave para a posição desejada
            transform.position = Vector3.Lerp(transform.position, desiredPosition, followSpeed * Time.deltaTime);
        }
    }
}