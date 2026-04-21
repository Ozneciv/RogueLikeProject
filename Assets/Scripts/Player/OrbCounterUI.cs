using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Contador de Orbs (Essência) coletados.
/// Estilizado para combinar com a HUD existente (barras de vida e armadura).
/// Adicione este script ao "Player Canvas Real".
/// </summary>
public class OrbCounterUI : MonoBehaviour
{
    [Header("Configuração Visual")]
    [Tooltip("Tamanho da fonte do contador")]
    public int fontSize = 32;

    [Tooltip("Cor do texto do número (roxo claro)")]
    public Color textColor = new Color(0.7f, 0.4f, 1f, 1f);

    [Tooltip("Cor do label 'Orbs'")]
    public Color labelColor = new Color(0.5f, 0.3f, 0.8f, 1f);

    [Tooltip("Cor do fundo do painel (escura como a barra de vida)")] 
    public Color panelColor = new Color(0.15f, 0.08f, 0.18f, 0.9f);

    [Tooltip("Cor da borda/outline")]
    public Color borderColor = new Color(0.3f, 0.15f, 0.35f, 1f);

    [Header("Posição (relativa ao canto superior direito)")]
    [Tooltip("Offset X a partir do canto superior direito")]
    public float offsetX = -30f;

    [Tooltip("Offset Y a partir do canto superior direito")]
    public float offsetY = -30f;

    [Header("Tamanho")]
    public float panelWidth = 200f;
    public float panelHeight = 50f;

    [Header("Animação")]
    [Tooltip("Duração da animação de pulso ao coletar")]
    public float pulseDuration = 0.25f;

    [Tooltip("Escala máxima durante o pulso")]
    public float pulseScale = 1.15f;

    // Referências internas
    private TextMeshProUGUI counterText;
    private TextMeshProUGUI labelText;
    private RectTransform counterContainer;
    private PlayerEssence playerEssence;
    private Coroutine pulseCoroutine;
    private Image fillBar;

    void Start()
    {
        CreateCounterUI();

        playerEssence = FindObjectOfType<PlayerEssence>();

        if (playerEssence != null)
        {
            playerEssence.onEssenceChanged.AddListener(OnEssenceChanged);
            UpdateCounter(playerEssence.currentEssence);
            Debug.Log("[ORB COUNTER] Conectado ao PlayerEssence com sucesso!");
        }
        else
        {
            Debug.LogWarning("[ORB COUNTER] PlayerEssence não encontrado! O contador não será atualizado automaticamente.");
            UpdateCounter(0);
        }
    }

    void OnDestroy()
    {
        if (playerEssence != null)
        {
            playerEssence.onEssenceChanged.RemoveListener(OnEssenceChanged);
        }
    }

    void Update()
    {
        // Tenta achar o PlayerEssence se ele for gerado dinamicamente depois do Start
        if (playerEssence == null)
        {
            playerEssence = FindObjectOfType<PlayerEssence>();
            if (playerEssence != null)
            {
                playerEssence.onEssenceChanged.AddListener(OnEssenceChanged);
                UpdateCounter(playerEssence.currentEssence);
                Debug.Log("[ORB COUNTER] Conectado ao PlayerEssence (no Update)!");
            }
        }
    }

    /// <summary>
    /// Cria a UI no estilo da HUD existente (similar às barras de vida/armadura)
    /// </summary>
    void CreateCounterUI()
    {
        int uiLayer = gameObject.layer;

        // === Container Principal ===
        GameObject containerObj = new GameObject("OrbCounter");
        containerObj.transform.SetParent(transform, false);
        containerObj.layer = uiLayer;

        counterContainer = containerObj.AddComponent<RectTransform>();
        // Ancora no canto superior direito
        counterContainer.anchorMin = new Vector2(1, 1);
        counterContainer.anchorMax = new Vector2(1, 1);
        counterContainer.pivot = new Vector2(1, 1);
        counterContainer.anchoredPosition = new Vector2(offsetX, offsetY);
        counterContainer.sizeDelta = new Vector2(panelWidth, panelHeight);

        // === Fundo Principal (estilo da barra de vida) ===
        GameObject bgObj = new GameObject("OrbCounter_BG");
        bgObj.transform.SetParent(counterContainer, false);
        bgObj.layer = uiLayer;

        RectTransform bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        bgRect.anchoredPosition = Vector2.zero;

        bgObj.AddComponent<CanvasRenderer>();
        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.color = panelColor;
        bgImage.raycastTarget = false;
        // Usa o mesmo sprite de fundo arredondado usado pelas barras do HUD
        bgImage.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4, 0, SpriteMeshType.FullRect, new Vector4(1, 1, 1, 1));
        bgImage.type = Image.Type.Sliced;

        // === Borda (outline sutil) ===
        GameObject borderObj = new GameObject("OrbCounter_Border");
        borderObj.transform.SetParent(counterContainer, false);
        borderObj.layer = uiLayer;

        RectTransform borderRect = borderObj.AddComponent<RectTransform>();
        borderRect.anchorMin = Vector2.zero;
        borderRect.anchorMax = Vector2.one;
        borderRect.sizeDelta = new Vector2(4, 4); // Um pouco maior que o fundo
        borderRect.anchoredPosition = Vector2.zero;

        borderObj.AddComponent<CanvasRenderer>();
        Image borderImage = borderObj.AddComponent<Image>();
        borderImage.color = borderColor;
        borderImage.raycastTarget = false;
        borderImage.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4, 0, SpriteMeshType.FullRect, new Vector4(1, 1, 1, 1));
        borderImage.type = Image.Type.Sliced;

        // Move a borda para trás do fundo
        borderObj.transform.SetAsFirstSibling();

        // === Barra de preenchimento decorativa (estilo fill como as barras) ===
        GameObject fillObj = new GameObject("OrbCounter_Fill");
        fillObj.transform.SetParent(counterContainer, false);
        fillObj.layer = uiLayer;

        RectTransform fillRect = fillObj.AddComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0, 0);
        fillRect.anchorMax = new Vector2(0, 1);
        fillRect.pivot = new Vector2(0, 0.5f);
        fillRect.anchoredPosition = new Vector2(4, 0);
        fillRect.sizeDelta = new Vector2(6, -8); // Barra fina vertical

        fillObj.AddComponent<CanvasRenderer>();
        fillBar = fillObj.AddComponent<Image>();
        fillBar.color = textColor; // Roxo como accent
        fillBar.raycastTarget = false;

        // === Ícone/Label "Orbs" ===
        GameObject labelObj = new GameObject("OrbCounter_Label");
        labelObj.transform.SetParent(counterContainer, false);
        labelObj.layer = uiLayer;

        RectTransform labelRect = labelObj.AddComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0.08f, 0);
        labelRect.anchorMax = new Vector2(0.5f, 1);
        labelRect.sizeDelta = Vector2.zero;
        labelRect.anchoredPosition = new Vector2(8, 0);

        labelObj.AddComponent<CanvasRenderer>();
        labelText = labelObj.AddComponent<TextMeshProUGUI>();
        labelText.text = "ORBS";
        labelText.fontSize = fontSize * 0.6f;
        labelText.color = labelColor;
        labelText.alignment = TextAlignmentOptions.Left;
        labelText.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
        labelText.raycastTarget = false;

        // === Número do Contador ===
        GameObject textObj = new GameObject("OrbCounter_Value");
        textObj.transform.SetParent(counterContainer, false);
        textObj.layer = uiLayer;

        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.4f, 0);
        textRect.anchorMax = new Vector2(1, 1);
        textRect.sizeDelta = Vector2.zero;
        textRect.anchoredPosition = new Vector2(-4, 0);

        textObj.AddComponent<CanvasRenderer>();
        counterText = textObj.AddComponent<TextMeshProUGUI>();
        counterText.fontSize = fontSize;
        counterText.color = textColor;
        counterText.alignment = TextAlignmentOptions.Right;
        counterText.fontStyle = FontStyles.Bold;
        counterText.text = "0";
        counterText.raycastTarget = false;

        Debug.Log("[ORB COUNTER] UI criada no estilo da HUD!");
    }

    void OnEssenceChanged(int newValue)
    {
        UpdateCounter(newValue);
        PlayPulseAnimation();
    }

    void UpdateCounter(int value)
    {
        if (counterText != null)
        {
            counterText.text = value.ToString();
        }
    }

    void PlayPulseAnimation()
    {
        if (counterContainer == null) return;

        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
        }

        pulseCoroutine = StartCoroutine(PulseRoutine());
    }

    IEnumerator PulseRoutine()
    {
        float halfDuration = pulseDuration / 2f;
        float elapsed = 0f;

        // Scale UP
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;
            float scale = Mathf.Lerp(1f, pulseScale, t);
            counterContainer.localScale = new Vector3(scale, scale, 1f);
            yield return null;
        }

        // Scale DOWN
        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;
            float scale = Mathf.Lerp(pulseScale, 1f, t);
            counterContainer.localScale = new Vector3(scale, scale, 1f);
            yield return null;
        }

        counterContainer.localScale = Vector3.one;
        pulseCoroutine = null;
    }
}
