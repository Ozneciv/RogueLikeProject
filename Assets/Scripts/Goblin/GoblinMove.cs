using UnityEngine;
// Não precisa mais de using UnityEngine.AI

[RequireComponent(typeof(Animator))]
public class GoblinAI_Transform : MonoBehaviour
{
    [Header("Alvos")]
    public Transform jogador;

    // --- CONFIGURAÇÕES EXISTENTES ---
    [Header("Configuração de Fuga")]
    public float distanciaSegura = 15f; 
    public float distanciaParaFugir = 8f; 
    public float velocidadeFuga = 6f; 

    [Header("Configuração de Ataque")]
    public GameObject prefabBomba;
    public Transform pontoDeArremesso;
    public float forcaArremesso = 10f;
    public float forcaArco = 5f; 
    public float intervaloAtaque = 2f;

    // --- NOVO: CONFIGURAÇÃO DE PERSEGUIÇÃO ---
    [Header("Configuração de Perseguição")]
    public float distanciaMaximaBusca = 25f; // Distância máx. para começar a correr atrás
    public float velocidadePerseguicao = 3f; // Velocidade de perseguição (mais lenta que a fuga)
    // ------------------------------------------

    private Rigidbody rb;
    private Animator anim;
    private float tempoUltimoAtaque;
    private bool estaFugindo = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();

        if (rb == null)
        {
            Debug.LogError("O Rigidbody é necessário para o movimento de IA e para o arremesso da bomba.");
            return;
        }
        if (anim == null)
        {
            Debug.LogError("O Script não encontrou o Animator no objeto: " + gameObject.name);
        }
        
        rb.freezeRotation = true; 

        if (jogador == null)
            jogador = GameObject.FindGameObjectWithTag("Player").transform;
    }

    // =======================================================
    // FUNÇÃO PRINCIPAL DE LÓGICA (UPDATE)
    // =======================================================
    void Update()
    {
        if (jogador == null) return;

        float distancia = Vector3.Distance(transform.position, jogador.position);

        // Lógica de Estados (Prioridade: Fugir > Atacar > Perseguir > Ocioso)
        
        if (distancia < distanciaParaFugir)
        {
            // ESTADO 1: FUGIR (Jogador muito perto)
            FugirDoJogador();
        }
        else if (distancia <= distanciaSegura)
        {
            // ESTADO 2: ATACAR (Distância ideal para arremessar)
            Atacar();
        }
        else if (distancia <= distanciaMaximaBusca) // <-- NOVO: Verifica se está no alcance de busca
        {
            // ESTADO 3: PERSEGUIR (Jogador está longe, mas no alcance de busca)
            PerseguirJogador();
        }
        else
        {
            // ESTADO 4: OCIOSO/PARAR (Jogador muito longe)
            PararMovimento();
            anim.SetBool("Running", false);
            estaFugindo = false;
        }
    }

    // =======================================================
    // NOVO: FUNÇÃO DE PERSEGUIÇÃO
    // =======================================================
    void PerseguirJogador()
    {
        // Se estava fugindo, para.
        estaFugindo = false;
        anim.SetBool("Running", true); // Reusa a animação de correr (ou crie uma 'IsRunning' no Animator)

        // 1. Calcula a direção *para* o jogador
        Vector3 direcaoPerseguicao = jogador.position - transform.position;
        direcaoPerseguicao.y = 0; // Garante que a perseguição é plana

        // 2. MOVIMENTO USANDO O RIGIDBODY
        // Define a velocidade de perseguição
        Vector3 velocidadeDesejada = direcaoPerseguicao.normalized * velocidadePerseguicao;
        rb.linearVelocity = velocidadeDesejada;

        // 3. Rotação (Olha para o alvo)
        if (direcaoPerseguicao != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direcaoPerseguicao);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
        }
    }
    // =======================================================

    // ... [Outras funções (PararMovimento, FugirDoJogador, Atacar, ArremessarBomba)] ...
    
    void PararMovimento()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
        }
    }

    void FugirDoJogador()
    {
        estaFugindo = true;
        anim.SetBool("Running", true);
        
        Vector3 direcaoFuga = transform.position - jogador.position;
        direcaoFuga.y = 0; 

        Vector3 velocidadeDesejada = direcaoFuga.normalized * velocidadeFuga;
        rb.linearVelocity = velocidadeDesejada;

        if (direcaoFuga != Vector3.zero)
        {
             Quaternion lookRotation = Quaternion.LookRotation(direcaoFuga);
             transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
        }
    }

    void Atacar()
{
    PararMovimento(); 
    anim.SetBool("Running", false);
    
    // Rotação: Olha para o alvo
    Vector3 direcaoAlvo = jogador.position - transform.position;
    direcaoAlvo.y = 0;
    Quaternion lookRotation = Quaternion.LookRotation(direcaoAlvo);
    transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);

    if (Time.time >= tempoUltimoAtaque + intervaloAtaque)
    {
        // 1. APENAS dispara a animação
        anim.SetTrigger("Attacking");
        
        // Reinicia o tempo aqui para ele não disparar a animação de novo antes da hora
        tempoUltimoAtaque = Time.time;
    }
}

// 2. Esta função será chamada pela própria ANIMAÇÃO através de um Evento
public void EventoDispararBomba()
{
    if (prefabBomba == null || pontoDeArremesso == null) return;

    // 1. Cria a bomba
    GameObject bomba = Instantiate(prefabBomba, pontoDeArremesso.position, pontoDeArremesso.rotation);
    
    // 2. PEGA OS COLIDORES (Obrigatório para a física funcionar)
    Collider colisorBomba = bomba.GetComponent<Collider>();
    Collider colisorGoblin = GetComponent<Collider>();

    // 3. IGNORA A COLISÃO (Isso evita que a bomba bata no Capsule Collider do Goblin)
    if (colisorBomba != null && colisorGoblin != null)
    {
        Physics.IgnoreCollision(colisorBomba, colisorGoblin);
    }

    // 4. APLICA A FORÇA
    Rigidbody rbBomba = bomba.GetComponent<Rigidbody>();
    if (rbBomba != null)
    {
        // Garante que a bomba não comece dormindo
        rbBomba.WakeUp(); 
        Vector3 forcaFinal = (transform.forward * forcaArremesso) + (Vector3.up * forcaArco);
        rbBomba.AddForce(forcaFinal, ForceMode.Impulse);
    }
}
}
