using UnityEngine;

public class ItemFloat : MonoBehaviour
{
    [Header("Configurações de Flutuação")]
    [Tooltip("A altura que o objeto vai subir e descer a partir do ponto inicial.")]
    public float floatHeight = 0.25f;

    [Tooltip("A velocidade com que o objeto sobe e desce.")]
    public float floatSpeed = 1f;

    [Header("Configurações de Rotação (Opcional)")]
    [Tooltip("A velocidade com que o objeto gira. Deixe em 0 para não girar.")]
    public float rotationSpeed = 15f;
    
    // --- MUDANÇA AQUI ---
    [Tooltip("O eixo em torno do qual o objeto vai girar (X, Y, Z). (0, 1, 0) para girar como um pião.")]
    public Vector3 rotationAxis = Vector3.up;

    // Posição inicial do objeto para referência
    private Vector3 startPosition;

    void Start()
    {
        // Guarda a posição inicial do objeto no momento em que o jogo começa.
        startPosition = transform.position;
    }

    void Update()
    {
        // --- Lógica de Flutuação (Sobe e Desce) ---
        float newY = startPosition.y + Mathf.Sin(Time.time * floatSpeed) * floatHeight;
        transform.position = new Vector3(startPosition.x, newY, startPosition.z);

        // --- Lógica de Rotação (Opcional) ---
        if (rotationSpeed > 0)
        {
            // Agora usa o 'rotationAxis' customizável em vez de um valor fixo.
            transform.Rotate(rotationAxis, rotationSpeed * Time.deltaTime);
        }
    }
}