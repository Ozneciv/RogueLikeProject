using UnityEngine;
using System.Collections;

/// <summary>
/// Zona de Nevoa Toxica - area persistente na arena do boss.
///
/// COMPORTAMENTO:
///   - Aplica DoT ao jogador enquanto estiver dentro do trigger
///   - Pode aplicar slow (opcional)
///   - Ativa por fase via BossEvents.OnPhaseChanged
///   - Nao se autodestroi
///
/// SETUP NA CENA:
///   1. Crie um GameObject na arena, adicione este script
///   2. Adicione um BoxCollider, marque Is Trigger, ajuste o tamanho
///   3. Arraste um prefab de nevoa (Fake Fog Presets) como filho, arraste no campo Fog Visual
///   4. Configure Active On Phase (0 = sempre ativa desde o inicio da luta)
/// </summary>
[RequireComponent(typeof(Collider))]
public class FogZone : MonoBehaviour
{
    [Header("Dano Continuo (DoT)")]
    [Tooltip("Dano aplicado ao jogador a cada tick enquanto estiver na nevoa.")]
    public int damagePerTick = 4;

    [Tooltip("Intervalo em segundos entre cada tick de dano.")]
    public float tickInterval = 1.0f;

    [Header("Lentidao (Opcional)")]
    [Tooltip("Aplica slow ao jogador enquanto estiver na nevoa.")]
    public bool applySlow = false;

    [Range(0.01f, 0.9f)]
    [Tooltip("Percentual de lentidao. (0.3 = 30% mais lento)")]
    public float slowPercent = 0.25f;

    [Header("Ativacao por Fase")]
    [Tooltip("0 = sempre ativa desde o inicio da luta.\n1/2/3 = ativa a partir dessa fase.")]
    [Range(0, 3)]
    public int activeOnPhase = 0;

    [Header("Visual")]
    [Tooltip("Filho com o particle system de nevoa. Ativado/desativado junto com a zona.")]
    public GameObject fogVisual;

    [Header("Debug")]
    public bool showDebugLog = false;

    // =====================================================
    // ESTADO INTERNO
    // =====================================================

    private bool zoneActive = false;
    private bool playerIsInside = false;
    private PlayerHealth cachedPlayerHealth;
    private PlayerDebuffs cachedPlayerDebuffs;
    private Coroutine dotCoroutine;

    // =====================================================
    // UNITY LIFECYCLE
    // =====================================================

    void Awake()
    {
        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
        {
            col.isTrigger = true;
            Debug.LogWarning("[FogZone] Collider nao estava como trigger - corrigido.");
        }

        SetZoneActive(false);
    }

    void OnEnable()
    {
        BossEvents.OnPhaseChanged     += OnPhaseChanged;
        BossEvents.OnBossFightStarted += OnFightStarted;
        BossEvents.OnBossDefeated     += OnBossDefeated;
    }

    void OnDisable()
    {
        BossEvents.OnPhaseChanged     -= OnPhaseChanged;
        BossEvents.OnBossFightStarted -= OnFightStarted;
        BossEvents.OnBossDefeated     -= OnBossDefeated;
        StopDot();
    }

    // =====================================================
    // EVENTOS DO BOSS
    // =====================================================

    private void OnFightStarted()
    {
        if (activeOnPhase == 0)
            SetZoneActive(true);
    }

    private void OnPhaseChanged(int newPhase)
    {
        if (activeOnPhase == 0) return;
        SetZoneActive(newPhase >= activeOnPhase);
    }

    private void OnBossDefeated()
    {
        SetZoneActive(false);
    }

    // =====================================================
    // ATIVACAO DA ZONA
    // =====================================================

    /// <summary>Ativa a zona imediatamente (use ao spawnar em runtime).</summary>
    public void Activate() => SetZoneActive(true);

    private void SetZoneActive(bool active)
    {
        zoneActive = active;

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = active;

        if (fogVisual != null) fogVisual.SetActive(active);

        if (!active && playerIsInside)
        {
            playerIsInside = false;
            StopDot();
            RemoveSlowFromPlayer();
        }

        if (showDebugLog)
            Debug.Log(string.Format("[FogZone] {0} - {1}", gameObject.name, active ? "ATIVA" : "DESATIVADA"));
    }

    // =====================================================
    // TRIGGER
    // =====================================================

    void OnTriggerEnter(Collider other)
    {
        if (!zoneActive || !other.CompareTag("Player") || playerIsInside) return;

        playerIsInside      = true;
        cachedPlayerHealth  = other.GetComponent<PlayerHealth>();
        cachedPlayerDebuffs = other.GetComponent<PlayerDebuffs>();

        if (applySlow && cachedPlayerDebuffs != null)
            cachedPlayerDebuffs.ApplySlow(slowPercent);

        dotCoroutine = StartCoroutine(DotRoutine());

        if (showDebugLog) Debug.Log("[FogZone] Player entrou na nevoa.");
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player") || !playerIsInside) return;

        playerIsInside = false;
        StopDot();
        RemoveSlowFromPlayer();

        if (showDebugLog) Debug.Log("[FogZone] Player saiu da nevoa.");
    }

    // =====================================================
    // DOT
    // =====================================================

    private IEnumerator DotRoutine()
    {
        while (playerIsInside && zoneActive)
        {
            yield return new WaitForSeconds(tickInterval);

            if (!playerIsInside || !zoneActive) yield break;

            if (cachedPlayerHealth != null)
                cachedPlayerHealth.TakeDamage(damagePerTick, gameObject);

            if (showDebugLog)
                Debug.Log(string.Format("[FogZone] Tick de dano: -{0}", damagePerTick));
        }
    }

    private void StopDot()
    {
        if (dotCoroutine != null)
        {
            StopCoroutine(dotCoroutine);
            dotCoroutine = null;
        }
    }

    private void RemoveSlowFromPlayer()
    {
        if (applySlow && cachedPlayerDebuffs != null)
            cachedPlayerDebuffs.RemoveSlow();
    }

    // =====================================================
    // API PUBLICA
    // =====================================================

    /// <summary>Reseta o estado da zona. Util ao reiniciar a arena.</summary>
    public void ResetZone()
    {
        playerIsInside = false;
        StopDot();
        RemoveSlowFromPlayer();
        SetZoneActive(activeOnPhase == 0);
    }
}

