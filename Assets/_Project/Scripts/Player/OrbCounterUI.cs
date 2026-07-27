using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Contador de Essência — canto inferior direito.
/// Paleta inspirada no popup do Eptinho.
/// Animação de emanação: ondas concêntricas crescendo e sumindo continuamente.
/// </summary>
public class OrbCounterUI : MonoBehaviour
{
    [Header("Visual do Painel")]
    public int   fontSize    = 30;
    // Paleta inspirada no popup do Eptinho
    public Color textColor   = new Color(0.95f, 0.88f, 0.55f, 1.00f); // dourado claro
    public Color labelColor  = new Color(0.72f, 0.76f, 0.78f, 0.90f); // cinza metálico
    public Color panelColor  = new Color(0.23f, 0.28f, 0.70f, 0.45f); // azul glass
    public Color borderColor = new Color(0.52f, 0.45f, 0.15f, 0.60f); // dourado oliva

    [Header("Orb Core")]
    public Color orbCoreColor = new Color(1.00f, 0.88f, 0.10f, 1.00f); // amarelo vivo
    public Color orbGlowColor = new Color(1.00f, 1.00f, 0.65f, 0.70f); // brilho interno
    public float orbSize      = 32f;

    [Header("Animação — Emanação")]
    [Tooltip("Número de ondas concêntricas simultâneas")]
    public int   rippleCount    = 3;
    [Tooltip("Duração de um ciclo completo de emanação")]
    public float rippleDuration = 1.6f;
    [Tooltip("Quanto a onda cresce em relação ao orb (ex: 2.0 = dobra o tamanho)")]
    public float rippleMaxScale = 2.2f;
    [Tooltip("Alpha inicial da onda")]
    public float rippleStartAlpha = 0.55f;
    public Color rippleColor  = new Color(1.00f, 0.92f, 0.20f, 1.00f);

    [Header("Partículas")]
    public int   particleCount    = 4;
    public float particleInterval = 1.8f;
    public float particleRadius   = 20f;
    public float particleLifetime = 0.65f;
    public float particleMaxSize  = 4.5f;
    public Color particleColor    = new Color(1f, 0.95f, 0.4f, 1f);

    [Header("Posição (canto inferior direito)")]
    public float offsetX = -60f;
    public float offsetY =  40f;

    [Header("Tamanho do Painel")]
    public float panelWidth  = 140f;
    public float panelHeight =  50f;

    [Header("Pulso ao Coletar")]
    public float pulseDuration = 0.22f;
    public float pulseScale    = 1.14f;

    // ── Referências internas ──────────────────────────────────────────────────
    private TextMeshProUGUI  counterText;
    private TextMeshProUGUI  labelText;
    private RectTransform    counterContainer;
    private RectTransform    orbCore;
    private Transform        rippleParent;
    private Transform        particleParent;
    private PlayerEssence    playerEssence;
    private Coroutine        collectPulseRoutine;

    private readonly List<Image>         rippleImages  = new List<Image>();
    private readonly List<RectTransform> particlePool  = new List<RectTransform>();

    // ─────────────────────────────────────────────────────────────────────────

    // Unity chama Reset() ao adicionar o componente e ao clicar "Reset" no Inspector.
    // Garante que os defaults do C# sobrescrevem qualquer valor antigo serializado na cena.
    private void Reset()
    {
        fontSize    = 30;
        textColor   = new Color(0.95f, 0.88f, 0.55f, 1.00f);
        labelColor  = new Color(0.72f, 0.76f, 0.78f, 0.90f);
        panelColor  = new Color(0.23f, 0.28f, 0.70f, 0.45f);
        borderColor = new Color(0.52f, 0.45f, 0.15f, 0.60f);

        orbCoreColor = new Color(1.00f, 0.88f, 0.10f, 1.00f);
        orbGlowColor = new Color(1.00f, 1.00f, 0.65f, 0.70f);
        orbSize      = 32f;

        rippleCount      = 3;
        rippleDuration   = 1.6f;
        rippleMaxScale   = 2.2f;
        rippleStartAlpha = 0.55f;
        rippleColor      = new Color(1.00f, 0.92f, 0.20f, 1.00f);

        particleCount    = 4;
        particleInterval = 1.8f;
        particleRadius   = 20f;
        particleLifetime = 0.65f;
        particleMaxSize  = 4.5f;
        particleColor    = new Color(1f, 0.95f, 0.4f, 1f);

        offsetX = -60f;
        offsetY =  40f;

        panelWidth  = 140f;
        panelHeight =  50f;

        pulseDuration = 0.22f;
        pulseScale    = 1.14f;
    }

    void Start()
    {
        CreateUI();

        playerEssence = Object.FindFirstObjectByType<PlayerEssence>();
        if (playerEssence != null)
        {
            playerEssence.onEssenceChanged.AddListener(OnEssenceChanged);
            UpdateCounter(playerEssence.currentEssence);
        }
        else { UpdateCounter(0); }

        // Inicia loops contínuos
        for (int i = 0; i < rippleCount; i++)
            StartCoroutine(RippleLoop(i * (rippleDuration / rippleCount)));

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
            playerEssence = Object.FindFirstObjectByType<PlayerEssence>();
            if (playerEssence != null)
            {
                playerEssence.onEssenceChanged.AddListener(OnEssenceChanged);
                UpdateCounter(playerEssence.currentEssence);
            }
        }

        // Esconde o Contador de Orbs quando o jogador morre ou quando a Tela de Morte é aberta
        PlayerHealth ph = playerEssence != null ? playerEssence.GetComponent<PlayerHealth>() : null;
        if (ph == null) ph = Object.FindFirstObjectByType<PlayerHealth>();

        bool isDead = (ph != null && ph.isDead) || (DeathScreenUI.Instance != null && DeathScreenUI.Instance.deathPanel != null && DeathScreenUI.Instance.deathPanel.activeSelf);

        if (counterContainer != null && counterContainer.gameObject.activeSelf == isDead)
        {
            counterContainer.gameObject.SetActive(!isDead);
        }
    }

    // ─── Construção da UI ─────────────────────────────────────────────────────

    void CreateUI()
    {
        int layer = gameObject.layer;
        Sprite circLg = MakeCircleSprite(64);
        Sprite circSm = MakeCircleSprite(16);

        // ── Container ──
        GameObject cGO = new GameObject("OrbCounter");
        cGO.transform.SetParent(transform, false);
        cGO.layer = layer;

        counterContainer                  = cGO.AddComponent<RectTransform>();
        counterContainer.anchorMin        = new Vector2(1f, 0f);
        counterContainer.anchorMax        = new Vector2(1f, 0f);
        counterContainer.pivot            = new Vector2(1f, 0f);
        counterContainer.anchoredPosition = new Vector2(offsetX, offsetY);
        counterContainer.sizeDelta        = new Vector2(panelWidth, panelHeight);

        // ── Fundo ──
        Image bgImg = MakeImgFull(cGO.transform, "BG", panelColor, layer);

        // ── Borda ──
        Image bordImg = MakeImgFull(cGO.transform, "Border", borderColor, layer);
        RectTransform bordR = bordImg.GetComponent<RectTransform>();
        bordR.sizeDelta     = new Vector2(2f, 2f);
        bordImg.fillCenter  = false;
        bordGO(bordImg).transform.SetAsFirstSibling();

        // ── Pivot central do orb (esquerda do painel) ──
        float orbCenterX = (panelHeight / 2f); // centro do orb = metade da altura do painel
        GameObject orbPivot = new GameObject("OrbPivot");
        orbPivot.transform.SetParent(cGO.transform, false);
        orbPivot.layer = layer;
        RectTransform pivR = orbPivot.AddComponent<RectTransform>();
        pivR.anchorMin = pivR.anchorMax = new Vector2(0f, 0.5f);
        pivR.pivot     = new Vector2(0.5f, 0.5f);
        pivR.sizeDelta = Vector2.zero;
        pivR.anchoredPosition = new Vector2(orbCenterX, 0f);

        // ── Ondas de emanação (filhas do pivot, atrás do orb) ──
        GameObject rParGO = new GameObject("Ripples");
        rParGO.transform.SetParent(orbPivot.transform, false);
        rParGO.layer = layer;
        RectTransform rParR = rParGO.AddComponent<RectTransform>();
        rParR.anchorMin = rParR.anchorMax = new Vector2(0.5f, 0.5f);
        rParR.pivot = new Vector2(0.5f, 0.5f);
        rParR.sizeDelta = Vector2.zero;
        rippleParent = rParGO.transform;

        for (int i = 0; i < rippleCount; i++)
        {
            GameObject rGO = new GameObject("Ripple_" + i);
            rGO.transform.SetParent(rippleParent, false);
            rGO.layer = layer;
            RectTransform rR = rGO.AddComponent<RectTransform>();
            rR.anchorMin = rR.anchorMax = new Vector2(0.5f, 0.5f);
            rR.pivot     = new Vector2(0.5f, 0.5f);
            rR.sizeDelta = new Vector2(orbSize, orbSize);
            rGO.AddComponent<CanvasRenderer>();
            Image rImg    = rGO.AddComponent<Image>();
            rImg.sprite   = circLg;
            rImg.color    = new Color(rippleColor.r, rippleColor.g, rippleColor.b, 0f);
            rImg.raycastTarget = false;
            rippleImages.Add(rImg);
        }

        // ── Orb core (na frente das ondas) ──
        GameObject orbGO = new GameObject("OrbCore");
        orbGO.transform.SetParent(orbPivot.transform, false);
        orbGO.layer = layer;
        orbCore = orbGO.AddComponent<RectTransform>();
        orbCore.anchorMin = orbCore.anchorMax = new Vector2(0.5f, 0.5f);
        orbCore.pivot     = new Vector2(0.5f, 0.5f);
        orbCore.sizeDelta = new Vector2(orbSize, orbSize);
        orbGO.AddComponent<CanvasRenderer>();
        Image orbImg = orbGO.AddComponent<Image>();
        orbImg.sprite = circLg;
        orbImg.color  = orbCoreColor;
        orbImg.raycastTarget = false;

        // Brilho interno
        GameObject glGO = new GameObject("Glow");
        glGO.transform.SetParent(orbGO.transform, false);
        glGO.layer = layer;
        RectTransform glR = glGO.AddComponent<RectTransform>();
        glR.anchorMin = new Vector2(0.12f, 0.42f);
        glR.anchorMax = new Vector2(0.52f, 0.82f);
        glR.sizeDelta = Vector2.zero;
        glGO.AddComponent<CanvasRenderer>();
        Image glImg   = glGO.AddComponent<Image>();
        glImg.sprite  = MakeCircleSprite(32);
        glImg.color   = orbGlowColor;
        glImg.raycastTarget = false;

        // ── Partículas ──
        GameObject ppGO = new GameObject("Particles");
        ppGO.transform.SetParent(orbPivot.transform, false);
        ppGO.layer = layer;
        RectTransform ppR = ppGO.AddComponent<RectTransform>();
        ppR.anchorMin = ppR.anchorMax = new Vector2(0.5f, 0.5f);
        ppR.pivot = new Vector2(0.5f, 0.5f);
        ppR.sizeDelta = Vector2.zero;
        particleParent = ppGO.transform;

        for (int i = 0; i < particleCount * 3; i++)
        {
            GameObject p = new GameObject("P_" + i);
            p.transform.SetParent(particleParent, false);
            p.layer = layer;
            RectTransform pR = p.AddComponent<RectTransform>();
            pR.anchorMin = pR.anchorMax = new Vector2(0.5f, 0.5f);
            pR.pivot     = new Vector2(0.5f, 0.5f);
            pR.sizeDelta = Vector2.one * particleMaxSize;
            p.AddComponent<CanvasRenderer>();
            Image pImg = p.AddComponent<Image>();
            pImg.sprite = circSm;
            pImg.color  = particleColor;
            pImg.raycastTarget = false;
            p.SetActive(false);
            particlePool.Add(pR);
        }

        // ── Valor (sem label — número centralizado ao lado do orb) ──
        float textX = panelHeight + 8f;

        GameObject vGO = new GameObject("Value");
        vGO.transform.SetParent(cGO.transform, false);
        vGO.layer = layer;
        RectTransform vR = vGO.AddComponent<RectTransform>();
        vR.anchorMin = new Vector2(0f, 0f);
        vR.anchorMax = new Vector2(1f, 1f);  // ocupa toda a altura
        vR.offsetMin = new Vector2(textX, 0f);
        vR.offsetMax = new Vector2(-10f, 0f);
        vGO.AddComponent<CanvasRenderer>();
        counterText           = vGO.AddComponent<TextMeshProUGUI>();
        counterText.text      = "0";
        counterText.fontSize  = fontSize;
        counterText.color     = textColor;
        counterText.alignment = TextAlignmentOptions.Left;  // centralizado verticalmente via anchorMin/Max
        counterText.fontStyle = FontStyles.Bold;
        counterText.raycastTarget = false;

        TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Fonts & Materials/Oswald Bold SDF");
        if (font != null) { counterText.font = font; }
    }

    // ─── Animação de Emanação ─────────────────────────────────────────────────

    /// <summary>
    /// Cada onda começa no tamanho do orb, cresce e perde opacidade — 
    /// dando impressão de energia emanando continuamente.
    /// As ondas são iniciadas com offset de tempo para ficarem defasadas.
    /// </summary>
    IEnumerator RippleLoop(float initialDelay)
    {
        yield return new WaitForSeconds(initialDelay);

        // Encontra qual onda este loop controla baseado no index
        int idx = Mathf.RoundToInt(initialDelay / (rippleDuration / rippleCount));
        idx = Mathf.Clamp(idx, 0, rippleImages.Count - 1);
        Image wave = rippleImages[idx];

        while (true)
        {
            float elapsed = 0f;

            while (elapsed < rippleDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / rippleDuration;

                // Escala: cresce de 1 → rippleMaxScale
                float s = Mathf.Lerp(1f, rippleMaxScale, t);
                if (wave != null)
                {
                    wave.rectTransform.sizeDelta = Vector2.one * (orbSize * s);

                    // Alpha: começa em rippleStartAlpha e vai a 0
                    // Curva: linear com suavização no fim
                    float a = rippleStartAlpha * (1f - Mathf.Pow(t, 0.7f));
                    Color c = wave.color;
                    c.a = a;
                    wave.color = c;
                }
                yield return null;
            }

            // Reseta instantaneamente ao centro (sem transição visível)
            if (wave != null)
            {
                wave.rectTransform.sizeDelta = Vector2.one * orbSize;
                Color c = wave.color; c.a = 0f; wave.color = c;
            }
        }
    }

    // ─── Partículas ───────────────────────────────────────────────────────────

    IEnumerator ParticleLoop()
    {
        yield return new WaitForSeconds(0.8f);
        while (true)
        {
            EmitBurst(particleCount);
            yield return new WaitForSeconds(particleInterval);
        }
    }

    void EmitBurst(int count)
    {
        if (particleParent == null) return;
        float step = 360f / count;
        for (int i = 0; i < count; i++)
        {
            RectTransform pr = GetPooled();
            if (pr == null) continue;
            float angle   = step * i + Random.Range(-30f, 30f);
            Vector2 dir   = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
            float speed   = Random.Range(0.7f, 1f);
            float sz      = Random.Range(particleMaxSize * 0.4f, particleMaxSize);
            pr.sizeDelta  = Vector2.one * sz;
            pr.anchoredPosition = dir * (orbSize * 0.5f);
            pr.gameObject.SetActive(true);
            StartCoroutine(AnimateParticle(pr, dir, speed));
        }
    }

    IEnumerator AnimateParticle(RectTransform pr, Vector2 dir, float speedMult)
    {
        Image img = pr.GetComponent<Image>();
        float elapsed = 0f, life = particleLifetime * speedMult;
        Vector2 start = pr.anchoredPosition;

        while (elapsed < life)
        {
            elapsed += Time.deltaTime;
            float t     = elapsed / life;
            float eased = 1f - Mathf.Pow(1f - t, 2f);
            pr.anchoredPosition = start + dir * (particleRadius * eased);
            pr.localScale       = Vector3.one * Mathf.Sin(t * Mathf.PI);
            if (img != null)
            {
                Color c = img.color;
                c.a = t < 0.55f ? 1f : Mathf.Lerp(1f, 0f, (t - 0.55f) / 0.45f);
                img.color = c;
            }
            yield return null;
        }
        pr.localScale = Vector3.one;
        pr.gameObject.SetActive(false);
    }

    RectTransform GetPooled()
    {
        foreach (var p in particlePool)
            if (!p.gameObject.activeSelf) return p;
        return null;
    }

    // ─── Eventos ─────────────────────────────────────────────────────────────

    void OnEssenceChanged(int val)
    {
        UpdateCounter(val);
        EmitBurst(particleCount + 2);
        if (collectPulseRoutine != null) StopCoroutine(collectPulseRoutine);
        collectPulseRoutine = StartCoroutine(CollectPulse());
    }

    void UpdateCounter(int val)
    {
        if (counterText != null) counterText.text = val.ToString();
    }

    IEnumerator CollectPulse()
    {
        float h = pulseDuration / 2f, e = 0f;
        while (e < h) { e += Time.deltaTime; float s = Mathf.Lerp(1f, pulseScale, e / h); counterContainer.localScale = new Vector3(s,s,1); yield return null; }
        e = 0f;
        while (e < h) { e += Time.deltaTime; float s = Mathf.Lerp(pulseScale, 1f, e / h); counterContainer.localScale = new Vector3(s,s,1); yield return null; }
        counterContainer.localScale = Vector3.one;
        collectPulseRoutine = null;
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    // retorna o gameobject de um Image para poder mover na hierarquia
    static GameObject bordGO(Image img) => img.gameObject;

    Image MakeImgFull(Transform parent, string name, Color color, int layer)
    {
        GameObject go = new GameObject("OC_" + name);
        go.transform.SetParent(parent, false);
        go.layer = layer;
        RectTransform r = go.AddComponent<RectTransform>();
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
        r.sizeDelta = Vector2.zero; r.anchoredPosition = Vector2.zero;
        go.AddComponent<CanvasRenderer>();
        Image img = go.AddComponent<Image>();
        img.color = color; img.raycastTarget = false;
        return img;
    }

    Sprite MakeCircleSprite(int res)
    {
        Texture2D tex  = new Texture2D(res, res, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode   = TextureWrapMode.Clamp;
        Color[] px = new Color[res * res];
        float c = (res - 1) / 2f;
        for (int y = 0; y < res; y++)
            for (int x = 0; x < res; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(c, c));
                float a = Mathf.Clamp01(1f - (d - (c - 1.2f)));
                px[y * res + x] = new Color(1f, 1f, 1f, a);
            }
        tex.SetPixels(px); tex.Apply();
        return Sprite.Create(tex, new Rect(0,0,res,res), new Vector2(0.5f,0.5f));
    }
}
