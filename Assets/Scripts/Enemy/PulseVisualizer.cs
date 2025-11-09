using UnityEngine;
using System.Collections;

public class PulseVisualizer : MonoBehaviour
{
    [Header("Configurações do Pulso")]
    [Tooltip("Duração total da animação do pulso (em segundos).")]
    public float pulseDuration = 0.5f;
    
    // --- MUDANÇA AQUI ---
    [Tooltip("A escala máxima que o pulso atingirá nos eixos X, Y e Z.")]
    public Vector3 maxScale = new Vector3(6f, 0.01f, 6f); // Novo Vector3 em vez de um float

    [Tooltip("A opacidade máxima do visual no início do pulso.")]
    [Range(0, 1)]
    public float maxAlpha = 0.5f;

    private Renderer visualRenderer;
    private Color startColor;

    void Awake()
    {
        visualRenderer = GetComponent<Renderer>();
        if (visualRenderer != null)
        {
            startColor = visualRenderer.material.color;
        }
        gameObject.SetActive(false);
    }

    public void TriggerPulse()
    {
        gameObject.SetActive(true);
        StartCoroutine(PulseAnimation());
    }

    private IEnumerator PulseAnimation()
    {
        float timer = 0f;
        transform.localScale = Vector3.zero;

        while (timer < pulseDuration)
        {
            timer += Time.deltaTime;
            float progress = timer / pulseDuration;

            // --- LÓGICA DE ESCALA ATUALIZADA ---
            // Agora interpola a escala do tamanho 0 até o 'maxScale' customizado.
            transform.localScale = Vector3.Lerp(Vector3.zero, maxScale, progress);

            if (visualRenderer != null)
            {
                float currentAlpha = Mathf.Lerp(maxAlpha, 0f, progress);
                visualRenderer.material.color = new Color(startColor.r, startColor.g, startColor.b, currentAlpha);
            }
            
            yield return null;
        }

        gameObject.SetActive(false);
    }
}