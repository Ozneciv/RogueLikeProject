using UnityEngine;
using System.Collections.Generic; // Necessário para usar Listas

public class TotemSpawner : MonoBehaviour
{
    [Header("Referências")]
    public GameObject skullPrefab;
    private Transform playerTransform;

    [Header("Configurações de Spawn")]
    public int totalSkullsToSpawn = 3;
    public float spawnInterval = 5f;
    public float spawnRadius = 10f;
    public float spawnHeightOffset = 1.5f;

    [Header("Ativação")]
    public float activationDistance = 20f;

    private float originalSpawnInterval;
    private bool isBuffed = false;
    private int skullsSpawned = 0;
    private float spawnTimer;
    private bool isActivated = false;
    private DummyHealth health;

    // --- MUDANÇA 1: Lista para rastrear as caveiras criadas ---
    private List<GameObject> activeSkulls = new List<GameObject>();

    void Start()
    {
        health = GetComponent<DummyHealth>();
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) playerTransform = player.transform;
        spawnTimer = spawnInterval;
    }
    // --- LÓGICA DE BUFF DO SINTONIZADOR ---

    public void SetBuff(bool active)
    {
        if (active && !isBuffed)
        {
            isBuffed = true;
            originalSpawnInterval = spawnInterval;
            spawnInterval /= 2f; // Spawna 2x mais rápido!
            // totalSkullsToSpawn += 5; // Opcional: Aumenta o limite total
            Debug.Log(gameObject.name + " foi BUFFADO pelo Sintonizador!");
        }
        else if (!active && isBuffed)
        {
            isBuffed = false;
            spawnInterval = originalSpawnInterval; // Volta ao normal
        }
    }

    void Update()
    {
        // Se o totem morrer, o OnDestroy cuida das caveiras.
        if (health != null && health.CurrentHealth <= 0)
        {
            // O DummyHealth vai destruir este objeto em breve.
            return;
        }

        if (!isActivated)
        {
            if (playerTransform != null && Vector3.Distance(transform.position, playerTransform.position) < activationDistance)
            {
                isActivated = true;

                EnemyIdentity id = GetComponent<EnemyIdentity>() ?? GetComponentInChildren<EnemyIdentity>() ?? GetComponentInParent<EnemyIdentity>();
                Debug.Log("[TOTEM] EnemyIdentity: " + (id != null ? id.nomeInimigo : "NULL") + " | BestiarioManager: " + (BestiarioManager.instancia != null));
                if (id != null && BestiarioManager.instancia != null)
                    BestiarioManager.instancia.Registrar(id);
            }
            return;
        }

        if (skullsSpawned < totalSkullsToSpawn)
        {
            spawnTimer -= Time.deltaTime;
            if (spawnTimer <= 0)
            {
                SpawnSkull();
                spawnTimer = spawnInterval;
            }
        }
    }

    void SpawnSkull()
    {
        skullsSpawned++;

        Vector2 randomCirclePoint = Random.insideUnitCircle.normalized * spawnRadius;
        Vector3 spawnPosition = transform.position + new Vector3(randomCirclePoint.x, 0, randomCirclePoint.y);
        spawnPosition.y = transform.position.y + spawnHeightOffset;

        // --- MUDANÇA 2: Guardar a caveira na lista ---
        GameObject newSkull = Instantiate(skullPrefab, spawnPosition, Quaternion.identity);
        activeSkulls.Add(newSkull);
    }

    // --- MUDANÇA 3: Função chamada automaticamente quando o Totem é destruído ---
    private void OnDestroy()
    {
        // Percorre a lista de caveiras criadas
        foreach (GameObject skull in activeSkulls)
        {
            // Se a caveira ainda existe (não foi destruída pelo jogador), nós a destruímos
            if (skull != null)
            {
                Instantiate(skull.GetComponent<DamageZone>().pulseVisualizer.gameObject, skull.transform.position, Quaternion.identity); // Opcional: Efeito visual ao sumir
                Destroy(skull);
            }
        }
        Debug.Log("Totem destruído: todas as caveiras vinculadas foram removidas.");
    }
}