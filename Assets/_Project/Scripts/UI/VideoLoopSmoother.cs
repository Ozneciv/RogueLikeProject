using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Suaviza a transição em loop do VideoPlayer no menu principal.
/// Aplica um pequeno crossfade suave no final do vídeo para eliminar cortes secos ou travamentos.
/// </summary>
public class VideoLoopSmoother : MonoBehaviour
{
    [Header("Componentes")]
    public VideoPlayer videoPlayer;
    public RawImage displayImage;

    [Header("Suavização do Loop")]
    [Tooltip("Duração do fade suave no ponto de rotação do loop (em segundos)")]
    public float fadeDuration = 0.35f;

    private Coroutine fadeRoutine;
    private Color originalColor = Color.white;

    private void Start()
    {
        if (videoPlayer == null) videoPlayer = GetComponent<VideoPlayer>();
        if (displayImage == null) displayImage = GetComponent<RawImage>();

        if (displayImage != null)
        {
            originalColor = displayImage.color;
        }

        if (videoPlayer != null)
        {
            videoPlayer.isLooping = true;
            videoPlayer.skipOnDrop = true;
            videoPlayer.waitForFirstFrame = true;
            videoPlayer.loopPointReached += OnLoopPointReached;
        }
    }

    private void OnLoopPointReached(VideoPlayer vp)
    {
        if (displayImage != null && gameObject.activeInHierarchy)
        {
            if (fadeRoutine != null) StopCoroutine(fadeRoutine);
            fadeRoutine = StartCoroutine(SmoothLoopFade());
        }
    }

    private IEnumerator SmoothLoopFade()
    {
        float elapsed = 0f;
        // Fade rápido de saída
        while (elapsed < fadeDuration * 0.5f)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / (fadeDuration * 0.5f);
            if (displayImage != null)
            {
                displayImage.color = Color.Lerp(originalColor, new Color(originalColor.r, originalColor.g, originalColor.b, 0.75f), t);
            }
            yield return null;
        }

        elapsed = 0f;
        // Fade rápido de entrada
        while (elapsed < fadeDuration * 0.5f)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / (fadeDuration * 0.5f);
            if (displayImage != null)
            {
                displayImage.color = Color.Lerp(new Color(originalColor.r, originalColor.g, originalColor.b, 0.75f), originalColor, t);
            }
            yield return null;
        }

        if (displayImage != null) displayImage.color = originalColor;
    }
}
