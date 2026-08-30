using System.Collections;
using UnityEngine;

/// <summary>
/// Pilar de Cristal destrutível (Fase 1 do Boss - Mestre do Solo).
/// Integrado ao sistema de dano (DummyHealth) para registrar Hit Impact VFX da arma,
/// números de dano flutuante, flash de material e explosão de estilhaços ao quebrar.
/// </summary>
[RequireComponent(typeof(DummyHealth))]
public class CrystalPillar : MonoBehaviour
{
    [Header("Configuração de Vida")]
    public int vidaMax = 30; 

    [Header("Feedback Visual & VFX")]
    public Renderer meshRenderer;
    public Color corDeDano = new Color(2f, 2f, 2f, 1f); // Piscar Branco Brilhante
    [Tooltip("Prefab de poeira/estilhaços ao receber cada golpe.")]
    public GameObject hitDustVFX;
    [Tooltip("Prefab de explosão/estilhaçamento de cristal ao ser totalmente destruído.")]
    public GameObject shatterDebrisVFX;

    private DummyHealth dummyHealth;
    private Color corOriginal;
    private int vidaAnterior;
    private bool piscando = false;

    private void Awake()
    {
        dummyHealth = GetComponent<DummyHealth>();
        if (dummyHealth != null)
        {
            dummyHealth.maxHealth = vidaMax;
            dummyHealth.ResetHealth();
            dummyHealth.onDeathOverride += DestruirPilar;
            vidaAnterior = dummyHealth.CurrentHealth;
        }

        if (meshRenderer == null) meshRenderer = GetComponentInChildren<Renderer>();
        if (meshRenderer != null && meshRenderer.material.HasProperty("_Color"))
        {
            corOriginal = meshRenderer.material.color;
        }
    }

    private void Start()
    {
        // Garante que o colisor do pilar seja sólido para dar impacto físico no golpe
        Collider col = GetComponent<Collider>() ?? GetComponentInChildren<Collider>();
        if (col != null) col.isTrigger = false;
    }

    private void Update()
    {
        if (dummyHealth == null || piscando) return;

        // Se a vida reduziu por qualquer ataque do jogador
        if (dummyHealth.CurrentHealth < vidaAnterior)
        {
            vidaAnterior = dummyHealth.CurrentHealth;
            if (meshRenderer != null)
            {
                StartCoroutine(EfeitoPiscarDano());
            }

            if (hitDustVFX != null)
            {
                Instantiate(hitDustVFX, transform.position + Vector3.up * 1.2f, Quaternion.identity);
            }
        }
    }

    private void DestruirPilar()
    {
        // Efeito de estilhaçamento de cristal ao quebrar o pilar
        if (shatterDebrisVFX != null)
        {
            Instantiate(shatterDebrisVFX, transform.position + Vector3.up * 1.5f, Quaternion.identity);
        }

        Destroy(gameObject);
    }

    private IEnumerator EfeitoPiscarDano()
    {
        piscando = true;
        if (meshRenderer != null && meshRenderer.material.HasProperty("_Color"))
        {
            meshRenderer.material.color = corDeDano;
        }

        yield return new WaitForSeconds(0.12f);

        if (meshRenderer != null && meshRenderer.material.HasProperty("_Color"))
        {
            meshRenderer.material.color = corOriginal;
        }
        piscando = false;
    }
}