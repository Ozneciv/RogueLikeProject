using UnityEngine;
using UnityEngine.AI;
using System.Collections;

/// <summary>
/// Fase 3 do Boss Cromático — Moveset da Floresta Tóxica.
///
/// RESPONSABILIDADES:
///   • Spawn aleatório de poças ácidas sob o jogador em intervalos randomizados
///   • Acid Spit: o boss cospe um projétil ácido pela boca em direção ao jogador
///   • Controla o limite máximo de poças simultâneas na arena
///   • Usa AttackCooldownMultiplier do BossPhase3Modifiers automaticamente (se presente)
///
/// SETUP NO UNITY:
///   1. Adicione este componente no mesmo GameObject do BossController
///   2. Crie um Prefab de poça: GameObject vazio → CapsuleCollider (isTrigger=true) + AcidPuddle.cs
///   3. Arraste o prefab no campo "acidPuddlePrefab"
///   4. (Cuspida) Reutilize o projétil já existente: crie um Prefab com Rigidbody + Collider (isTrigger)
///      + CrystalSpikeProjectile.cs (dê a ele um visual/material ácido) e arraste em "acidSpitProjectilePrefab"
///   5. BossController é encontrado automaticamente (ou arraste manualmente)
///
/// DEPENDÊNCIAS:
///   • BossPhase3Modifiers (opcional, no mesmo GameObject) — aplica -50% ao cooldown dos ataques
///   • AcidPuddle.cs — script do prefab da poça
///   • CrystalSpikeProjectile.cs — projétil já existente, reutilizado pela cuspida de ácido
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
    // SPAWNER
    // =====================================================

    private void SpawnPuddleAt(Vector3 position)
    {
        if (acidPuddlePrefab == null)
        {
            Debug.LogWarning("[Phase3AcidPuddles] ⚠️ acidPuddlePrefab não atribuído no Inspector!");
            return;
        }

        if (AcidPuddle.ActiveCount >= maxSimultaneousPuddles)
        {
            if (showDebugLog)
                Debug.Log($"[Phase3AcidPuddles] Cap atingido ({maxSimultaneousPuddles} poças). Spawn ignorado.");
            return;
        }

        Instantiate(acidPuddlePrefab, position, Quaternion.identity);

        if (showDebugLog)
            Debug.Log($"[Phase3AcidPuddles] 🟢 Poça spawnou em {position} | Ativas: {AcidPuddle.ActiveCount + 1}/{maxSimultaneousPuddles}");
    }

    // =====================================================
    // HELPERS
    // =====================================================

    private bool IsBossPaused()
    {
        if (bossController == null) return false;
        return bossController.IsStunned || bossController.IsDead;
    }

    private Vector3 GetPlayerPosition()
    {
        if (playerTransform == null) return transform.position;
        return playerTransform.position;
    }
}
