using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RoomController : MonoBehaviour
{
    [Header("Configurações da Sala")]
    public bool isSafeRoom = false;
    public bool doorsAreLocked = false;

    [Header("Configurações de Horda")]
    public List<GameObject> biomeEnemyPool; 
    public int minEnemyTypes = 2;
    public int maxEnemyTypes = 5;

    [Header("Ondas")]
    [Range(1, 5)] public int minWaves = 1;
    [Range(1, 5)] public int maxWaves = 3;
    public int minEnemiesPerWave = 3;
    public int maxEnemiesPerWave = 5;

    [Header("Spawn")]
    public BoxCollider spawnArea; 
    public GameObject spawnIndicatorPrefab;
    public float spawnDelay = 1.5f;
    
    // --- NOVA VARIÁVEL AQUI ---
    [Tooltip("Altura extra do chão para spawnar os inimigos (ex: 2 para o ar, 0 para o chão).")]
    public float spawnHeightOffset = 2f; 

    [Header("Referências")]
    public GameObject[] doors;

    private List<GameObject> chosenEnemyTypes = new List<GameObject>();
    private int totalWavesThisRoom;
    private int currentWave = 0;
    private List<GameObject> activeEnemies = new List<GameObject>();
    private bool hasTriggered = false;
    private bool combatActive = false; 
    private int enemiesPendingSpawn = 0; 

    void Start()
    {
        if (isSafeRoom) return;
        if (biomeEnemyPool == null || biomeEnemyPool.Count == 0)
        {
            Debug.LogError($"[RoomController] ERRO: Lista de inimigos vazia em {gameObject.name}");
            return;
        }

        PickRoomEnemies();
        totalWavesThisRoom = Random.Range(minWaves, maxWaves + 1);
        UnlockDoors();
    }

    void Update()
    {
        if (hasTriggered && combatActive)
        {
            CheckWaveStatus();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !isSafeRoom && !hasTriggered)
        {
            hasTriggered = true;
            StartCoroutine(StartCombatEncounter());
        }
    }

    IEnumerator StartCombatEncounter()
    {
        LockDoors();
        Debug.Log("Portas trancadas. Iniciando combate em 1s...");
        yield return new WaitForSeconds(1f);
        
        SpawnNextWave();
        combatActive = true;
    }

    void SpawnNextWave()
    {
        currentWave++;
        int enemyCount = Random.Range(minEnemiesPerWave, maxEnemiesPerWave + 1);
        enemyCount += (currentWave - 1); 

        Debug.Log($"--- INICIANDO ONDA {currentWave}/{totalWavesThisRoom} ({enemyCount} inimigos) ---");

        enemiesPendingSpawn += enemyCount;

        for (int i = 0; i < enemyCount; i++)
        {
            StartCoroutine(SpawnEnemyWithDelay());
        }
    }

    IEnumerator SpawnEnemyWithDelay()
    {
        if (chosenEnemyTypes.Count == 0 || spawnArea == null) 
        {
            enemiesPendingSpawn--; 
            yield break;
        }

        GameObject enemyPrefab = chosenEnemyTypes[Random.Range(0, chosenEnemyTypes.Count)];
        
        // Pega a posição já com a altura corrigida
        Vector3 spawnPos = GetRandomPositionInArea();

        GameObject indicator = null;
        if (spawnIndicatorPrefab != null)
        {
            // O indicador (círculo roxo) deve ficar no CHÃO, então subtraímos a altura
            Vector3 indicatorPos = new Vector3(spawnPos.x, spawnArea.transform.position.y + 0.05f, spawnPos.z);
            indicator = Instantiate(spawnIndicatorPrefab, indicatorPos, Quaternion.identity);
        }

        yield return new WaitForSeconds(spawnDelay);

        if (indicator != null) Destroy(indicator);

        if (enemyPrefab != null)
        {
            GameObject newEnemy = Instantiate(enemyPrefab, spawnPos, enemyPrefab.transform.rotation);
            MagicStone_AI magicStone = newEnemy.GetComponent<MagicStone_AI>();
            if (magicStone != null)
            {
                magicStone.SetRoomBounds(spawnArea); // Passa o BoxCollider da área de spawn
            }          
            activeEnemies.Add(newEnemy);
            
        }
        
        enemiesPendingSpawn--;
    }

    void CheckWaveStatus()
    {
        activeEnemies.RemoveAll(item => item == null);

        if (activeEnemies.Count == 0 && enemiesPendingSpawn == 0)
        {
            if (currentWave < totalWavesThisRoom)
            {
                combatActive = false; 
                Debug.Log("Onda vencida! Próxima em 2s...");
                Invoke("StartNextWaveDelayed", 2f);
            }
            else
            {
                Debug.Log("SALA LIMPA! Destrancando.");
                UnlockDoors();
                this.enabled = false; 
            }
        }
    }

    void StartNextWaveDelayed()
    {
        SpawnNextWave();
        combatActive = true; 
    }

    // --- FUNÇÃO ATUALIZADA COM ALTURA ---
    Vector3 GetRandomPositionInArea()
    {
        if (spawnArea == null) return transform.position;
        
        Bounds bounds = spawnArea.bounds;
        float randomX = Random.Range(bounds.min.x, bounds.max.x);
        float randomZ = Random.Range(bounds.min.z, bounds.max.z);
        
        // Usa a posição Y da área + o offset que você definir
        float fixedY = spawnArea.transform.position.y + spawnHeightOffset;

        return new Vector3(randomX, fixedY, randomZ);
    }

    void PickRoomEnemies()
    {
        if (biomeEnemyPool.Count == 0) return;
        List<GameObject> availablePool = new List<GameObject>(biomeEnemyPool);
        int typesToPick = Mathf.Clamp(Random.Range(minEnemyTypes, maxEnemyTypes + 1), 1, availablePool.Count);

        for (int i = 0; i < typesToPick; i++)
        {
            if (availablePool.Count == 0) break;
            int randomIndex = Random.Range(0, availablePool.Count);
            chosenEnemyTypes.Add(availablePool[randomIndex]);
            availablePool.RemoveAt(randomIndex);
        }
    }

    void LockDoors() 
    { 
        doorsAreLocked = true; 
        foreach (GameObject d in doors) if(d) d.SetActive(true); 
    }
    
    void UnlockDoors() 
    { 
        doorsAreLocked = false; 
        foreach (GameObject d in doors) if(d) d.SetActive(false); 
    }
}