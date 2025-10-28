using UnityEngine;
using UnityEngine.UI; // Namespace para a UI

public class PlayerHealth : MonoBehaviour
{
    [Header("Configurações de Vida")]
    public int maxHealth = 100;
    private int currentHealth;

    // --- NOVAS VARIÁVEIS DE ARMADURA ---
    [Header("Configurações de Armadura")]
    public int maxArmor = 200;
    private int currentArmor;

    [Header("Componentes")]
    public PlayerM playerMovement;

    [Header("UI (Interface)")]
    public Slider healthBarSlider;
    // --- NOVA REFERÊNCIA PARA A UI DA ARMADURA ---
    [Tooltip("Arraste aqui a IMAGEM da sua barra de armadura.")]
    public Image armorBarImage;

    void Start()
    {
        currentHealth = maxHealth;
        currentArmor = maxArmor; // Inicia com armadura cheia
        UpdateHealthBar();
        UpdateArmorBar(); // Atualiza a barra de armadura no início
    }

    public void TakeDamage(int damage)
    {
        if (currentHealth <= 0) return;

        // --- LÓGICA DE DANO ATUALIZADA ---
        // Primeiro, o dano é aplicado à armadura.
        if (currentArmor > 0)
        {
            int damageToArmor = Mathf.Min(damage, currentArmor); // Calcula quanto dano a armadura pode absorver
            currentArmor -= damageToArmor;
            damage -= damageToArmor; // Subtrai o dano absorvido do total
            
            Debug.Log("Armadura absorveu " + damageToArmor + " de dano. Armadura restante: " + currentArmor);
            UpdateArmorBar();
        }

        // Se ainda houver dano restante (após a armadura ser quebrada), ele vai para a vida.
        if (damage > 0)
        {
            currentHealth -= damage;
            Debug.Log("Jogador recebeu " + damage + " de dano na vida. Vida atual: " + currentHealth);
            UpdateHealthBar();
        }

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            playerMovement.Die();
        }
    }

    // --- NOVA FUNÇÃO PARA RESTAURAR ARMADURA ---
    public void RestoreArmor(int amount)
    {
        currentArmor += amount;
        // Garante que a armadura não ultrapasse o valor máximo
        if (currentArmor > maxArmor)
        {
            currentArmor = maxArmor;
        }
        Debug.Log("Armadura restaurada! Valor atual: " + currentArmor);
        UpdateArmorBar();
    }

    private void UpdateHealthBar()
    {
        if (healthBarSlider != null)
        {
            healthBarSlider.value = (float)currentHealth / maxHealth;
        }
    }

    // --- NOVA FUNÇÃO PARA ATUALIZAR A BARRA DE ARMADURA ---
    private void UpdateArmorBar()
    {
        if (armorBarImage != null)
        {
            // Converte a armadura (ex: 150 de 200) para um valor entre 0 e 1 (ex: 0.75)
            armorBarImage.fillAmount = (float)currentArmor / maxArmor;
        }
    }
}