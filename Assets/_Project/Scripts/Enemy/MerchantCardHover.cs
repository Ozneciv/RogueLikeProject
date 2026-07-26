using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// =================================================================================
/// INTERAÇÕES VISUAIS E ANIMAÇÃO DE FLUTUAÇÃO DAS CARTAS DE TARÔ
/// =================================================================================
/// Desenvolvido por: Vicenzo (Branch: VicenzoWS)
/// 
/// Funcionalidades Principais Implementadas:
/// 1. Elevação Dinâmica de 22px (liftAmountY) no eixo Y ao passar o cursor do mouse (Hover).
/// 2. Tint de Cor Avermelhado Escuro (#FF6666) na borda e fundo do card no Hover.
/// 3. Oscilação de Flutuação Mística (Wave Float) com onda senoidal orgânica e deslocamento de fase aleatório por carta.
/// 4. Efeitos de Clique (Press/Release Feedback) com transições suaves usando Vector3.Lerp.
/// =================================================================================
/// </summary>
public class MerchantCardHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header(" Configurações de Elevação & Escala no Hover")]
    public float hoverScale = 1.08f;
    public float clickScale = 0.95f;
    [Tooltip("Distância que a carta sobe ao passar o mouse por cima")]
    public float liftAmountY = 22f; 
    public float transitionSpeed = 14f;

    [Header(" Flutuação Mística em Repouso (Wave Float)")]
    public bool enableFloating = true;
    [Tooltip("Altura da oscilação (subindo e descendo)")]
    public float floatAmplitude = 6.0f;
    [Tooltip("Velocidade da oscilação suave")]
    public float floatSpeed = 2.2f;
    private float floatPhase;

    [Header(" Tint Avermelhada no Hover")]
    public bool enableColorChange = true;
    public Color normalBorderColor = Color.white; // Preserva a cor original do Canvas
    public Color hoverBorderColor = new Color(1.0f, 0.40f, 0.40f, 1.0f); // Leve tom avermelhado rubi

    private Vector3 initialScale;
    private Vector3 initialPosition;
    private Vector3 targetScale;
    private Vector3 targetPositionOffset;

    private Image borderImage;
    private bool isHovered = false;
    private bool isPressed = false;

    void Awake()
    {
        initialScale = transform.localScale;
        initialPosition = transform.localPosition;
        targetScale = initialScale;
        targetPositionOffset = Vector3.zero;

        // Fase aleatória para que cada carta flutue de forma desincronizada e orgânica
        floatPhase = Random.Range(0f, Mathf.PI * 2f);

        borderImage = GetComponent<Image>();
    }

    void OnEnable()
    {
        ResetToNormal();
    }

    public void ResetToNormal()
    {
        isHovered = false;
        isPressed = false;
        targetScale = initialScale;
        targetPositionOffset = Vector3.zero;
        transform.localScale = initialScale;
        transform.localPosition = initialPosition;

        if (enableColorChange && borderImage != null)
        {
            borderImage.color = normalBorderColor;
        }
    }

    void Update()
    {
        // 1. Calcula a flutuação mística sinusoidal (subindo e descendo suavemente)
        float floatOffsetY = (enableFloating && !isPressed) ? Mathf.Sin(Time.unscaledTime * floatSpeed + floatPhase) * floatAmplitude : 0f;

        // 2. Animação de lerp fluida para posição (elevação + flutuação) e escala
        Vector3 targetPos = initialPosition + targetPositionOffset + new Vector3(0f, floatOffsetY, 0f);
        transform.localPosition = Vector3.Lerp(transform.localPosition, targetPos, Time.unscaledDeltaTime * transitionSpeed);
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.unscaledDeltaTime * transitionSpeed);

        // 3. Animação de transição de cor (Levemente avermelhada no hover)
        if (enableColorChange && borderImage != null)
        {
            Color currentTargetColor = isHovered ? hoverBorderColor : normalBorderColor;
            borderImage.color = Color.Lerp(borderImage.color, currentTargetColor, Time.unscaledDeltaTime * transitionSpeed);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        if (!isPressed)
        {
            targetScale = initialScale * hoverScale;
            targetPositionOffset = new Vector3(0f, liftAmountY, 0f);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        isPressed = false;
        targetScale = initialScale;
        targetPositionOffset = Vector3.zero;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isPressed = true;
        targetScale = initialScale * clickScale;
        targetPositionOffset = Vector3.zero;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isPressed = false;
        if (isHovered)
        {
            targetScale = initialScale * hoverScale;
            targetPositionOffset = new Vector3(0f, liftAmountY, 0f);
        }
        else
        {
            targetScale = initialScale;
            targetPositionOffset = Vector3.zero;
        }
    }
}
