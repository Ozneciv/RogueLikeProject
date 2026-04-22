using UnityEngine;

public class HipOffsetFix : MonoBehaviour
{
    [Tooltip("Ajuste esse valor até o pé ficar perfeito no chão quando estiver no Play")]
    public float fixHeight = 1.0f;

    void LateUpdate()
    {
        // A animação roda no Update e joga os ossos para baixo.
        // O LateUpdate roda DEPOIS, pegando a posição da animação e apenas empurrando pra cima.
        // Isso NÃO vai empilhar ao infinito, porque a animação reseta o osso todo frame.
        transform.localPosition += new Vector3(0, fixHeight, 0);
    }
}
