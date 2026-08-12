using UnityEngine;
using System.Collections;

/// <summary>
/// Componente do prefab de sangue ácido que o Boss pinga durante a invisibilidade.
/// Possui animação de nascimento (cresce) e desvanecimento suave (fade-out + shrink).
/// </summary>
public class ToxicBlood : MonoBehaviour
{
    [Tooltip("Tempo em segundos que a poça fica ativa no chão antes de desvanecer.")]
    public float lifetime = 3.5f;

    [Tooltip("Duração da animação de desvanecimento no final do lifetime.")]
    public float fadeDuration = 0.8f;

    private Renderer bloodRenderer;
    private Material bloodMaterial;
    private Vector3 initialScale;
    private Color initialColor;

    void Start()
    {
        bloodRenderer = GetComponent<Renderer>();
        if (bloodRenderer == null) bloodRenderer = GetComponentInChildren<Renderer>();

        if (bloodRenderer != null)
        {
            bloodMaterial = bloodRenderer.material;
            initialColor = bloodMaterial.color;
        }

        initialScale = transform.localScale;
        StartCoroutine(BloodLifecycleRoutine());
    }

    private IEnumerator BloodLifecycleRoutine()
    {
        // 1. Efeito de Nascimento (Cresce do zero)
        float spawnElapsed = 0f;
        float spawnDuration = 0.25f;
        while (spawnElapsed < spawnDuration)
        {
            spawnElapsed += Time.deltaTime;
            float t = spawnElapsed / spawnDuration;
            transform.localScale = Vector3.Lerp(Vector3.zero, initialScale, t);
            yield return null;
        }
        transform.localScale = initialScale;

        // 2. Permanece ativo no chão
        float activeTime = Mathf.Max(0.1f, lifetime - fadeDuration);
        yield return new WaitForSeconds(activeTime);

        // 3. Fade-out suave com Shrink
        float fadeElapsed = 0f;
        while (fadeElapsed < fadeDuration)
        {
            fadeElapsed += Time.deltaTime;
            float t = fadeElapsed / fadeDuration;

            // Encolhe o tamanho
            transform.localScale = Vector3.Lerp(initialScale, Vector3.zero, t);

            // Desvanece a opacidade
            if (bloodMaterial != null)
            {
                Color c = initialColor;
                c.a = Mathf.Lerp(initialColor.a, 0f, t);
                bloodMaterial.color = c;
            }

            yield return null;
        }

        Destroy(gameObject);
    }
}
