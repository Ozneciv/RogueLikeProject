using UnityEngine;

/// <summary>
/// Marca um ponto de conexão em um prefab de sala.
///
/// SETUP NO PREFAB:
///   1. Crie um GameObject filho vazio no local exato do socket (borda da sala).
///   2. Adicione este componente nele.
///   3. Defina o PointType: APENAS UM filho por sala deve ser "Entrada".
///      Todos os demais sockets de saída devem ser "Saida".
///   4. Aponte o forward do Transform para FORA da sala (use a seta azul na cena).
///      O AlignRooms usa o forward para encaixar os sockets corretamente.
///   5. Ajuste o floorOffset: distância (positiva) do socket até o chão da sala.
///      Se o socket está exatamente no chão, deixe 0. Se o socket está 1m acima
///      do chão, coloque 1. O Gizmo verde mostra onde o chão será detectado.
///
/// TAGS DE COMPATIBILIDADE:
///   Por padrão, use "Standard". No futuro, use tags diferentes
///   (ex: "Organic", "Narrow") para impedir que estilos incompatíveis se conectem.
/// </summary>
public class ConnectionPoint : MonoBehaviour
{
    public enum PointType { Entrada, Saida }

    [Tooltip("Entrada = este ponto RECEBE conexões de outras salas.\n" +
             "Saida = este ponto GERA novas salas durante a geração procedural.")]
    public PointType pointType = PointType.Saida;

    [Tooltip("Tag de compatibilidade. Dois ConnectionPoints só se conectam se tiverem a mesma tag.\n" +
             "Use 'Standard' para a maioria das salas.")]
    public string connectionTag = "Standard";

    [Tooltip("Distância em metros do socket até o chão desta sala.\n" +
             "0 = socket está no nível do chão.\n" +
             "Valores positivos = socket está acima do chão (mais comum).\n" +
             "O Gizmo verde mostra onde o chão será detectado pelo LevelGenerator.")]
    public float floorOffset = 0f;

    /// <summary>
    /// Retorna a posição Y do chão desta sala no espaço de mundo.
    /// = posição Y do socket - floorOffset.
    /// </summary>
    public float GetFloorWorldY() => transform.position.y - floorOffset;

    /// <summary>
    /// Marcado como true pelo LevelGenerator quando este ponto já foi usado.
    /// Não edite manualmente.
    /// </summary>
    [HideInInspector]
    public bool isOccupied = false;

#if UNITY_EDITOR
    // Gizmo para visualizar o ponto e sua direção na cena do Unity
    void OnDrawGizmos()
    {
        Color gizmoColor = (pointType == PointType.Entrada)
            ? new Color(0f, 0.8f, 1f, 0.9f)   // Azul ciano = Entrada
            : new Color(1f, 0.6f, 0f, 0.9f);  // Laranja = Saída

        Gizmos.color = gizmoColor;
        Gizmos.DrawSphere(transform.position, 0.3f);
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * 1.5f);

        // Linha verde: mostra onde o gerador considera que o chão está
        if (floorOffset != 0f)
        {
            Vector3 floorPos = transform.position - Vector3.up * floorOffset;
            Gizmos.color = new Color(0.2f, 1f, 0.2f, 0.8f);
            Gizmos.DrawSphere(floorPos, 0.2f);
            Gizmos.DrawLine(transform.position, floorPos);
            Gizmos.DrawLine(floorPos + Vector3.right * 0.5f, floorPos - Vector3.right * 0.5f);
            Gizmos.DrawLine(floorPos + Vector3.forward * 0.5f, floorPos - Vector3.forward * 0.5f);
        }
    }
#endif
}
