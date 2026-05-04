using UnityEngine;
using System.Collections;

/// <summary>
/// Gerenciador de Soundtrack do jogo.
/// Persiste entre cenas e controla música de fundo.
///
/// SETUP:
///   1. Crie um GameObject vazio chamado "MusicManager" na primeira cena
///   2. Adicione este script a ele
///   3. Arraste os AudioClips das músicas nos campos do Inspector
///   4. O Manager persiste automaticamente entre cenas (DontDestroyOnLoad)
///
/// USO NO CÓDIGO:
///   MusicManager.instance.PlayTrack(0);          // Toca a faixa 0
///   MusicManager.instance.PlayTrack("Dungeon");  // Toca pelo nome
///   MusicManager.instance.FadeToTrack(1, 2f);    // Fade de 2s para faixa 1
///   MusicManager.instance.SetVolume(0.8f);        // Volume geral
///   MusicManager.instance.Stop();                 // Para a música
/// </summary>
public class MusicManager : MonoBehaviour
{
    public static MusicManager instance { get; private set; }

    [System.Serializable]
    public class MusicTrack
    {
        [Tooltip("Nome identificador da faixa (para chamar por nome no código)")]
        public string trackName;
        [Tooltip("AudioClip da música")]
        public AudioClip clip;
        [Tooltip("Volume específico desta faixa (1 = volume padrão)")]
        [Range(0f, 1f)] public float volume = 1f;
        [Tooltip("Faixa toca em loop?")]
        public bool loop = true;
    }

    [Header("Faixas de Música")]
    [Tooltip("Adicione todas as músicas aqui. Arraste os AudioClips do Project.")]
    public MusicTrack[] tracks;

    [Header("Configurações")]
    [Tooltip("Volume geral da música (0 = mudo, 1 = máximo)")]
    [Range(0f, 1f)] public float masterVolume = 0.7f;
    [Tooltip("Faixa a tocar automaticamente ao iniciar (deixe -1 para não tocar)")]
    public int playOnStart = 0;
    [Tooltip("Tempo padrão de fade entre músicas (segundos)")]
    public float defaultFadeDuration = 1.5f;

    // Dois AudioSources para crossfade suave
    private AudioSource sourceA;
    private AudioSource sourceB;
    private bool usingSourceA = true;

    private int currentTrackIndex = -1;
    private Coroutine fadeCoroutine;

    // ─────────────────────────────────────────
    void Awake()
    {
        // Singleton com DontDestroyOnLoad
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);

        // Cria os dois AudioSources para crossfade
        sourceA = gameObject.AddComponent<AudioSource>();
        sourceB = gameObject.AddComponent<AudioSource>();
        ConfigureSource(sourceA);
        ConfigureSource(sourceB);
    }

    void Start()
    {
        if (playOnStart >= 0 && tracks != null && playOnStart < tracks.Length)
            PlayTrack(playOnStart);
    }

    void ConfigureSource(AudioSource src)
    {
        src.playOnAwake = false;
        src.spatialBlend = 0f; // 2D — música de fundo global
        src.loop = true;
        src.volume = 0f;
    }

    // ─────────────────────────────────────────
    // API PÚBLICA
    // ─────────────────────────────────────────

    /// <summary>Toca uma faixa pelo índice com fade.</summary>
    public void PlayTrack(int index, float fadeDuration = -1f)
    {
        if (tracks == null || index < 0 || index >= tracks.Length) return;
        if (index == currentTrackIndex) return; // já tocando

        if (fadeDuration < 0) fadeDuration = defaultFadeDuration;

        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(CrossfadeTo(tracks[index], fadeDuration));
        currentTrackIndex = index;
    }

    /// <summary>Toca uma faixa pelo nome com fade.</summary>
    public void PlayTrack(string name, float fadeDuration = -1f)
    {
        if (tracks == null) return;
        for (int i = 0; i < tracks.Length; i++)
        {
            if (tracks[i].trackName == name)
            {
                PlayTrack(i, fadeDuration);
                return;
            }
        }
        Debug.LogWarning($"[MusicManager] Faixa '{name}' não encontrada!");
    }

    /// <summary>Para a música com fade out.</summary>
    public void Stop(float fadeDuration = -1f)
    {
        if (fadeDuration < 0) fadeDuration = defaultFadeDuration;
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeOutAll(fadeDuration));
        currentTrackIndex = -1;
    }

    /// <summary>Ajusta o volume geral.</summary>
    public void SetVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        AudioSource active = usingSourceA ? sourceA : sourceB;
        if (active.isPlaying)
            active.volume = masterVolume * GetCurrentTrackVolume();
    }

    /// <summary>Pausa/retoma a música atual.</summary>
    public void SetPaused(bool paused)
    {
        if (paused) { sourceA.Pause(); sourceB.Pause(); }
        else        { sourceA.UnPause(); sourceB.UnPause(); }
    }

    public int GetCurrentTrackIndex() => currentTrackIndex;

    // ─────────────────────────────────────────
    // CROSSFADE
    // ─────────────────────────────────────────

    IEnumerator CrossfadeTo(MusicTrack track, float duration)
    {
        AudioSource incoming = usingSourceA ? sourceB : sourceA;
        AudioSource outgoing = usingSourceA ? sourceA : sourceB;

        float targetVol = masterVolume * track.volume;

        incoming.clip   = track.clip;
        incoming.loop   = track.loop;
        incoming.volume = 0f;
        incoming.Play();

        float elapsed  = 0f;
        float startVol = outgoing.volume;

        while (elapsed < duration)
        {
            elapsed      += Time.unscaledDeltaTime;
            float t       = Mathf.Clamp01(elapsed / duration);
            incoming.volume = Mathf.Lerp(0f,        targetVol, t);
            outgoing.volume = Mathf.Lerp(startVol,  0f,        t);
            yield return null;
        }

        outgoing.Stop();
        outgoing.clip = null;
        incoming.volume = targetVol;

        usingSourceA = !usingSourceA;
        fadeCoroutine = null;
    }

    IEnumerator FadeOutAll(float duration)
    {
        float elapsed = 0f;
        float volA = sourceA.volume;
        float volB = sourceB.volume;

        while (elapsed < duration)
        {
            elapsed      += Time.unscaledDeltaTime;
            float t       = Mathf.Clamp01(elapsed / duration);
            sourceA.volume = Mathf.Lerp(volA, 0f, t);
            sourceB.volume = Mathf.Lerp(volB, 0f, t);
            yield return null;
        }

        sourceA.Stop();
        sourceB.Stop();
        fadeCoroutine = null;
    }

    float GetCurrentTrackVolume()
    {
        if (currentTrackIndex < 0 || tracks == null || currentTrackIndex >= tracks.Length) return 1f;
        return tracks[currentTrackIndex].volume;
    }
}
