using UnityEngine;

public class TotemSpawner : MonoBehaviour
{
    [Header("Referências")]
    public GameObject skullPrefab;
    private Transform playerTransform;

    [Header("Configurações de Spawn")]
    public int totalSkullsToSpawn = 3;
    public float spawnInterval = 5f;
    public float spawnRadius = 10f;
    // --- NOVA VARIÁVEL AQUI ---
    [Tooltip("A altura acima do totem em que as caveiras irão aparecer.")]
    public float spawnHeightOffset = 1.5f;

    [Header("Ativação")]
    public float activationDistance = 20f;

    private int skullsSpawned = 0;
    private float spawnTimer;
    private bool isActivated = false;
    private DummyHealth health;

    void Start()
    {
        health = GetComponent<DummyHealth>();
        playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
        spawnTimer = spawnInterval;
    }

    void Update()
    {
        if (health != null && health.CurrentHealth <= 0)
        {
            this.enabled = false;
            return;
        }

        if (!isActivated)
        {
            if (playerTransform != null && Vector3.Distance(transform.position, playerTransform.position) < activationDistance)
            {
                Debug.Log("Totem ativado!");
                isActivated = true;
            }
            return;
        }
        
        if (skullsSpawned >= totalSkullsToSpawn)
        {
            return;
        }

        spawnTimer -= Time.deltaTime;
        if (spawnTimer <= 0)
        {
            SpawnSkull();
            spawnTimer = spawnInterval;
        }
    }

    void SpawnSkull()
    {
        skullsSpawned++;
        Debug.Log("Totem invocando caveira " + skullsSpawned + "/" + totalSkullsToSpawn);

        Vector2 randomCirclePoint = Random.insideUnitCircle.normalized * spawnRadius;
        Vector3 spawnPosition = transform.position + new Vector3(randomCirclePoint.x, 0, randomCirclePoint.y);

        // --- LÓGICA DA ALTURA MODIFICADA ---
        // Agora usa a altura do totem + o offset definido.
        spawnPosition.y = transform.position.y + spawnHeightOffset;

        Instantiate(skullPrefab, spawnPosition, Quaternion.identity);

        if (skullsSpawned >= totalSkullsToSpawn)
        {
            Debug.Log("Totem terminou de invocar.");
        }
    }
}