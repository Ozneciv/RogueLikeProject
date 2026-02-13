using UnityEngine;
using TMPro;
using System.Collections;

public class FloatingDamageText : MonoBehaviour
{
    // --- MUDANÇA PRINCIPAL AQUI ---
    [Header("Referências")]
    [Tooltip("Arraste o objeto de texto (filho) que tem o TextMeshProUGUI aqui.")]
    public TextMeshProUGUI textMesh; // Agora é público!

    [Header("Animação")]
    public float lifetime = 1f;
    public float floatSpeed = 1.5f;
    
    [Header("Critical Hit Settings")]
    [Tooltip("Cor do texto para dano crítico")]
    public Color criticalColor = Color.red;
    
    [Tooltip("Multiplicador de tamanho para críticos")]
    [Range(1f, 3f)]
    public float criticalSizeMultiplier = 1.5f;
    
    [Tooltip("Multiplicador de duração para críticos")]
    [Range(1f, 3f)]
    public float criticalLifetimeMultiplier = 1.5f;

    private float timer;
    private Color startColor;
    private float baseFontSize;

    void Awake()
    {
        // Agora, em vez de procurar, apenas checamos se a referência foi arrastada no Inspector.
        if (textMesh == null)
        {
            Debug.LogError("FloatingDamageText: A referência para o 'textMesh' NÃO FOI ARRASTADA no Inspector!", this.gameObject);
            return; // Para o script se a referência estiver faltando.
        }
        
        startColor = textMesh.color;
        baseFontSize = textMesh.fontSize;
        timer = lifetime;
    }

    void Update()
    {
        if (textMesh == null) return; // Não faz nada se a referência estiver quebrada

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

    public void SetText(string text)
    {
        if (textMesh == null)
        {
            Debug.LogError("SetText falhou porque a referência ao textMesh é NULA!", this.gameObject);
            return;
        }
        textMesh.text = text;
    }
    
    /// <summary>
    /// Configura o texto como dano crítico com visual aprimorado.
    /// </summary>
    public void SetCritical(bool isCritical)
    {
        if (textMesh == null) return;
        
        if (isCritical)
        {
            // Aplicar cor vermelha
            textMesh.color = criticalColor;
            startColor = criticalColor;
            
            // Aumentar tamanho da fonte
            textMesh.fontSize = baseFontSize * criticalSizeMultiplier;
            
            // Aumentar duração
            lifetime *= criticalLifetimeMultiplier;
            timer = lifetime;
        }
    }

}