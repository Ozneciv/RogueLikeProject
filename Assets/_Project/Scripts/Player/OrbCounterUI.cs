using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Contador de Essência — canto inferior direito.
/// Orb amarelo com pulso contínuo + partículas indie-style + label "ESSÊNCIA".
/// </summary>
public class OrbCounterUI : MonoBehaviour
{
    [Header("Visual do Painel")]
    public int   fontSize    = 32;
    public Color textColor   = new Color(1f,    0.88f, 0.25f, 1f);
    public Color labelColor  = new Color(0.95f, 0.78f, 0.2f,  0.9f);
    // Cor do painel — agora escuro acinzentado quente (sem roxo)
    public Color panelColor  = new Color(0.10f, 0.09f, 0.07f, 0.90f);
    public Color borderColor = new Color(0.75f, 0.55f, 0.05f, 0.55f);

    [Header("Orb")]
    public Color orbColor     = new Color(1f,    0.88f, 0.08f, 1f);
    public Color orbGlowColor = new Color(1f,    1f,    0.65f, 0.75f);
    public float orbSize      = 30f;
    public float orbPulseAmp  = 0.16f;   // amplitude da respiração
    public float orbPulseSpeed = 1.8f;   // ciclos por segundo

    [Header("Partículas (indie sparkles)")]
    [Tooltip("Quantidade de partículas por emissão")]
    public int   particleCount    = 4;
    [Tooltip("Segundos entre cada emissão de partículas")]
    public float particleInterval = 1.4f;
    [Tooltip("Distância máxima que a partícula viaja")]
    public float particleRadius   = 22f;
    [Tooltip("Duração de vida de cada partícula")]
    public float particleLifetime = 0.7f;
    [Tooltip("Tamanho máximo de cada partícula (px)")]
    public float particleMaxSize  = 5f;
    public Color particleColor    = new Color(1f, 0.96f, 0.4f, 1f);

    [Header("Posição (canto inferior direito)")]
    public float offsetX = -20f;
    public float offsetY =  70f;   // mais alto para não cortar

    [Header("Tamanho do Painel")]
    public float panelWidth  = 215f;
    public float panelHeight =  54f;

    [Header("Pulso ao Coletar")]
    public float pulseDuration = 0.22f;
    public float pulseScale    = 1.15f;

    // ── Referências internas ──────────────────────────────────────────────────
    private TextMeshProUGUI counterText;
    private TextMeshProUGUI labelText;
    private RectTransform   counterContainer;
    private RectTransform   orbTransform;
    private Transform       particleParent;     // pai das partículas (filho do orb)
    private PlayerEssence   playerEssence;
    private Coroutine       collectPulseRoutine;

    // Pool simples de partículas pré-criadas
    private readonly List<RectTransform> particlePool = new List<RectTransform>();

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
        StartCoroutine(ParticleLoop());
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

    // ─── Construção da UI ─────────────────────────────────────────────────────

    void CreateCounterUI()
    {
        int layer = gameObject.layer;

        // ── Container ──
        GameObject cObj = new GameObject("OrbCounter");
        cObj.transform.SetParent(transform, false);
        cObj.layer = layer;

        counterContainer                  = cObj.AddComponent<RectTransform>();
        counterContainer.anchorMin        = new Vector2(1f, 0f);
        counterContainer.anchorMax        = new Vector2(1f, 0f);
        counterContainer.pivot            = new Vector2(1f, 0f);
        counterContainer.anchoredPosition = new Vector2(offsetX, offsetY);
        counterContainer.sizeDelta        = new Vector2(panelWidth, panelHeight);

        // ── Fundo ──
        MakeImg(cObj.transform, "BG", Vector2.zero, Vector2.one, Vector2.zero, panelColor, layer, toFront: false);

        // ── Borda ──
        GameObject bordGO = MakeImg(cObj.transform, "Border", Vector2.zero, Vector2.one, new Vector2(2f, 2f), borderColor, layer, toFront: false);
        bordGO.GetComponent<Image>().fillCenter = false;

        // ── Orb ──
        GameObject orbGO = new GameObject("Orb");
        orbGO.transform.SetParent(cObj.transform, false);
        orbGO.layer = layer;

        orbTransform                  = orbGO.AddComponent<RectTransform>();
        float margin                  = (panelHeight - orbSize) / 2f;
        orbTransform.anchorMin        = new Vector2(0f, 0.5f);
        orbTransform.anchorMax        = new Vector2(0f, 0.5f);
        orbTransform.pivot            = new Vector2(0.5f, 0.5f);
        orbTransform.sizeDelta        = new Vector2(orbSize, orbSize);
        orbTransform.anchoredPosition = new Vector2(margin + orbSize * 0.5f, 0f);

        orbGO.AddComponent<CanvasRenderer>();
        Image orbImg       = orbGO.AddComponent<Image>();
        orbImg.color       = orbColor;
        orbImg.sprite      = MakeCircleSprite(64);
        orbImg.raycastTarget = false;

        // Brilho interno
        GameObject glowGO = new GameObject("Glow");
        glowGO.transform.SetParent(orbGO.transform, false);
        glowGO.layer = layer;
        RectTransform gr = glowGO.AddComponent<RectTransform>();
        gr.anchorMin        = new Vector2(0.12f, 0.42f);
        gr.anchorMax        = new Vector2(0.52f, 0.82f);
        gr.sizeDelta        = Vector2.zero;
        glowGO.AddComponent<CanvasRenderer>();
        Image glowImg       = glowGO.AddComponent<Image>();
        glowImg.color       = orbGlowColor;
        glowImg.sprite      = MakeCircleSprite(32);
        glowImg.raycastTarget = false;

        // Pai das partículas (filho do orb, não interfere na escala do painel)
        GameObject ppGO = new GameObject("Particles");
        ppGO.transform.SetParent(orbGO.transform, false);
        ppGO.layer = layer;
        RectTransform ppR = ppGO.AddComponent<RectTransform>();
        ppR.anchorMin = new Vector2(0.5f, 0.5f);
        ppR.anchorMax = new Vector2(0.5f, 0.5f);
        ppR.pivot     = new Vector2(0.5f, 0.5f);
        ppR.sizeDelta = Vector2.zero;
        ppR.anchoredPosition = Vector2.zero;
        particleParent = ppGO.transform;

        // Pré-cria o pool de partículas (inativas)
        Sprite circSmall = MakeCircleSprite(16);
        for (int i = 0; i < particleCount * 2; i++)
        {
            GameObject p = new GameObject("P_" + i);
            p.transform.SetParent(particleParent, false);
            p.layer = layer;
            RectTransform pr = p.AddComponent<RectTransform>();
            pr.anchorMin = pr.anchorMax = new Vector2(0.5f, 0.5f);
            pr.pivot     = new Vector2(0.5f, 0.5f);
            pr.sizeDelta = Vector2.one * particleMaxSize;
            p.AddComponent<CanvasRenderer>();
            Image pi     = p.AddComponent<Image>();
            pi.color     = particleColor;
            pi.sprite    = circSmall;
            pi.raycastTarget = false;
            p.SetActive(false);
            particlePool.Add(pr);
        }

        // ── Label "ESSÊNCIA" ──
        float textX = margin + orbSize + 10f;

        GameObject labelGO = new GameObject("Label");
        labelGO.transform.SetParent(cObj.transform, false);
        labelGO.layer = layer;
        RectTransform lr = labelGO.AddComponent<RectTransform>();
        lr.anchorMin = new Vector2(0f, 0.5f);
        lr.anchorMax = new Vector2(1f, 1f);
        lr.offsetMin = new Vector2(textX, 2f);
        lr.offsetMax = new Vector2(-8f, 0f);
        labelGO.AddComponent<CanvasRenderer>();
        labelText           = labelGO.AddComponent<TextMeshProUGUI>();
        labelText.text      = "ESSÊNCIA";
        labelText.fontSize  = fontSize * 0.46f;
        labelText.color     = labelColor;
        labelText.alignment = TextAlignmentOptions.BottomLeft;
        labelText.fontStyle = FontStyles.Bold;
        labelText.raycastTarget = false;

        // ── Valor numérico ──
        GameObject valGO = new GameObject("Value");
        valGO.transform.SetParent(cObj.transform, false);
        valGO.layer = layer;
        RectTransform vr = valGO.AddComponent<RectTransform>();
        vr.anchorMin = new Vector2(0f, 0f);
        vr.anchorMax = new Vector2(1f, 0.54f);
        vr.offsetMin = new Vector2(textX, 0f);
        vr.offsetMax = new Vector2(-8f, -2f);
        valGO.AddComponent<CanvasRenderer>();
        counterText           = valGO.AddComponent<TextMeshProUGUI>();
        counterText.text      = "0";
        counterText.fontSize  = fontSize;
        counterText.color     = textColor;
        counterText.alignment = TextAlignmentOptions.TopLeft;
        counterText.fontStyle = FontStyles.Bold;
        counterText.raycastTarget = false;

        TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Fonts & Materials/Oswald Bold SDF");
        if (font != null) { labelText.font = font; counterText.font = font; }
    }

    // ─── Animações ────────────────────────────────────────────────────────────

    /// <summary>Respiração suave e contínua do orb.</summary>
    IEnumerator OrbIdlePulse()
    {
        while (true)
        {
            if (orbTransform != null)
            {
                float s = 1f + orbPulseAmp * Mathf.Sin(Time.time * orbPulseSpeed * Mathf.PI * 2f);
                orbTransform.localScale = new Vector3(s, s, 1f);
            }
            yield return null;
        }
    }

    /// <summary>Emite partículas periodicamente (indie sparkles).</summary>
    IEnumerator ParticleLoop()
    {
        yield return new WaitForSeconds(0.5f); // espera UI inicializar
        while (true)
        {
            EmitParticleBurst(particleCount);
            yield return new WaitForSeconds(particleInterval);
        }
    }

    void EmitParticleBurst(int count)
    {
        if (particleParent == null) return;

        // Distribui os ângulos uniformemente + leve variação aleatória
        float step = 360f / count;
        for (int i = 0; i < count; i++)
        {
            RectTransform pr = GetPooledParticle();
            if (pr == null) continue;

            float angle    = step * i + Random.Range(-25f, 25f);
            float radians  = angle * Mathf.Deg2Rad;
            Vector2 dir    = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
            float   size   = Random.Range(particleMaxSize * 0.4f, particleMaxSize);
            float   speed  = Random.Range(0.75f, 1f);

            pr.sizeDelta         = Vector2.one * size;
            pr.anchoredPosition  = dir * (orbSize * 0.5f); // começa na borda do orb
            pr.gameObject.SetActive(true);

            StartCoroutine(AnimateParticle(pr, dir, speed));
        }
    }

    IEnumerator AnimateParticle(RectTransform pr, Vector2 dir, float speedMult)
    {
        Image img     = pr.GetComponent<Image>();
        float elapsed = 0f;
        float life    = particleLifetime * speedMult;
        Vector2 start = pr.anchoredPosition;

        while (elapsed < life)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / life;

            // Movimento: desacelera conforme afasta (ease-out)
            float eased = 1f - Mathf.Pow(1f - t, 2f);
            pr.anchoredPosition = start + dir * (particleRadius * eased);

            // Escala: cresce no início, some no fim
            float scl = Mathf.Sin(t * Mathf.PI);
            pr.localScale = Vector3.one * scl;

            // Alpha: desaparece nos últimos 40%
            if (img != null)
            {
                Color c = img.color;
                c.a     = t < 0.6f ? 1f : Mathf.Lerp(1f, 0f, (t - 0.6f) / 0.4f);
                img.color = c;
            }

            yield return null;
        }

        pr.localScale = Vector3.one;
        pr.gameObject.SetActive(false);
    }

    RectTransform GetPooledParticle()
    {
        foreach (var p in particlePool)
            if (!p.gameObject.activeSelf) return p;
        return null;
    }

    void OnEssenceChanged(int newValue)
    {
        UpdateCounter(newValue);
        EmitParticleBurst(particleCount + 2); // burst extra ao coletar
        if (collectPulseRoutine != null) StopCoroutine(collectPulseRoutine);
        collectPulseRoutine = StartCoroutine(CollectPulseRoutine());
    }

    void UpdateCounter(int value)
    {
        if (counterText != null) counterText.text = value.ToString();
    }

    IEnumerator CollectPulseRoutine()
    {
        float half = pulseDuration / 2f, elapsed = 0f;
        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            float s = Mathf.Lerp(1f, pulseScale, elapsed / half);
            counterContainer.localScale = new Vector3(s, s, 1f);
            yield return null;
        }
        elapsed = 0f;
        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            float s = Mathf.Lerp(pulseScale, 1f, elapsed / half);
            counterContainer.localScale = new Vector3(s, s, 1f);
            yield return null;
        }
        counterContainer.localScale = Vector3.one;
        collectPulseRoutine = null;
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    GameObject MakeImg(Transform parent, string name,
                       Vector2 anchorMin, Vector2 anchorMax,
                       Vector2 sizeDelta, Color color,
                       int layer, bool toFront)
    {
        GameObject go = new GameObject("OrbCounter_" + name);
        go.transform.SetParent(parent, false);
        go.layer = layer;
        RectTransform r = go.AddComponent<RectTransform>();
        r.anchorMin = anchorMin; r.anchorMax = anchorMax;
        r.sizeDelta = sizeDelta; r.anchoredPosition = Vector2.zero;
        go.AddComponent<CanvasRenderer>();
        Image img = go.AddComponent<Image>();
        img.color = color; img.raycastTarget = false;
        if (toFront) go.transform.SetAsLastSibling();
        return go;
    }

    Sprite MakeCircleSprite(int res)
    {
        Texture2D tex  = new Texture2D(res, res, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode   = TextureWrapMode.Clamp;
        Color[] px     = new Color[res * res];
        float  c       = (res - 1) / 2f;
        for (int y = 0; y < res; y++)
            for (int x = 0; x < res; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(c, c));
                float a = Mathf.Clamp01(1f - (d - (c - 1.2f)));
                px[y * res + x] = new Color(1f, 1f, 1f, a);
            }
        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f));
    }
}
