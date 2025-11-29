using UnityEngine;
using UnityEngine.UI; // Necessário para o Slider
using System.Collections;

public class DummyHealth : MonoBehaviour
{
    [Header("Vida")]
    public int maxHealth = 100;
    public int CurrentHealth { get; private set; }

    [Header("Referências UI")]
    [Tooltip("Arraste o Slider da barra de vida aqui.")]
    public Slider healthBarSlider;
    
    [Header("Feedback de Dano")]
    public GameObject floatingDamageTextPrefab;
    public Vector3 textOffset = new Vector3(0, 2f, 0);
    public Color hitColor = Color.red;
    public float hitFlashTime = 0.2f;
    
    [HideInInspector] public bool isInvulnerable = false; 

    private Color originalColor;
    private Renderer dummyRenderer;

    private void Start()
    {
        CurrentHealth = maxHealth;

        // Procura o renderizador nos filhos (para funcionar com a estrutura Pai/Filho)
        dummyRenderer = GetComponentInChildren<Renderer>();
        
        if (dummyRenderer != null)
        {
            originalColor = dummyRenderer.material.color;
        }

        UpdateHealthBar();
    }

    public void TakeDamage(int damage)
    {
        if (isInvulnerable) return;

        if (CurrentHealth <= 0) return;

        CurrentHealth -= damage;

        UpdateHealthBar();

        // Texto Flutuante
        if (floatingDamageTextPrefab != null)
        {
            GameObject textObject = Instantiate(floatingDamageTextPrefab, transform.position + textOffset, Quaternion.identity);
            // Tenta pegar o script do texto (seja qual for o nome que você usou: FloatingDamageText)
            FloatingDamageText dmgScript = textObject.GetComponent<FloatingDamageText>();
            if(dmgScript != null) dmgScript.SetText(damage.ToString());
        }

        // Flash Vermelho
        if (dummyRenderer != null)
        {
            StopAllCoroutines(); 
            StartCoroutine(FlashRed());
        }

        if (CurrentHealth <= 0)
        {
            CurrentHealth = 0;
            Die();
        }
    }

    void UpdateHealthBar()
    {
        if (healthBarSlider != null)
        {
            float healthPercent = (float)CurrentHealth / maxHealth;
            healthBarSlider.value = healthPercent;
            
            if (CurrentHealth <= 0) healthBarSlider.gameObject.SetActive(false);
        }
    }

    IEnumerator FlashRed()
    {
        dummyRenderer.material.color = hitColor;
        yield return new WaitForSeconds(hitFlashTime);
        dummyRenderer.material.color = originalColor;
    }

    private void Die()
    {
        // --- REMOVIDO O BLOCO DO KAMIKAZE QUE CAUSAVA O ERRO ---
        
        Debug.Log(gameObject.name + " foi destruído.");
        Destroy(gameObject);
    }
}