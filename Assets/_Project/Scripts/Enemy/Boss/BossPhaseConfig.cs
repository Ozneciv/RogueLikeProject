using UnityEngine;

/// <summary>
/// ScriptableObject com os parâmetros de configuração do Boss Cromático.
///
/// COMO USAR:
///   1. No Unity: Assets → Create → Boss → Boss Phase Config
///   2. Preencha os valores no Inspector (ou use os defaults provisórios)
///   3. Arraste no campo "phaseConfig" do BossController
///
/// PARA O MATHEUS:
///   Você pode editar os valores numéricos aqui sem mexer em nenhum script.
///   Crie múltiplas configs para testar balanceamento diferente.
/// </summary>
[CreateAssetMenu(fileName = "BossPhaseConfig", menuName = "Boss/Boss Phase Config")]
public class BossPhaseConfig : ScriptableObject
{
    // =====================================================
    // VIDA
    // =====================================================

    [Header("Vida do Boss")]
    [Tooltip("HP máximo do boss. Provisório — Matheus define o valor final.")]
    public int maxHealth = 1500;

    // =====================================================
    // THRESHOLDS DE FASE
    // =====================================================

    [Header("Transições de Fase (% do HP)")]
    [Tooltip("HP% abaixo do qual entra na Fase 2 (0.0 a 1.0). Ex: 0.70 = 70% HP.")]
    [Range(0.01f, 0.99f)]
    public float phase2Threshold = 0.70f;

    [Tooltip("HP% abaixo do qual entra na Fase 3 (0.0 a 1.0). Ex: 0.35 = 35% HP.")]
    [Range(0.01f, 0.99f)]
    public float phase3Threshold = 0.35f;

    // =====================================================
    // MOVIMENTO
    // =====================================================

    [Header("Velocidade de Movimento")]
    [Tooltip("Velocidade base do NavMeshAgent nas fases 1 e 2.")]
    public float baseSpeed = 4.5f;

    [Tooltip("Velocidade de rotação do boss em graus/segundo.")]
    public float rotationSpeed = 160f;

    // =====================================================
    // ATAQUES BASE (provisórios — cada fase pode ter seus próprios)
    // =====================================================

    [Header("Ataque Melee Base")]
    [Tooltip("Dano do golpe corpo a corpo básico.")]
    public int baseMeleeDamage = 30;

    [Tooltip("Range do ataque melee.")]
    public float baseMeleeRange = 4.2f;

    [Tooltip("Cooldown do ataque melee (segundos).")]
    public float baseMeleeCooldown = 1.5f;

    // =====================================================
    // FASE 1 — SPAWN DE MOBS
    // =====================================================

    [Header("Fase 1 — Spawn de Mobs")]
    [Tooltip("Máximo de mobs do boss vivos simultaneamente na arena.")]
    public int phase1MaxMobs = 8;

    [Tooltip("Cooldown mínimo entre waves de mobs (segundos).")]
    public float phase1SpawnCooldown = 3f;

    [Tooltip("Quantos hits seguidos o player precisa dar para o boss contra-atacar com mobs.")]
    public int phase1HitCounterThreshold = 3;

    [Tooltip("Raio ao redor do centro da arena onde os mobs podem spawnar.")]
    public float phase1SpawnRadius = 15f;

    [Tooltip("% de HP para spawnar a primeira wave fixa (Totem). Ex: 0.95 = 95%.")]
    [Range(0.5f, 1.0f)]
    public float phase1FirstWaveThreshold = 0.95f;

    [Tooltip("% de HP para spawnar a segunda wave fixa (Spiders + Cristalus). Ex: 0.85 = 85%.")]
    [Range(0.5f, 1.0f)]
    public float phase1SecondWaveThreshold = 0.85f;
}