using UnityEngine;

[RequireComponent(typeof(DummyHealth))]
public class CristalCasulo : MonoBehaviour
{
    [Header("Estágios Visuais (Malhas 3D)")]
    [Tooltip("Arraste os modelos 3D (Meshes) DIRETO DA SUA PASTA aqui!")]
    public Mesh[] malhasVisuais;

    [Header("Configurações de Reação")]
    [Tooltip("Força com que o player é empurrado para trás ao bater no casulo.")]
    public float forcaDoKnockback = 10f;

    [Header("Efeitos Visuais (VFX)")]
    [Tooltip("Prefab do efeito que aparece quando o casulo empurra o player (ex: ondas de choque)")]
    public GameObject vfxKnockbackPrefab;
    [Tooltip("Prefab do efeito de luz que aparece durante a animação de morte")]
    public GameObject vfxMortePrefab;

    [Header("Animação de Morte")]
    public float velocidadeSubida = 2f;
    public float velocidadeGiro = 360f;
    public float tempoAteDestruir = 2.5f;

    private DummyHealth meuHealth; 
    private DummyHealth bossHealth; 
    
    private MeshFilter meuMeshFilter; 
    private int indiceAtual = -1;

    private int vidaAnterior;
    private GameObject playerNaCena;
    private bool estaMorto = false;

    private void Awake()
    {
        meuHealth = GetComponent<DummyHealth>();
        meuMeshFilter = GetComponentInChildren<MeshFilter>();
    }

    private void Start()
    {
        if (meuHealth != null)
        {
            vidaAnterior = meuHealth.CurrentHealth;
            meuHealth.onDeathOverride += IniciarMorte; 
        }

        playerNaCena = GameObject.FindGameObjectWithTag("Player");
        AtualizarVisual();
    }

    private void Update()
    {
        // Se estiver morto, sai fora para não atrapalhar o timing do dano
        if (estaMorto) return; 

        AtualizarVisual();
        VerificarDanoEKnockback();
    }

    public void Setup(DummyHealth healthDoBoss)
    {
        bossHealth = healthDoBoss;
    }

    // =========================================================================
    // LÓGICA DE DANO COMUM (ANTES DA MORTE)
    // =========================================================================
    private void VerificarDanoEKnockback()
    {
        if (meuHealth == null) return;

        if (meuHealth.CurrentHealth < vidaAnterior)
        {
            int danoTomado = vidaAnterior - meuHealth.CurrentHealth;
            
            // Repassa o dano imediatamente APENAS se não foi o hit fatal
            if (bossHealth != null)
            {
                bossHealth.TakeDamage(danoTomado); 
            }

            if (playerNaCena != null)
            {
                AplicarKnockback(playerNaCena);
            }

            vidaAnterior = meuHealth.CurrentHealth;
        }
    }

    private void AplicarKnockback(GameObject player)
    {
        Vector3 direcaoEmpurrao = (player.transform.position - transform.position).normalized;
        direcaoEmpurrao.y = 0.2f; 

        Rigidbody rbPlayer = player.GetComponent<Rigidbody>();
        if (rbPlayer != null)
        {
            rbPlayer.linearVelocity = Vector3.zero; 
            rbPlayer.AddForce(direcaoEmpurrao * forcaDoKnockback, ForceMode.Impulse);
            
            if (vfxKnockbackPrefab != null)
            {
                Instantiate(vfxKnockbackPrefab, transform.position, Quaternion.identity);
            }
        }
    }

    private void AtualizarVisual()
    {
        if (meuHealth == null || malhasVisuais.Length == 0 || meuMeshFilter == null) return;
        if (meuHealth.maxHealth <= 0) return;

        float porcentagemVida = (float)meuHealth.CurrentHealth / meuHealth.maxHealth;
        float danoTomado = 1f - porcentagemVida;
        
        int novoIndice = Mathf.FloorToInt(danoTomado * malhasVisuais.Length);
        novoIndice = Mathf.Clamp(novoIndice, 0, malhasVisuais.Length - 1);

        if (novoIndice != indiceAtual)
        {
            if (malhasVisuais[novoIndice] != null)
            {
                meuMeshFilter.mesh = malhasVisuais[novoIndice];
            }
            indiceAtual = novoIndice;
        }
    }

    // =========================================================================
    // LÓGICA DE MORTE E TRANSIÇÃO
    // =========================================================================
    private void IniciarMorte()
    {
        if (estaMorto) return;
        estaMorto = true;

        // 1. Pega EXATAMENTE o dano desse último golpe fatal antes de separar o visual
        int danoFatal = 0;
        if (meuHealth != null)
        {
            danoFatal = vidaAnterior - meuHealth.CurrentHealth;
            vidaAnterior = meuHealth.CurrentHealth; 
        }

        if (meuMeshFilter != null)
        {
            GameObject visualDoCasulo = meuMeshFilter.gameObject;
            visualDoCasulo.transform.SetParent(null); 

            EfeitoVoo voo = visualDoCasulo.AddComponent<EfeitoVoo>();
            voo.velocidadeSubida = velocidadeSubida;
            voo.velocidadeGiro = velocidadeGiro;
            
            // 2. MÁGICA: Passamos a "maleta" de dano final direto para a arte voadora!
            voo.Configurar(bossHealth, danoFatal);

            if (vfxMortePrefab != null)
            {
                GameObject luzVfx = Instantiate(vfxMortePrefab, visualDoCasulo.transform.position, Quaternion.identity);
                luzVfx.transform.SetParent(visualDoCasulo.transform);
            }

            Destroy(visualDoCasulo, tempoAteDestruir);
        }
    }
} 

// =========================================================================
// SCRIPT DE VOO (INDEPENDENTE E BLINDADO)
// =========================================================================
public class EfeitoVoo : MonoBehaviour
{
    public float velocidadeSubida;
    public float velocidadeGiro;

    private DummyHealth bossHealthParaAvisar;
    private int danoGuardado;
    private bool jaAplicouDano = false;

    // Recebe as ordens finais antes da lógica original do casulo morrer
    public void Configurar(DummyHealth boss, int dano)
    {
        bossHealthParaAvisar = boss;
        danoGuardado = dano;
    }

    private void Update()
    {
        transform.Translate(Vector3.up * velocidadeSubida * Time.deltaTime, Space.World);
        transform.Rotate(Vector3.up * velocidadeGiro * Time.deltaTime);
    }

    private void OnDestroy()
    {

        if (!jaAplicouDano && bossHealthParaAvisar != null && danoGuardado > 0)
        {
            jaAplicouDano = true;
            bossHealthParaAvisar.TakeDamage(danoGuardado);
        }
    }
}