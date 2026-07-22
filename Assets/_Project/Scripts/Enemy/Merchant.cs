using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Controlador de Proximidade e Interação do Mercador das Sombras (Pacto de Sangue).
/// 
/// RECURSOS:
///   1. Pitch Shifting Aleatório (Distorção Demoníaca): A cada ciclo de 0.15s, o pitch dos sussurros
///      flutua aleatoriamente entre 0.75x e 1.25x, criando uma voz assustadora e imprevisível.
///   2. Suporte ao Retrato 'tensoeptinho_0': Carrega automaticamente o sprite do Eptinho tenso.
///   3. Configurações Exatas do Inspector:
///      - interactionRadius: 3m
///      - whisperingVolume: 1.75
///      - exitFadeDuration: 2.0s
///      - fontShiftInterval: 0.15s
///      - textPosY: 0.65 (topo central)
///      - textPosX: 0.5
///      - minCharLength: 5 / maxCharLength: 18
///      - minFontSize: 20 / maxFontSize: 34
/// </summary>
public class Merchant : MonoBehaviour
{
    [Header("Raio de Interação")]
    [Tooltip("Tamanho da zona de interação (em metros). Ao entrar nesta área, o som reduz, as escrituras surgem e o Eptinho alerta.")]
    public float interactionRadius = 3f;

    [Header("Configuração de Áudio (Sussurros)")]
    [Tooltip("Arraste o arquivo de áudio dos sussurros (Whispering) aqui ou coloque em Resources/Whispering.")]
    public AudioClip whisperingClip;
    [Range(0.1f, 3f)] public float whisperingVolume = 1.75f;
    [Tooltip("Tempo de fade out dos sussurros e restauração da música ao sair (segundos).")]
    public float exitFadeDuration = 2.0f;
    
    [Header("Distorção Aleatória de Pitch (Voz Demoníaca)")]
    public bool enableRandomPitch = true;
    [Range(0.5f, 1.0f)] public float minPitch = 0.75f;
    [Range(1.0f, 1.6f)] public float maxPitch = 1.25f;

    private AudioSource audioSource;
    private Coroutine audioTransitionRoutine;

    [Header("Retrato e Fala do Eptinho Tenso / Assustado")]
    [Tooltip("Arraste o Sprite do Eptinho tenso aqui (ex: tensoeptinho_0).")]
    public Sprite eptinhoTensoSprite;
    [TextArea(2, 4)]
    public string eptinhoWarningMessage = "Estou sentindo uma energia perturbadora vindo dele... Tenha muito cuidado!";

    [Header("Escrituras Ocultas (Texto Místico Vermelho Escuro)")]
    [Tooltip("Fontes TMP personalizadas para alternar a cada ciclo (opcional).")]
    public List<TMP_FontAsset> occultFonts = new List<TMP_FontAsset>();
    [Tooltip("Intervalo entre cada troca aleatória de caracteres, tamanho, pitch e cor (em segundos).")]
    public float fontShiftInterval = 0.15f;

    [Header("Posição e Tamanho do Texto na Tela (Ajuste em Tempo Real)")]
    [Tooltip("Posição vertical na tela (0 = base da tela, 0.5 = centro, 1 = topo).")]
    [Range(0f, 1f)] public float textPosY = 0.65f;
    [Tooltip("Posição horizontal na tela (0.5 = centro da tela).")]
    [Range(0f, 1f)] public float textPosX = 0.5f;
    [Tooltip("Largura da área do texto.")]
    public float textWidth = 1000f;
    [Tooltip("Altura da área do texto.")]
    public float textHeight = 100f;

    [Header("Tamanho e Comprimento Aleatório do Texto")]
    public int minCharLength = 5;
    public int maxCharLength = 18;
    public int minFontSize = 20;
    public int maxFontSize = 34;

    // Conjunto de caracteres enigmáticos compatíveis com 100% das fontes TMP
    private static readonly string occultCharPool = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ!@#$%^&*()_+=-~<>?/[]{}";

    // Tons de vermelho sangue escuro para variação contínua
    private static readonly string[] darkRedColors = new string[]
    {
        "#660000", "#550000", "#770000", "#440000", "#880011", "#330000", "#770505", "#500000", "#600303"
    };

    // ── Referências Internas ──────────────────────────────────────────────────
    private MerchantUIController uiController;
    private bool canInteract = false;
    private bool eptinhoWarnedThisApproach = false;

    private GameObject occultUIOverlay;
    private TextMeshProUGUI occultTextComp;
    private Coroutine occultTextRoutine;

    void Awake()
    {
        // Configura AudioSource para os sussurros
        audioSource = gameObject.GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0.0f; // 2D puro para audibilidade total
        audioSource.volume = whisperingVolume;
        audioSource.pitch = 1.0f;

        // Garante Rigidbody Cinemático para colisão confiável
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;

        EnsureTriggerColliderExists();
    }

    void Start()
    {
        uiController = MerchantUIController.Instance;

        LoadAudioAndSpriteResources();
    }

    private void LoadAudioAndSpriteResources()
    {
        if (whisperingClip == null)
        {
            whisperingClip = Resources.Load<AudioClip>("Whispering");
            if (whisperingClip == null) whisperingClip = Resources.Load<AudioClip>("whisper");
            if (whisperingClip == null) whisperingClip = Resources.Load<AudioClip>("freesound_communi");
        }

        if (whisperingClip != null && audioSource != null)
        {
            audioSource.clip = whisperingClip;
        }

        if (eptinhoTensoSprite == null)
        {
            eptinhoTensoSprite = Resources.Load<Sprite>("tensoeptinho_0");
            if (eptinhoTensoSprite == null) eptinhoTensoSprite = Resources.Load<Sprite>("EPTONHO_TENSO");
            if (eptinhoTensoSprite == null) eptinhoTensoSprite = Resources.Load<Sprite>("EPTONHO_TRISTE");
        }
    }

    private void EnsureTriggerColliderExists()
    {
        SphereCollider sc = GetComponent<SphereCollider>();
        if (sc == null) sc = gameObject.AddComponent<SphereCollider>();
        sc.isTrigger = true;
        sc.radius = interactionRadius;
    }

    private bool IsPlayerCollider(Collider other)
    {
        if (other == null) return false;
        if (other.CompareTag("Player")) return true;
        if (other.GetComponentInParent<PlayerHealth>() != null) return true;
        if (other.GetComponentInParent<PlayerM>() != null) return true;
        if (other.name.ToLower().Contains("player")) return true;
        return false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (IsPlayerCollider(other))
        {
            canInteract = true;
            Debug.Log("[MERCHANT] 🧙‍♂️ Player ENTROU na zona do Mercador!");

            if (audioTransitionRoutine != null)
            {
                StopCoroutine(audioTransitionRoutine);
                audioTransitionRoutine = null;
            }

            // 1. Reduz a música do jogo e ativa sussurros
            if (MusicManager.instance != null)
            {
                MusicManager.instance.SetMusicDucking(0.0f, 0.8f);
            }

            if (audioSource != null && audioSource.clip != null && !audioSource.isPlaying)
            {
                audioSource.volume = whisperingVolume;
                audioSource.pitch = 1.0f;
                audioSource.Play();
            }

            // 2. Exibe escrituras vermelhas na tela
            StartOccultEffects();

            // 3. Dispara popup do Eptinho com o retrato tenso de forma garantida
            if (!eptinhoWarnedThisApproach && EptinhoPopupController.instancia != null)
            {
                eptinhoWarnedThisApproach = true;

                Sprite spriteFinal = eptinhoTensoSprite;
                if (spriteFinal == null) spriteFinal = Resources.Load<Sprite>("tensoeptinho_0");
                if (spriteFinal == null) spriteFinal = Resources.Load<Sprite>("EPTONHO_TENSO");
                if (spriteFinal == null) spriteFinal = Resources.Load<Sprite>("EPTONHO");

                EptinhoPopupController.instancia.MostrarPopupCustomizado(spriteFinal, eptinhoWarningMessage);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (IsPlayerCollider(other))
        {
            canInteract = false;
            eptinhoWarnedThisApproach = false;
            Debug.Log("[MERCHANT] Player SAIU da zona do Mercador.");

            StopOccultEffects();

            // Transição ultrasuave de áudio ao sair
            SmoothTransitionOnExit(exitFadeDuration);

            if (uiController != null && uiController.IsUiOpen())
            {
                uiController.ClosePanel();
            }
        }
    }

    private void SmoothTransitionOnExit(float duration)
    {
        if (audioTransitionRoutine != null) StopCoroutine(audioTransitionRoutine);
        audioTransitionRoutine = StartCoroutine(FadeOutWhispersAndRestoreMusic(duration));
    }

    private IEnumerator FadeOutWhispersAndRestoreMusic(float duration)
    {
        float elapsed = 0f;
        float startWhisperVol = audioSource != null ? audioSource.volume : 0f;

        if (MusicManager.instance != null)
        {
            MusicManager.instance.SetMusicDucking(1.0f, duration);
        }

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;

            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.volume = Mathf.Lerp(startWhisperVol, 0f, t);
            }
            yield return null;
        }

        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
            audioSource.volume = 0f;
            audioSource.pitch = 1.0f;
        }
        audioTransitionRoutine = null;
    }

    void Update()
    {
        if (canInteract)
        {
            UpdateRealtimeTextPosition();

            bool uiIsOpen = uiController != null && uiController.IsUiOpen();

            if (!uiIsOpen && Input.GetKeyDown(KeyCode.F))
            {
                Debug.Log("[MERCHANT] ⚔️ Tecla [F] pressionada! Abrindo o Pacto...");
                StopOccultEffects();

                if (audioSource != null && audioSource.isPlaying)
                {
                    audioSource.Stop();
                    audioSource.pitch = 1.0f;
                }

                if (uiController == null) uiController = MerchantUIController.Instance;
                if (uiController != null)
                {
                    uiController.OpenPanel(this.transform);
                }
            }
        }
    }

    private void OnValidate()
    {
        EnsureTriggerColliderExists();
        UpdateRealtimeTextPosition();
    }

    private void UpdateRealtimeTextPosition()
    {
        if (occultTextComp != null)
        {
            RectTransform tr = occultTextComp.GetComponent<RectTransform>();
            if (tr != null)
            {
                tr.anchorMin = new Vector2(textPosX, textPosY);
                tr.anchorMax = new Vector2(textPosX, textPosY);
                tr.pivot = new Vector2(0.5f, 0.5f);
                tr.anchoredPosition = Vector2.zero;
                tr.sizeDelta = new Vector2(textWidth, textHeight);
            }
        }
    }

    private void StartOccultEffects()
    {
        if (occultUIOverlay == null)
        {
            CreateOccultUI();
        }

        if (occultUIOverlay != null && !occultUIOverlay.activeSelf)
        {
            occultUIOverlay.SetActive(true);
        }

        if (occultTextRoutine == null)
        {
            occultTextRoutine = StartCoroutine(AnimateOccultText());
        }
    }

    private void StopOccultEffects()
    {
        if (occultTextRoutine != null)
        {
            StopCoroutine(occultTextRoutine);
            occultTextRoutine = null;
        }

        if (occultUIOverlay != null && occultUIOverlay.activeSelf)
        {
            occultUIOverlay.SetActive(false);
        }

        if (audioSource != null)
        {
            audioSource.pitch = 1.0f;
        }
    }

    private string GenerateRandomOccultString()
    {
        int length = Random.Range(minCharLength, maxCharLength + 1);

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        for (int i = 0; i < length; i++)
        {
            char randomChar = occultCharPool[Random.Range(0, occultCharPool.Length)];
            sb.Append(randomChar);
            if (i < length - 1) sb.Append(" ");
        }

        string chosenColor = darkRedColors[Random.Range(0, darkRedColors.Length)];
        int randomSize = Random.Range(minFontSize, maxFontSize + 1);

        return $"<color={chosenColor}><b><size={randomSize}>{sb.ToString()}</size></b></color>";
    }

    private IEnumerator AnimateOccultText()
    {
        int fontIdx = 0;

        while (canInteract && (uiController == null || !uiController.IsUiOpen()))
        {
            if (occultTextComp != null)
            {
                occultTextComp.text = GenerateRandomOccultString();

                if (occultFonts != null && occultFonts.Count > 0)
                {
                    TMP_FontAsset fontToUse = occultFonts[fontIdx % occultFonts.Count];
                    if (fontToUse != null)
                    {
                        occultTextComp.font = fontToUse;
                    }
                    fontIdx++;
                }
            }

            // Distorção Aleatória de Pitch a cada ciclo do sussurro (Voz Demoníaca Imprevisível)
            if (enableRandomPitch && audioSource != null && audioSource.isPlaying)
            {
                audioSource.pitch = Random.Range(minPitch, maxPitch);
            }

            yield return new WaitForSeconds(fontShiftInterval);
        }

        if (audioSource != null) audioSource.pitch = 1.0f;
        occultTextRoutine = null;
    }

    private void CreateOccultUI()
    {
        if (occultUIOverlay != null) return;

        Canvas targetCanvas = null;
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach (var c in canvases)
        {
            if (c != null && c.gameObject.activeInHierarchy && (c.name.Contains("HUD") || c.name.Contains("Player") || c.renderMode == RenderMode.ScreenSpaceOverlay))
            {
                targetCanvas = c;
                break;
            }
        }

        occultUIOverlay = new GameObject("MerchantOccultUI");

        if (targetCanvas != null)
        {
            occultUIOverlay.transform.SetParent(targetCanvas.transform, false);
        }
        else
        {
            Canvas c = occultUIOverlay.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = 400;

            CanvasScaler cs = occultUIOverlay.AddComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1920, 1080);
        }

        RectTransform mainR = occultUIOverlay.GetComponent<RectTransform>();
        if (mainR == null) mainR = occultUIOverlay.AddComponent<RectTransform>();
        mainR.anchorMin = Vector2.zero; mainR.anchorMax = Vector2.one; mainR.sizeDelta = Vector2.zero;

        GameObject textObj = new GameObject("OccultScripturesText");
        textObj.transform.SetParent(occultUIOverlay.transform, false);

        occultTextComp = textObj.AddComponent<TextMeshProUGUI>();
        occultTextComp.fontStyle = FontStyles.Bold;
        occultTextComp.alignment = TextAlignmentOptions.Center;

        UpdateRealtimeTextPosition();
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }
}