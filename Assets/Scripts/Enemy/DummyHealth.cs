using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class DummyHealth : MonoBehaviour
{
    [Header("Vida")]
    public int maxHealth = 100;
    public int CurrentHealth { get; private set; }

    [Header("Referências UI")]
    public Slider healthBarSlider;
    // A imagem que realmente tem a cor (dentro do Slider)
    private Image healthBarFill; 

    [Header("Cores da Barra")]
    public Color normalColor = Color.red;
    [Tooltip("Cor da barra quando está sendo protegido pelo Sintonizador.")]
    public Color buffedColor = Color.cyan;

    [Header("Feedback de Dano")]
    public GameObject floatingDamageTextPrefab;
    public Vector3 textOffset = new Vector3(0, 2f, 0);
    public Color hitColor = Color.red;
    public float hitFlashTime = 0.2f;
    
    [HideInInspector] public bool isInvulnerable = false; 
    [HideInInspector] public bool isBuffed = false;

    private Color originalRenderColor;
    private Renderer dummyRenderer;

    private void Start()
    {
        CurrentHealth = maxHealth;

        dummyRenderer = GetComponentInChildren<Renderer>();
        if (dummyRenderer != null)
        {
            originalRenderColor = dummyRenderer.material.color;
        }

        // Tenta encontrar a imagem de preenchimento dentro do Slider automaticamente
        if (healthBarSlider != null)
        {
            if (healthBarSlider.fillRect != null)
            {
                healthBarFill = healthBarSlider.fillRect.GetComponent<Image>();
            }
            // Garante a cor inicial
            if (healthBarFill != null) healthBarFill.color = normalColor;
        }

        UpdateHealthBar();
    }

    // --- NOVA FUNÇÃO CHAMADA PELO SINTONIZADOR ---
    public void SetBuffedStatus(bool buffed)
    {
        isBuffed = buffed;
        
        // Muda a cor da barra
        if (healthBarFill != null)
        {
            healthBarFill.color = buffed ? buffedColor : normalColor;
        }
    }

    public void TakeDamage(int damage)
    {
        if (isInvulnerable) return;
        if (CurrentHealth <= 0) return;

        // Se estiver buffado, recebe dano reduzido (ex: metade)
        if (isBuffed) damage = Mathf.RoundToInt(damage * 0.5f);

        CurrentHealth -= damage;

        UpdateHealthBar();

        if (floatingDamageTextPrefab != null)
        {
            GameObject textObject = Instantiate(floatingDamageTextPrefab, transform.position + textOffset, Quaternion.identity);
            FloatingDamageText dmgScript = textObject.GetComponent<FloatingDamageText>();
            if(dmgScript != null) dmgScript.SetText(damage.ToString());
        }

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
        dummyRenderer.material.color = originalRenderColor;
    }

    private void Die()
    {
        Debug.Log(gameObject.name + " foi destruído.");
        Destroy(gameObject);
    }
}