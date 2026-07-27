using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Componente de UI do Timer de Speedrun do Modo Endless.
/// Exibe um relógio digital no canto superior esquerdo do Canvas enquanto a Run Endless estiver ativa.
/// </summary>
public class EndlessTimerHUD : MonoBehaviour
{
    public static EndlessTimerHUD Instance { get; private set; }

    [Header("Referências da UI (Canvas)")]
    [Tooltip("Texto TMP do Timer de Speedrun no Canvas.")]
    public TextMeshProUGUI timerText;

    [Tooltip("Container/Painel pai do Timer no Canvas.")]
    public GameObject timerPanel;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoInitialize()
    {
        if (Instance == null)
        {
            GameObject go = new GameObject("EndlessTimerHUD_AutoInit");
            DontDestroyOnLoad(go);
            go.AddComponent<EndlessTimerHUD>();
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        EnsureUIReferences();
        if (timerPanel != null)
        {
            timerPanel.SetActive(false);
        }
    }

    private void Update()
    {
        bool isEndless = (RunManager.instance != null && RunManager.instance.isEndlessMode) ||
                         (RunStatsManager.Instance != null && RunStatsManager.Instance.isEndlessMode);

        bool isRunActive = (RunManager.instance != null && RunManager.instance.currentRoomNumber >= 1) ||
                           (RunStatsManager.Instance != null && RunStatsManager.Instance.isRunActive);

        bool shouldShow = isEndless && isRunActive;

        if (shouldShow)
        {
            EnsureUIReferences();
        }

        if (timerPanel != null && timerPanel.activeSelf != shouldShow)
        {
            timerPanel.SetActive(shouldShow);
        }

        if (shouldShow && timerText != null && RunStatsManager.Instance != null)
        {
            string timeStr = RunStatsManager.Instance.FormatTime(RunStatsManager.Instance.survivalTimer);
            timerText.text = $"<color=#ffd700>⏱️</color> <color=#ffffff><b>{timeStr}</b></color>";
        }
    }

    private void EnsureUIReferences()
    {
        if (timerText == null)
        {
            timerText = GetComponentInChildren<TextMeshProUGUI>(true);
        }

        if (timerPanel == null && timerText != null)
        {
            timerPanel = timerText.gameObject;
        }

        // Se não houver referências atribuídas no Inspector, gera uma estrutura limpa no topo esquerdo do Canvas real do Player
        if (timerText == null)
        {
            Canvas parentCanvas = GetComponentInParent<Canvas>();
            if (parentCanvas == null)
            {
                parentCanvas = FindTargetPlayerCanvas();
            }

            if (parentCanvas != null)
            {
                GameObject panelGo = new GameObject("EndlessTimerPanel");
                panelGo.transform.SetParent(parentCanvas.transform, false);

                RectTransform rt = panelGo.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0f, 1f);
                rt.anchoredPosition = new Vector2(25f, -25f);
                rt.sizeDelta = new Vector2(200f, 45f);

                Image bg = panelGo.AddComponent<Image>();
                bg.color = new Color(0.04f, 0.04f, 0.08f, 0.75f);

                Shadow shadow = panelGo.AddComponent<Shadow>();
                shadow.effectColor = new Color(0f, 0f, 0f, 0.6f);
                shadow.effectDistance = new Vector2(3f, -3f);

                GameObject textGo = new GameObject("txtTimer");
                textGo.transform.SetParent(panelGo.transform, false);
                RectTransform textRt = textGo.AddComponent<RectTransform>();
                textRt.anchorMin = Vector2.zero;
                textRt.anchorMax = Vector2.one;
                textRt.offsetMin = new Vector2(10f, 0f);
                textRt.offsetMax = new Vector2(-10f, 0f);

                timerText = textGo.AddComponent<TextMeshProUGUI>();
                timerText.fontSize = 22f;
                timerText.fontStyle = FontStyles.Bold;
                timerText.alignment = TextAlignmentOptions.MidlineLeft;
                timerText.color = Color.white;

                timerPanel = panelGo;
            }
        }
    }

    private static Canvas FindTargetPlayerCanvas()
    {
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach (var c in canvases)
        {
            if (c != null && c.gameObject.activeInHierarchy)
            {
                if (c.name.Contains("Player Canvas") || c.name.Contains("PlayerCanvas") || c.name.Contains("HUD"))
                {
                    return c;
                }
            }
        }
        foreach (var c in canvases)
        {
            if (c != null && c.gameObject.activeInHierarchy && c.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                return c;
            }
        }
        return canvases.Length > 0 ? canvases[0] : null;
    }
}
