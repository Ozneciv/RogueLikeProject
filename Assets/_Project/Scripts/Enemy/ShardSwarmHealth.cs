using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Sistema de vida exclusivo do Shard Swarm.
/// Cópia do DummyHealth com método SetHealth() para permitir divisão de HP no Split.
/// </summary>
public class ShardSwarmHealth : MonoBehaviour
{
    [Header("Vida")]
    public int maxHealth = 100;
    public int CurrentHealth { get; private set; }

    [Header("Referências UI")]
    public Slider healthBarSlider;
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
            if (dummyRenderer.material.HasProperty("_BaseColor"))
            {
                hasBaseColor = true;
                originalBaseColor = dummyRenderer.material.GetColor("_BaseColor");
            }
        }

        if (healthBarSlider == null)
        {
            healthBarSlider = GetComponentInChildren<Slider>(true);

            if (healthBarSlider == null)
                Debug.LogWarning("[" + gameObject.name + "] Slider de vida não encontrado!");
            else
                Debug.Log("[" + gameObject.name + "] Slider encontrado: " + healthBarSlider.gameObject.name);
        }

        if (healthBarSlider != null)
        {
            if (healthBarSlider.fillRect != null)
            {
                healthBarFill = healthBarSlider.fillRect.GetComponent<Image>();
            }
            if (healthBarFill != null) healthBarFill.color = normalColor;
            healthBarSlider.gameObject.SetActive(false);
        }

        UpdateHealthBar();
    }

    public void SetBuffedStatus(bool buffed)
    {
        isBuffed = buffed;
        if (healthBarFill != null)
        {
            healthBarFill.color = buffed ? buffedColor : normalColor;
        }
    }

    /// <summary>
    /// Define o HP diretamente (usado pelo Split do ShardSwarm).
    /// </summary>
    public void SetHealth(int hp)
    {
        CurrentHealth = Mathf.Clamp(hp, 0, maxHealth);
        UpdateHealthBar();

        if (CurrentHealth <= 0)
        {
            Die();
        }
    }

    public void TakeDamage(int damage, bool isCritical = false)
    {
        if (isInvulnerable) return;
        if (CurrentHealth <= 0) return;

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

        EnemyDrops drops = GetComponent<EnemyDrops>()
                        ?? GetComponentInChildren<EnemyDrops>()
                        ?? GetComponentInParent<EnemyDrops>();
        if (drops != null)
        {
            Debug.Log("[DROPS] EnemyDrops encontrado, chamando OnDeath()...");
            drops.OnDeath();
        }

        Destroy(gameObject);
    }
}
