using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class LevelGenerator : MonoBehaviour
{
    [Header("Prefabs das Salas")]
    public GameObject startRoomPrefab;      // Sua sala 20x20 segura
    public List<GameObject> mainRoomPrefabs;  // Suas salas 20x80, 80x80, etc.
    public GameObject transitionRoomPrefab; // Seu corredor 10x20

    [Header("Prefabs Especiais")]
    public GameObject merchantRoomPrefab;
    public GameObject merchantPrefab;
    [Tooltip("O prefab da sala final com a porta de 'próximo nível'.")]
    public GameObject exitRoomPrefab;
    [Tooltip("Prefab pequeno para selar becos sem saída (ex: parede/beco curto). Evita que corredores terminem no vazio.")]
    public GameObject deadEndPrefab;

    [Header("Regras de Geração")]
    public int maxMainRooms = 10;
    [Tooltip("Quantas vezes cada prefab de sala principal pode aparecer por run. Aumente se tiver poucos prefabs cadastrados (ex: 2 prefabs → precisa de pelo menos 5 aqui para chegar em maxMainRooms=10).")]
    public int roomLimitPerType = 5;
    [Range(0, 1)]
    [Tooltip("Chance de spawnar o mercador em becos sem saída. Se nenhum sortear, o último beco sem saída garante o spawn.")]
    public float merchantRoomChance = 0.25f;

    [Header("Configurações")]
    public float extraLoadDelay = 1.0f;

    private Dictionary<string, int> roomCounts = new Dictionary<string, int>();
    private List<Socket> openSockets = new List<Socket>();
    private Transform playerSpawnPoint;
    private bool merchantRoomSpawned = false;
    // --- MUDANÇA 2: Trava para a Saída ---
    private bool exitRoomSpawned = false;

    // --- ECONOMIA: rastreia sequência de salas para inicializar RoomControllers ---
    private int roomSequenceCounter = 0;

    // Classe auxiliar (sem mudanças)
    private class Socket
    {
        public Transform SocketTransform;
        public Vector3 Direction;
    }

    void Start()
    {
        // Espera o GameManager chamar
    }

    public void GenerateLevel()
    {
        roomCounts.Clear();
        openSockets.Clear();
        merchantRoomSpawned = false;
        exitRoomSpawned = false;
        roomSequenceCounter = 0;

        GameObject startRoom = Instantiate(startRoomPrefab, Vector3.zero, Quaternion.identity);
        int currentRoomCount = 1;

        playerSpawnPoint = FindSocket(startRoom.transform, "Player_StartPoint");
        if (playerSpawnPoint == null)
        {
            Debug.LogError("Sala Inicial não tem um 'Player_StartPoint'!");
            return;
        }

        AddSocketsToFrontier(startRoom.transform, Vector3.zero, true); 
        StartCoroutine(GenerationLoop(currentRoomCount));
    }

    IEnumerator GenerationLoop(int roomCount)
    {
        // O loop principal constrói o caminho
        while (roomCount < maxMainRooms && openSockets.Count > 0)
        {
            int randomIndex = Random.Range(0, openSockets.Count);
            Socket currentSocket = openSockets[randomIndex];
            openSockets.RemoveAt(randomIndex); 

            GameObject transitionRoom = Instantiate(transitionRoomPrefab);
            Transform transitionEntrada = FindSocket(transitionRoom.transform, "Entrance");
            Transform transitionSaida = FindSocket(transitionRoom.transform, "Exit");
            
            if (transitionEntrada == null || transitionSaida == null)
            {
                Debug.LogError("Prefab da Sala de Transição está faltando 'Entrance' ou 'Exit'");
                Destroy(transitionRoom);
                continue;
            }
            
            AlignRooms(currentSocket.SocketTransform, transitionEntrada);
            
            GameObject roomPrefabToSpawn = GetValidRoomPrefab(currentSocket.Direction);

            if (roomPrefabToSpawn != null)
            {
                // SUCESSO: Constrói a próxima sala principal
                GameObject newMainRoom = Instantiate(roomPrefabToSpawn);
                Transform mainEntrada = FindMatchingSocket(newMainRoom.transform, currentSocket.Direction);
                AlignRooms(transitionSaida, mainEntrada);

                roomCount++;
                roomSequenceCounter++;
                string roomName = roomPrefabToSpawn.name;
                if (!roomCounts.ContainsKey(roomName)) roomCounts[roomName] = 0;
                roomCounts[roomName]++;

                // --- ECONOMIA: Inicializa o RoomController com o índice da sala ---
                RoomController roomCtrl = newMainRoom.GetComponentInChildren<RoomController>();
                if (roomCtrl != null)
                    roomCtrl.Initialize(roomSequenceCounter);

                AddSocketsToFrontier(newMainRoom.transform, currentSocket.Direction, false);
            }
            else
            {
                // FALHA: Beco sem saída. Adiciona o *soquete da transição* de volta à lista.
                // Mas agora como um "beco sem saída" oficial.
                openSockets.Add(new Socket { SocketTransform = transitionSaida, Direction = currentSocket.Direction });
                Debug.LogWarning("Nenhuma sala encontrada que se encaixe no soquete " + currentSocket.SocketTransform.name + ". Marcando como beco sem saída.");
            }

            yield return null;
        }
        
        Debug.Log("Geração do Caminho Principal Concluída! Processando becos sem saída...");

        // --- MUDANÇA 3: Lógica de Beco sem Saída (Executada APÓS o loop principal) ---
        
        // Primeiro, escolhemos UMA saída
        if (openSockets.Count > 0)
        {
            int exitIndex = Random.Range(0, openSockets.Count);
            Socket exitSocket = openSockets[exitIndex];
            openSockets.RemoveAt(exitIndex); // Remove o soquete da saída da lista

            // Constrói a sala de saída
            SpawnExitRoom(exitSocket.SocketTransform);
            exitRoomSpawned = true;
        }

        // Todos os soquetes restantes são becos sem saída.
        // Primeiro tenta sortear o mercador. Caso o sorteio falhe em todos,
        // garante o spawn no ÚLTIMO beco sem saída (fallback 100%).
        List<Socket> deadEnds = new List<Socket>(openSockets);
        for (int i = 0; i < deadEnds.Count; i++)
        {
            Socket socket = deadEnds[i];
            bool isLast = (i == deadEnds.Count - 1);

            if (!merchantRoomSpawned && (Random.value < merchantRoomChance || isLast))
            {
                SpawnMerchantRoom(socket.SocketTransform);
                merchantRoomSpawned = true;
            }
            else
            {
                // Sela o beco com a sala Dead End (se definida); do contrário só loga aviso.
                SealDeadEnd(socket.SocketTransform);
            }
        }
        
        Debug.Log("Geração de Nível Completa (com Salas Especiais)!");
        
        yield return new WaitForSeconds(extraLoadDelay);

        // SPAWN DE ITENS (nível já está pronto)
        ItemSpawner itemSpawner = FindFirstObjectByType<ItemSpawner>();
        if (itemSpawner != null)
        {
            itemSpawner.SpawnItems();
        }
        else
        {
            Debug.LogWarning("ItemSpawner não encontrado na cena!");
        }
        
        if (GameManager.instance != null)
        {
            GameManager.instance.OnLevelReady(playerSpawnPoint);
        }
    }

    // Função de "costura" (sem mudanças)
    void AlignRooms(Transform socketA, Transform socketB)
    {
        if (socketA == null || socketB == null) { /* ... (código de erro) ... */ return; }
        Transform roomB = socketB.root;
        Quaternion targetRotation = Quaternion.LookRotation(-socketA.forward, socketA.up);
        Quaternion correctionRotation = targetRotation * Quaternion.Inverse(socketB.rotation);
        roomB.rotation = correctionRotation * roomB.rotation;
        Vector3 translation = socketA.position - socketB.position;
        roomB.position += translation;
    }

    // GetValidRoomPrefab (sem mudanças)
    GameObject GetValidRoomPrefab(Vector3 incomingDirection)
    {
        string requiredSocketName = GetSocketNameFromDirection(-incomingDirection);
        if (requiredSocketName == null) return null;
        List<GameObject> validPrefabs = new List<GameObject>();
        foreach (GameObject prefab in mainRoomPrefabs)
        {
            string name = prefab.name;
            int count = roomCounts.ContainsKey(name) ? roomCounts[name] : 0;
            if (count >= roomLimitPerType) continue; 
            if (FindSocket(prefab.transform, requiredSocketName) != null)
            {
                validPrefabs.Add(prefab);
            }
        }
        if (validPrefabs.Count == 0) return null; 
        return validPrefabs[Random.Range(0, validPrefabs.Count)];
    }

    // AddSocketsToFrontier (sem mudanças, já está correto)
    void AddSocketsToFrontier(Transform room, Vector3 incomingDirection, bool isStartRoom)
    {
        Vector3 dirOp = -incomingDirection;
        List<Socket> foundSockets = new List<Socket>();
        foreach (Transform child in room.GetComponentsInChildren<Transform>())
        {
            Vector3 socketDir = Vector3.zero;
            if (child.name == "North") socketDir = Vector3.forward;
            else if (child.name == "South") socketDir = Vector3.back;
            else if (child.name == "East") socketDir = Vector3.right;
            else if (child.name == "West") socketDir = Vector3.left;
            if (socketDir == Vector3.zero || socketDir == dirOp) continue;
            foundSockets.Add(new Socket { SocketTransform = child, Direction = socketDir });
        }
        if (isStartRoom && foundSockets.Count > 0)
        {
            List<Socket> validStartSockets = new List<Socket>();
            foreach (Socket s in foundSockets)
            {
                if (s.SocketTransform.name != "West")
                {
                    validStartSockets.Add(s);
                }
            }
            if (validStartSockets.Count > 0)
            {
                int randomIndex = Random.Range(0, validStartSockets.Count);
                openSockets.Add(validStartSockets[randomIndex]);
            }
        }
        else
        {
            openSockets.AddRange(foundSockets);
        }
    }

    // SpawnMerchantRoom (agora se conecta a um soquete de transição)
    void SpawnMerchantRoom(Transform corridorExitSocket)
    {
        if (merchantRoomPrefab == null || merchantPrefab == null) return;
        GameObject room = Instantiate(merchantRoomPrefab);
        Transform entrada = FindSocket(room.transform, "Entrance"); 
        if (entrada == null) { Destroy(room); return; }
        AlignRooms(corridorExitSocket, entrada); 
        Transform spawnPoint = FindSocket(room.transform, "Merchant_SpawnPoint");
        if (spawnPoint == null) return;
        Instantiate(merchantPrefab, spawnPoint.position, spawnPoint.rotation);
        Debug.Log("Sala do Mercador criada!");
    }

    // Sala de Saída — conecta ao corredor de transição pelo Entrance
    void SpawnExitRoom(Transform corridorExitSocket)
    {
        if (exitRoomPrefab == null)
        {
            Debug.LogWarning("Nenhum prefab de Sala de Saída (ExitRoom) definido no LevelGenerator.");
            return;
        }
        
        GameObject room = Instantiate(exitRoomPrefab);
        Transform entrada = FindSocket(room.transform, "Entrance"); 
        if (entrada == null) 
        {
            Debug.LogError("Prefab da Sala de Saída não tem um soquete 'Entrance'!");
            Destroy(room); 
            return; 
        }
        
        AlignRooms(corridorExitSocket, entrada); 
        Debug.Log("Sala de Saída criada!");
    }

    // Sela um beco sem saída com o prefab Dead End (evita corredores no vazio)
    void SealDeadEnd(Transform corridorExitSocket)
    {
        if (deadEndPrefab == null)
        {
            Debug.LogWarning("[LevelGenerator] Dead End não selado: campo 'deadEndPrefab' não definido no Inspector.");
            return;
        }

        GameObject deadEnd = Instantiate(deadEndPrefab);
        Transform entrada = FindSocket(deadEnd.transform, "Entrance");
        if (entrada == null)
        {
            // Fallback: alinha pela própria raiz
            deadEnd.transform.position = corridorExitSocket.position;
            deadEnd.transform.rotation = Quaternion.LookRotation(-corridorExitSocket.forward);
            return;
        }

        AlignRooms(corridorExitSocket, entrada);
        Debug.Log("Dead End selado.");
    }
    
    // FindMatchingSocket (sem mudanças)
    Transform FindMatchingSocket(Transform room, Vector3 incomingDirection)
    {
        string socketName = GetSocketNameFromDirection(-incomingDirection);
        if (socketName != null) { return FindSocket(room, socketName); }
        return FindSocket(room, "Entrance");
    }

    // GetSocketNameFromDirection (sem mudanças)
    string GetSocketNameFromDirection(Vector3 direction)
    {
        if (direction == Vector3.forward) return "North";
        if (direction == Vector3.back) return "South";
        if (direction == Vector3.right) return "East";
        if (direction == Vector3.left) return "West";
        return null;
    }

    // FindSocket (sem mudanças)
    Transform FindSocket(Transform room, string socketName)
    {
        return room.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name == socketName);
    }


}