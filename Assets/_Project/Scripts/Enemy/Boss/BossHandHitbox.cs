using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum HandSide
{
    Left,
    Right
}

/// <summary>
/// Script de Hitbox & Trail exclusivo de UMA mão específica do Boss.
///  • SEM TELEPORTES: Removido qualquer deslocamento bruto de posição.
///  • TRAVA ANTI-MORTE INSTANTÂNEA: Garante exatamente 1 hit por ataque por mão (hasHitPlayerThisAttack).
///  • CONTROLE TOTAL DE TRAIL VIA ANIMAÇÃO: Ativa o trail apenas quando chamado pelos Animation Events.
/// </summary>
public class BossHandHitbox : MonoBehaviour
{
    [Header("Identificação da Mão")]
    public HandSide handSide = HandSide.Right;

    [Header("GameObject da Mão para o Trail (Inspector)")]
    [Tooltip("Arraste aqui o GameObject 'hand' (coordenada/ponto na mão) para o Trail seguir exatamente ele.")]
    public Transform handTransform;

    [Header("Configurações de Dano & Hitbox desta Mão")]
    public Collider handCollider;
    public int damage = 35;
    public float knockbackForce = 6.0f;
    public float raioVarreduraArco = 3.0f;

    [Header("Sincronização com Animação (Fallback)")]
    [Tooltip("Tempo de espera (em segundos) entre o início da animação e a pancada real.")]
    public float hitWindupDelay = 0.40f;
    public float duracaoJanelaImpacto = 0.25f;

    [Header("VFX & Trail Exclusivo desta Mão")]
    public BossMeleeTrail meleeTrail;
    public GameObject vfxImpactoChaoPrefab;
    public GameObject vfxAtaqueMeleePrefab;

    private bool isActive = false;
    private bool hasHitPlayerThisAttack = false; // TRAVA RIGOROSA CONTRA MULTI-HIT DE MORTE INSTANTÂNEA
    private BossController bossController;
    private Coroutine attackRoutine;

    void Awake()
    {
        if (handCollider == null)
            handCollider = GetComponent<Collider>();

        if (handCollider != null)
        {
            handCollider.isTrigger = true;
            handCollider.enabled = false;
        }

        bossController = GetComponentInParent<BossController>();
        
        // Garante que o Trail esteja posicionado no GameObject hand da mão
        Transform targetTrailTransform = handTransform != null ? handTransform : transform;
        if (meleeTrail == null)
            meleeTrail = targetTrailTransform.GetComponent<BossMeleeTrail>() ?? targetTrailTransform.gameObject.AddComponent<BossMeleeTrail>();
    }

    /// <summary>
    /// Inicia a janela de ataque (modo fallback).
    /// </summary>
    public void EnableHitbox(float totalDuration, int attackDamage = 35, float pushForce = 15f)
    {
        damage = attackDamage;
        knockbackForce = pushForce;
        hasHitPlayerThisAttack = false;

        if (attackRoutine != null) StopCoroutine(attackRoutine);
        attackRoutine = StartCoroutine(RotinaAtaqueFallback(totalDuration));
    }

    private IEnumerator RotinaAtaqueFallback(float totalDuration)
    {
        // Vira para o player de forma natural, SEM teleportar o corpo
        Transform rootBoss = bossController != null ? bossController.transform : transform.root;
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (rootBoss != null && player != null)
        {
            Vector3 lookDir = (player.transform.position - rootBoss.position).normalized;
            lookDir.y = 0f;
            if (lookDir != Vector3.zero) rootBoss.rotation = Quaternion.LookRotation(lookDir);
        }

        // Aguarda a mão descer na animação (hitWindupDelay)
        yield return new WaitForSeconds(hitWindupDelay);

        // Dispara o momento de impacto
        OnAnimationEvent_EnableHitbox();

        yield return new WaitForSeconds(duracaoJanelaImpacto);

        // Finaliza o golpe
        DisableHitbox();
    }

    // =========================================================================
    // ANIMATION EVENTS (Controlados 100% pelos marcadores da timeline do Unity)
    // =========================================================================

    /// <summary>
    /// Evento de Animação: Liga APENAS a Hitbox de dano no frame exato do impacto.
    /// </summary>
    public void OnAnimationEvent_EnableHitbox()
    {
        isActive = true;
        hasHitPlayerThisAttack = false; // Libera a trava para dar o 1° e único hit deste golpe
        if (handCollider != null) handCollider.enabled = true;
    }

    /// <summary>
    /// Evento de Animação: Liga APENAS o Trail no frame em que a mão começa o balanço.
    /// </summary>
    public void OnAnimationEvent_EnableTrailOnly()
    {
        if (meleeTrail != null) meleeTrail.AtivarTrail(0.45f);
    }

    /// <summary>
    /// Evento de Animação: Desliga APENAS o Trail no frame exato desejado.
    /// </summary>
    public void OnAnimationEvent_DisableTrailOnly()
    {
        if (meleeTrail != null) meleeTrail.DesativarTrail();
    }

    /// <summary>
    /// Evento de Animação: Desliga a Hitbox e apaga o Trail.
    /// </summary>
    public void OnAnimationEvent_DisableHitbox()
    {
        DisableHitbox();
    }

    /// <summary>
    /// Evento de Animação: Efeito de impacto no chão e trepidação da câmera.
    /// </summary>
    public void OnAnimationEvent_GroundImpact()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (vfxImpactoChaoPrefab != null && player != null)
        {
            Vector3 posImpacto = player.transform.position + Vector3.up * 0.1f;
            Instantiate(vfxImpactoChaoPrefab, posImpacto, Quaternion.identity);
        }
        BossController.TriggerCameraShake(0.3f, 0.2f);
    }

    public void DisableHitbox()
    {
        isActive = false;
        hasHitPlayerThisAttack = false;
        if (handCollider != null) handCollider.enabled = false;
        if (meleeTrail != null) meleeTrail.DesativarTrail();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isActive || hasHitPlayerThisAttack) return;
        if (other.CompareTag("Player"))
        {
            AplicarDanoEKnockbackNoPlayer(other.gameObject);
        }
    }

    private void Update()
    {
        // Varredura por área enquanto ativa (para o Boss alto acertas o player sem falhas de collider)
        if (!isActive || hasHitPlayerThisAttack) return;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            Transform rootBoss = bossController != null ? bossController.transform : transform.root;
            Vector3 origin = rootBoss != null ? rootBoss.position + Vector3.up * 1.0f : transform.position;
            Vector3 playerPos = player.transform.position + Vector3.up * 1.0f;
            float dist = Vector3.Distance(origin, playerPos);

            if (dist <= raioVarreduraArco)
            {
                Vector3 dirToPlayer = (playerPos - origin).normalized;
                Vector3 bossForward = rootBoss != null ? rootBoss.forward : transform.forward;
                float angle = Vector3.Angle(bossForward, dirToPlayer);

                if (angle <= 65f)
                {
                    AplicarDanoEKnockbackNoPlayer(player);
                }
            }
        }
    }

    private void AplicarDanoEKnockbackNoPlayer(GameObject player)
    {
        if (player == null || hasHitPlayerThisAttack) return;

        // TRAVA RIGOROSA ANTI-MORTE INSTANTÂNEA: Garante APENAS 1 HIT por ataque!
        hasHitPlayerThisAttack = true;

        PlayerHealth playerHealth = player.GetComponent<PlayerHealth>() ?? player.GetComponentInParent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(damage, gameObject);
        }

        Rigidbody playerRb = player.GetComponent<Rigidbody>() ?? player.GetComponentInParent<Rigidbody>();
        if (playerRb != null && !playerRb.isKinematic)
        {
            Vector3 dir = (player.transform.position - transform.position).normalized;
            dir.y = 0.25f;
            playerRb.AddForce(dir * knockbackForce, ForceMode.Impulse);
        }

        if (vfxAtaqueMeleePrefab != null)
        {
            Instantiate(vfxAtaqueMeleePrefab, player.transform.position + Vector3.up * 1.0f, Quaternion.identity);
        }

        BossController.TriggerCameraShake(0.3f, 0.2f);

        Debug.Log($"[BossHandHitbox - {handSide}] 💥 GOLPE ÚNICO DA MÃO {handSide.ToString().ToUpper()} ACERTOU O PLAYER! Dano: {damage}");
    }
}
