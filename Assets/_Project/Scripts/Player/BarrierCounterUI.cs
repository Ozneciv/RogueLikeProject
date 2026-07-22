using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// UI de Contador de Salas / Barreiras Restantes — Canto inferior esquerdo.
/// Ativado ao equipar o "Detector de Barreiras" (equip_detector_barreiras) no Crafting.
/// Paleta e visual alinhados com o OrbCounterUI (Painel glassmorphic azul/dourado).
/// </summary>
public class BarrierCounterUI : MonoBehaviour
{
    public static BarrierCounterUI Instance { get; private set; }

    [Header("Visual do Painel")]
    public int fontSize = 30;
    public Color textColor   = new Color(0.90f, 0.84f, 0.47f, 1.00f); // dourado claro
    public Color labelColor  = new Color(0.76f, 0.81f, 0.84f, 1.00f); // cinza metálico
    public Color panelColor  = new Color(0.05f, 0.04f, 0.09f, 0.94f); // azul glass escuro
    public Color borderColor = new Color(0.44f, 0.38f, 0.59f, 0.70f); // borda neon roxa

    [Header("Posição")]
    public float offsetX = 1700f;
    public float offsetY = 120f;

    [Header("Tamanho do Painel")]
    public float panelWidth  = 180f;
    public float panelHeight = 80f;

    [Header("Pulso ao Limpar Sala")]
    public float pulseDuration = 0.25f;
    public float pulseScale    = 1.15f;

    // ── Referências Internas ──────────────────────────────────────────────────
    private Canvas          parentCanvas;
    private TextMeshProUGUI counterText;
    private TextMeshProUGUI labelText;
    private RectTransform   containerRect;
    private Coroutine       pulseRoutine;

    public const string EQUIPMENT_ID = "equip_detector_barreiras";

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnEnable()
    {
        EquipmentManager.OnEquipmentStateChanged += CheckEquipmentState;
        RoomController.OnRoomCleared             += OnRoomClearedHandler;
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;

        CheckEquipmentState();
        UpdateDisplay();
    }

    void OnDisable()
    {
        EquipmentManager.OnEquipmentStateChanged -= CheckEquipmentState;
        RoomController.OnRoomCleared             -= OnRoomClearedHandler;
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private Image bgImage;
    private Image borderImage;
    private float updateTimer = 0f;

    void Start()
    {
        CheckEquipmentState();
        UpdateDisplay();
    }

    void Update()
    {
        if (isVisible)
        {
            ApplyRealtimeVisuals();

            updateTimer += Time.deltaTime;
            if (updateTimer >= 0.5f)
            {
                updateTimer = 0f;
                UpdateDisplay();
            }
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        ApplyRealtimeVisuals();
    }
#endif

    public void ApplyRealtimeVisuals()
    {
        if (containerRect != null)
        {
            containerRect.anchoredPosition = new Vector2(offsetX, offsetY);
            containerRect.sizeDelta = new Vector2(panelWidth, panelHeight);
        }

        if (bgImage != null) bgImage.color = panelColor;
        if (borderImage != null) borderImage.color = borderColor;
        if (labelText != null) labelText.color = labelColor;
        if (counterText != null) counterText.fontSize = fontSize;
    }

    // ─── API PÚBLICA ──────────────────────────────────────────────────────────

    public static void EnsureExistsAndSetVisible(bool visible)
    {
        if (Instance == null)
        {
            GameObject go = new GameObject("BarrierCounterUI_Root");
            Instance = go.AddComponent<BarrierCounterUI>();
        }

        if (Instance != null)
        {
            Instance.SetVisible(visible);
        }
    }

    private bool isVisible = false;

    public void SetVisible(bool visible)
    {
        isVisible = visible;
        if (visible)
        {
            EnsureCanvasAndUIBuilt();
            gameObject.SetActive(true);
            UpdateDisplay();
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    private void CheckEquipmentState()
    {
        bool equipped = false;
        if (EquipmentManager.Instance != null)
        {
            equipped = EquipmentManager.Instance.IsEquipped(EQUIPMENT_ID);
        }

        SetVisible(equipped);
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        CheckEquipmentState();
        UpdateDisplay();
    }

    private void OnRoomClearedHandler(RoomController room)
    {
        UpdateDisplay();

        if (gameObject.activeInHierarchy && pulseRoutine == null && containerRect != null)
        {
            pulseRoutine = StartCoroutine(PulseAnim());
        }
    }

    // ─── CÁLCULO E ATUALIZAÇÃO DO DISPLAY ─────────────────────────────────────

    public void UpdateDisplay()
    {
        if (counterText == null) return;

        string activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        RoomController[] allRooms = FindObjectsByType<RoomController>(FindObjectsSortMode.None);

        if (activeScene == "Base" || allRooms == null || allRooms.Length == 0)
        {
            counterText.text = "<color=#88D4FF>SAFE AREA</color>";
            if (labelText != null) labelText.text = "DETECTOR DE ONDAS";
            return;
        }

        if (labelText != null) labelText.text = "ONDAS RESTANTES";

        // Procura a sala de combate atualmente trancada/ativa
        RoomController activeRoom = null;
        foreach (var r in allRooms)
        {
            if (r != null && r.doorsAreLocked)
            {
                activeRoom = r;
                break;
            }
        }

        if (activeRoom == null)
        {
            // Nenhuma barreira trancada no momento
            counterText.text = "<color=#66FF88>DESBLOQUEADO!</color>";
        }
        else
        {
            int totalWaves = activeRoom.TotalWaves;
            int currWave   = activeRoom.CurrentWaveNumber;
            int remaining  = Mathf.Max(0, totalWaves - currWave + 1);

            if (remaining > 0)
            {
                counterText.text = $"<color=#FFD154>{remaining}</color> <size=15><color=#B4C4D0>(Onda {currWave}/{totalWaves})</color></size>";
            }
            else
            {
                counterText.text = "<color=#66FF88>ÚLTIMA ONDA!</color>";
            }
        }
    }

    // ─── CONSTRUÇÃO DA UI (CANTO INFERIOR ESQUERDO) ───────────────────────────

    private void EnsureCanvasAndUIBuilt()
    {
        if (GetComponent<Canvas>() == null)
        {
            Canvas c = gameObject.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = 500;

            CanvasScaler cs = gameObject.AddComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1920, 1080);

            gameObject.AddComponent<GraphicRaycaster>();
        }

        if (containerRect == null)
        {
            CreateUI();
        }
    }

    private void CreateUI()
    {
        int layer = gameObject.layer;

        // Limpa filhos antigos se houver
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        // Criamos o Container do Painel (filho do Canvas root) com tamanho exato (180x80)
        GameObject panelGO = new GameObject("PanelContainer");
        panelGO.transform.SetParent(transform, false);
        panelGO.layer = layer;

        containerRect = panelGO.AddComponent<RectTransform>();
        containerRect.anchorMin        = new Vector2(0f, 0f);
        containerRect.anchorMax        = new Vector2(0f, 0f);
        containerRect.pivot            = new Vector2(0f, 0f);
        containerRect.anchoredPosition = new Vector2(offsetX, offsetY);
        containerRect.sizeDelta        = new Vector2(panelWidth, panelHeight);

        // Fundo Glassmorphic (anexado ao PanelContainer)
        bgImage = MakeImgFull(panelGO.transform, "BG", panelColor, layer);

        // Borda (anexada ao PanelContainer)
        borderImage = MakeImgFull(panelGO.transform, "Border", borderColor, layer);
        RectTransform bordR = borderImage.GetComponent<RectTransform>();
        bordR.sizeDelta     = new Vector2(2f, 2f);
        borderImage.fillCenter  = false;

        // Título / Label
        GameObject labelGO = new GameObject("Label");
        labelGO.transform.SetParent(panelGO.transform, false);
        labelGO.layer = layer;

        RectTransform labelR = labelGO.AddComponent<RectTransform>();
        labelR.anchorMin = new Vector2(0f, 0.55f);
        labelR.anchorMax = new Vector2(1f, 0.95f);
        labelR.sizeDelta = Vector2.zero;

        labelText = labelGO.AddComponent<TextMeshProUGUI>();
        labelText.text = "SALAS RESTANTES";
        labelText.fontSize = 15f;
        labelText.fontStyle = FontStyles.Bold;
        labelText.alignment = TextAlignmentOptions.Center;
        labelText.color = labelColor;

        // Valor / Contador
        GameObject valGO = new GameObject("Value");
        valGO.transform.SetParent(panelGO.transform, false);
        valGO.layer = layer;

        RectTransform valR = valGO.AddComponent<RectTransform>();
        valR.anchorMin = new Vector2(0f, 0.05f);
        valR.anchorMax = new Vector2(1f, 0.55f);
        valR.sizeDelta = Vector2.zero;

        counterText = valGO.AddComponent<TextMeshProUGUI>();
        counterText.text = "0 SALAS";
        counterText.fontSize = fontSize;
        counterText.fontStyle = FontStyles.Bold;
        counterText.alignment = TextAlignmentOptions.Center;
        counterText.color = textColor;
    }

    private static Image MakeImgFull(Transform parent, string name, Color color, int layer)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.layer = layer;

        RectTransform r = go.AddComponent<RectTransform>();
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.sizeDelta = Vector2.zero;

        Image img = go.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    private static Canvas FindTargetCanvas()
    {
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach (var c in canvases)
        {
            if (c.name.Contains("Player Canvas") || c.name.Contains("HUD") || c.renderMode == RenderMode.ScreenSpaceOverlay)
                return c;
        }
        return canvases.Length > 0 ? canvases[0] : null;
    }

    private IEnumerator PulseAnim()
    {
        Vector3 orig = Vector3.one;
        Vector3 target = Vector3.one * pulseScale;
        float elapsed = 0f;

        while (elapsed < pulseDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / pulseDuration;
            containerRect.localScale = Vector3.Lerp(orig, target, Mathf.Sin(t * Mathf.PI));
            yield return null;
        }

        containerRect.localScale = Vector3.one;
        pulseRoutine = null;
    }
}
