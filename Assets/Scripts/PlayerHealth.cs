using UnityEngine;
using UnityEngine.UI; // Namespace necessário para interagir com a UI

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHealth = 100;
    private int currentHealth;

    [Header("Components")]
    public PlayerM playerMovement; // Arraste o script PlayerM aqui

    [Header("UI")]
    public Slider healthBarSlider; // Arraste o componente Slider da sua barra de vida aqui

    void Start()
    {
        currentHealth = maxHealth;
        UpdateHealthBar();
    }

    public void TakeDamage(int damage)
    {
        if (currentHealth <= 0) return; // Não recebe mais dano se já estiver morrendo

        currentHealth -= damage;
        UpdateHealthBar();

        Debug.Log("Jogador recebeu " + damage + " de dano. Vida atual: " + currentHealth);

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            playerMovement.Die();
        }
    }

    private void UpdateHealthBar()
    {
        if (healthBarSlider != null)
        {
            // Converte a vida (ex: 80 de 100) para um valor entre 0 e 1 (ex: 0.8)
            healthBarSlider.value = (float)currentHealth / maxHealth;
        }
    }
}