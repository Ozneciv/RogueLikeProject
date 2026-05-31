using UnityEngine;

public class DetectorDeInimigo : MonoBehaviour
{
    [Header("Referencias")]
    public EnemyIdentity enemyIdentity;

    [Header("Modo de Deteccao")]
    [Tooltip("Se ativo, registra quando o Player entra em um Trigger deste objeto.")]
    public bool usarTrigger = true;

    [Tooltip("Se ativo, registra por distancia no Update.")]
    public bool usarDistancia = false;

    [Tooltip("Distancia usada no modo por distancia.")]
    public float distanciaDeteccao = 12f;

    [Header("Debug")]
    public bool debugLogs = false;

    private Transform playerTransform;
    private bool jaTentouRegistrar = false;

    void Awake()
    {
        if (enemyIdentity == null)
            enemyIdentity = GetComponent<EnemyIdentity>()
                           ?? GetComponentInChildren<EnemyIdentity>()
                           ?? GetComponentInParent<EnemyIdentity>();
    }

    void Start()
    {
        CachePlayer();

        if (enemyIdentity == null)
            Debug.LogWarning("[DETECTOR INIMIGO] EnemyIdentity nao encontrado em " + gameObject.name);
    }

    void Update()
    {
        if (!usarDistancia) return;
        if (jaTentouRegistrar) return;

        if (enemyIdentity == null || enemyIdentity.foiEncontrado) return;

        if (playerTransform == null)
            CachePlayer();

        if (playerTransform == null) return;

        float dist = Vector3.Distance(transform.position, playerTransform.position);
        if (dist <= distanciaDeteccao)
            TentarRegistrar("distancia");
    }

    void OnTriggerEnter(Collider other)
    {
        if (!usarTrigger) return;
        if (jaTentouRegistrar) return;
        if (enemyIdentity == null || enemyIdentity.foiEncontrado) return;
        if (!other.CompareTag("Player")) return;

        TentarRegistrar("trigger");
    }

    private void TentarRegistrar(string origem)
    {
        jaTentouRegistrar = true;

        if (BestiarioManager.instancia == null)
        {
            if (debugLogs)
                Debug.LogWarning("[DETECTOR INIMIGO] BestiarioManager.instancia NULL ao registrar via " + origem);

            // Permite nova tentativa quando o manager estiver disponivel.
            jaTentouRegistrar = false;
            return;
        }

        BestiarioManager.instancia.Registrar(enemyIdentity);

        if (debugLogs)
        {
            string nome = enemyIdentity != null && enemyIdentity.enemyData != null
                ? enemyIdentity.enemyData.enemyName
                : gameObject.name;
            Debug.Log("[DETECTOR INIMIGO] Tentativa de registro via " + origem + ": " + nome);
        }
    }

    private void CachePlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            playerTransform = player.transform;
    }

    void OnDrawGizmosSelected()
    {
        if (!usarDistancia) return;
        Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, distanciaDeteccao);
    }
}
