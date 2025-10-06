using UnityEngine;
using TMPro;
using System.Collections;

public class DummyHealth : MonoBehaviour
{
    public TextMeshProUGUI damageText;
    public float displayTime = 0.5f;

    [Header("Visual Feedback")]
    public Color hitColor = Color.red; // Cor que o dummy terá ao ser atingido
    public float hitFlashTime = 0.2f; // Tempo que ele fica vermelho
    private Color originalColor;
    private Renderer dummyRenderer;

    private void Start()
    {
        // Pega o componente Renderer e a cor original no início do jogo
        dummyRenderer = GetComponent<Renderer>();
        if (dummyRenderer != null)
        {
            originalColor = dummyRenderer.material.color;
        }

        if (damageText != null)
        {
            damageText.text = "";
        }
    }

    public void TakeDamage(int damage)
    {
        Debug.Log("Dummy recebeu " + damage + " de dano.");

        if (damageText != null)
        {
            damageText.text = damage.ToString();
            Invoke("ClearDamageText", displayTime);
        }

        if (dummyRenderer != null)
        {
            // Muda a cor para vermelho
            dummyRenderer.material.color = hitColor;
            // Depois de um tempo, invoca a função para voltar à cor original
            Invoke("ResetColor", hitFlashTime);
        }
    }

    private void ClearDamageText()
    {
        if (damageText != null)
        {
            damageText.text = "";
        }
    }

    private void ResetColor()
    {
        // Volta para a cor original
        if (dummyRenderer != null)
        {
            dummyRenderer.material.color = originalColor;
        }
    }
}