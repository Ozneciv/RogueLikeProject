using UnityEngine;
using System.Collections;

/// <summary>
/// Status de Eletrocutado (Debuff do Rastro Elétrico da Estrela).
///  • Dura 3 segundos por padrão.
///  • Causa dano por segundo (~5 HP/s) e aplica redução de velocidade (25% slow).
///  • Toda vez que o jogador toca no rastro novamente, o timer de 3s é RESTAURADO.
/// </summary>
public class ElectrocutedStatus : MonoBehaviour
{
    private int damagePerTick = 5;
    private float slowPercent = 0.50f; // Redução de 50% de velocidade (altamente perceptível!)
    private float duration = 3.0f;
    private float timer = 0f;

    private PlayerHealth playerHealth;
    private PlayerDebuffs playerDebuffs;
    private PlayerM playerM;
    private Coroutine electrocutedCoroutine;

    public static void ApplyElectrocuted(GameObject playerObj, int damage, float slow, float statusDuration)
    {
        if (playerObj == null) return;

        ElectrocutedStatus status = playerObj.GetComponent<ElectrocutedStatus>();
        if (status == null)
        {
            status = playerObj.AddComponent<ElectrocutedStatus>();
        }

        status.RefreshStatus(damage, slow, statusDuration);
    }

    public void RefreshStatus(int damage, float slow, float statusDuration)
    {
        damagePerTick = (damage > 0) ? damage : 5;
        slowPercent = (slow > 0f) ? slow : 0.50f;
        duration = statusDuration;

        // RESTAURA O TIMER DE 3 SEGUNDOS TODA VEZ QUE O JOGADOR PISA NO RASTRO NOVAMENTE
        timer = duration;

        if (playerHealth == null) playerHealth = GetComponent<PlayerHealth>() ?? GetComponentInParent<PlayerHealth>() ?? GetComponentInChildren<PlayerHealth>();
        if (playerDebuffs == null) playerDebuffs = GetComponent<PlayerDebuffs>() ?? GetComponentInParent<PlayerDebuffs>() ?? GetComponentInChildren<PlayerDebuffs>();
        if (playerM == null) playerM = GetComponent<PlayerM>() ?? GetComponentInParent<PlayerM>() ?? GetComponentInChildren<PlayerM>();

        // APLICA REDUÇÃO DE 50% NA VELOCIDADE DIRETAMENTE NO MOVEMENT DO PLAYER
        if (playerM != null)
        {
            playerM.debuffSpeedMultiplier = (1f - slowPercent);
        }
        if (playerDebuffs != null)
        {
            playerDebuffs.ApplySlow(slowPercent);
        }

        Debug.LogWarning($"⚡⚡ [STATUS ELETROCUTADO ATIVO] Velocidade do Player reduzida em {slowPercent * 100}%! | Dano: {damagePerTick}/s | Duração: {duration}s ⚡⚡");

        ElectricShockVFX.AttachToPlayer(gameObject, duration);

        if (electrocutedCoroutine == null)
        {
            electrocutedCoroutine = StartCoroutine(ElectrocutedRoutine());
        }
    }

    private IEnumerator ElectrocutedRoutine()
    {
        float tickTimer = 0f;

        // Aplica o primeiro tick imediato ao entrar em contato
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(damagePerTick, gameObject);
        }

        while (timer > 0f)
        {
            timer -= Time.deltaTime;
            tickTimer += Time.deltaTime;

            // Dano por segundo (~5/s) durante o status de eletrocutado
            if (tickTimer >= 1.0f)
            {
                tickTimer = 0f;
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(damagePerTick, gameObject);
                    ElectricShockVFX.AttachToPlayer(gameObject, 0.4f);
                }
            }

            yield return null;
        }

        // Fim dos 3 segundos do status: restaura a velocidade normal do player
        if (playerM != null)
        {
            playerM.debuffSpeedMultiplier = 1.0f;
        }
        if (playerDebuffs != null)
        {
            playerDebuffs.RemoveSlow();
        }

        electrocutedCoroutine = null;
        Destroy(this);
    }

    void OnDestroy()
    {
        if (playerM != null)
        {
            playerM.debuffSpeedMultiplier = 1.0f;
        }
        if (playerDebuffs != null)
        {
            playerDebuffs.RemoveSlow();
        }
    }
}
