using UnityEngine;
using System.Collections;

/// <summary>
/// Poça Ácida — Fase 3 do Boss Cromático.
///
/// COMPORTAMENTO:
///   • Aplica dano contínuo (DoT) ao jogador enquanto ele estiver dentro do trigger
///   • Aplica lentidão (slow) ao entrar; o slow persiste enquanto o player estiver
///     em QUALQUER poça ativa (múltiplas sobreposições são tratadas corretamente)
///   • Se autodestrói após [lifetime] segundos
///
/// COMO CRIAR O PREFAB:
///   1. Crie um GameObject vazio e adicione este script
///   2. Adicione um CapsuleCollider → marque "Is Trigger"
///      Ajuste o raio do Collider para o tamanho visual desejado
///   3. Adicione um Material semi-transparente (verde ácido) no MeshRenderer
///   4. Salve como Prefab e arraste no BossPhase3AcidPuddles.acidPuddlePrefab
///
/// SOBRE OS CONTADORES ESTÁTICOS:
///   AcidPuddle.ActiveCount    → poças ativas na cena (lido pelo spawner para o cap)
///   Chame AcidPuddle.ResetStaticCounters() ao reiniciar a arena/run
/// </summary>
[RequireComponent(typeof(Collider))]
public class AcidPuddle : MonoBehaviour
{
    // =====================================================
    // INSPECTOR
    // =====================================================

    [Header("Dano Contínuo (DoT)")]
    [Tooltip("Dano aplicado ao jogador a cada tick enquanto estiver dentro da poça.")]
    public int damagePerTick = 5;

    [Tooltip("Intervalo em segundos entre cada tick de dano.\n" +
             "O primeiro tick ocorre após este intervalo (não imediato ao entrar).")]
    public float tickInterval = 0.8f;

    [Header("Lentidão")]
    [Tooltip("Redução de velocidade aplicada ao jogador. (0.3 = 30% mais lento)")]
    [Range(0.01f, 0.9f)]
    public float slowPercent = 0.3f;

    [Header("Duração")]
    [Tooltip("Tempo em segundos até a poça desaparecer automaticamente.")]
    public float lifetime = 8f;

    [Header("Debug")]
    public bool showDebugLog = false;

    // =====================================================
    // CONTADORES ESTÁTICOS
    // (compartilhados entre todas as instâncias de AcidPuddle)
    // =====================================================

    /// <summary>Número de poças ácidas ativas na cena neste momento.</summary>
    public static int ActiveCount { get; private set; } = 0;

    /// <summary>
    /// Número de poças que o jogador está sobrepondo simultaneamente.
    /// Garante que o slow só é removido ao sair da ÚLTIMA poça.
    /// </summary>
    private static int playerInsideCount = 0;

    // =====================================================
    // ESTADO DA INSTÂNCIA
    // =====================================================

    private bool playerIsInside = false;
    private PlayerHealth cachedPlayerHealth;
    private PlayerDebuffs cachedPlayerDebuffs;
    private Coroutine dotCoroutine;

    // =====================================================
    // UNITY LIFECYCLE
    // =====================================================

    void Start()
    {
        ActiveCount++;

        // Garante que o collider está configurado como trigger
        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
        {
            col.isTrigger = true;
            Debug.LogWarning("[AcidPuddle] Collider não estava como trigger — corrigido automaticamente.");
        }

        // Auto-destruição pelo tempo de vida
        Destroy(gameObject, lifetime);
    }

    void OnDestroy()
    {
        ActiveCount = Mathf.Max(0, ActiveCount - 1);

        // Edge-case: poça destruída (timeout) enquanto player ainda estava dentro
        if (playerIsInside)
        {
            playerIsInside = false;
            playerInsideCount = Mathf.Max(0, playerInsideCount - 1);

            if (playerInsideCount <= 0 && cachedPlayerDebuffs != null)
            {
                cachedPlayerDebuffs.RemoveSlow();
                if (showDebugLog)
                    Debug.Log("[AcidPuddle] Poça expirou com player dentro → slow removido.");
            }
        }
    }

    // =====================================================
    // TRIGGER — ENTRADA / SAÍDA
    // =====================================================

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        // Cache de componentes na primeira entrada
        if (cachedPlayerHealth == null) cachedPlayerHealth = other.GetComponent<PlayerHealth>();
        if (cachedPlayerDebuffs == null) cachedPlayerDebuffs = other.GetComponent<PlayerDebuffs>();

        playerIsInside = true;
        playerInsideCount++;

        // Aplica lentidão — PlayerDebuffs.ApplySlow trata re-aplicação automaticamente
        if (cachedPlayerDebuffs != null)
        {
            cachedPlayerDebuffs.ApplySlow(slowPercent);
            if (showDebugLog)
                Debug.Log($"[AcidPuddle] 🐌 Slow {slowPercent * 100:F0}% aplicado. " +
                          $"Poças sobrepostas: {playerInsideCount}");
        }

        // Inicia DoT desta instância
        if (dotCoroutine != null) StopCoroutine(dotCoroutine);
        dotCoroutine = StartCoroutine(DotRoutine());
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playerIsInside = false;
        playerInsideCount = Mathf.Max(0, playerInsideCount - 1);

        // Para o DoT desta instância
        if (dotCoroutine != null)
        {
            StopCoroutine(dotCoroutine);
            dotCoroutine = null;
        }

        // Remove slow APENAS quando o player sai de todas as poças
        if (playerInsideCount <= 0 && cachedPlayerDebuffs != null)
        {
            cachedPlayerDebuffs.RemoveSlow();
            if (showDebugLog)
                Debug.Log("[AcidPuddle] 🐌 Slow removido — player saiu de todas as poças.");
        }
        else if (showDebugLog && playerInsideCount > 0)
        {
            Debug.Log($"[AcidPuddle] Player saiu desta poça, mas ainda está em {playerInsideCount} poça(s). " +
                      "Slow mantido.");
        }
    }

    // =====================================================
    // DoT
    // =====================================================

    private IEnumerator DotRoutine()
    {
        // Primeiro tick ocorre após o intervalo (não imediato ao entrar)
        yield return new WaitForSeconds(tickInterval);

        while (playerIsInside)
        {
            if (cachedPlayerHealth != null)
            {
                cachedPlayerHealth.TakeDamage(damagePerTick, gameObject);
                if (showDebugLog)
                    Debug.Log($"[AcidPuddle] ☠️ DoT tick: -{damagePerTick} HP");
            }

            yield return new WaitForSeconds(tickInterval);
        }
    }

    // =====================================================
    // UTILITÁRIO ESTÁTICO
    // =====================================================

    /// <summary>
    /// Reseta os contadores estáticos para 0.
    /// Chame ao reiniciar a arena, mudar de cena ou iniciar uma nova Run.
    /// Exemplo: chamado por BossPhase3AcidPuddles.OnBossDefeated()
    /// </summary>
    public static void ResetStaticCounters()
    {
        ActiveCount       = 0;
        playerInsideCount = 0;
    }
}
