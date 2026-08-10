using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Controlador da Fase 1 - Mestre do Solo
/// O Boss fica escondido em um Casulo.
/// Ele prende o jogador em Círculos (Pilares) ou Quadrados (Espinhos) e invoca mobs.
/// </summary>
[RequireComponent(typeof(BossController))]
public class BossPhase1_MestreDoSolo : MonoBehaviour
{
    [Header("Referências")]
    private BossController bossController;
    private BossPhase1_MobSpawner mobSpawner;
    private NavMeshAgent agent;
    private Animator animator;
    private Transform playerTransform;
    private Rigidbody rb;
    private Collider bossCollider; 
    private List<Renderer> renderersDoBoss = new List<Renderer>();

    [Header("Prefabs dos Ataques")]
    public GameObject pilarPrefab;
    public GameObject espinhoPrefab;

    [Header("Visuais do Cristal (Fase 1)")]
    [Tooltip("Arraste o prefab do cristal")]
    public GameObject cristalPrefab;
    private GameObject cristalInstanciado;

    [Header("Configurações de Combate")]
    public float offsetAlturaFinal = -1.5f;
    public float tempoEntreAtaques = 1f;
    public float raioDaPrisao = 10f;
    public float profundidadeSpawn = 4f;
    public float tempoEmergindo = 0.5f;
    public float tempoVidaPrisao = 6f;
    public float distanciaRecuo = 5f;

    private bool phase1Ativa = false;
    private bool atacando = false;
    public bool Atacando => atacando;

    private void Awake()
    {
        bossController = GetComponent<BossController>();
        mobSpawner = GetComponent<BossPhase1_MobSpawner>();
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
        rb = GetComponent<Rigidbody>();
        bossCollider = GetComponent<Collider>();
        
        Renderer[] todosRenderers = GetComponentsInChildren<Renderer>(true);
        foreach(Renderer r in todosRenderers)
        {
            if (r.enabled == true) 
            {
                renderersDoBoss.Add(r);
            }
        }
    }

    private void OnEnable()
    {
        BossEvents.OnPhaseChanged += ControlarFase;
        BossEvents.OnBossFightStarted += BuscarPlayer;
    }

    private void OnDisable()
    {
        BossEvents.OnPhaseChanged -= ControlarFase;
        BossEvents.OnBossFightStarted -= BuscarPlayer;
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
            
            // 1. TÉCNICA DO FANTASMA
            if (agent != null) agent.enabled = false; 
            if (rb != null) rb.isKinematic = true;
            if (bossCollider != null) bossCollider.enabled = false;
            foreach (Renderer r in renderersDoBoss) { if (r != null) r.enabled = false; }

            // 2. Instancia o Casulo
            if (cristalPrefab != null)
            {
                cristalInstanciado = Instantiate(cristalPrefab, transform.position, transform.rotation);
                
                // 3. PREPARA O CASULO E O GATILHO DE MORTE
                DummyHealth casuloHealth = cristalInstanciado.GetComponent<DummyHealth>();
                if (casuloHealth != null)
                {
                    DummyHealth vidaDoBoss = bossController.GetComponent<DummyHealth>();
                    float limiteFase2 = bossController.phaseConfig != null ? bossController.phaseConfig.phase2Threshold : 0.7f;
                    
                    // O Casulo recebe a quantidade exata de vida necessária para passar de fase
                    int danoNecessario = Mathf.RoundToInt(vidaDoBoss.maxHealth * (1f - limiteFase2));
                    casuloHealth.maxHealth = danoNecessario;
                    casuloHealth.ResetHealth();

                    // Conecta com o script CristalCasulo se houver
                    CristalCasulo casuloScript = cristalInstanciado.GetComponent<CristalCasulo>();
                    if (casuloScript != null)
                    {
                        casuloScript.Setup(vidaDoBoss);
                    }

                    // O GATILHO MESTRE: Quando o Casulo morrer, ele dá o dano no Boss forçando a Fase 2!
                    casuloHealth.onDeathOverride += () => 
                    { 
                        Debug.Log("[Fase 1] O Casulo quebrou! Avisando o Boss..."); 
                        if (vidaDoBoss != null)
                        {
                            vidaDoBoss.TakeDamage(danoNecessario); 
                        }
                    };
                }
            }

            StartCoroutine(RotinaDeAtaquesFase1());
            Debug.Log("[Fase 1] Mestre do Solo: Casulo Ativado!");
        }
        else
        {
            // FIM DA FASE 1
            phase1Ativa = false; 
            StopAllCoroutines();
            atacando = false;

            // DESTROI O CASULO CASO AINDA EXISTA NA CENA
            if (cristalInstanciado != null)
            {
                Destroy(cristalInstanciado);
                cristalInstanciado = null;
            }

            // Devolve o visual sólido para o Boss e ativa sua inteligência
            foreach (Renderer r in renderersDoBoss) { if (r != null) r.enabled = true; }
            if (bossCollider != null) bossCollider.enabled = true;
            if (rb != null) 
            {
                rb.isKinematic = false;
                rb.linearVelocity = Vector3.zero;
            }
            if (agent != null) 
            {
                agent.enabled = true;
                if (agent.isOnNavMesh) agent.isStopped = false;
            }
            
            Debug.Log("[Fase 1] Fim! Boss revelado para a Fase 2.");
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
            if (bossController != null && !bossController.IsStunned && !bossController.IsDead && !atacando)
            {
                int ataqueSorteado = Random.Range(0, 3);
                
                if (ataqueSorteado == 0)
                {
                    yield return StartCoroutine(Ataque_Prisao(pilarPrefab, false));
                    if (mobSpawner != null) mobSpawner.SpawnWave(BossPhase1_MobSpawner.WaveType.PostPrison_Pillar);
                }
                else if (ataqueSorteado == 1)
                {
                    yield return StartCoroutine(Ataque_Prisao(espinhoPrefab, true));
                    if (mobSpawner != null) mobSpawner.SpawnWave(BossPhase1_MobSpawner.WaveType.PostPrison_Spike);
                }
                else
                {
                    yield return StartCoroutine(RecuarDoPlayer());
                }
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

        if (animator != null)
        {
            animator.ResetTrigger("Spell");
            animator.SetTrigger("Spell");
            animator.Play("Spell", 0, 0f);
        }
        
        if (playerTransform != null)
        {
            Vector3 direcaoOlhar = (playerTransform.position - transform.position).normalized;
            direcaoOlhar.y = 0;
            if (direcaoOlhar.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.LookRotation(direcaoOlhar);
        }

        yield return new WaitForSeconds(0.25f);

        List<Vector3> posicoesFinais = new List<Vector3>();
        Vector3 centro = playerTransform != null ? playerTransform.position : transform.position;

        if (!formatoQuadrado)
        {
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
            Vector3[] direcoesQuadrado = new Vector3[] {
                new Vector3(-1, 0,  1), new Vector3(0, 0,  1), new Vector3(1, 0,  1), 
                new Vector3(-1, 0,  0),                        new Vector3(1, 0,  0), 
                new Vector3(-1, 0, -1), new Vector3(0, 0, -1), new Vector3(1, 0, -1)  
            };

            foreach (Vector3 dir in direcoesQuadrado)
            {
                posicoesFinais.Add(centro + (dir * (raioDaPrisao * 0.8f))); 
            }
        }

        List<Transform> objetosCriados = new List<Transform>();
        for (int i = 0; i < posicoesFinais.Count; i++)
        {
            Vector3 posFinal = posicoesFinais[i];
            posFinal.y = transform.position.y + offsetAlturaFinal; 

            Vector3 posSubsolo = posFinal + (Vector3.down * profundidadeSpawn);
            
            if (prefabObstaculo != null)
            {
                Quaternion rotacao = Quaternion.LookRotation(centro - posFinal);
                GameObject obj = Instantiate(prefabObstaculo, posSubsolo, rotacao);
                objetosCriados.Add(obj.transform);
                
                Destroy(obj, tempoVidaPrisao);
            }
        }

        yield return StartCoroutine(ErguerObjetosDoChao(objetosCriados, posicoesFinais));

        if (agent != null && agent.enabled && agent.isOnNavMesh)
            agent.isStopped = false;

        atacando = false;
    }

    private IEnumerator ErguerObjetosDoChao(List<Transform> objetos, List<Vector3> posicoesFinais)
    {
        float tempoDecorrido = 0f;
        List<Vector3> posicoesIniciais = new List<Vector3>();
        
        foreach (var obj in objetos)
        {
            if (obj != null) posicoesIniciais.Add(obj.position);
            else posicoesIniciais.Add(Vector3.zero);
        }

        while (tempoDecorrido < tempoEmergindo)
        {
            tempoDecorrido += Time.deltaTime;
            
            float t = Mathf.Clamp01(tempoDecorrido / tempoEmergindo);
            float curvaSobeSutil = Mathf.Sin(t * Mathf.PI * 0.5f);

            for (int i = 0; i < objetos.Count; i++)
            {
                if (objetos[i] != null) 
                {
                    Vector3 alvoFinal = posicoesFinais[i];
                    alvoFinal.y = transform.position.y + offsetAlturaFinal;
                    objetos[i].position = Vector3.Lerp(posicoesIniciais[i], alvoFinal, curvaSobeSutil);
                }
            }
            yield return null;
        }

        atacando = false;
    }

    // =====================================================
    // IA DE FUGA (KITING)
    // =====================================================
    private IEnumerator RecuarDoPlayer()
    {
        if (bossController != null && (bossController.IsStunned || bossController.IsDead)) yield break;

        if (bossController != null) bossController.OverrideMovement = true;

        try
        {
            atacando = true;

            Vector3 direcaoOposta = (transform.position - (playerTransform != null ? playerTransform.position : transform.position)).normalized;
            direcaoOposta.y = 0;

            if (direcaoOposta.sqrMagnitude < 0.1f) direcaoOposta = transform.forward;

            Vector3 pontoDeFuga = transform.position + (direcaoOposta * distanciaRecuo);

            NavMeshHit hit;
            if (NavMesh.SamplePosition(pontoDeFuga, out hit, 10f, NavMesh.AllAreas))
            {
                if (agent != null && agent.enabled && agent.isOnNavMesh)
                {
                    agent.isStopped = false;
                    agent.SetDestination(hit.position);
                }

                yield return null;

                float tempoCorrendo = 0f;
                
                while (agent != null && agent.enabled && agent.isOnNavMesh && (agent.pathPending || agent.remainingDistance > 1.5f))
                {
                    tempoCorrendo += Time.deltaTime;

                    if (animator != null) animator.SetFloat("Speed", agent.velocity.magnitude);

                    if (tempoCorrendo > 2.5f || (tempoCorrendo > 0.5f && agent.velocity.sqrMagnitude < 0.1f)) 
                    {
                        Debug.Log("[Fase 1] Boss desistiu de fugir (travou ou demorou).");
                        break; 
                    }
                    
                    yield return null;
                }
            }

            if (agent != null && agent.enabled && agent.isOnNavMesh)
                agent.isStopped = false;
            if (animator != null) animator.SetFloat("Speed", 0f);
            
            if (playerTransform != null)
            {
                Vector3 olharPlayer = (playerTransform.position - transform.position).normalized;
                olharPlayer.y = 0;
                if (olharPlayer.sqrMagnitude > 0.01f)
                    transform.rotation = Quaternion.LookRotation(olharPlayer);
            }
        }
        finally 
        {
            if (bossController != null) bossController.OverrideMovement = false;
            atacando = false; 
        }
    }

    public void RevidarAtaque()
    {
        if (phase1Ativa && !atacando && bossController != null && !bossController.IsStunned && !bossController.IsDead)
        {
            if (Random.Range(0, 2) == 0)
                StartCoroutine(Ataque_Prisao(pilarPrefab, false));
            else
                StartCoroutine(Ataque_Prisao(espinhoPrefab, true));
        }
    }

    /// <summary>
    /// Invoca uma prisão de pilares forçadamente, independente da fase ativa.
    /// Chamado pela BossPhase2_Refraction durante a invisibilidade.
    /// </summary>
    public void InvocarPrisaoForado()
    {
        if (atacando || bossController == null || bossController.IsDead || bossController.IsStunned) return;

        // Sorteia entre pilares ou espinhos
        if (pilarPrefab != null || espinhoPrefab != null)
        {
            GameObject prefab = (pilarPrefab != null && espinhoPrefab != null)
                ? (Random.Range(0, 2) == 0 ? pilarPrefab : espinhoPrefab)
                : (pilarPrefab != null ? pilarPrefab : espinhoPrefab);

            bool quadrado = (prefab == espinhoPrefab);
            StartCoroutine(Ataque_Prisao(prefab, quadrado));
        }
    }
}