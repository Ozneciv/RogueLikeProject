using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody), typeof(Animator))]
public class MutanteIA : MonoBehaviour
{
    [Header("Configurações de Alvo")]
    public Transform jogador;
    public float distanciaParaAtacar = 2.5f;
    public float velocidadeCorrer = 5f;

    [Header("Configurações de Combate")]
    public float tempoEntreAtaques = 0.8f; // Tempo de cada animação de golpe
    public float intervaloEntreCombos = 2f; // Descanso após o 3º golpe

    private Rigidbody rb;
    private Animator anim;
    private bool Attack = false;
    private float ultimoComboTime;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();
        rb.freezeRotation = true; // Impede o mutante de cair

        if (jogador == null)
            jogador = GameObject.FindGameObjectWithTag("Player").transform;
    }

    void Update()
    {
        if (jogador == null || Attack) return;

        float distancia = Vector3.Distance(transform.position, jogador.position);

        if (distancia > distanciaParaAtacar)
        {
            SeguirJogador();
        }
        else if (Time.time >= ultimoComboTime + intervaloEntreCombos)
        {
            StartCoroutine(SequenciaDeAtaque());
        }
        else
        {
            PararEIdle();
        }
    }

    void SeguirJogador()
    {
        Vector3 direcao = (jogador.position - transform.position).normalized;
        direcao.y = 0; // Mantém a direção no plano horizontal

        // IMPORTANTE: Mantemos o rb.velocity.y original para a gravidade continuar agindo
        rb.linearVelocity = new Vector3(direcao.x * velocidadeCorrer, rb.linearVelocity.y, direcao.z * velocidadeCorrer);

        // Rotação suave
        if (direcao != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direcao);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 10f);
        }

        // Atualiza o Animator
        anim.SetFloat("Speed", velocidadeCorrer);
    }

    void PararEIdle()
    {
        rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
        anim.SetFloat("Speed", 0);
    }

    IEnumerator SequenciaDeAtaque()
    {
        Attack = true;
        PararEIdle();

        // Olhar para o jogador antes de começar
        transform.LookAt(new Vector3(jogador.position.x, transform.position.y, jogador.position.z));

        // Ataque 1
        anim.SetTrigger("Attack1");
        yield return new WaitForSeconds(tempoEntreAtaques);

        // Ataque 2
        anim.SetTrigger("Attack2");
        yield return new WaitForSeconds(tempoEntreAtaques);

        // Ataque 3
        anim.SetTrigger("Attack3");
        yield return new WaitForSeconds(tempoEntreAtaques);

        // Finaliza
        Attack = false;
        ultimoComboTime = Time.time;
    }
}
