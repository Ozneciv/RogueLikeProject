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
    private Color originalBaseColor;
    private bool hasBaseColor = false;
    private Renderer dummyRenderer;

    private void Start()
    {
        CurrentHealth = maxHealth;

        dummyRenderer = GetComponentInChildren<Renderer>();
        if (dummyRenderer != null)
        {
            originalRenderColor = dummyRenderer.material.color;
            // Suporte a shaders URP/HDRP que usam _BaseColor em vez de _Color
            if (dummyRenderer.material.HasProperty("_BaseColor"))
            {
                hasBaseColor = true;
                originalBaseColor = dummyRenderer.material.GetColor("_BaseColor");
            }
        }

        // Auto-busca o Slider nos filhos se não foi atribuído no Inspector
        if (healthBarSlider == null)
        {
            healthBarSlider = GetComponentInChildren<Slider>(true); // true = inclui desativados

            if (healthBarSlider == null)
                Debug.LogWarning("[" + gameObject.name + "] Slider de vida não encontrado! Crie um Canvas (World Space) com um Slider filho.");
            else
                Debug.Log("[" + gameObject.name + "] Slider encontrado automaticamente: " + healthBarSlider.gameObject.name);
        }

        // Busca a imagem Fill dentro do Slider para controlar a cor
        if (healthBarSlider != null)
        {
            if (healthBarSlider.fillRect != null)
            {
                healthBarFill = healthBarSlider.fillRect.GetComponent<Image>();
            }
            // Garante a cor inicial
            if (healthBarFill != null) healthBarFill.color = normalColor;

            // Começa escondida — só aparece após o primeiro hit
            healthBarSlider.gameObject.SetActive(false);
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

    public void TakeDamage(int damage, bool isCritical = false)
    {
        if (isInvulnerable) return;
        if (CurrentHealth <= 0) return;

        // Se estiver buffado, recebe dano reduzido (ex: metade)
        if (isBuffed) damage = Mathf.RoundToInt(damage * 0.5f);

        CurrentHealth -= damage;

        UpdateHealthBar();
        ShowHealthBar();

        if (floatingDamageTextPrefab != null)
        {
            GameObject textObject = Instantiate(floatingDamageTextPrefab, transform.position + textOffset, Quaternion.identity);
            FloatingDamageText dmgScript = textObject.GetComponent<FloatingDamageText>();
            if (dmgScript != null)
            {
                dmgScript.SetText(damage.ToString());
                dmgScript.SetCritical(isCritical);
            }
        }

        if (dummyRenderer != null)
        {
            StartCoroutine(FlashRed());
        }

        if (CurrentHealth <= 0)
        {
            CurrentHealth = 0;
            Die();
        }
    }

    void ShowHealthBar()
    {
        if (healthBarSlider == null) return;
        healthBarSlider.gameObject.SetActive(true);
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
        if (hasBaseColor) dummyRenderer.material.SetColor("_BaseColor", hitColor);
        yield return new WaitForSeconds(hitFlashTime);
        dummyRenderer.material.color = originalRenderColor;
        if (hasBaseColor) dummyRenderer.material.SetColor("_BaseColor", originalBaseColor);
    }

    private void Die()
    {
        Debug.Log(gameObject.name + " foi destruído.");

        // Chama o sistema de drops se existir
        // Busca em filhos e pais também — cobre hierarquias mais complexas de prefab
        EnemyDrops drops = GetComponent<EnemyDrops>()
                        ?? GetComponentInChildren<EnemyDrops>()
                        ?? GetComponentInParent<EnemyDrops>();
        if (drops != null)
        {
            Debug.Log("[DROPS] EnemyDrops encontrado, chamando OnDeath()...");
            drops.OnDeath();
        }
        else
        {
            Debug.LogWarning("[DROPS] EnemyDrops NÃO encontrado em " + gameObject.name + "! Adicione o componente EnemyDrops.");
        }

        Destroy(gameObject);
    }
}