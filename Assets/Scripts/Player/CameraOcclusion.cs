using UnityEngine;

/// <summary>
/// Detecta objetos da Layer "Boundary" entre a câmera e o jogador (Tag "Player")
/// e oculta/restaura o MeshRenderer deles para evitar obstrução da visão.
/// Anexe este script à Main Camera.
/// </summary>
public class CameraOcclusion : MonoBehaviour
{
    [Header("Configurações")]
    [Tooltip("Layer dos objetos que podem obstruir a visão (ex.: 'Boundary').")]
    [SerializeField] private LayerMask boundaryLayer;

    // Referência ao Transform do jogador (encontrado pela Tag "Player").
    private Transform playerTransform;

    // Referência ao último objeto que foi ocultado.
    private MeshRenderer hiddenRenderer;

    private void Start()
    {
        // Busca o jogador pela tag "Player".
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");

        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }
        else
        {
            Debug.LogWarning("[CameraOcclusion] Nenhum GameObject com a tag 'Player' foi encontrado na cena!");
        }
    }

    private void Update()
    {
        if (playerTransform == null)
            return;

        // Direção da câmera até o jogador.
        Vector3 directionToPlayer = playerTransform.position - transform.position;
        float distanceToPlayer = directionToPlayer.magnitude;

        RaycastHit hit;
        bool hitSomething = Physics.Raycast(
            transform.position,
            directionToPlayer.normalized,
            out hit,
            distanceToPlayer,
            boundaryLayer
        );

        if (hitSomething)
        {
            MeshRenderer hitRenderer = hit.collider.GetComponent<MeshRenderer>();

            // Só troca visibilidade se o objeto atingido mudou.
            if (hitRenderer != null && hitRenderer != hiddenRenderer)
            {
                // Restaura o objeto anterior (se existir).
                RestoreHidden();

                // Oculta o novo objeto.
                hiddenRenderer = hitRenderer;
                hiddenRenderer.enabled = false;
            }
            // Se hitRenderer == null, o objeto não tem MeshRenderer — ignora.
        }
        else
        {
            // Nenhum objeto da Boundary no caminho — restaura o anterior.
            RestoreHidden();
        }
    }

    /// <summary>
    /// Reativa o MeshRenderer do objeto que estava oculto e limpa a referência.
    /// </summary>
    private void RestoreHidden()
    {
        if (hiddenRenderer != null)
        {
            hiddenRenderer.enabled = true;
            hiddenRenderer = null;
        }
    }
}
