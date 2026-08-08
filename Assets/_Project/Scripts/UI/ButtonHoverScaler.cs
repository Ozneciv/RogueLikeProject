using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Efeito de Hover e Áudio Responsivo nos Botões do Menu:
/// 1. Aumenta a escala do botão (+12%) ao passar o mouse.
/// 2. Dispara o Efeito Sonoro de Hover (Tick Cristalino).
/// 3. Dispara o Efeito Sonoro de Clique (Impacto Grave).
/// </summary>
public class ButtonHoverScaler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Sprites de Moldura")]
    public Sprite normalSprite;
    public Sprite hoverSprite;

    [Header("Animação de Escala")]
    public float hoverScale = 1.12f;
    public float pressScale = 0.94f;
    public float scaleSpeed = 18f;

    private Vector3 originalScale = Vector3.one;
    private Vector3 targetScale = Vector3.one;
    private Image buttonImage;
    private Button buttonComp;
    private Color originalColor = Color.white;
    private Color hoverColor = new Color(1.15f, 1.15f, 1.25f, 1f);

    private void Awake()
    {
        originalScale = transform.localScale;
        targetScale = originalScale;
        buttonImage = GetComponent<Image>();
        buttonComp = GetComponent<Button>();
        if (buttonImage != null) originalColor = buttonImage.color;
    }

    private void OnEnable()
    {
        transform.localScale = originalScale;
        targetScale = originalScale;
    }

    private void Update()
    {
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.unscaledDeltaTime * scaleSpeed);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (buttonComp != null && !buttonComp.interactable) return;

        targetScale = originalScale * hoverScale;

        if (buttonImage != null)
        {
            if (hoverSprite != null) buttonImage.sprite = hoverSprite;
            buttonImage.color = hoverColor;
        }

        // Toca o som de hover ao passar o mouse
        if (MenuAudioFX.Instance != null)
        {
            MenuAudioFX.Instance.PlayHoverSound();
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetScale = originalScale;

        if (buttonImage != null)
        {
            if (normalSprite != null) buttonImage.sprite = normalSprite;
            buttonImage.color = originalColor;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (buttonComp != null && !buttonComp.interactable) return;

        targetScale = originalScale * pressScale;

        // Toca o som de clique ao pressionar
        if (MenuAudioFX.Instance != null)
        {
            MenuAudioFX.Instance.PlayClickSound();
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (buttonComp != null && !buttonComp.interactable) return;
        targetScale = (eventData.hovered.Contains(gameObject)) ? (originalScale * hoverScale) : originalScale;
    }
}
