using UnityEngine;

/// <summary>
/// Script de Teste Isolado para o Bismutado.
/// Permite testar as animações do Bismutado em qualquer cena de teste usando atalhos:
///   - [W] ou [Seta para Cima] : Alterna Caminhada em Tripé (isWalking)
///   - [Espaço] ou [Clique do Mouse] : Dispara Golpe de Esmagamento com a Lança Rosa
/// </summary>
public class BismutadoAnimationTester : MonoBehaviour
{
    [Header("Deslocamento no Teste")]
    public bool enableForwardMovement = true;
    public float moveSpeed = 3.0f;

    private BismutadoProceduralAnimation animScript;

    void Awake()
    {
        animScript = GetComponent<BismutadoProceduralAnimation>();
        if (animScript == null) animScript = GetComponentInChildren<BismutadoProceduralAnimation>();
    }

    void Update()
    {
        if (animScript == null) return;

        // Tecla W ou Seta para Cima alterna a caminhada das 3 pernas
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
        {
            animScript.isWalking = !animScript.isWalking;
            Debug.Log($"🤖 [TESTE BISMUTADO] Caminhada de Tripé (isWalking): {animScript.isWalking}");
        }

        // Se a caminhada estiver ativa, desloca o Bismutado para a frente no espaço
        if (animScript.isWalking && enableForwardMovement && !animScript.isAttacking)
        {
            transform.Translate(Vector3.forward * moveSpeed * Time.deltaTime);
        }

        // Tecla Espaço ou Clique no Mouse dispara o ataque de esmagamento com a lança
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
        {
            animScript.TriggerCrystalSlam();
            Debug.Log("⚔️ [TESTE BISMUTADO] Golpe com a Lança de Cristal ativado!");
        }
    }
}
