using UnityEngine;
using TMPro;
using System.Collections;

public class DummyHealth : MonoBehaviour
{
    // --- MUDANÇA 1: Remover referências antigas e adicionar a do prefab ---
    // public TextMeshProUGUI damageText; // Não precisamos mais disso
    public GameObject floatingDamageTextPrefab; // Arraste o seu novo prefab aqui
    public Vector3 textOffset = new Vector3(0, 2f, 0); // Ajuste a altura em que o texto aparece

    [Header("Visual Feedback")]
    public Color hitColor = Color.red;
    public float hitFlashTime = 0.2f;
    private Color originalColor;
    private Renderer dummyRenderer;

    private void Start()
    {
        dummyRenderer = GetComponent<Renderer>();
        if (dummyRenderer != null)
        {
            originalColor = dummyRenderer.material.color;
        }
    }

    public void TakeDamage(int damage)
    {
        Debug.Log(gameObject.name + " recebeu " + damage + " de dano.");

        // --- MUDANÇA 2: Lógica para instanciar o texto flutuante ---
        if (floatingDamageTextPrefab != null)
        {
            // Cria uma instância do prefab na posição do inimigo + um deslocamento (offset)
            GameObject textObject = Instantiate(floatingDamageTextPrefab, transform.position + textOffset, Quaternion.identity);
            
            // Pega o script do objeto recém-criado e define o texto
            textObject.GetComponent<FloatingDamageText>().SetText(damage.ToString());
        }

        // Lógica do flash vermelho continua a mesma
        if (dummyRenderer != null)
        {
            dummyRenderer.material.color = hitColor;
            Invoke("ResetColor", hitFlashTime);
        }
    }

    private void ResetColor()
    {
        if (dummyRenderer != null)
        {
            dummyRenderer.material.color = originalColor;
        }
    }
}