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
    public float baseSpeed = 3.5f;

    [Tooltip("Velocidade de rotação do boss em graus/segundo.")]
    public float rotationSpeed = 120f;

    // =====================================================
    // ATAQUES BASE (provisórios — cada fase pode ter seus próprios)
    // =====================================================

    [Header("Ataque Melee Base")]
    [Tooltip("Dano do golpe corpo a corpo básico.")]
    public int baseMeleeDamage = 25;

    [Tooltip("Range do ataque melee.")]
    public float baseMeleeRange = 4f;

    [Tooltip("Cooldown do ataque melee (segundos).")]
    public float baseMeleeCooldown = 2.5f;
}
