using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Script de Correção e Estilização Automática das Fontes e Hover dos Botões do Menu Principal.
/// 1. Garante a existência do EventSystem e GraphicRaycaster para os cliques e hovers funcionarem.
/// 2. Aplica a fonte estilizada Oswald Bold SDF do projeto.
/// 3. Força a cor para Branco Puro (#FFFFFF) com efeito Negrito e alinhamento central.
/// 4. Adiciona o componente ButtonHoverScaler para garantir animação de Hover (escala + troca de sprite).
/// </summary>
public class FixButtonFonts : MonoBehaviour
{
    [Header("Configuração de Fonte")]
    public TMP_FontAsset fontAsset;
    public Color textColor = Color.white;
    public float fontSize = 24f;

    [Header("Sprites de Hover (Opcional)")]
    public Sprite normalFrameSprite;
    public Sprite hoverFrameSprite;

    private void Awake()
    {
        EnsureEventSystemAndRaycaster();
        ApplyFontFixes();
    }

    private void Start()
    {
        EnsureEventSystemAndRaycaster();
        ApplyFontFixes();
    }

    private void EnsureEventSystemAndRaycaster()
    {
        // 1. Garante que existe um EventSystem ativo na cena para capturar o passar do mouse (Hover)
        if (Object.FindFirstObjectByType<EventSystem>() == null)
        {
            GameObject esGo = new GameObject("EventSystem");
            esGo.AddComponent<EventSystem>();
            esGo.AddComponent<StandaloneInputModule>();
            Debug.Log("[FIX BUTTONS] EventSystem criado automaticamente na cena.");
        }

        // 2. Garante GraphicRaycaster no Canvas pai
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null && canvas.GetComponent<GraphicRaycaster>() == null)
        {
            canvas.gameObject.AddComponent<GraphicRaycaster>();
        }
    }

    public void ApplyFontFixes()
    {
        // Tenta carregar a fonte padrão do projeto Oswald Bold SDF se não atribuída
        if (fontAsset == null)
        {
            fontAsset = Resources.Load<TMP_FontAsset>("Fonts & Materials/Oswald Bold SDF");
        }

        Button[] buttons = GetComponentsInChildren<Button>(true);
        foreach (Button btn in buttons)
        {
            if (btn == null) continue;

            // Corrige o Target Graphic do botão para ser a Image da moldura (não o texto!)
            Image btnImg = btn.GetComponent<Image>();
            if (btnImg != null)
            {
                btn.targetGraphic = btnImg;
                btnImg.raycastTarget = true; // Garante que a imagem escuta o ponteiro do mouse
            }

            // Adiciona o componente de Hover com animação de escala garantida
            ButtonHoverScaler hoverComp = btn.GetComponent<ButtonHoverScaler>();
            if (hoverComp == null)
            {
                hoverComp = btn.gameObject.AddComponent<ButtonHoverScaler>();
            }

            if (normalFrameSprite != null) hoverComp.normalSprite = normalFrameSprite;
            if (hoverFrameSprite != null) hoverComp.hoverSprite = hoverFrameSprite;

            // Estiliza o Texto TextMeshPro do botão
            TextMeshProUGUI tmpText = btn.GetComponentInChildren<TextMeshProUGUI>(true);
            if (tmpText != null)
            {
                if (fontAsset != null)
                {
                    tmpText.font = fontAsset;
                }

                tmpText.color = textColor;
                tmpText.fontSize = fontSize;
                tmpText.fontStyle = FontStyles.Bold;
                tmpText.alignment = TextAlignmentOptions.Center;
                tmpText.enableWordWrapping = false;
                tmpText.overflowMode = TextOverflowModes.Overflow;
                tmpText.raycastTarget = false; // Impede que o texto bloqueie o raio do mouse na imagem

                // Ativa a sombra/underlay escuro no TMP para legibilidade
                if (tmpText.fontMaterial != null)
                {
                    tmpText.fontMaterial.EnableKeyword("UNDERLAY_ON");
                    tmpText.fontMaterial.SetColor("_UnderlayColor", new Color(0f, 0f, 0f, 0.85f));
                    tmpText.fontMaterial.SetFloat("_UnderlayOffsetX", 1f);
                    tmpText.fontMaterial.SetFloat("_UnderlayOffsetY", -1f);
                    tmpText.fontMaterial.SetFloat("_UnderlaySoftness", 0.2f);
                }
            }
        }

        Debug.Log($"[FIX BUTTON FONTS] Hover e fontes brancas configurados com sucesso em {buttons.Length} botão(ões).");
    }
}
