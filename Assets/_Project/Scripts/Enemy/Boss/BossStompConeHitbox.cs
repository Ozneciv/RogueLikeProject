using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Hitbox Progressivo em formato de CONE para o VFX do Stomp (Pisada do Boss).
/// O dano cresce e viaja pelo chão acompanhando a expansão visual do efeito!
/// </summary>
public class BossStompConeHitbox : MonoBehaviour
{
    [Header("⚔️ Configurações de Dano")]
    [Tooltip("Dano causado ao atingir o jogador.")]
    public int damage = 35;

    [Tooltip("Força de empurrão (knockback).")]
    public float knockbackForce = 8.0f;

    [Tooltip("Elevação vertical do knockback.")]
    public float knockbackUpwardRatio = 0.35f;

    [Header("📐 Geometria do Cone")]
    [Tooltip("Alcance máximo frontal da onda no chão (em metros).")]
    public float maxDistance = 6.0f;

    [Tooltip("Ângulo total de abertura do cone (em graus, ex: 60° a 80°).")]
    [Range(20f, 180f)]
    public float coneAngle = 70f;

    [Tooltip("Tempo total de expansão da onda no solo (em segundos, sincronizado com o VFX).")]
    public float waveDuration = 0.40f;

    [Tooltip("Largura da faixa da frente de onda ativa (em metros).")]
    public float waveFrontThickness = 1.5f;

    [Header("🎯 Detecção")]
    [Tooltip("LayerMask do Jogador para otimização de colisão.")]
    public LayerMask targetLayers = ~0;

    [Header("Debug")]
    public bool showDebugLogs = true;
    public bool drawGizmos = true;

    private HashSet<GameObject> hitTargets = new HashSet<GameObject>();
    private Coroutine activeWaveRoutine;
    private GameObject bossOwner;

    private void Awake()
    {
        FindBossOwner();
    }

    private void OnEnable()
    {
        FindBossOwner();
        StartWave();
    }

    private void OnDisable()
    {
        if (activeWaveRoutine != null)
        {
            StopCoroutine(activeWaveRoutine);
            activeWaveRoutine = null;
        }
        hitTargets.Clear();
    }

    private void FindBossOwner()
    {
        if (bossOwner == null)
        {
            BossController boss = GetComponentInParent<BossController>();
            if (boss != null) bossOwner = boss.gameObject;
            else bossOwner = gameObject;
        }
    }

    /// <summary>
    /// Inicia a expansão progressiva do dano em cone
    /// </summary>
    public void StartWave(GameObject customOwner = null)
    {
        if (customOwner != null) bossOwner = customOwner;
        else FindBossOwner();

        if (activeWaveRoutine != null) StopCoroutine(activeWaveRoutine);
        activeWaveRoutine = StartCoroutine(ConeWaveRoutine());
    }

    private IEnumerator ConeWaveRoutine()
    {
        hitTargets.Clear();
        float timer = 0f;
        Vector3 origin = transform.position;

        while (timer < waveDuration)
        {
            timer += Time.deltaTime;
            float progress = Mathf.Clamp01(timer / waveDuration);
            float currentRadius = progress * maxDistance;

            // Pega todos os colisores no raio atual
            Collider[] colliders = Physics.OverlapSphere(origin, currentRadius + 0.5f, targetLayers);
            Vector3 forwardDir = transform.forward;
            forwardDir.y = 0f;
            if (forwardDir == Vector3.zero) forwardDir = Vector3.forward;
            forwardDir.Normalize();

            foreach (Collider col in colliders)
            {
                if (col == null || col.isTrigger) continue;
                if (!col.CompareTag("Player") && !col.name.ToLower().Contains("player")) continue;

                GameObject targetObj = col.gameObject;
                if (hitTargets.Contains(targetObj)) continue;

                // Verifica se está dentro da frente de onda ativa
                Vector3 toTarget = col.transform.position - origin;
                toTarget.y = 0f;
                float dist = toTarget.magnitude;

                if (dist >= Mathf.Max(0f, currentRadius - waveFrontThickness) && dist <= currentRadius + 0.8f)
                {
                    // Verifica se está dentro do ângulo do CONE
                    Vector3 dirToTarget = toTarget.normalized;
                    float angle = Vector3.Angle(forwardDir, dirToTarget);

                    if (angle <= (coneAngle * 0.5f))
                    {
                        // ACERTOU DENTRO DO CONE!
                        hitTargets.Add(targetObj);
                        ApplyDamageAndKnockback(col, origin);
                    }
                }
            }

            yield return null;
        }

        activeWaveRoutine = null;
    }

    private void ApplyDamageAndKnockback(Collider col, Vector3 origin)
    {
        // 1. Dano
        PlayerHealth ph = col.GetComponent<PlayerHealth>() ?? col.GetComponentInParent<PlayerHealth>();
        if (ph != null)
        {
            ph.TakeDamage(damage, bossOwner);
            if (showDebugLogs) Debug.Log($"💥 [STOMP CONE HITBOX] Jogador atingido pela onda de choque do Stomp! Dano: {damage}");
        }

        // 2. Knockback
        Rigidbody rb = col.GetComponent<Rigidbody>() ?? col.GetComponentInParent<Rigidbody>();
        if (rb != null && !rb.isKinematic)
        {
            Vector3 pushDir = (col.transform.position - origin);
            pushDir.y = 0f;
            if (pushDir == Vector3.zero) pushDir = transform.forward;
            pushDir = (pushDir.normalized + Vector3.up * knockbackUpwardRatio).normalized;

            rb.linearVelocity = Vector3.zero;
            rb.AddForce(pushDir * knockbackForce, ForceMode.Impulse);
        }

        // Camera Shake
        BossController.TriggerCameraShake(0.40f, 0.25f);
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;

        Gizmos.color = new Color(0f, 0.9f, 1f, 0.35f);
        Vector3 origin = transform.position;
        Vector3 forward = transform.forward;
        forward.y = 0f;
        if (forward == Vector3.zero) forward = Vector3.forward;
        forward.Normalize();

        Quaternion leftRot = Quaternion.Euler(0, -coneAngle * 0.5f, 0);
        Quaternion rightRot = Quaternion.Euler(0, coneAngle * 0.5f, 0);

        Vector3 leftDir = leftRot * forward;
        Vector3 rightDir = rightRot * forward;

        Gizmos.DrawLine(origin, origin + leftDir * maxDistance);
        Gizmos.DrawLine(origin, origin + rightDir * maxDistance);
        Gizmos.DrawWireSphere(origin + forward * maxDistance, 0.5f);
    }
}
