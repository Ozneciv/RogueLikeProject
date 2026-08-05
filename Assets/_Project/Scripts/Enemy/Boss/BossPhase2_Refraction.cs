using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Controlador da Fase 2 — Refração de Luz / Invisibilidade
/// 
/// O Boss ativa a refração (invisibilidade) cada vez que perde 30% de vida,
/// podendo usar a habilidade até 3 vezes durante a luta.
/// 
/// COMPORTAMENTO:
///   1. Ao cruzar um limiar de HP, o boss desaparece (fade out com shimmer)
///   2. Fica invulnerável e se reposiciona ao redor do player (flanqueia)
///   3. Após a duração, reaparece (fade in) e volta a lutar
///   
/// LIMIARES DE ATIVAÇÃO (configuráveis):
///   - Uso 1: 70% HP (início da Fase 2)
///   - Uso 2: 52% HP  
///   - Uso 3: 35% HP (final da Fase 2)
///   
/// SETUP NO UNITY:
///   1. Adicione este script no mesmo GameObject do BossController
///   2. Não precisa de configuração extra — se inscreve nos BossEvents automaticamente
///   3. Ajuste os parâmetros no Inspector se necessário
/// </summary>
[RequireComponent(typeof(BossController))]
[AddComponentMenu("Boss/BossPhase2_Refraction")]
public class BossPhase2_Refraction : MonoBehaviour
{
    // ── Referências ──────────────────────────────────────────────
    private BossController bossController;
    private DummyHealth health;
    private NavMeshAgent agent;
    private Transform playerTransform;
    private Renderer[] renderers;
    private List<MaterialData> originalMaterialData = new List<MaterialData>();

    // ── Configuração da Refração ────────────────────────────────
    [Header("Refração de Luz (Invisibilidade)")]
    [Tooltip("Número máximo de usos da refração durante a luta")]
    public int maxRefractionUses = 3;

    [Tooltip("Duração do efeito de refração (segundos)")]
    public float refractionDuration = 4f;

    [Tooltip("Tempo para transicionar para invisível (fade out)")]
    public float fadeOutTime = 0.6f;

    [Tooltip("Tempo para transicionar para visível (fade in)")]
    public float fadeInTime = 0.9f;

    [Tooltip("Opacidade mínima durante refração (0 = totalmente invisível)")]
    [Range(0f, 0.2f)]
    public float minOpacity = 0f;

    [Header("Reposicionamento")]
    [Tooltip("Distância de reposicionamento ao redor do player")]
    public float repositionDistance = 8f;

    [Tooltip("Velocidade de reposicionamento durante refração")]
    public float repositionSpeed = 10f;

    [Header("Efeito Visual")]
    [Tooltip("Cor do brilho Fresnel durante refração")]
    public Color refractionGlowColor = new Color(0.5f, 0.8f, 1f, 0.3f);

    [Tooltip("Intensidade do shimmer (tremulação)")]
    public float shimmerIntensity = 0.15f;

    [Tooltip("Velocidade do shimmer")]
    public float shimmerSpeed = 8f;

    [Header("Limiares de HP para Refração")]
    [Tooltip("Porcentagens de HP que ativam a refração (0.0 a 1.0). Devem estar em ordem decrescente.")]
    public float[] refractionThresholds = { 0.70f, 0.52f, 0.35f };

    [Header("Debug")]
    public bool showDebugLog = true;

    // ── Estado Interno ──────────────────────────────────────────
    private int refractionUsesRemaining;
    private int nextThresholdIndex = 0;
    private bool isRefracting = false;
    private bool isPhase2Active = false;
    private Coroutine refractionCoroutine;
    private float originalAgentSpeed = -1f;

    // ── Cache para restaurar materiais ──────────────────────────
    private struct MaterialData
    {
        public Material material;
        public Color originalColor;
        public Color originalBaseColor;
        public bool hasColor;
        public bool hasBaseColor;
        public float originalAlpha;
        public int originalRenderQueue;
        public Shader originalShader;
    }

    // ═══════════════════════════════════════════════════════════════
    // LIFECYCLE
    // ═══════════════════════════════════════════════════════════════

    void Awake()
    {
        bossController = GetComponent<BossController>();
        health = GetComponent<DummyHealth>();
        agent = GetComponent<NavMeshAgent>();
    }

    void OnEnable()
    {
        BossEvents.OnPhaseChanged += OnPhaseChanged;
        BossEvents.OnBossFightStarted += OnFightStarted;
        BossEvents.OnBossDefeated += OnBossDefeated;
        if (bossController != null) bossController.OnTookDamage += OnTookDamage;
    }

    void OnDisable()
    {
        BossEvents.OnPhaseChanged -= OnPhaseChanged;
        BossEvents.OnBossFightStarted -= OnFightStarted;
        BossEvents.OnBossDefeated -= OnBossDefeated;
        if (bossController != null) bossController.OnTookDamage -= OnTookDamage;
    }

    void Start()
    {
        refractionUsesRemaining = maxRefractionUses;

        // Cacheia renderers e materiais originais
        renderers = GetComponentsInChildren<Renderer>();
        CacheOriginalMaterials();
    }

    void Update()
    {
        if (!isPhase2Active || isRefracting) return;
        if (bossController.IsDead || bossController.IsStunned) return;

        // Monitora HP para ativar refração
        CheckRefractionThreshold();
    }

    // ═══════════════════════════════════════════════════════════════
    // EVENTOS DO BOSS
    // ═══════════════════════════════════════════════════════════════

    void OnFightStarted()
    {
        // Busca o player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) playerTransform = player.transform;
    }

    void OnPhaseChanged(int newPhase)
    {
        if (newPhase == 2)
        {
            isPhase2Active = true;
            if (showDebugLog) Debug.Log("[BossPhase2] 👁️ Fase 2 ATIVADA — Refração de Luz!");

            // A primeira refração ativa imediatamente ao entrar na fase 2
            if (refractionUsesRemaining > 0 && nextThresholdIndex == 0)
            {
                nextThresholdIndex++;
                refractionUsesRemaining--;
                ActivateRefraction();
            }
        }
        else if (newPhase == 3)
        {
            // Desativa a fase 2 quando entra na fase 3
            isPhase2Active = false;

            // Se estava refratando, cancela
            if (isRefracting)
            {
                CancelRefraction();
            }

            if (showDebugLog) Debug.Log("[BossPhase2] Fase 2 DESATIVADA — entrando na Fase 3.");
        }
    }

    void OnBossDefeated()
    {
        // Se morreu durante refração, restaura visual
        if (isRefracting)
        {
            CancelRefraction();
        }
        isPhase2Active = false;
    }

    // ═══════════════════════════════════════════════════════════════
    // SISTEMA DE REFRAÇÃO
    // ═══════════════════════════════════════════════════════════════

    void CheckRefractionThreshold()
    {
        if (refractionUsesRemaining <= 0 || nextThresholdIndex >= refractionThresholds.Length) return;
        if (health == null) return;

        float healthPercent = bossController.HealthPercent;
        float currentThreshold = refractionThresholds[nextThresholdIndex];

        if (healthPercent <= currentThreshold)
        {
            nextThresholdIndex++;
            refractionUsesRemaining--;
            ActivateRefraction();
        }
    }

    void ActivateRefraction()
    {
        if (isRefracting) return;
        refractionCoroutine = StartCoroutine(RefractionRoutine());

        int useNumber = maxRefractionUses - refractionUsesRemaining;
        if (showDebugLog)
            Debug.Log($"[BossPhase2] 👁️ Refração ATIVADA! (uso {useNumber}/{maxRefractionUses}, HP: {bossController.HealthPercent * 100:F0}%)");
    }

    private void OnTookDamage()
    {
        if (isRefracting)
        {
            if (showDebugLog)
                Debug.Log("[BossPhase2] 💥 Boss foi atingido durante a invisibilidade! Invisibilidade CANCELADA/REVELADA!");

            CancelRefraction();
        }
    }

    void CancelRefraction()
    {
        if (refractionCoroutine != null)
        {
            StopCoroutine(refractionCoroutine);
            refractionCoroutine = null;
        }

        isRefracting = false;
        bossController.SetRefraction(false);
        if (health != null) health.isInvulnerable = false;
        RestoreOriginalMaterials();
        SetMaterialsTransparent(false);

        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            if (originalAgentSpeed > 0f)
                agent.speed = originalAgentSpeed;
        }

        // Restaura a barra de vida que é escondida durante a refração
        if (health != null && health.healthBarSlider != null)
            health.healthBarSlider.gameObject.SetActive(true);
    }

    IEnumerator RefractionRoutine()
    {
        isRefracting = true;

        // Notifica o BossController e sistema de eventos
        bossController.SetRefraction(true);

        // Libera o NavMeshAgent para andar por aí imediatamente
        originalAgentSpeed = agent != null ? agent.speed : 5f;
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            if (repositionSpeed > 0f) agent.speed = repositionSpeed;
            if (playerTransform != null) agent.SetDestination(playerTransform.position);
        }

        // ── 1. Fade Out → Invisível ──────────────────────────────
        yield return StartCoroutine(FadeRenderers(1f, minOpacity, fadeOutTime));

        // ── 2. Permite tomar dano (não fica invulnerável) e esconde barra de vida ───
        if (health != null) health.isInvulnerable = false;

        if (health != null && health.healthBarSlider != null)
            health.healthBarSlider.gameObject.SetActive(false);

        // Calcula um ponto inicial de reposicionamento ao redor do player
        yield return StartCoroutine(RepositionRoutine());

        // ── 4. Espera a duração da refração enquanto flanqueia/anda ao redor do player com shimmer ─────────
        float currentAngle = UnityEngine.Random.Range(45f, 315f);
        Vector3 flankTarget = GetFlankPointAroundPlayer(currentAngle);
        float angleChangeTimer = 0f;

        float waitTimer = 0f;
        while (waitTimer < refractionDuration)
        {
            waitTimer += Time.deltaTime;
            angleChangeTimer += Time.deltaTime;

            ApplyShimmerEffect();

            // A cada 1.0s ou ao se aproximar do ponto, avança o ângulo para orbitar ao redor do player
            if (angleChangeTimer >= 1.0f || (playerTransform != null && Vector3.Distance(transform.position, flankTarget) < 2.5f))
            {
                angleChangeTimer = 0f;
                currentAngle += UnityEngine.Random.Range(45f, 90f);
                flankTarget = GetFlankPointAroundPlayer(currentAngle);
            }

            // Move em direção ao ponto de flanqueamento (orbitando ao redor do player à distância)
            if (playerTransform != null)
            {
                if (agent != null && agent.enabled && agent.isOnNavMesh)
                {
                    agent.isStopped = false;
                    agent.SetDestination(flankTarget);
                }
                else
                {
                    float speed = repositionSpeed > 0f ? repositionSpeed : 5f;
                    Vector3 target = flankTarget;
                    target.y = transform.position.y;
                    if (Vector3.Distance(transform.position, target) > 0.5f)
                    {
                        transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
                        Vector3 lookDir = target - transform.position;
                        if (lookDir.sqrMagnitude > 0.01f)
                            transform.rotation = Quaternion.LookRotation(lookDir);
                    }
                }
            }

            yield return null;
        }

        // Restaura velocidade original
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.speed = originalAgentSpeed;
        }

        // ── 5. Fade In → Visível ────────────────────────────────
        yield return StartCoroutine(FadeRenderers(minOpacity, 1f, fadeInTime));

        // ── 6. Remove invulnerabilidade ─────────────────────────
        if (health != null) health.isInvulnerable = false;

        RestoreOriginalMaterials();
        bossController.SetRefraction(false);

        // Restaura a barra de vida
        if (health != null && health.healthBarSlider != null)
            health.healthBarSlider.gameObject.SetActive(true);

        // Reativa o NavMeshAgent para o modo normal
        if (agent != null && agent.enabled && agent.isOnNavMesh)
            agent.isStopped = false;

        isRefracting = false;
        refractionCoroutine = null;

        if (showDebugLog)
            Debug.Log($"[BossPhase2] 👁️ Refração ENCERRADA. Usos restantes: {refractionUsesRemaining}");
    }

    IEnumerator RepositionRoutine()
    {
        if (playerTransform == null) yield break;

        float randomAngle = UnityEngine.Random.Range(90f, 270f);
        Vector3 dirFromPlayer = Quaternion.Euler(0, randomAngle, 0) * playerTransform.forward;
        Vector3 targetPos = playerTransform.position + dirFromPlayer * repositionDistance;

        // Valida a posição no NavMesh com raio de busca seguro
        if (NavMesh.SamplePosition(targetPos, out NavMeshHit navHit, 3.0f, NavMesh.AllAreas))
        {
            targetPos = navHit.position;
        }

        // Em vez de Warp brusco (que pode fazer clipar no chão), navega até o ponto
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.SetDestination(targetPos);
        }

        // Pequeno delay para o shimmer visual no início do reposicionamento
        float shimmerDelay = 0.3f;
        float elapsed = 0f;
        while (elapsed < shimmerDelay)
        {
            elapsed += Time.deltaTime;
            ApplyShimmerEffect();
            yield return null;
        }
    }

    private Vector3 GetFlankPointAroundPlayer(float angleDegrees)
    {
        if (playerTransform == null) return transform.position;

        float dist = repositionDistance > 0f ? repositionDistance : 7f;
        Vector3 offset = Quaternion.Euler(0, angleDegrees, 0) * Vector3.forward * dist;
        Vector3 targetPos = playerTransform.position + offset;

        if (NavMesh.SamplePosition(targetPos, out NavMeshHit navHit, 4.0f, NavMesh.AllAreas))
        {
            return navHit.position;
        }
        return targetPos;
    }

    // ═══════════════════════════════════════════════════════════════
    // EFEITOS VISUAIS
    // ═══════════════════════════════════════════════════════════════

    void SetMaterialAlpha(MaterialData data, float alpha)
    {
        if (data.material == null) return;

        if (data.hasColor)
        {
            Color c = data.material.color;
            c.a = alpha;
            data.material.color = c;
        }

        if (data.hasBaseColor)
        {
            Color bc = data.material.GetColor("_BaseColor");
            bc.a = alpha;
            data.material.SetColor("_BaseColor", bc);
        }
    }

    void CacheOriginalMaterials()
    {
        if (renderers == null || renderers.Length == 0)
            renderers = GetComponentsInChildren<Renderer>(true);

        originalMaterialData.Clear();
        foreach (Renderer rend in renderers)
        {
            if (rend == null) continue;
            foreach (Material mat in rend.materials)
            {
                if (mat == null) continue;
                MaterialData data = new MaterialData
                {
                    material = mat,
                    hasColor = mat.HasProperty("_Color"),
                    hasBaseColor = mat.HasProperty("_BaseColor"),
                    originalRenderQueue = mat.renderQueue,
                    originalShader = mat.shader,
                };

                if (data.hasColor)
                {
                    data.originalColor = mat.color;
                    data.originalAlpha = mat.color.a;
                }
                if (data.hasBaseColor)
                {
                    data.originalBaseColor = mat.GetColor("_BaseColor");
                    if (!data.hasColor) data.originalAlpha = data.originalBaseColor.a;
                }

                originalMaterialData.Add(data);
            }
        }
    }

    IEnumerator FadeRenderers(float fromAlpha, float toAlpha, float duration)
    {
        if (toAlpha < 1f)
            SetMaterialsTransparent(true);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float currentAlpha = Mathf.Lerp(fromAlpha, toAlpha, Mathf.SmoothStep(0f, 1f, t));

            foreach (MaterialData data in originalMaterialData)
            {
                if (data.material == null) continue;

                SetMaterialAlpha(data, currentAlpha);

                if (currentAlpha < 0.5f && data.material.HasProperty("_EmissionColor"))
                {
                    float glowIntensity = (1f - currentAlpha) * 0.3f;
                    data.material.SetColor("_EmissionColor", refractionGlowColor * glowIntensity);
                    data.material.EnableKeyword("_EMISSION");
                }
            }
            yield return null;
        }

        foreach (MaterialData data in originalMaterialData)
        {
            if (data.material == null) continue;
            SetMaterialAlpha(data, toAlpha);
        }

        if (toAlpha >= 1f)
            SetMaterialsTransparent(false);
    }

    void SetMaterialsTransparent(bool transparent)
    {
        foreach (MaterialData data in originalMaterialData)
        {
            if (data.material == null) continue;

            if (transparent)
            {
                data.material.SetFloat("_Surface", 1);
                data.material.SetFloat("_Blend", 0);
                data.material.SetOverrideTag("RenderType", "Transparent");
                data.material.renderQueue = 3000;
                data.material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                data.material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                data.material.SetInt("_ZWrite", 1);
                data.material.EnableKeyword("_ALPHABLEND_ON");
                data.material.DisableKeyword("_ALPHATEST_ON");
                data.material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                data.material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }
            else
            {
                data.material.shader = data.originalShader;
                data.material.renderQueue = data.originalRenderQueue;
                data.material.SetOverrideTag("RenderType", "Opaque");
                data.material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                data.material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                data.material.SetInt("_ZWrite", 1);
                data.material.DisableKeyword("_ALPHABLEND_ON");
                data.material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
                data.material.SetFloat("_Surface", 0);

                if (data.material.HasProperty("_EmissionColor"))
                {
                    data.material.SetColor("_EmissionColor", Color.black);
                    data.material.DisableKeyword("_EMISSION");
                }
            }
        }
    }

    void ApplyShimmerEffect()
    {
        if (!isRefracting) return;

        float shimmer = minOpacity + Mathf.Abs(Mathf.Sin(Time.time * shimmerSpeed)) * shimmerIntensity;

        foreach (MaterialData data in originalMaterialData)
        {
            if (data.material == null) continue;
            SetMaterialAlpha(data, shimmer);
        }
    }

    void RestoreOriginalMaterials()
    {
        foreach (MaterialData data in originalMaterialData)
        {
            if (data.material == null) continue;
            if (data.hasColor) data.material.color = data.originalColor;
            data.material.renderQueue = data.originalRenderQueue;
            data.material.shader = data.originalShader;

            if (data.hasBaseColor)
                data.material.SetColor("_BaseColor", data.originalBaseColor);

            if (data.material.HasProperty("_EmissionColor"))
            {
                data.material.SetColor("_EmissionColor", Color.black);
                data.material.DisableKeyword("_EMISSION");
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // DEBUG
    // ═══════════════════════════════════════════════════════════════

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 1f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, repositionDistance);
    }
#endif
}
