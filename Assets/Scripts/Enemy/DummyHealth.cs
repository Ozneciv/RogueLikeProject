using UnityEngine;
using System.Collections;

public class DummyHealth : MonoBehaviour
{
    [Header("Vida")]
    public int maxHealth = 100;
    // --- MUDANÇA 1: Propriedade para a Vida Atual ---
    // Outros scripts (como o TotemSpawner) podem LER a vida, mas apenas este script pode MODIFICÁ-LA.
    public int CurrentHealth { get; private set; }

    [Header("Feedback de Dano")]
    public GameObject floatingDamageTextPrefab;
    public Vector3 textOffset = new Vector3(0, 2f, 0);
    public Color hitColor = Color.red;
    public float hitFlashTime = 0.2f;
    
    private Color originalColor;
    private Renderer dummyRenderer;

    private void Start()
    {
        // --- MUDANÇA 2: Inicializar a Vida ---
        CurrentHealth = maxHealth;

        dummyRenderer = GetComponent<Renderer>();
        if (dummyRenderer != null)
        {
            originalColor = dummyRenderer.material.color;
        }
    }

    public void TakeDamage(int damage)
    {
        // Se já estiver com vida zero ou menos, não faz nada.
        if (CurrentHealth <= 0) return;

        // --- MUDANÇA 3: Subtrair o Dano da Vida ---
        CurrentHealth -= damage;

        Debug.Log(gameObject.name + " recebeu " + damage + " de dano. Vida restante: " + CurrentHealth);

        // Instancia o texto flutuante de dano
        if (floatingDamageTextPrefab != null)
        {
            GameObject textObject = Instantiate(floatingDamageTextPrefab, transform.position + textOffset, Quaternion.identity);
            textObject.GetComponent<FloatingDamageText>().SetText(damage.ToString());
        }

        // Feedback visual de flash vermelho
        if (dummyRenderer != null)
        {
            dummyRenderer.material.color = hitColor;
            Invoke("ResetColor", hitFlashTime);
        }

        // --- MUDANÇA 4: Checar se Morreu ---
        if (CurrentHealth <= 0)
        {
            CurrentHealth = 0; // Garante que a vida não fique negativa
            Die();
        }
    }

    private void ResetColor()
    {
        if (dummyRenderer != null)
        {
            dummyRenderer.material.color = originalColor;
        }
    }

    // --- MUDANÇA 5: Função de Morte ---
    private void Die()
    {
        Debug.Log(gameObject.name + " foi destruído.");
        
        // Opcional: Adicionar um efeito de explosão/morte aqui antes de destruir.
        // Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);

        // Remove o objeto do jogo.
        Destroy(gameObject);
    }
}