using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Efeito de Animação no Título do Menu Principal:
/// 1. Pulso de escala e brilho imediato + periódico na logo.
/// 2. Brilho/Reflexo metálico (Shine Beam Sweep) passando estritamente DENTRO dos limites e contornos da fonte.
/// </summary>
public class TitleGlowShineEffect : MonoBehaviour
{
    [Header("Configurações do Pulso de Glow")]
    public Image titleImage;
    [Tooltip("Intervalo entre cada pulso de brilho (em segundos)")]
    public float pulseInterval = 3.0f;
    [Tooltip("Intensidade do brilho no pico")]
    public float glowMultipler = 1.6f;
    public float scalePulseAmount = 1.04f;
    public Color glowTint = new Color(1.0f, 0.98f, 0.85f, 1f);

    [Header("Reflexo / Sheen Passante (Dentro das Letras)")]
    public bool enableShineSweep = true;
    public Color shineColor = new Color(1f, 1f, 1f, 0.75f);
    public float sweepDuration = 0.75f;

    private Color baseColor = Color.white;
    private Vector3 baseScale = Vector3.one;
    private RectTransform shineBeamRt;
    private Image shineBeamImg;

    private void Start()
    {
        if (titleImage == null) titleImage = GetComponent<Image>();
        if (titleImage != null)
        {
            baseColor = titleImage.color;
            baseScale = titleImage.rectTransform.localScale;

            // Adiciona a Mask oficial da Unity para restringir qualquer filho (brilho) estritamente DENTRO dos contornos da fonte
            Mask maskComp = titleImage.GetComponent<Mask>();
            if (maskComp == null)
            {
                maskComp = titleImage.gameObject.AddComponent<Mask>();
                maskComp.showMaskGraphic = true;
            }

            // Remove RectMask2D se existir para não dar conflito com a Mask Alpha
            RectMask2D rectMask = titleImage.GetComponent<RectMask2D>();
            if (rectMask != null) Destroy(rectMask);
        }

        if (enableShineSweep && titleImage != null)
        {
            CreateShineBeam();
        }

        StartCoroutine(GlowPulseLoop());
    }

    private void CreateShineBeam()
    {
        GameObject shineGo = new GameObject("TitleShineBeam");
        shineGo.transform.SetParent(titleImage.transform, false);

        shineBeamRt = shineGo.AddComponent<RectTransform>();
        shineBeamRt.anchorMin = new Vector2(0f, 0f);
        shineBeamRt.anchorMax = new Vector2(0f, 1f);
        shineBeamRt.pivot = new Vector2(0.5f, 0.5f);
        shineBeamRt.sizeDelta = new Vector2(60f, 0f);
        shineBeamRt.anchoredPosition = new Vector2(-400f, 0f);
        shineBeamRt.localEulerAngles = new Vector3(0f, 0f, -25f); // Inclinado

        shineBeamImg = shineGo.AddComponent<Image>();
        shineBeamImg.color = shineColor;
        shineBeamImg.raycastTarget = false;
        shineGo.SetActive(false);
    }

    private IEnumerator GlowPulseLoop()
    {
        yield return new WaitForSeconds(0.3f);

        while (true)
        {
            // 1. Dispara o brilho passante DENTRO dos contornos das letras
            if (enableShineSweep && shineBeamRt != null && titleImage != null)
            {
                StartCoroutine(AnimateShineSweep());
            }

            // 2. Pulso de brilho e escala no título
            float elapsed = 0f;
            float halfDuration = 0.45f;

            // Fade In + Scale Up
            while (elapsed < halfDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / halfDuration;
                float smoothT = Mathf.Sin(t * Mathf.PI * 0.5f);
                if (titleImage != null)
                {
                    titleImage.color = Color.Lerp(baseColor, glowTint * glowMultipler, smoothT);
                    titleImage.rectTransform.localScale = Vector3.Lerp(baseScale, baseScale * scalePulseAmount, smoothT);
                }
                yield return null;
            }

            elapsed = 0f;
            // Fade Out + Scale Down
            while (elapsed < halfDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / halfDuration;
                float smoothT = Mathf.Sin((1f - t) * Mathf.PI * 0.5f);
                if (titleImage != null)
                {
                    titleImage.color = Color.Lerp(baseColor, glowTint * glowMultipler, smoothT);
                    titleImage.rectTransform.localScale = Vector3.Lerp(baseScale, baseScale * scalePulseAmount, smoothT);
                }
                yield return null;
            }

            if (titleImage != null)
            {
                titleImage.color = baseColor;
                titleImage.rectTransform.localScale = baseScale;
            }

            yield return new WaitForSeconds(pulseInterval);
        }
    }

    private IEnumerator AnimateShineSweep()
    {
        if (shineBeamRt == null || titleImage == null) yield break;

        shineBeamRt.gameObject.SetActive(true);
        float width = titleImage.rectTransform.rect.width;
        float startX = -width * 0.8f;
        float endX = width * 0.8f;
        float elapsed = 0f;

        while (elapsed < sweepDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / sweepDuration;
            float posX = Mathf.Lerp(startX, endX, t);
            shineBeamRt.anchoredPosition = new Vector2(posX, 0f);
            yield return null;
        }

        shineBeamRt.gameObject.SetActive(false);
    }
}
