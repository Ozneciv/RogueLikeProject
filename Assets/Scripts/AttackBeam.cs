using UnityEngine;

public class AttackBeam : MonoBehaviour
{
    public int damage = 25;
    public float radius = 2f;

    void Start()
    {
        // Detecta todos os colliders em um raio a partir da posição do raio
        Collider[] hits = Physics.OverlapSphere(transform.position, radius);

        foreach (Collider hit in hits)
        {
            // Se encontrar o jogador, aplica o dano
            if (hit.CompareTag("Player"))
            {
                hit.GetComponent<PlayerHealth>().TakeDamage(damage);
            }
        }
        // Adicionar um efeito visual de explosão/impacto aqui seria ótimo.
        
        // Destrói o objeto do raio após um curto período para dar tempo do efeito visual tocar.
        Destroy(gameObject, 2f);
    }
}