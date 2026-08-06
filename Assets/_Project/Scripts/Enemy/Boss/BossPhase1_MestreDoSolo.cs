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

    private bool phase1Ativa = false;
    private bool atacando = false;

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

                    // O GATILHO MESTRE: Quando o Casulo morrer, ele dá o dano no Boss forçando a Fase 2!
                    casuloHealth.onDeathOverride = () => 
                    { 
                        Debug.Log("[Fase 1] O Casulo quebrou! Avisando o Boss..."); 
                        vidaDoBoss.TakeDamage(danoNecessario); 
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

            // Destrói o cristal da cena
            if (cristalInstanciado != null) Destroy(cristalInstanciado);

            // Devolve o visual sólido para o Boss
            foreach (Renderer r in renderersDoBoss) { if (r != null) r.enabled = true; }
            if (bossCollider != null) bossCollider.enabled = true;
            if (rb != null) rb.isKinematic = false;
            if (agent != null) agent.enabled = true;
            
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
            if (!atacando && !bossController.IsStunned && !bossController.IsDead)
            {
                int ataqueSorteado = Random.Range(0, 2);
                
                if (ataqueSorteado == 0)
                {
                    yield return StartCoroutine(Ataque_Prisao(pilarPrefab, false));
                    if (mobSpawner != null) mobSpawner.SpawnWave(BossPhase1_MobSpawner.WaveType.PostPrison_Pillar);
                }
                else
                {
                    yield return StartCoroutine(Ataque_Prisao(espinhoPrefab, true));
                    if (mobSpawner != null) mobSpawner.SpawnWave(BossPhase1_MobSpawner.WaveType.PostPrison_Spike);
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

        if (animator != null) animator.SetTrigger("Spell");
        
        if (playerTransform != null)
        {
            Vector3 direcaoOlhar = (playerTransform.position - transform.position).normalized;
            direcaoOlhar.y = 0;
            if (direcaoOlhar.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.LookRotation(direcaoOlhar);
        }

        yield return new WaitForSeconds(1f);

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
        atacando = false;
    }

    private IEnumerator ErguerObjetosDoChao(List<Transform> objetos, List<Vector3> posicoesFinais)
    {
        float tempoDecorrido = 0f;
        List<Vector3> posicoesIniciais = new List<Vector3>();
        
        foreach (var obj in objetos) posicoesIniciais.Add(obj.position);

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
    }
}