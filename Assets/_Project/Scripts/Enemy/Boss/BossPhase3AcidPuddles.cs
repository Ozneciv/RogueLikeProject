using UnityEngine;
using System.Collections;

/// <summary>
/// Fase 3 do Boss Cromático — Sistema de Poças Ácidas.
///
/// RESPONSABILIDADES:
///   • Spawn aleatório de poças ácidas sob o jogador em intervalos randomizados
///   • Acid Slam: ataque terrestre que telegrafeia e spawna poça no ponto de impacto
///   • Controla o limite máximo de poças simultâneas na arena
///   • Usa AttackCooldownMultiplier do BossPhase3Modifiers automaticamente (se presente)
///
/// SETUP NO UNITY:
///   1. Adicione este componente no mesmo GameObject do BossController
///   2. Crie um Prefab: GameObject vazio → CapsuleCollider (isTrigger=true) + AcidPuddle.cs
///   3. Arraste o prefab no campo "acidPuddlePrefab"
///   4. BossController é encontrado automaticamente (ou arraste manualmente)
///
/// DEPENDÊNCIAS:
///   • BossPhase3Modifiers (opcional, no mesmo GameObject) — aplica -50% ao cooldown do slam
///   • AcidPuddle.cs — script do prefab da poça
/// </summary>
public class BossPhase3AcidPuddles : MonoBehaviour
{
    // =====================================================
    // INSPECTOR
    // =====================================================

    [Header("Referências")]
    [Tooltip("Prefab da poça ácida. Deve ter CapsuleCollider (isTrigger=true) + AcidPuddle.cs.")]
    public AcidPuddle acidPuddlePrefab;

    [Tooltip("BossController deste boss. Auto-encontrado se nulo.")]
    public BossController bossController;

    [Header("Spawn Aleatório — Sob o Player")]
    [Tooltip("Intervalo mínimo (segundos) entre spawns aleatórios de poças ácidas.")]
    public float spawnIntervalMin = 3f;

    [Tooltip("Intervalo máximo (segundos) entre spawns aleatórios de poças ácidas.")]
    public float spawnIntervalMax = 6f;

    [Header("Acid Slam — Ataque Terrestre")]
    [Tooltip("Cooldown base em segundos entre Acid Slams.\n" +
             "Se BossPhase3Modifiers estiver presente, é multiplicado por attackCooldownMultiplier (0.5).")]
    public float slamCooldownBase = 8f;

    [Tooltip("Duração da telegrafagem do slam (boss para e sinaliza o ataque antes de impactar).")]
    public float slamTelegraphDuration = 1.2f;

    [Header("Limite de Poças na Arena")]
    [Tooltip("Máximo de poças ácidas simultâneas. Novos spawns são bloqueados ao atingir o limite.")]
    [Range(1, 20)]
    public int maxSimultaneousPuddles = 6;

    [Header("Debug")]
    public bool showDebugLog = true;

    // =====================================================
    // ESTADO INTERNO
    // =====================================================

    private Transform playerTransform;
    private BossPhase3Modifiers phase3Modifiers;
    private Coroutine randomSpawnCoroutine;
    private Coroutine slamCoroutine;
    private bool phase3Active = false;

    // =====================================================
    // UNITY LIFECYCLE
    // =====================================================

    void Awake()
    {
        if (bossController == null)
            bossController = GetComponent<BossController>();

        phase3Modifiers = GetComponent<BossPhase3Modifiers>();
    }

    void OnEnable()
    {
        BossEvents.OnPhaseChanged += OnPhaseChanged;
        BossEvents.OnBossDefeated += OnBossDefeated;
    }

    void OnDisable()
    {
        BossEvents.OnPhaseChanged -= OnPhaseChanged;
        BossEvents.OnBossDefeated -= OnBossDefeated;
        StopAllPhase3Coroutines();
    }

    // =====================================================
    // HANDLERS DE EVENTO
    // =====================================================

    private void OnPhaseChanged(int newPhase)
    {
        if (newPhase == 3)
            ActivatePhase3();
    }

    private void OnBossDefeated()
    {
        StopAllPhase3Coroutines();
        AcidPuddle.ResetStaticCounters();
    }

    // =====================================================
    // ATIVAÇÃO
    // =====================================================

    private void ActivatePhase3()
    {
        if (phase3Active) return;
        phase3Active = true;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            playerTransform = player.transform;
        else
            Debug.LogWarning("[Phase3AcidPuddles] ⚠️ Player não encontrado pela tag 'Player'!");

        randomSpawnCoroutine = StartCoroutine(RandomPuddleSpawnRoutine());
        slamCoroutine        = StartCoroutine(AcidSlamRoutine());

        if (showDebugLog)
            Debug.Log("[Phase3AcidPuddles] 🧪 Sistema de poças ácidas ativado!");
    }

    private void StopAllPhase3Coroutines()
    {
        phase3Active = false;

        if (randomSpawnCoroutine != null)
        {
            StopCoroutine(randomSpawnCoroutine);
            randomSpawnCoroutine = null;
        }

        if (slamCoroutine != null)
        {
            StopCoroutine(slamCoroutine);
            slamCoroutine = null;
        }
    }

    // =====================================================
    // SPAWN ALEATÓRIO — SOB O PLAYER
    // =====================================================

    private IEnumerator RandomPuddleSpawnRoutine()
    {
        // Beat inicial antes de começar
        yield return new WaitForSeconds(spawnIntervalMin);

        while (phase3Active)
        {
            float interval = Random.Range(spawnIntervalMin, spawnIntervalMax);
            yield return new WaitForSeconds(interval);

            if (!phase3Active) yield break;
            if (IsBossPaused()) continue;

            SpawnPuddleAt(GetPlayerPosition());
        }
    }

    // =====================================================
    // ACID SLAM — ATAQUE TERRESTRE
    // =====================================================

    private IEnumerator AcidSlamRoutine()
    {
        // Beat inicial diferente do spawn aleatório para não sobrepor
        float initialDelay = GetEffectiveSlamCooldown() * 0.6f;
        yield return new WaitForSeconds(initialDelay);

        while (phase3Active)
        {
            yield return new WaitForSeconds(GetEffectiveSlamCooldown());

            if (!phase3Active) yield break;
            if (IsBossPaused()) continue;

            // ── Fase de Telegrafagem ──────────────────────────────────────────
            // Captura a posição do player no início do telegraph
            Vector3 targetPosition = GetPlayerPosition();

            if (showDebugLog)
                Debug.Log("[Phase3AcidPuddles] 💢 Acid Slam — Telegrafando...");

            // Opcional: conectar animação aqui via bossController.animator.SetTrigger(...)
            yield return new WaitForSeconds(slamTelegraphDuration);

            // ── Impacto ───────────────────────────────────────────────────────
            if (!phase3Active) yield break;
            if (bossController != null && bossController.IsDead) yield break;

            SpawnPuddleAt(targetPosition);

            if (showDebugLog)
                Debug.Log($"[Phase3AcidPuddles] 💥 Acid Slam impactou em {targetPosition}");
        }
    }

    // =====================================================
    // SPAWNER
    // =====================================================

    private void SpawnPuddleAt(Vector3 position)
    {
        if (AcidPuddle.ActiveCount >= maxSimultaneousPuddles)
        {
            if (showDebugLog)
                Debug.Log($"[Phase3AcidPuddles] Cap atingido ({maxSimultaneousPuddles} poças). Spawn ignorado.");
            return;
        }

        Vector3 spawnPos = position;
        if (Physics.Raycast(position + Vector3.up * 2f, Vector3.down, out RaycastHit hit, 5f))
        {
            spawnPos = hit.point + Vector3.up * 0.05f;
        }

        if (acidPuddlePrefab != null)
        {
            Instantiate(acidPuddlePrefab, spawnPos, Quaternion.identity);
        }
        else
        {
            // Fallback Procedural de Poça Ácida com Trigger e DoT
            GameObject puddleObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            puddleObj.name = "AcidPuddle_Procedural";
            puddleObj.transform.position = spawnPos;
            puddleObj.transform.localScale = new Vector3(3.5f, 0.05f, 3.5f);

            Collider col = puddleObj.GetComponent<Collider>();
            if (col != null) col.isTrigger = true;

            Renderer rend = puddleObj.GetComponent<Renderer>();
            if (rend != null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                mat.color = new Color(0.2f, 0.95f, 0.2f, 0.65f);
                rend.material = mat;
            }

            AcidPuddle puddleComp = puddleObj.AddComponent<AcidPuddle>();
            puddleComp.damagePerTick = 6;
            puddleComp.tickInterval = 0.75f;
            puddleComp.slowPercent = 0.35f;
            puddleComp.lifetime = 7.0f;
        }

        if (showDebugLog)
            Debug.Log($"[Phase3AcidPuddles] 🟢 Poça ácida spawnou em {spawnPos} | Ativas: {AcidPuddle.ActiveCount + 1}/{maxSimultaneousPuddles}");
    }

    // =====================================================
    // HELPERS
    // =====================================================

    private bool IsBossPaused()
    {
        if (bossController == null) return false;
        return bossController.IsStunned || bossController.IsDead;
    }

    private float GetEffectiveSlamCooldown()
    {
        float multiplier = (phase3Modifiers != null) ? phase3Modifiers.AttackCooldownMultiplier : 1f;
        return slamCooldownBase * multiplier;
    }

    private Vector3 GetPlayerPosition()
    {
        if (playerTransform == null) return transform.position;
        return playerTransform.position;
    }
}
