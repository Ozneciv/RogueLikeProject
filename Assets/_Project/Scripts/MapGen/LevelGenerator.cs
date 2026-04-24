using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Gerador de nível procedural baseado em ConnectionPoints.
///
/// COMO FUNCIONA:
///   1. Instancia a Safe Room na origem.
///   2. Coleta todos os ConnectionPoints do tipo "Saida" da sala inicial.
///   3. Para cada saída livre, tenta parear com a "Entrada" de um novo prefab compatível.
///   4. Usa AlignRooms para encaixar os dois transforms perfeitamente.
///   5. Repete até atingir maxMainRooms ou esgotar as saídas.
///   6. Saídas restantes viram: ExitRoom, MerchantRoom ou DeadEnd.
///
/// SETUP NO INSPECTOR:
///   • startRoomPrefab    → Sala inicial (Safe Room) com ConnectionPoints configurados.
///   • mainRoomPrefabs    → Lista de salas de combate, orgânicas, Y-shape, etc.
///   • transitionRoomPrefab → Corredor de transição com 1 Entrada + 1 Saida.
///   • merchantRoomPrefab, merchantPrefab, exitRoomPrefab, deadEndPrefab → especiais.
///
/// REGRA DE SETUP DOS PREFABS:
///   Cada prefab DEVE ter exatamente 1 ConnectionPoint(Entrada) e N ConnectionPoints(Saida).
///   O forward de cada ConnectionPoint deve apontar para FORA da sala.
/// </summary>
public class LevelGenerator : MonoBehaviour
{
    // =========================================================
    // INSPECTOR
    // =========================================================

    [Header("Prefabs das Salas")]
    [Tooltip("A sala inicial (Safe Room). Deve ter 1 Entrada (usada como âncora) e N Saídas.")]
    public GameObject startRoomPrefab;

    [Tooltip("Pool de salas de combate, orgânicas, Y-shape, etc. Misture quantas quiser.")]
    public List<GameObject> mainRoomPrefabs;

    [Tooltip("Corredor de ligação entre salas. Deve ter 1 Entrada e 1 Saída.")]
    public GameObject transitionRoomPrefab;

    [Header("Prefabs Especiais")]
    public GameObject merchantRoomPrefab;
    public GameObject merchantPrefab;

    [Tooltip("Sala final com portal para o próximo nível. Deve ter 1 Entrada.")]
    public GameObject exitRoomPrefab;

    [Tooltip("Prefab que sela becos sem saída organicamente (raízes, rochas). Deve ter 1 Entrada.")]
    public GameObject deadEndPrefab;

    [Header("Regras de Geração")]
    [Tooltip("Número alvo de salas principais (sem contar especiais).")]
    public int maxMainRooms = 10;

    [Tooltip("Quantas vezes o mesmo prefab pode aparecer por run. Evita repetição excessiva.")]
    public int roomLimitPerType = 5;

    [Range(0f, 1f)]
    [Tooltip("Chance de cada beco sem saída virar sala do Mercador. O último garante o spawn caso nenhum sorteie.")]
    public float merchantRoomChance = 0.25f;

    [Header("Configurações")]
    [Tooltip("Delay extra após a geração antes de notificar o GameManager (para assets carregarem).")]
    public float extraLoadDelay = 1.0f;

    // =========================================================
    // ESTADO INTERNO
    // =========================================================

    /// <summary>Contagem de usos de cada prefab (por nome) na run atual.</summary>
    private Dictionary<string, int> roomCounts = new Dictionary<string, int>();

    /// <summary>Fila de ConnectionPoints do tipo Saida ainda não conectados.</summary>
    private List<ConnectionPoint> openOutputs = new List<ConnectionPoint>();

    private Transform playerSpawnPoint;
    private bool merchantRoomSpawned = false;
    private bool exitRoomSpawned = false;
    private int roomSequenceCounter = 0;

    // =========================================================
    // API PÚBLICA
    // =========================================================

    /// <summary>Chamado pelo GameManager para iniciar a geração.</summary>
    public void GenerateLevel()
    {
        roomCounts.Clear();
        openOutputs.Clear();
        merchantRoomSpawned = false;
        exitRoomSpawned = false;
        roomSequenceCounter = 0;

        // --- Instancia a Safe Room na origem ---
        GameObject startRoom = Instantiate(startRoomPrefab, Vector3.zero, Quaternion.identity);

        playerSpawnPoint = FindNamedChild(startRoom.transform, "Player_StartPoint");
        if (playerSpawnPoint == null)
        {
            Debug.LogError("[LevelGenerator] Safe Room não tem um filho chamado 'Player_StartPoint'!");
            return;
        }

        // Registra as saídas da Safe Room (pula a Entrada, que fica no West/Nave)
        RegisterOutputPoints(startRoom, isStartRoom: true);

        StartCoroutine(GenerationLoop(roomCount: 1));
    }

    // =========================================================
    // LOOP DE GERAÇÃO
    // =========================================================

    IEnumerator GenerationLoop(int roomCount)
    {
        while (roomCount < maxMainRooms && openOutputs.Count > 0)
        {
            // Pega uma saída aleatória da fila
            int idx = Random.Range(0, openOutputs.Count);
            ConnectionPoint currentOutput = openOutputs[idx];
            openOutputs.RemoveAt(idx);

            if (currentOutput.isOccupied) continue;
            currentOutput.isOccupied = true;

            // --- Corredor de Transição ---
            if (transitionRoomPrefab == null)
            {
                Debug.LogError("[LevelGenerator] transitionRoomPrefab não definido!");
                continue;
            }

            GameObject transitionRoom = Instantiate(transitionRoomPrefab);
            ConnectionPoint transEntrada = GetInputPoint(transitionRoom, currentOutput.connectionTag, currentOutput.transform);
            ConnectionPoint transSaida   = GetFirstOutputPoint(transitionRoom, currentOutput.connectionTag);

            if (transEntrada == null || transSaida == null)
            {
                Debug.LogError("[LevelGenerator] Prefab de Transição precisa de 1 Entrada e 1 Saída com ConnectionPoint.");
                Destroy(transitionRoom);
                continue;
            }

            AlignRooms(currentOutput.transform, transEntrada.transform);
            transEntrada.isOccupied = true;

            // --- Sala Principal ---
            GameObject roomPrefab = GetCompatibleRoomPrefab(transSaida.connectionTag);

            if (roomPrefab != null)
            {
                GameObject newRoom = Instantiate(roomPrefab);
                ConnectionPoint roomEntrada = GetInputPoint(newRoom, transSaida.connectionTag, transSaida.transform);

                if (roomEntrada == null)
                {
                    Debug.LogError($"[LevelGenerator] Prefab '{roomPrefab.name}' não tem ConnectionPoint(Entrada). Verifique o prefab.");
                    Destroy(newRoom);
                    Destroy(transitionRoom);
                    continue;
                }

                AlignRooms(transSaida.transform, roomEntrada.transform);
                transSaida.isOccupied = true;
                roomEntrada.isOccupied = true;

                // Contagem e índice
                roomCount++;
                roomSequenceCounter++;
                string prefabName = roomPrefab.name;
                if (!roomCounts.ContainsKey(prefabName)) roomCounts[prefabName] = 0;
                roomCounts[prefabName]++;

                // Inicializa o RoomController (sistema de ondas/economia)
                RoomController roomCtrl = newRoom.GetComponentInChildren<RoomController>();
                if (roomCtrl != null)
                    roomCtrl.Initialize(roomSequenceCounter);

                // Registra as saídas da nova sala
                RegisterOutputPoints(newRoom, isStartRoom: false);
            }
            else
            {
                // Sem sala compatível — devolve a saída da transição como beco
                openOutputs.Add(transSaida);
                Debug.LogWarning($"[LevelGenerator] Nenhum prefab compatível com tag '{transSaida.connectionTag}'. Beco registrado.");
            }

            yield return null;
        }

        Debug.Log("[LevelGenerator] Caminho principal concluído. Processando saídas restantes...");
        yield return StartCoroutine(ProcessRemainingOutputs());
    }

    // =========================================================
    // PROCESSAMENTO DE SAÍDAS RESTANTES
    // =========================================================

    IEnumerator ProcessRemainingOutputs()
    {
        // 1. Escolhe UMA saída para ser a ExitRoom
        if (openOutputs.Count > 0 && !exitRoomSpawned)
        {
            int exitIdx = Random.Range(0, openOutputs.Count);
            ConnectionPoint exitOutput = openOutputs[exitIdx];
            openOutputs.RemoveAt(exitIdx);
            SpawnSpecialRoom(exitRoomPrefab, exitOutput, "Sala de Saída");
            exitRoomSpawned = true;
        }

        // 2. Demais saídas → Mercador ou DeadEnd
        List<ConnectionPoint> deadEnds = new List<ConnectionPoint>(openOutputs);
        openOutputs.Clear();

        for (int i = 0; i < deadEnds.Count; i++)
        {
            bool isLast = (i == deadEnds.Count - 1);

            if (!merchantRoomSpawned && (Random.value < merchantRoomChance || isLast))
            {
                SpawnMerchantRoom(deadEnds[i]);
                merchantRoomSpawned = true;
            }
            else
            {
                SpawnSpecialRoom(deadEndPrefab, deadEnds[i], "Dead End");
            }
        }

        Debug.Log("[LevelGenerator] Geração de Nível Completa!");
        yield return new WaitForSeconds(extraLoadDelay);

        // Spawn de itens
        ItemSpawner itemSpawner = FindFirstObjectByType<ItemSpawner>();
        if (itemSpawner != null) itemSpawner.SpawnItems();
        else Debug.LogWarning("[LevelGenerator] ItemSpawner não encontrado na cena!");

        if (GameManager.instance != null)
            GameManager.instance.OnLevelReady(playerSpawnPoint);
    }

    // =========================================================
    // SPAWN DE SALAS ESPECIAIS
    // =========================================================

    void SpawnSpecialRoom(GameObject prefab, ConnectionPoint targetOutput, string label)
    {
        if (prefab == null)
        {
            Debug.LogWarning($"[LevelGenerator] Prefab de '{label}' não definido no Inspector.");
            return;
        }

        GameObject room = Instantiate(prefab);
        ConnectionPoint entrada = GetInputPoint(room, targetOutput.connectionTag);

        if (entrada == null)
        {
            // Fallback: alinha pela raiz do prefab
            room.transform.position = targetOutput.transform.position;
            room.transform.rotation = Quaternion.LookRotation(-targetOutput.transform.forward);
            Debug.LogWarning($"[LevelGenerator] '{label}' não tem ConnectionPoint(Entrada). Usando fallback de posição.");
            return;
        }

        AlignRooms(targetOutput.transform, entrada.transform);
        targetOutput.isOccupied = true;
        entrada.isOccupied = true;
        Debug.Log($"[LevelGenerator] {label} criado.");
    }

    void SpawnMerchantRoom(ConnectionPoint targetOutput)
    {
        if (merchantRoomPrefab == null || merchantPrefab == null)
        {
            Debug.LogWarning("[LevelGenerator] merchantRoomPrefab ou merchantPrefab não definido.");
            return;
        }

        GameObject room = Instantiate(merchantRoomPrefab);
        ConnectionPoint entrada = GetInputPoint(room, targetOutput.connectionTag);

        if (entrada == null)
        {
            Destroy(room);
            Debug.LogError("[LevelGenerator] merchantRoomPrefab não tem ConnectionPoint(Entrada)!");
            return;
        }

        AlignRooms(targetOutput.transform, entrada.transform);
        targetOutput.isOccupied = true;
        entrada.isOccupied = true;

        // Spawn do NPC Mercador
        Transform spawnPoint = FindNamedChild(room.transform, "Merchant_SpawnPoint");
        if (spawnPoint != null)
            Instantiate(merchantPrefab, spawnPoint.position, spawnPoint.rotation);

        Debug.Log("[LevelGenerator] Sala do Mercador criada!");
    }

    // =========================================================
    // SELEÇÃO DE PREFAB
    // =========================================================

    /// <summary>
    /// Busca um prefab da mainRoomPrefabs que:
    /// 1. Tenha um ConnectionPoint(Entrada) com a tag compatível.
    /// 2. Não tenha atingido o roomLimitPerType.
    /// Retorna null se nenhum for encontrado.
    /// </summary>
    GameObject GetCompatibleRoomPrefab(string requiredTag)
    {
        List<GameObject> valid = new List<GameObject>();

        foreach (GameObject prefab in mainRoomPrefabs)
        {
            string prefabName = prefab.name;
            int count = roomCounts.ContainsKey(prefabName) ? roomCounts[prefabName] : 0;
            if (count >= roomLimitPerType) continue;

            // Verifica se o prefab tem pelo menos 1 ConnectionPoint(Entrada) com a tag certa
            ConnectionPoint[] cps = prefab.GetComponentsInChildren<ConnectionPoint>();
            bool hasCompatibleInput = cps.Any(cp =>
                cp.pointType == ConnectionPoint.PointType.Entrada &&
                cp.connectionTag == requiredTag &&
                !cp.isOccupied);

            if (hasCompatibleInput)
                valid.Add(prefab);
        }

        if (valid.Count == 0) return null;
        return valid[Random.Range(0, valid.Count)];
    }

    // =========================================================
    // REGISTRO DE SAÍDAS
    // =========================================================

    /// <summary>
    /// Coleta todos os ConnectionPoints(Saida) de uma sala instanciada e os
    /// adiciona à fila openOutputs.
    /// Se isStartRoom == true, pula a saída marcada como West (Nave/Crash Site).
    /// </summary>
    void RegisterOutputPoints(GameObject room, bool isStartRoom)
    {
        ConnectionPoint[] allCPs = room.GetComponentsInChildren<ConnectionPoint>();
        List<ConnectionPoint> outputs = allCPs
            .Where(cp => cp.pointType == ConnectionPoint.PointType.Saida && !cp.isOccupied)
            .ToList();

        if (isStartRoom && outputs.Count > 0)
        {
            // Na Safe Room, apenas UMA saída aleatória é aberta inicialmente
            // (a saída West fica reservada para a Nave/Crash Site — não deve gerar salas)
            List<ConnectionPoint> validStart = outputs
                .Where(cp => !cp.gameObject.name.Contains("West") && !cp.gameObject.name.Contains("Nave"))
                .ToList();

            if (validStart.Count > 0)
                openOutputs.Add(validStart[Random.Range(0, validStart.Count)]);
            else if (outputs.Count > 0)
                openOutputs.Add(outputs[Random.Range(0, outputs.Count)]);
        }
        else
        {
            openOutputs.AddRange(outputs);
        }
    }

    // =========================================================
    // ALINHAMENTO (sem mudanças — já funcionava bem)
    // =========================================================

    /// <summary>
    /// Alinha a sala que contém socketB de modo que socketB coincida com socketA,
    /// com os forwards opostos (as salas se "encaixam" pela boca dos sockets).
    /// </summary>
    void AlignRooms(Transform socketA, Transform socketB)
    {
        if (socketA == null || socketB == null)
        {
            Debug.LogError("[LevelGenerator] AlignRooms: um dos sockets é null!");
            return;
        }

        Transform roomB = socketB.root;
        Quaternion targetRotation = Quaternion.LookRotation(-socketA.forward, socketA.up);
        Quaternion correctionRotation = targetRotation * Quaternion.Inverse(socketB.rotation);
        roomB.rotation = correctionRotation * roomB.rotation;
        roomB.position += socketA.position - socketB.position;
    }

    // =========================================================
    // HELPERS DE BUSCA
    // =========================================================

    /// <summary>
    /// Retorna o melhor ConnectionPoint(Entrada) disponível para conectar com o socket de origem.
    ///
    /// Se o prefab tiver apenas 1 Entrada, retorna ela diretamente.
    /// Se tiver múltiplas Entradas (candidatas), escolhe a que exige MENOS rotação para se
    /// alinhar com o socket de origem — ou seja, a cujo forward é mais oposto ao forward do socket.
    ///
    /// Isso permite prefabs com 2+ posições de entrada possíveis, onde o gerador escolhe
    /// automaticamente a mais natural para cada conexão.
    /// </summary>
    ConnectionPoint GetInputPoint(GameObject room, string tag, Transform incomingSocket = null)
    {
        var candidates = room.GetComponentsInChildren<ConnectionPoint>()
            .Where(cp =>
                cp.pointType == ConnectionPoint.PointType.Entrada &&
                cp.connectionTag == tag &&
                !cp.isOccupied)
            .ToList();

        if (candidates.Count == 0) return null;
        if (candidates.Count == 1 || incomingSocket == null) return candidates[0];

        // Escolhe a Entrada cujo forward é mais oposto ao forward do socket de saída.
        // O AlignRooms vai rotacionar a sala para que os dois se oponham — quanto mais
        // alinhados já estiverem, menos rotação é necessária e mais natural fica o layout.
        ConnectionPoint best = null;
        float bestDot = float.MinValue;

        foreach (var cp in candidates)
        {
            // Queremos: cp.forward ≈ -incomingSocket.forward (opostos = encaixe perfeito)
            float dot = Vector3.Dot(cp.transform.forward, -incomingSocket.forward);
            if (dot > bestDot)
            {
                bestDot = dot;
                best = cp;
            }
        }

        return best;
    }

    /// <summary>Retorna o primeiro ConnectionPoint(Saida) não ocupado com a tag especificada.</summary>
    ConnectionPoint GetFirstOutputPoint(GameObject room, string tag)
    {
        return room.GetComponentsInChildren<ConnectionPoint>()
            .FirstOrDefault(cp =>
                cp.pointType == ConnectionPoint.PointType.Saida &&
                cp.connectionTag == tag &&
                !cp.isOccupied);
    }

    /// <summary>Encontra um filho pelo nome exato (para Player_StartPoint e Merchant_SpawnPoint).</summary>
    Transform FindNamedChild(Transform parent, string childName)
    {
        return parent.GetComponentsInChildren<Transform>()
            .FirstOrDefault(t => t.name == childName);
    }
}