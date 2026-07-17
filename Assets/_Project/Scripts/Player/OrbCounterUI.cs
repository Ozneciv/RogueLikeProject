using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Contador de Essência — canto inferior direito.
/// Exibe um orb amarelo pulsante + label "ESSÊNCIA" + valor numérico.
/// </summary>
public class OrbCounterUI : MonoBehaviour
{
    [Header("Visual")]
    public int   fontSize    = 32;
    public Color textColor   = new Color(1f,   0.85f, 0.2f,  1f); // Dourado
    public Color labelColor  = new Color(1f,   0.75f, 0.1f,  0.85f);
    public Color panelColor  = new Color(0.06f,0.05f, 0.02f, 0.88f);
    public Color borderColor = new Color(0.8f, 0.6f,  0.0f,  0.5f);

    [Header("Orb")]
    [Tooltip("Cor do orb pulsante")]
    public Color orbColor      = new Color(1f, 0.85f, 0.05f, 1f);
    [Tooltip("Cor do brilho interno do orb")]
    public Color orbGlowColor  = new Color(1f, 1f,    0.6f,  0.7f);
    [Tooltip("Tamanho base do orb em pixels")]
    public float orbSize       = 28f;
    [Tooltip("Quanto o orb pulsa (amplitude de escala)")]
    public float orbPulseAmp   = 0.18f;
    [Tooltip("Velocidade da pulsação contínua")]
    public float orbPulseSpeed = 2.2f;

    [Header("Posição (canto inferior direito)")]
    public float offsetX = -20f;
    public float offsetY =  20f;

    [Header("Tamanho do Painel")]
    public float panelWidth  = 210f;
    public float panelHeight =  52f;

    [Header("Pulso ao Coletar")]
    public float pulseDuration = 0.25f;
    public float pulseScale    = 1.18f;

    // — Referências internas —
    private TextMeshProUGUI counterText;
    private TextMeshProUGUI labelText;
    private RectTransform   counterContainer;
    private RectTransform   orbTransform;
    private Image           orbImage;
    private PlayerEssence   playerEssence;
    private Coroutine       pulseCoroutine;

    // ─────────────────────────────────────────────────────────────────────────

    void Start()
    {
        CreateCounterUI();

        playerEssence = FindObjectOfType<PlayerEssence>();
        if (playerEssence != null)
        {
            playerEssence.onEssenceChanged.AddListener(OnEssenceChanged);
            UpdateCounter(playerEssence.currentEssence);
        }
        else
        {
            Debug.LogWarning("[ORB COUNTER] PlayerEssence não encontrado.");
            UpdateCounter(0);
        }

        StartCoroutine(OrbIdlePulse());
    }

    void OnDestroy()
    {
        if (playerEssence != null)
            playerEssence.onEssenceChanged.RemoveListener(OnEssenceChanged);
    }

    void Update()
    {
        if (playerEssence == null)
        {
            playerEssence = FindObjectOfType<PlayerEssence>();
            if (playerEssence != null)
            {
                playerEssence.onEssenceChanged.AddListener(OnEssenceChanged);
                UpdateCounter(playerEssence.currentEssence);
            }
        }
    }

    // ─── Construção da UI ────────────────────────────────────────────────────

    void CreateCounterUI()
    {
        int layer = gameObject.layer;

        // ── Container principal ──
        GameObject containerObj = new GameObject("OrbCounter");
        containerObj.transform.SetParent(transform, false);
        containerObj.layer = layer;

        counterContainer = containerObj.AddComponent<RectTransform>();
        // Ancora no canto INFERIOR DIREITO
        counterContainer.anchorMin        = new Vector2(1f, 0f);
        counterContainer.anchorMax        = new Vector2(1f, 0f);
        counterContainer.pivot            = new Vector2(1f, 0f);
        counterContainer.anchoredPosition = new Vector2(offsetX, offsetY);
        counterContainer.sizeDelta        = new Vector2(panelWidth, panelHeight);

        // ── Fundo ──
        CreateImage(counterContainer, "BG", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, panelColor, layer, first: false);

        // ── Borda ──
        GameObject bord = CreateImage(counterContainer, "Border", Vector2.zero, Vector2.one, new Vector2(3,3), Vector2.zero, borderColor, layer, first: true);
        bord.GetComponent<Image>().fillCenter = false;

        // ── Orb amarelo (círculo) ──
        GameObject orbObj = new GameObject("Orb");
        orbObj.transform.SetParent(containerObj.transform, false);
        orbObj.layer = layer;

        orbTransform = orbObj.AddComponent<RectTransform>();
        float orbMargin = (panelHeight - orbSize) / 2f;
        orbTransform.anchorMin        = new Vector2(0f, 0.5f);
        orbTransform.anchorMax        = new Vector2(0f, 0.5f);
        orbTransform.pivot            = new Vector2(0f, 0.5f);
        orbTransform.sizeDelta        = new Vector2(orbSize, orbSize);
        orbTransform.anchoredPosition = new Vector2(orbMargin, 0f);

        orbObj.AddComponent<CanvasRenderer>();
        orbImage       = orbObj.AddComponent<Image>();
        orbImage.color = orbColor;
        // Círculo perfeito via sprite circular gerado em código
        orbImage.sprite      = CreateCircleSprite(64);
        orbImage.raycastTarget = false;

        // Brilho interno do orb (filho menor, cor mais clara)
        GameObject glowObj = new GameObject("OrbGlow");
        glowObj.transform.SetParent(orbObj.transform, false);
        glowObj.layer = layer;
        RectTransform glowRect = glowObj.AddComponent<RectTransform>();
        glowRect.anchorMin        = new Vector2(0.15f, 0.45f);
        glowRect.anchorMax        = new Vector2(0.55f, 0.85f);
        glowRect.sizeDelta        = Vector2.zero;
        glowObj.AddComponent<CanvasRenderer>();
        Image glowImg       = glowObj.AddComponent<Image>();
        glowImg.color       = orbGlowColor;
        glowImg.sprite      = CreateCircleSprite(32);
        glowImg.raycastTarget = false;

        // ── Label "ESSÊNCIA" ──
        float textStartX = orbMargin + orbSize + 8f;

        GameObject labelObj = new GameObject("OrbCounter_Label");
        labelObj.transform.SetParent(containerObj.transform, false);
        labelObj.layer = layer;
        RectTransform lr = labelObj.AddComponent<RectTransform>();
        lr.anchorMin        = new Vector2(0f, 0.52f);
        lr.anchorMax        = new Vector2(1f, 1f);
        lr.offsetMin        = new Vector2(textStartX, 0f);
        lr.offsetMax        = new Vector2(-8f, 0f);
        labelObj.AddComponent<CanvasRenderer>();
        labelText           = labelObj.AddComponent<TextMeshProUGUI>();
        labelText.text      = "ESSÊNCIA";
        labelText.fontSize  = fontSize * 0.48f;
        labelText.color     = labelColor;
        labelText.alignment = TextAlignmentOptions.BottomLeft;
        labelText.fontStyle = FontStyles.Bold;
        labelText.raycastTarget = false;

        // ── Valor numérico ──
        GameObject valObj = new GameObject("OrbCounter_Value");
        valObj.transform.SetParent(containerObj.transform, false);
        valObj.layer = layer;
        RectTransform vr = valObj.AddComponent<RectTransform>();
        vr.anchorMin        = new Vector2(0f, 0f);
        vr.anchorMax        = new Vector2(1f, 0.55f);
        vr.offsetMin        = new Vector2(textStartX, 0f);
        vr.offsetMax        = new Vector2(-8f, 0f);
        valObj.AddComponent<CanvasRenderer>();
        counterText           = valObj.AddComponent<TextMeshProUGUI>();
        counterText.text      = "0";
        counterText.fontSize  = fontSize;
        counterText.color     = textColor;
        counterText.alignment = TextAlignmentOptions.TopLeft;
        counterText.fontStyle = FontStyles.Bold;
        counterText.raycastTarget = false;

        // Aplica fonte customizada se disponível
        TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Fonts & Materials/Oswald Bold SDF");
        if (font != null)
        {
            labelText.font   = font;
            counterText.font = font;
        }
    }

    // ─── Helpers de construção ────────────────────────────────────────────────

    GameObject CreateImage(RectTransform parent, string name,
                           Vector2 anchorMin, Vector2 anchorMax,
                           Vector2 sizeDelta, Vector2 anchoredPos,
                           Color color, int layer, bool first)
    {
        GameObject go = new GameObject("OrbCounter_" + name);
        go.transform.SetParent(parent, false);
        go.layer = layer;
        RectTransform r = go.AddComponent<RectTransform>();
        r.anchorMin        = anchorMin;
        r.anchorMax        = anchorMax;
        r.sizeDelta        = sizeDelta;
        r.anchoredPosition = anchoredPos;
        go.AddComponent<CanvasRenderer>();
        Image img       = go.AddComponent<Image>();
        img.color       = color;
        img.raycastTarget = false;
        if (first) go.transform.SetAsFirstSibling();
        return go;
    }

    /// <summary>Gera um Sprite circular suave via textura procedural.</summary>
    Sprite CreateCircleSprite(int resolution)
    {
        Texture2D tex    = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
        tex.filterMode   = FilterMode.Bilinear;
        Color[] pixels   = new Color[resolution * resolution];
        float  center    = (resolution - 1) / 2f;
        float  radius    = center;

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                // Suavização na borda (anti-alias suave)
                float alpha = Mathf.Clamp01(1f - (dist - (radius - 1f)));
                pixels[y * resolution + x] = new Color(1f, 1f, 1f, alpha);
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, resolution, resolution), new Vector2(0.5f, 0.5f));
    }

    // ─── Lógica de update ────────────────────────────────────────────────────

    void OnEssenceChanged(int newValue)
    {
        UpdateCounter(newValue);
        PlayCollectPulse();
    }

    void UpdateCounter(int value)
    {
        if (counterText != null)
            counterText.text = value.ToString();
    }

    // ─── Animações ───────────────────────────────────────────────────────────

    /// <summary>Pulsação contínua e suave do orb (idle).</summary>
    IEnumerator OrbIdlePulse()
    {
        while (true)
        {
            if (orbTransform != null)
            {
                float s = 1f + orbPulseAmp * Mathf.Sin(Time.time * orbPulseSpeed * Mathf.PI);
                orbTransform.localScale = new Vector3(s, s, 1f);
            }
            yield return null;
        }
    }

    /// <summary>Pulso rápido do painel inteiro ao coletar essência.</summary>
    void PlayCollectPulse()
    {
        if (counterContainer == null) return;
        if (pulseCoroutine != null) StopCoroutine(pulseCoroutine);
        pulseCoroutine = StartCoroutine(CollectPulseRoutine());
    }

    IEnumerator CollectPulseRoutine()
    {
        float half    = pulseDuration / 2f;
        float elapsed = 0f;

        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            float scale = Mathf.Lerp(1f, pulseScale, elapsed / half);
            counterContainer.localScale = new Vector3(scale, scale, 1f);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            float scale = Mathf.Lerp(pulseScale, 1f, elapsed / half);
            counterContainer.localScale = new Vector3(scale, scale, 1f);
            yield return null;
        }

        counterContainer.localScale = Vector3.one;
        pulseCoroutine = null;
    }
}
