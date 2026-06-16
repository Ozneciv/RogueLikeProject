using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Impede que a entidade (inimigo, NPC, etc.) saia dos limites do NavMesh.
///
/// COMO FUNCIONA:
///   A cada FixedUpdate, verifica se a posição atual está sobre o NavMesh.
///   Se NÃO estiver (caiu da borda), puxa de volta para o ponto válido mais próximo.
///   Funciona com Rigidbody, transform.position direto, ou qualquer sistema de movimento.
///
/// SETUP:
///   1. Adicione este componente no prefab do inimigo (ao lado do script de IA).
///   2. Pronto. Não precisa configurar nada — os defaults funcionam.
///
/// NOTAS:
///   • NÃO requer NavMeshAgent — usa apenas NavMesh.SamplePosition (leitura pura).
///   • O NavMesh precisa estar assado (o LevelGenerator faz isso em runtime).
///   • Se o NavMesh ainda não existir quando o inimigo spawnar, o script
///     aguarda silenciosamente até que ele esteja disponível.
/// </summary>
public class NavMeshBoundaryConstraint : MonoBehaviour
{
    [Header("Configuração")]
    [Tooltip("Raio máximo de busca pelo ponto válido mais próximo no NavMesh. " +
             "Aumente se inimigos spawnarem muito longe da malha.")]
    public float searchRadius = 5f;

    [Tooltip("Distância mínima da borda do NavMesh para considerar 'fora dos limites'. " +
             "Valores maiores fazem o inimigo ser puxado antes de sair completamente.")]
    public float maxDistanceFromNavMesh = 1.5f;

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        NavMeshHit hit;
        // Busca o ponto válido mais próximo no NavMesh
        bool foundValidPoint = NavMesh.SamplePosition(
            transform.position, 
            out hit, 
            searchRadius, 
            NavMesh.AllAreas
        );

        if (foundValidPoint)
        {
            // Calcula a distância apenas no plano horizontal (X e Z)
            // Isso permite que inimigos voadores ou pulando não fiquem presos
            Vector2 entityPos = new Vector2(transform.position.x, transform.position.z);
            Vector2 navMeshPos = new Vector2(hit.position.x, hit.position.z);
            float horizontalDistance = Vector2.Distance(entityPos, navMeshPos);

            if (horizontalDistance <= maxDistanceFromNavMesh)
            {
                // Se o inimigo caiu abaixo do NavMesh (atravessou o chão), teleporta ele de volta para cima
                if (transform.position.y < hit.position.y - 0.5f)
                {
                    Vector3 newPos = transform.position;
                    newPos.y = hit.position.y + 0.1f;

                    if (rb != null && !rb.isKinematic)
                    {
                        rb.position = newPos;
                        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
                    }
                    else
                    {
                        transform.position = newPos;
                    }
                }
                return;
            }

            // Se chegou aqui, o inimigo está fora dos limites horizontais. Puxa de volta para a borda.
            Vector3 correctedPos = hit.position;

            if (rb != null && !rb.isKinematic)
            {
                // Teleporta a posição horizontal para a borda mantendo a física Y intacta (sem travar a gravidade)
                Vector3 newPos = new Vector3(correctedPos.x, rb.position.y, correctedPos.z);
                
                // Se estiver abaixo da malha, força a altura do NavMesh para trazê-lo de volta
                if (rb.position.y < correctedPos.y)
                {
                    newPos.y = correctedPos.y + 0.1f;
                    rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
                }

                rb.position = newPos;
                // Zera apenas a velocidade horizontal para parar o momentum de queda para fora do mapa
                rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            }
            else
            {
                Vector3 newPos = correctedPos;
                if (transform.position.y >= correctedPos.y)
                {
                    newPos.y = transform.position.y;
                }
                else
                {
                    newPos.y = correctedPos.y + 0.1f;
                }
                transform.position = newPos;
            }
        }
    }
}
