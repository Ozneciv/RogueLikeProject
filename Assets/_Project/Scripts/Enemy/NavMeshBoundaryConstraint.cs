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
        // Tenta encontrar o ponto mais próximo no NavMesh
        NavMeshHit hit;
        bool isOnNavMesh = NavMesh.SamplePosition(
            transform.position, 
            out hit, 
            maxDistanceFromNavMesh, 
            NavMesh.AllAreas
        );

        if (isOnNavMesh)
        {
            // Está dentro (ou muito perto) do NavMesh — tudo OK
            return;
        }

        // Está FORA do NavMesh — busca o ponto válido mais próximo com raio maior
        bool foundValidPoint = NavMesh.SamplePosition(
            transform.position, 
            out hit, 
            searchRadius, 
            NavMesh.AllAreas
        );

        if (foundValidPoint)
        {
            // Puxa de volta para o NavMesh
            Vector3 correctedPos = hit.position;

            // Preserva a altura Y atual para não interferir com gravidade/voo
            // (a menos que a diferença seja muito grande, indicando queda no void)
            if (Mathf.Abs(transform.position.y - correctedPos.y) < 3f)
            {
                correctedPos.y = transform.position.y;
            }

            if (rb != null && !rb.isKinematic)
            {
                // Com Rigidbody dinâmico: move via Rigidbody para não quebrar a física
                rb.MovePosition(correctedPos);
                // Zera velocidade para evitar que o momentum empurre de volta para fora
                rb.linearVelocity = Vector3.zero;
            }
            else
            {
                // Sem Rigidbody ou kinematic: move direto via transform
                transform.position = correctedPos;
            }
        }
        // Se não encontrou ponto válido (NavMesh não existe ainda), não faz nada
    }
}
