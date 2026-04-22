using UnityEngine;
using TMPro;

/// <summary>
/// Exibe o status da Ultimate na tela (cooldown, pronto, ativo).
/// </summary>
public class UltimateUI : MonoBehaviour
{
    [Header("References")]
    public PlayerUltimate ultimateScript;
    
    [Header("UI Settings")]
    [Tooltip("Mostrar UI da Ultimate")]
    public bool showUI = true;
    
    [Tooltip("Posição X na tela")]
    public float positionX = -10f;
    
    [Tooltip("Posição Y na tela")]
    public float positionY = -10f;
    
    [Tooltip("Tamanho da fonte")]
    [Range(14f, 40f)]
    public float fontSize = 24f;
    
    private TextMeshProUGUI ultimateText;
    private GameObject uiPanel;
    
    void Start()
    {
        if (ultimateScript == null)
        {
            ultimateScript = GetComponent<PlayerUltimate>();
        }
        
        if (ultimateScript == null)
        {
            Debug.LogWarning("UltimateUI: PlayerUltimate não encontrado!");
            return;
        }
        
        CreateUI();
    }
    
    void Update()
    {
        if (!showUI || ultimateText == null || ultimateScript == null) return;
        
        UpdateUI();
    }
    
    void CreateUI()
    {
        // Criar Canvas dedicado
        GameObject canvasObj = new GameObject("UltimateUICanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 99;
        
        UnityEngine.UI.CanvasScaler scaler = canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        
        // Criar painel
        uiPanel = new GameObject("UltimatePanel");
        uiPanel.transform.SetParent(canvas.transform, false);
        
        RectTransform panelRect = uiPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1, 1);
        panelRect.anchorMax = new Vector2(1, 1);
        panelRect.pivot = new Vector2(1, 1);
        panelRect.anchoredPosition = new Vector2(positionX, positionY);
        panelRect.sizeDelta = new Vector2(300, 80);
        
        // Background
        UnityEngine.UI.Image bgImage = uiPanel.AddComponent<UnityEngine.UI.Image>();
        bgImage.color = new Color(0, 0, 0, 0.8f);
        
        // Texto
        GameObject textObj = new GameObject("UltimateText");
        textObj.transform.SetParent(uiPanel.transform, false);
        
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(10, 10);
        textRect.offsetMax = new Vector2(-10, -10);
        
        ultimateText = textObj.AddComponent<TextMeshProUGUI>();
        ultimateText.fontSize = fontSize;
        ultimateText.alignment = TextAlignmentOptions.Center;
        ultimateText.color = Color.white;
        ultimateText.font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        
        uiPanel.SetActive(showUI);
    }
    
    void UpdateUI()
    {
        if (ultimateScript.IsUltimateActive())
        {
            // Ultimate ativa
            ultimateText.text = "<color=#FFD700>💥 ULTIMATE ATIVA! 💥</color>";
        }
        else if (ultimateScript.IsUltimateReady())
        {
            // Pronta para usar
            ultimateText.text = "<color=#00FF00>⚡ ULTIMATE PRONTA!\nPressione [U]</color>";
        }
        else
        {
            // Em cooldown
            float cooldown = ultimateScript.GetCooldownRemaining();
            ultimateText.text = $"<color=#FF6B6B>⏱️ COOLDOWN\n{cooldown:F1}s</color>";
        }
        
        // Atualizar posição e tamanho do Inspector
        if (uiPanel != null)
        {
            RectTransform panelRect = uiPanel.GetComponent<RectTransform>();
            if (panelRect != null)
            {
                panelRect.anchoredPosition = new Vector2(positionX, positionY);
            }
        }
        
        if (ultimateText != null)
        {
            ultimateText.fontSize = fontSize;
        }
    }
}
