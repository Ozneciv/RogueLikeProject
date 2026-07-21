using System.Collections;
using UnityEngine;

public class CrystalPillar : MonoBehaviour
{
    [Header("Configuração")]
    public int vida = 3; 
    public string tagDoAtaque = "Untagged"; 

    [Header("Feedback Visual")]
    public Renderer meshRenderer;
    public Color corDeDano = Color.white;
    private Color corOriginal;
    private bool piscando = false;

    private void Start()
    {
        if (meshRenderer == null) meshRenderer = GetComponentInChildren<Renderer>();
        if (meshRenderer != null) corOriginal = meshRenderer.material.color;
    }

    // O Pilar detecta o impacto físico sozinho
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag(tagDoAtaque))
        {
            ReceberDano(1);
        }
    }

    // Caso o ataque do jogador seja um Trigger (transponível)
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(tagDoAtaque))
        {
            ReceberDano(1);
        }
    }

    private void ReceberDano(int dano)
    {
        vida -= dano;

        // Feedback de impacto visual rápido (Piscar)
        if (meshRenderer != null && !piscando) 
        {
            StartCoroutine(EfeitoPiscarDano());
        }

        // Destrói estritamente ESTE pilar quando a vida zera
        if (vida <= 0)
        {
            Destroy(gameObject); 
        }
    }

    private IEnumerator EfeitoPiscarDano()
    {
        piscando = true;
        meshRenderer.material.color = corDeDano;
        yield return new WaitForSeconds(0.1f); // Duração do flash branco
        meshRenderer.material.color = corOriginal;
        piscando = false;
    }
}