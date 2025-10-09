using UnityEngine;
using System.Collections; // Necessário para usar Coroutines

public class DamageZone : MonoBehaviour
{
    [Header("Configurações de Dano")]
    [Tooltip("A quantidade de dano que o jogador recebe a cada intervalo.")]
    public int damageAmount = 20;

    [Tooltip("O intervalo de tempo (em segundos) entre cada aplicação de dano.")]
    public float damageInterval = 1.0f;

    // Variáveis internas para controlar o processo
    private PlayerHealth playerHealth;
    private Coroutine damageCoroutine;

    private void OnTriggerEnter(Collider other)
    {
        // Quando o jogador ENTRA na área
        if (other.CompareTag("Player"))
        {
            // Pega a referência do script de vida do jogador
            playerHealth = other.GetComponent<PlayerHealth>();

            // Se encontrou o script de vida, inicia a coroutine que causa dano
            if (playerHealth != null)
            {
                // Inicia a coroutine e guarda uma referência a ela
                damageCoroutine = StartCoroutine(DealDamageOverTime());
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Quando o jogador SAI da área
        if (other.CompareTag("Player"))
        {
            // Se uma coroutine de dano estiver rodando, pare-a
            if (damageCoroutine != null)
            {
                StopCoroutine(damageCoroutine);
            }
            // Limpa a referência ao jogador
            playerHealth = null;
        }
    }

    private IEnumerator DealDamageOverTime()
    {
        Debug.Log("Jogador entrou na zona de dano.");
        // Este loop continuará enquanto o jogador estiver dentro da zona
        while (true)
        {
            // Causa dano no jogador
            playerHealth.TakeDamage(damageAmount);
            
            // Espera pelo intervalo de tempo definido antes de continuar o loop
            yield return new WaitForSeconds(damageInterval);
        }
    }
}