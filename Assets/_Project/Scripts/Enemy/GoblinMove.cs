using UnityEngine;
using System.Collections;

/// <summary>
/// IA do Goblin. Estados: Idle → Pursue → Strafe/Attack → Flee → MeleePickaxe
/// Mineiro subterrâneo: arremessa bombas à distância e ataca com picareta no corpo-a-corpo.
/// Movimentação suave via MoveTowards no Rigidbody.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Animator))]
public class GoblinAI_Transform : MonoBehaviour
{
    // ── Referências ──────────────────────────────────────────────────
    [Header("Referências")]
    public Transform jogador;
    public GameObject prefabBomba;
    public Transform pontoDeArremesso;

    // ── Distâncias ───────────────────────────────────────────────────
    [Header("Distâncias")]
    [Tooltip("Distância mínima para o Goblin manter do jogador (gatilho de fuga).")]
    public float distanciaFuga = 6f;
    [Tooltip("Distância ideal para arremessar. O Goblin tenta ficar aqui.")]
    public float distanciaAtaque = 12f;
    [Tooltip("Distância máxima para o Goblin começar a perseguir.")]
    public float distanciaMaxBusca = 28f;

    // ── Velocidades ──────────────────────────────────────────────────
    [Header("Velocidades")]
    public float velocidadePerseguicao = 7f;
    public float velocidadeFuga = 11f;
    public float velocidadeStrafe = 4f;
    [Tooltip("Aceleração do movimento (mais alto = mais responsivo, mas pode parecer 'travado').")]
    public float aceleracao = 12f;

    // ── Ataque à Distância (Bomba) ────────────────────────────────────
    [Header("Arremesso")]
    public float forcaArremesso = 12f;
    public float forcaArco = 6f;
    public float intervaloAtaque = 2.8f;

    // ── Ataque Melee (Picareta) ───────────────────────────────────────
    [Header("Melee - Picareta")]
    [Tooltip("Distância máxima para o ataque de picareta (tem prioridade sobre a fuga).")]
    public float distanciaMelee = 2.5f;
    [Tooltip("Dano causado pelo golpe de picareta.")]
    public int danoMelee = 20;
    [Tooltip("Cooldown entre golpes de picareta.")]
    public float cooldownMelee = 1.5f;
    [Tooltip("Raio da hitbox do golpe de picareta.")]
    public float raioHitMelee = 1.8f;

    // ── Strafe ───────────────────────────────────────────────────────
    [Header("Strafe (movimento lateral ao atacar)")]
    [Tooltip("Duração de cada ciclo de strafe antes de mudar de direção.")]
    public float strafeChangeDuration = 1.2f;

    // ── Privados ─────────────────────────────────────────────────────    // Privados
    private Rigidbody rb;
    private Animator anim;
    private float tempoUltimoAtaque;
    private float strafeTimer;
    private int strafeDir = 1;
    private Vector3 velocidadeAtual = Vector3.zero;

    // Buff (Crystal Tuner)
    private bool isBuffed = false;
    private float velPerseguicaoOriginal;
    private float velFugaOriginal;
    private float intervaloOriginal;

    // Estado simples
    private enum Estado { Idle, Perseguir, Atacar, Fugir, MeleePickaxe }
    private Estado estadoAtual = Estado.Idle;
    private bool registradoNoBestiario = false;

    // Melee picaxe
    private float timerMelee = 0f;
    private bool realizandoMelee = false;

    // ─────────────────────────────────────────────────────────────────
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();
        rb.freezeRotation = true;

        velPerseguicaoOriginal = velocidadePerseguicao;
        velFugaOriginal = velocidadeFuga;
        intervaloOriginal = intervaloAtaque;

        if (jogador == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) jogador = p.transform;
        }

        strafeTimer = strafeChangeDuration;
    }

    // ── Buff do Crystal Tuner ─────────────────────────────────────────
    public void SetBuff(bool active)
    {
        if (active && !isBuffed)
        {
            isBuffed = true;
            velocidadePerseguicao *= 1.15f;
            velocidadeFuga *= 1.15f;
            intervaloAtaque /= 2f;
        }
        else if (!active && isBuffed)
        {
            isBuffed = false;
            velocidadePerseguicao = velPerseguicaoOriginal;
            velocidadeFuga = velFugaOriginal;
            intervaloAtaque = intervaloOriginal;
        }
    }

    // ─────────────────────────────────────────────────────────────────
    void Update()
    {
        if (jogador == null) return;

        if (timerMelee > 0f) timerMelee -= Time.deltaTime;

        float dist = Vector3.Distance(transform.position, jogador.position);
        AtualizarEstado(dist);

        // Gira em direção ao player (exceto durante fuga pura)
        if (estadoAtual != Estado.Fugir)
            OlharParaJogador();
    }

    void FixedUpdate()
    {
        if (jogador == null) return;
        ExecutarMovimento();
    }

    // ─────────────────────────────────────────────────────────────────
    void AtualizarEstado(float dist)
    {
        // Prioridade: Melee > Fuga > Ataque ranged > Perseguição > Idle
        if (dist <= distanciaMelee)        MudarEstado(Estado.MeleePickaxe);
        else if (dist < distanciaFuga)     MudarEstado(Estado.Fugir);
        else if (dist <= distanciaAtaque)  MudarEstado(Estado.Atacar);
        else if (dist <= distanciaMaxBusca) MudarEstado(Estado.Perseguir);
        else                               MudarEstado(Estado.Idle);

        // Lógica do estado atual
        switch (estadoAtual)
        {
            case Estado.Atacar:
                TentarAtacar();
                AtualizarStrafe();
                break;
            case Estado.MeleePickaxe:
                TentarAtaqueMelee();
                break;
        }
    }

    void MudarEstado(Estado novo)
    {
        if (estadoAtual == novo) return;

        // Registra no bestiário na primeira vez que sai do Idle
        if (!registradoNoBestiario && estadoAtual == Estado.Idle && novo != Estado.Idle)
        {
            registradoNoBestiario = true;
            EnemyIdentity id = GetComponent<EnemyIdentity>() ?? GetComponentInChildren<EnemyIdentity>() ?? GetComponentInParent<EnemyIdentity>();
            Debug.Log("[GOBLIN] EnemyIdentity: " + (id != null ? id.nomeInimigo : "NULL") + " | BestiarioManager: " + (BestiarioManager.instancia != null));
            if (id != null && BestiarioManager.instancia != null)
                BestiarioManager.instancia.Registrar(id);
        }

        estadoAtual = novo;
        anim.SetBool("Running", novo == Estado.Perseguir || novo == Estado.Fugir);
    }

    // ─────────────────────────────────────────────────────────────────
    void ExecutarMovimento()
    {
        Vector3 alvo = Vector3.zero;

        switch (estadoAtual)
        {
            case Estado.Perseguir:
                alvo = DirecaoPara(jogador.position) * velocidadePerseguicao;
                break;

            case Estado.Fugir:
                alvo = DirecaoFugindo() * velocidadeFuga;
                OlharParaDirecao(DirecaoFugindo());
                break;

            case Estado.MeleePickaxe:
                // Para completamente para golpear — o Goblin planta o pé
                alvo = Vector3.zero;
                break;

            case Estado.Atacar:
                // Strafe lento ao atacar — evita o efeito de deslizamento
                Vector3 lateral = transform.right * strafeDir * velocidadeStrafe * 0.3f;
                alvo = new Vector3(lateral.x, 0, lateral.z);
                break;

            case Estado.Idle:
            default:
                alvo = Vector3.zero;
                break;
        }

        // Suaviza a velocidade (aceleração) para movimento fluido
        velocidadeAtual = Vector3.MoveTowards(
            velocidadeAtual,
            new Vector3(alvo.x, 0, alvo.z),
            aceleracao * Time.fixedDeltaTime
        );

        rb.linearVelocity = new Vector3(velocidadeAtual.x, rb.linearVelocity.y, velocidadeAtual.z);
    }

    // ─────────────────────────────────────────────────────────────────
    void AtualizarStrafe()
    {
        strafeTimer -= Time.deltaTime;
        if (strafeTimer <= 0f)
        {
            strafeDir = (Random.value > 0.5f) ? 1 : -1;
            strafeTimer = strafeChangeDuration + Random.Range(-0.3f, 0.5f);
        }
    }

    void TentarAtacar()
    {
        if (Time.time >= tempoUltimoAtaque + intervaloAtaque)
        {
            anim.SetTrigger("Attacking");
            tempoUltimoAtaque = Time.time;
        }
    }

    // ─────────────────────────────────────────────────────────────────
    /// <summary>
    /// Ataque de picareta corpo-a-corpo. Triggered quando o player entra em distanciaMelee.
    /// O Goblin para, olha para o player e desfere um golpe com a picareta.
    /// </summary>
    void TentarAtaqueMelee()
    {
        if (realizandoMelee || timerMelee > 0f) return;
        StartCoroutine(ExecutarMelee());
    }

    IEnumerator ExecutarMelee()
    {
        realizandoMelee = true;
        timerMelee = cooldownMelee;

        // Telegrafagem: o Goblin levanta a picareta (wind-up)
        anim.SetTrigger("MeleeAttack");
        Debug.Log("[GOBLIN] PICARETA! Wind-up...");

        yield return new WaitForSeconds(0.35f); // timing do swing

        // Hitbox em frente ao Goblin
        Vector3 centroHit = transform.position + transform.forward * 1.2f + Vector3.up * 0.5f;
        Collider[] hits = Physics.OverlapSphere(centroHit, raioHitMelee);

        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                PlayerHealth ph = hit.GetComponent<PlayerHealth>();
                if (ph != null)
                {
                    ph.TakeDamage(danoMelee, gameObject);
                    Debug.Log("[GOBLIN] Picareta acertou o player! Dano: " + danoMelee);
                }
                break;
            }
        }

        yield return new WaitForSeconds(0.4f); // recovery
        realizandoMelee = false;
    }

    // ─────────────────────────────────────────────────────────────────
    // Chamado pelo Animation Event
    public void EventoDispararBomba()
    {
        if (prefabBomba == null || pontoDeArremesso == null) return;

        GameObject bomba = Instantiate(prefabBomba, pontoDeArremesso.position, Quaternion.identity);

        // Ignore colisão com o próprio Goblin
        Collider cBomba = bomba.GetComponent<Collider>();
        Collider cGoblin = GetComponent<Collider>();
        if (cBomba != null && cGoblin != null)
            Physics.IgnoreCollision(cBomba, cGoblin);

        // Passa referência do dono e raio de acordo com o buff
        BombaExplosiva script = bomba.GetComponent<BombaExplosiva>();
        if (script != null)
        {
            script.owner = gameObject;
            script.raioExplosao = isBuffed ? 4f : 2f; // buffado = raio maior
        }

        // Aplica força parabólica em direção ao jogador
        Rigidbody rbBomba = bomba.GetComponent<Rigidbody>();
        if (rbBomba != null)
        {
            rbBomba.WakeUp();
            Vector3 direcao = (jogador != null)
                ? (jogador.position - pontoDeArremesso.position).normalized
                : transform.forward;

            Vector3 forca = direcao * forcaArremesso + Vector3.up * forcaArco;
            rbBomba.AddForce(forca, ForceMode.Impulse);
        }
    }

    // ─────────────────────────────────────────────────────────────────
    // Helpers
    Vector3 DirecaoPara(Vector3 alvo)
    {
        Vector3 dir = alvo - transform.position;
        dir.y = 0;
        return dir.normalized;
    }

    Vector3 DirecaoFugindo()
    {
        Vector3 dir = transform.position - jogador.position;
        dir.y = 0;
        return dir.normalized;
    }

    void OlharParaJogador()
    {
        Vector3 dir = jogador.position - transform.position;
        dir.y = 0;
        if (dir.sqrMagnitude > 0.01f)
            OlharParaDirecao(dir);
    }

    void OlharParaDirecao(Vector3 dir)
    {
        if (dir.sqrMagnitude < 0.01f) return;
        Quaternion rot = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(transform.rotation, rot, Time.deltaTime * 10f);
    }
}
