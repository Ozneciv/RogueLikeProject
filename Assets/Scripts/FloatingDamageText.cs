using UnityEngine;
using TMPro;

public class FloatingDamageText : MonoBehaviour
{
    [Header("Animação")]
    public float lifetime = 1f;
    public float floatSpeed = 1.5f;

    private TextMeshProUGUI textMesh;
    private float timer;
    private Color startColor;

    void Awake()
    {
        textMesh = GetComponentInChildren<TextMeshProUGUI>();
        if (textMesh == null)
        {
            Debug.LogError("FloatingDamageText: Não foi possível encontrar o componente TextMeshProUGUI nos filhos!");
        }
        startColor = textMesh.color;
        timer = lifetime;
    }

    void Update()
    {
        transform.position += Vector3.up * floatSpeed * Time.deltaTime;
        transform.LookAt(transform.position + Camera.main.transform.forward);

        timer -= Time.deltaTime;
        if (timer <= 0)
        {
            Destroy(gameObject);
        }
        else
        {
            textMesh.color = new Color(startColor.r, startColor.g, startColor.b, timer / lifetime);
        }
    }

    // Função pública para que o inimigo possa definir qual número mostrar
    public void SetText(string text)
    {
        // MENSAGEM DE DEBUG AQUI
        Debug.Log("SetText foi chamado! Novo texto deveria ser: " + text);

        if (textMesh == null)
        {
            textMesh = GetComponentInChildren<TextMeshProUGUI>();
        }
        
        if (textMesh != null)
        {
            textMesh.text = text;
        }
        else
        {
            Debug.LogError("SetText falhou porque a referência ao textMesh é NULA!");
        }
    }
}