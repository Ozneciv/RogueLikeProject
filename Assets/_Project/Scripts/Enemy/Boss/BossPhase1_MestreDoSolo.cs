using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(BossController))]
public class BossPhase1_MestreDoSolo : MonoBehaviour
{
    [Header("Referências")]
    private BossController bossController;
    private NavMeshAgent agent;
    private Animator animator;
    private Transform playerTransform;
    private Rigidbody rb;
    private Collider bossCollider; 
    private Renderer[] renderersDoBoss;

    [Header("Prefabs dos Ataques")]
    public GameObject pilarPrefab;
    public GameObject espinhoPrefab;

    [Header("Visuais do Cristal (Fase 1)")]
    [Tooltip("Arraste o prefab do cristal COM O DUMMYHEALTH aqui.")]
    public GameObject cristalPrefab;
    private GameObject cristalInstanciado;
    
    // --- LIGAÇÃO DE VIDA CASULO <-> BOSS ---
    private DummyHealth casuloHealth;
    private int ultimaVidaCasulo;

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
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
        rb = GetComponent<Rigidbody>();
        bossCollider = GetComponent<Collider>();
        renderersDoBoss = GetComponentsInChildren<Renderer>();
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
            
            // 1. TÉCNICA DO FANTASMA: Desliga a malha 3D e colisões do Boss
            if (agent != null) agent.enabled = false; 
            if (rb != null) rb.isKinematic = true;
            if (bossCollider != null) bossCollider.enabled = false;
            foreach (Renderer r in renderersDoBoss) { if (r != null) r.enabled = false; }

            // 2. Instancia o Casulo
            if (cristalPrefab != null)
            {
                cristalInstanciado = Instantiate(cristalPrefab, transform.position, transform.rotation);
                
                // 3. SINCRONIZA A VIDA DO CASULO COM A DO BOSS
                casuloHealth = cristalInstanciado.GetComponent<DummyHealth>();
                if (casuloHealth != null)
                {
                    DummyHealth vidaDoBoss = bossController.GetComponent<DummyHealth>();
                    float limiteFase2 = bossController.phaseConfig != null ? bossController.phaseConfig.phase2Threshold : 0.7f;
                    
                    // O Casulo terá EXATAMENTE a quantidade de vida necessária para forçar a Fase 2
                    int danoNecessario = Mathf.RoundToInt(vidaDoBoss.maxHealth * (1f - limiteFase2));
                    casuloHealth.maxHealth = danoNecessario;
                    casuloHealth.ResetHealth();
                    ultimaVidaCasulo = casuloHealth.CurrentHealth;

                    // Desativa a destruição automática do DummyHealth do casulo para podermos limpar ele com calma na transição
                    casuloHealth.onDeathOverride = () => { Debug.Log("[Fase 1] O Casulo esvaziou a vida!"); };
                }
            }

            StartCoroutine(RotinaDeAtaquesFase1());
        }
        else
        {
            // FIM DA FASE 1 - A vida do boss chegou em 70% e o BossController acionou essa transição
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

    private void Update()
    {
        // =========================================================
        // A MÁGICA: Repassa todo o dano do Casulo para o Boss real
        // =========================================================
        if (phase1Ativa && casuloHealth != null)
        {
            if (casuloHealth.CurrentHealth < ultimaVidaCasulo)
            {
                // Descobre quanto de dano o jogador causou no casulo
                int danoTomado = ultimaVidaCasulo - casuloHealth.CurrentHealth;
                ultimaVidaCasulo = casuloHealth.CurrentHealth;

                // Repassa esse dano para o Boss. 
                // Quando isso bater o limite (70%), o BossController iniciará a Fase 2 sozinho!
                bossController.GetComponent<DummyHealth>().TakeDamage(danoTomado);

                // O Casulo sente dor e revida!
                if (!atacando && !bossController.IsStunned)
                {
                    RevidarAtaque();
                }
            }
        }
    }

    private IEnumerator RotinaDeAtaquesFase1()
    {
        yield return new WaitForSeconds(tempoEntreAtaques);
        while (phase1Ativa)
        {
            if (!atacando && !bossController.IsStunned && !bossController.IsDead)
            {
                yield return StartCoroutine(Ataque_Prisao(Random.Range(0, 2) == 0 ? pilarPrefab : espinhoPrefab, Random.Range(0, 2) != 0));
            }
            yield return new WaitForSeconds(tempoEntreAtaques);
        }
    }

    private IEnumerator Ataque_Prisao(GameObject prefabObstaculo, bool formatoQuadrado)
    {
        atacando = true;
        
        if (animator != null) animator.SetTrigger("Spell");
        yield return new WaitForSeconds(1f);

        List<Vector3> posicoesFinais = new List<Vector3>();
        Vector3 centro = playerTransform != null ? playerTransform.position : transform.position;
        float alturaAlvo = centro.y + offsetAlturaFinal;

        if (!formatoQuadrado)
        {
            for (int i = 0; i < 8; i++)
            {
                float angulo = i * Mathf.PI * 2 / 8;
                posicoesFinais.Add(centro + new Vector3(Mathf.Cos(angulo), 0, Mathf.Sin(angulo)) * raioDaPrisao + Vector3.up * alturaAlvo);
            }
        }
        else
        {
            Vector3[] d = { new Vector3(-1,0,1), new Vector3(0,0,1), new Vector3(1,0,1), new Vector3(-1,0,0), new Vector3(1,0,0), new Vector3(-1,0,-1), new Vector3(0,0,-1), new Vector3(1,0,-1) };
            foreach (Vector3 dir in d) posicoesFinais.Add(centro + (dir * (raioDaPrisao * 0.8f)) + Vector3.up * alturaAlvo);
        }

        List<Transform> objetosCriados = new List<Transform>();
        foreach (Vector3 posFinal in posicoesFinais)
        {
            Vector3 posSubsolo = posFinal + (Vector3.down * profundidadeSpawn);
            if (prefabObstaculo != null)
            {
                Vector3 lookTarget = new Vector3(centro.x, posFinal.y, centro.z);
                GameObject obj = Instantiate(prefabObstaculo, posSubsolo, Quaternion.LookRotation(lookTarget - posFinal));
                objetosCriados.Add(obj.transform);
                Destroy(obj, tempoVidaPrisao);
            }
        }

        yield return StartCoroutine(ErguerObjetosDoChao(objetosCriados, posicoesFinais));
        atacando = false;
    }

    private IEnumerator ErguerObjetosDoChao(List<Transform> objetos, List<Vector3> posicoesFinais)
    {
        float t = 0f;
        List<Vector3> posIniciais = new List<Vector3>();
        foreach (var o in objetos) posIniciais.Add(o.position);

        while (t < tempoEmergindo)
        {
            t += Time.deltaTime;
            float curva = Mathf.Sin(Mathf.Clamp01(t / tempoEmergindo) * Mathf.PI * 0.5f);
            for (int i = 0; i < objetos.Count; i++)
            {
                if (objetos[i]) objetos[i].position = Vector3.Lerp(posIniciais[i], posicoesFinais[i], curva);
            }
            yield return null;
        }
    }

    private void RevidarAtaque() { if (phase1Ativa && !atacando) StartCoroutine(Ataque_Prisao(Random.value > 0.5f ? pilarPrefab : espinhoPrefab, Random.value > 0.5f)); }
}