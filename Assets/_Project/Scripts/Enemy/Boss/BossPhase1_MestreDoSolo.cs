using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Controlador da Fase 1 - Mestre do Solo
/// O Boss agora prende o jogador em Círculos (Pilares) ou Quadrados (Espinhos)
/// que brotam do chão
/// </summary>
[RequireComponent(typeof(BossController))]
public class BossPhase1_MestreDoSolo : MonoBehaviour
{
    [Header("Referências")]
    private BossController bossController;
    private NavMeshAgent agent;
    private Animator animator;
    private Transform playerTransform;

    [Header("Prefabs dos Ataques")]
    public GameObject pilarPrefab;
    public GameObject espinhoPrefab;

    [Header("Configurações de Combate")]

    [Tooltip("Ajuste fino da altura final.")]
    public float offsetAlturaFinal = -1.5f;
    [Tooltip("Tempo que o boss espera parado antes de lançar a próxima prisão.")]
    public float tempoEntreAtaques = 1f;
    [Tooltip("Distância do player que os obstáculos vão nascer.")]
    public float raioDaPrisao = 10f;
    [Tooltip("Quão fundo no chão os obstáculos começam antes de subir.")]
    public float profundidadeSpawn = 4f;
    [Tooltip("Velocidade que os obstáculos emergem do chão (segundos).")]
    public float tempoEmergindo = 0.5f;
    [Tooltip("Distância que o boss tenta se afastar após prender o jogador.")]
    public float distanciaRecuo = 8f;
    [Tooltip("Tempo até a prisão desmoronar sozinha (evita lotar a arena).")]
    public float tempoVidaPrisao = 6f;

    private bool phase1Ativa = false;
    private bool atacando = false;

    private void Awake()
    {
        bossController = GetComponent<BossController>();
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
    }

    private void OnEnable()
    {
        BossEvents.OnPhaseChanged += ControlarFase;
        BossEvents.OnBossFightStarted += BuscarPlayer;
        bossController.OnTookDamage += RevidarAtaque;
    }

    private void OnDisable()
    {
        BossEvents.OnPhaseChanged -= ControlarFase;
        BossEvents.OnBossFightStarted -= BuscarPlayer;
        bossController.OnTookDamage -= RevidarAtaque;
    }

    private void BuscarPlayer()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) playerTransform = p.transform;
    }

    private void ControlarFase(int novaFase)
    {
        if (novaFase == 1)
        {
            phase1Ativa = true;
            StartCoroutine(RotinaDeAtaquesFase1());
            Debug.Log("[Fase 1] Mestre do Solo: Ativada!");
        }
        else
        {
            phase1Ativa = false; 
            StopAllCoroutines();
            atacando = false;
            if (agent != null && agent.isOnNavMesh)
            {
                agent.enabled = true;
                agent.isStopped = false;
            }
        }
    }

    // =====================================================
    // MÁQUINA DE ATAQUES
    // =====================================================
    private IEnumerator RotinaDeAtaquesFase1()
    {
        yield return new WaitForSeconds(tempoEntreAtaques);

        while (phase1Ativa)
        {
            if (!atacando && !bossController.IsStunned && !bossController.IsDead)
            {
                // Sorteia aleatoriamente: 0 = Círculo de Pilares, 1 = Quadrado de Espinhos
                int ataqueSorteado = Random.Range(0, 2);
                
                if (ataqueSorteado == 0)
                    yield return StartCoroutine(Ataque_Prisao(pilarPrefab, false));
                else
                    yield return StartCoroutine(Ataque_Prisao(espinhoPrefab, true));

                // Após o ataque, o boss se afasta do player
                yield return StartCoroutine(RecuarDoPlayer());
            }
            yield return new WaitForSeconds(tempoEntreAtaques);
        }
    }

    // =====================================================
    // LÓGICA DE GERAR AS PRISÕES (CÍRCULO OU QUADRADO)
    // =====================================================
    private IEnumerator Ataque_Prisao(GameObject prefabObstaculo, bool formatoQuadrado)
    {
        atacando = true;
        agent.isStopped = true; // Boss para de andar para conjurar

        // Animação de conjurar (se tiver)
        if (animator != null) animator.SetTrigger("Spell");
        
        // Vira o boss para o player durante a conjuração
        Vector3 direcaoOlhar = (playerTransform.position - transform.position).normalized;
        direcaoOlhar.y = 0;
        transform.rotation = Quaternion.LookRotation(direcaoOlhar);

        yield return new WaitForSeconds(1f); // Tempo que ele fica conjurando

        List<Vector3> posicoesFinais = new List<Vector3>();
        Vector3 centro = playerTransform.position;

        if (!formatoQuadrado)
        {
            // GERAR CÍRCULO (8 pontos)
            int qtdPilares = 8;
            for (int i = 0; i < qtdPilares; i++)
            {
                float angulo = i * Mathf.PI * 2 / qtdPilares;
                Vector3 pos = centro + new Vector3(Mathf.Cos(angulo), 0, Mathf.Sin(angulo)) * raioDaPrisao;
                posicoesFinais.Add(pos);
            }
        }
        else
        {
            // GERAR QUADRADO (8 pontos em volta)
            Vector3[] direcoesQuadrado = new Vector3[] {
                new Vector3(-1, 0,  1), new Vector3(0, 0,  1), new Vector3(1, 0,  1), // Topo
                new Vector3(-1, 0,  0),                        new Vector3(1, 0,  0), // Lados
                new Vector3(-1, 0, -1), new Vector3(0, 0, -1), new Vector3(1, 0, -1)  // Base
            };

            foreach (Vector3 dir in direcoesQuadrado)
            {
                posicoesFinais.Add(centro + (dir * (raioDaPrisao * 0.8f))); 
            }
        }

        // Instancia os objetos no subsolo
        List<Transform> objetosCriados = new List<Transform>();
        for (int i = 0; i < posicoesFinais.Count; i++)
        {
            Vector3 posFinal = posicoesFinais[i];
            posFinal.y = transform.position.y + offsetAlturaFinal; 

            Vector3 posSubsolo = posFinal + (Vector3.down * profundidadeSpawn);
            
            if (prefabObstaculo != null)
            {
                // Faz os objetos olharem para o centro (player)
                Quaternion rotacao = Quaternion.LookRotation(centro - posFinal);
                GameObject obj = Instantiate(prefabObstaculo, posSubsolo, rotacao);
                objetosCriados.Add(obj.transform);
                
                // Limpa o objeto da cena após 'tempoVidaPrisao' segundos
                Destroy(obj, tempoVidaPrisao);
            }
        }

        // Animação deles saindo do chão
        yield return StartCoroutine(ErguerObjetosDoChao(objetosCriados, posicoesFinais));

        if (agent != null && agent.enabled && agent.isOnNavMesh)
            agent.isStopped = false;

        atacando = false;
    }

    // =====================================================
    // EFEITO VISUAL: SAINDO DO CHÃO
    // =====================================================
    private IEnumerator ErguerObjetosDoChao(List<Transform> objetos, List<Vector3> posicoesFinais)
    {
        float tempoDecorrido = 0f;
        List<Vector3> posicoesIniciais = new List<Vector3>();
        
        // Salva onde eles nasceram (subsolo)
        foreach (var obj in objetos) posicoesIniciais.Add(obj.position);

        while (tempoDecorrido < tempoEmergindo)
        {
            tempoDecorrido += Time.deltaTime;
            
            // Cria um efeito "Ease Out" (sobe rápido e freia no final)
            float t = Mathf.Clamp01(tempoDecorrido / tempoEmergindo);
            float curvaSobeSutil = Mathf.Sin(t * Mathf.PI * 0.5f);

            for (int i = 0; i < objetos.Count; i++)
            {
                // Verifica se o player já não destruiu o objeto enquanto ele subia
                if (objetos[i] != null) 
                {
                    Vector3 alvoFinal = posicoesFinais[i];
                    alvoFinal.y = transform.position.y + offsetAlturaFinal;
                    objetos[i].position = Vector3.Lerp(posicoesIniciais[i], alvoFinal, curvaSobeSutil);
                }
            }
            yield return null;
        }
    }

    // =====================================================
    // IA DE FUGA (KITING)
    // =====================================================
    private IEnumerator RecuarDoPlayer()
    {
        // Verifica estados impeditivos antes de começar
        if (bossController.IsStunned || bossController.IsDead) yield break;

        // TOMA CONTROLE: Desliga qualquer interferência do BossController
        bossController.OverrideMovement = true;

        try
        {
            atacando = true;

            // Descobre a direção oposta ao jogador
            Vector3 direcaoOposta = (transform.position - playerTransform.position).normalized;
            direcaoOposta.y = 0;

            // Prevenção caso estejam na exata mesma coordenada
            if (direcaoOposta.sqrMagnitude < 0.1f) direcaoOposta = transform.forward;

            Vector3 pontoDeFuga = transform.position + (direcaoOposta * distanciaRecuo);

            // Tenta achar chão válido no NavMesh
            NavMeshHit hit;
            if (NavMesh.SamplePosition(pontoDeFuga, out hit, 10f, NavMesh.AllAreas))
            {
                agent.isStopped = false;
                agent.SetDestination(hit.position);

                // Espera 1 frame para o NavMesh calcular a rota
                yield return null;

                float tempoCorrendo = 0f;
                
                // Loop de movimento
                while (agent.pathPending || agent.remainingDistance > 1.5f)
                {
                    tempoCorrendo += Time.deltaTime;

                    // Atualiza Animator
                    if (animator != null) animator.SetFloat("Speed", agent.velocity.magnitude);

                    // Timeout ou detecção de travamento (colisão com parede)
                    if (tempoCorrendo > 2.5f || (tempoCorrendo > 0.5f && agent.velocity.sqrMagnitude < 0.1f)) 
                    {
                        Debug.Log("[Fase 1] Boss desistiu de fugir (travou ou demorou).");
                        break; 
                    }
                    
                    yield return null;
                }
            }
            else
            {
                Debug.LogWarning("[Fase 1] O Boss tentou fugir, mas não encontrou NavMesh válido perto do ponto de fuga!");
            }

            // Chegou no destino (ou desistiu). Mantém o boss andando.
            if (agent != null && agent.enabled && agent.isOnNavMesh)
                agent.isStopped = false;
            if (animator != null) animator.SetFloat("Speed", 0f);
            
            // Vira de volta pro player
            Vector3 olharPlayer = (playerTransform.position - transform.position).normalized;
            olharPlayer.y = 0;
            transform.rotation = Quaternion.LookRotation(olharPlayer);
        }
        finally 
        {
            // GARANTIA: Devolve o controle, não importa o que aconteça
            bossController.OverrideMovement = false;
            atacando = false; 
        }
    }
    public void RevidarAtaque()
        {
            // Só revida se estiver na Fase 1, não estiver já atacando, não estiver atordoado e não estiver morto
            if (phase1Ativa && !atacando && !bossController.IsStunned && !bossController.IsDead)
            {
                // Decide aleatoriamente qual prisão usar no contra-ataque
                if (Random.Range(0, 2) == 0)
                    StartCoroutine(Ataque_Prisao(pilarPrefab, false));
                else
                    StartCoroutine(Ataque_Prisao(espinhoPrefab, true));
            }
        }




}