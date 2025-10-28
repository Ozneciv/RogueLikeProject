using UnityEngine;
using System.Collections;

public class DamageZone : MonoBehaviour
{
    [Header("Configurações de Dano")]
    [Tooltip("A quantidade de dano que o jogador recebe a cada intervalo.")]
    public int damageAmount = 20;
    [Tooltip("O intervalo de tempo (em segundos) entre cada aplicação de dano.")]
    public float damageInterval = 1.0f;

    // --- NOVA REFERÊNCIA AQUI ---
    [Header("Referências Visuais")]
    [Tooltip("Arraste o objeto 'AreaVisualizer' que tem o script PulseVisualizer aqui.")]
    public PulseVisualizer pulseVisualizer;

    private PlayerHealth playerHealth;
    private Coroutine damageCoroutine;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerHealth = other.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                damageCoroutine = StartCoroutine(DealDamageOverTime());
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (damageCoroutine != null)
            {
                StopCoroutine(damageCoroutine);
            }
            playerHealth = null;
        }
    }

    private IEnumerator DealDamageOverTime()
    {
        Debug.Log("Jogador entrou na zona de dano.");
        while (true)
        {
            // Causa o dano no jogador
            playerHealth.TakeDamage(damageAmount);
            
            // --- ATIVA O PULSO VISUAL ---
            if (pulseVisualizer != null)
            {
                pulseVisualizer.TriggerPulse();
            }
            
            // Espera pelo intervalo de tempo definido antes de continuar o loop
            yield return new WaitForSeconds(damageInterval);
        }
    }
}