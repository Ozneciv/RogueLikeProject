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

    [Header("Boss Fight")]
    [Tooltip("Prefab da sala de Boss Fight (Round 4). Deve ter 1 ConnectionPoint(Entrada).\nQuando o round boss for detectado, apenas Safe Room + Boss Room são gerados.")]
    public GameObject bossRoomPrefab;

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

    [Header("Alinhamento")]
    [Tooltip("Posição Y do chão de todas as salas. Ajuste se as salas flutuarem ou afundarem.")]
    public float roomFloorY = 0f;

    [Header("Anti-Sobreposição")]
    [Tooltip("Margem (metros) aplicada ao bounds check. Aumente se salas legítimas forem descartadas (falso positivo). Diminua se ainda houver colisões.")]
    public float overlapTolerance = 0.5f;

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

    /// <summary>Bounds de todas as salas já confirmadas no mapa — (referência + bounds) para exclusão de vizinhos diretos.</summary>
    private List<(GameObject room, Bounds bounds)> placedRoomBounds = new List<(GameObject, Bounds)>();

    // =========================================================
    // API PÚBLICA
    // =========================================================

    /// <summary>Chamado pelo GameManager para iniciar a geração.</summary>
    public void GenerateLevel()
    {
        roomCounts.Clear();
        openOutputs.Clear();
        placedRoomBounds.Clear();
        merchantRoomSpawned = false;
        exitRoomSpawned = false;
        roomSequenceCounter = 0;

        // --- Progressão de Rounds: ajusta maxMainRooms dinamicamente ---
        bool isBossRound = false;
        if (RunManager.instance != null)
        {
            isBossRound = RunManager.instance.isBossRound;
            if (!isBossRound)
            {
                maxMainRooms = RunManager.instance.GetMaxRoomsForCurrentLevel();
                Debug.Log($"[LevelGenerator] 🗺️ Round {RunManager.instance.currentLevel}/{RunManager.instance.totalLevels} — gerando {maxMainRooms} salas principais.");
            }
            else
            {
                Debug.Log($"[LevelGenerator] 🏆 Round {RunManager.instance.currentLevel}/{RunManager.instance.totalLevels} — BOSS FIGHT!");
            }
        }

        // --- Instancia a Safe Room na origem ---
        // Usa a rotação salva no prefab (não força identity) para respeitar correções de eixo do FBX
        GameObject startRoom = Instantiate(startRoomPrefab, Vector3.zero, startRoomPrefab.transform.rotation);

        playerSpawnPoint = FindNamedChild(startRoom.transform, "Player_StartPoint");
        if (playerSpawnPoint == null)
        {
            Debug.LogError("[LevelGenerator] Safe Room não tem um filho chamado 'Player_StartPoint'!");
            return;
        }

        // Registra bounds da Safe Room como âncora para o sistema anti-sobreposição
        RegisterRoomBounds(startRoom);

        // Registra as saídas da Safe Room (pula a Entrada, que fica no West/Nave)
        RegisterOutputPoints(startRoom, isStartRoom: true);

        if (isBossRound)
            StartCoroutine(BossGenerationCoroutine());
        else
            StartCoroutine(GenerationLoop(roomCount: 1));
    }

    // =========================================================
    // BOSS GENERATION
    // =========================================================

    /// <summary>
    /// Corrotina usada no round de Boss Fight.
    /// Pula a geração procedural normal e conecta o bossRoomPrefab
    /// diretamente à primeira saída disponível da Safe Room.
    /// </summary>
    IEnumerator BossGenerationCoroutine()
    {
        if (bossRoomPrefab == null)
        {
            Debug.LogError("[LevelGenerator] ❌ bossRoomPrefab não definido no Inspector! " +
                           "Arraste o prefab de Boss Fight no campo 'Boss Room Prefab' do LevelGenerator.");
            yield break;
        }

        if (openOutputs.Count == 0)
        {
            Debug.LogError("[LevelGenerator] ❌ Safe Room não tem saídas disponíveis para conectar o Boss!");
            yield break;
        }

        // Pega a primeira saída da Safe Room
        ConnectionPoint bossOutput = openOutputs[0];
        openOutputs.RemoveAt(0);
        bossOutput.isOccupied = true;

        GameObject bossRoom = Instantiate(bossRoomPrefab);
        ConnectionPoint bossEntrada = GetInputPoint(bossRoom, bossOutput.connectionTag, bossOutput.transform);

        if (bossEntrada != null)
        {
            AlignRooms(bossOutput.transform, bossEntrada.transform);
            bossEntrada.isOccupied = true;
            Debug.Log("[LevelGenerator] ✅ Sala de Boss conectada à Safe Room!");
        }
        else
        {
            // Fallback: posiciona na frente da saída sem alinhar sockets
            bossRoom.transform.position = bossOutput.transform.position + bossOutput.transform.forward * 20f;
            Debug.LogWarning("[LevelGenerator] ⚠️ Boss Room não tem ConnectionPoint(Entrada) compatível. Usando posição fallback.");
        }

        Debug.Log("[LevelGenerator] 🏆 BOSS FIGHT gerado!");
        yield return new WaitForSeconds(extraLoadDelay);

        ItemSpawner itemSpawner = FindFirstObjectByType<ItemSpawner>();
        if (itemSpawner != null) itemSpawner.SpawnItems();
        else Debug.LogWarning("[LevelGenerator] ItemSpawner não encontrado na cena!");

        if (GameManager.instance != null)
            GameManager.instance.OnLevelReady(playerSpawnPoint);
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
                Debug.LogError("[LevelGenerator] transitionRoomPrefab não definido no Inspector!");
                continue;
            }

            // O corredor de transição é agnóstico de tag — busca pelo PointType apenas.
            // Isso evita falhas quando as connectionTags do prefab não batem exatamente.
            GameObject transitionRoom = Instantiate(transitionRoomPrefab);
            ConnectionPoint[] transCPs = transitionRoom.GetComponentsInChildren<ConnectionPoint>();

            ConnectionPoint transEntrada = null;
            ConnectionPoint transSaida   = null;

            foreach (var cp in transCPs)
            {
                if (transEntrada == null && cp.pointType == ConnectionPoint.PointType.Entrada && !cp.isOccupied)
                    transEntrada = cp;
                if (transSaida == null && cp.pointType == ConnectionPoint.PointType.Saida && !cp.isOccupied)
                    transSaida = cp;
                if (transEntrada != null && transSaida != null) break;
            }

            if (transEntrada == null)
            {
                Debug.LogError("[LevelGenerator] ❌ transitionRoomPrefab não tem nenhum ConnectionPoint do tipo Entrada. " +
                               "Adicione um CP filho com PointType = Entrada no prefab.");
                Destroy(transitionRoom);
                continue;
            }

            if (transSaida == null)
            {
                Debug.LogError("[LevelGenerator] ❌ transitionRoomPrefab não tem nenhum ConnectionPoint do tipo Saida. " +
                               "Adicione um CP filho com PointType = Saida no prefab.");
                Destroy(transitionRoom);
                continue;
            }

            // Propaga a tag da saída atual para a saída da transição,
            // garantindo que a busca por mainRoomPrefab funcione corretamente.
            transSaida.connectionTag = currentOutput.connectionTag;

            AlignRooms(currentOutput.transform, transEntrada.transform);
            transEntrada.isOccupied = true;
            Debug.Log($"[LevelGenerator] ✅ Transição conectada à saída '{currentOutput.gameObject.name}' (tag='{currentOutput.connectionTag}').");

            // --- Sala Principal ---
            GameObject roomPrefab = GetCompatibleRoomPrefab(transSaida.connectionTag);

            if (roomPrefab != null)
            {
                GameObject newRoom = Instantiate(roomPrefab);
                ConnectionPoint roomEntrada = GetInputPoint(newRoom, transSaida.connectionTag, transSaida.transform);

                if (roomEntrada == null)
                {
                    Debug.LogError($"[LevelGenerator] ❌ Prefab '{roomPrefab.name}' não tem ConnectionPoint(Entrada) com tag='{transSaida.connectionTag}'. " +
                                   $"Verifique o prefab. Transição descartada.");
                    Destroy(newRoom);
                    Destroy(transitionRoom);
                    continue;
                }

                AlignRooms(transSaida.transform, roomEntrada.transform);

                // --- Verificação de Sobreposição ---
                // Checa DEPOIS de posicionar (bounds dependem da posição final).
                // A sala de origem de currentOutput e o próprio corredor são EXCLUÍDOS da checagem:
                // salas vizinhas sempre se tocam por design — isso não é colisão real.
                GameObject sourceRoom = currentOutput.transform.root.gameObject;
                if (HasOverlapWithExistingRooms(transitionRoom, excludeRoom: sourceRoom) ||
                    HasOverlapWithExistingRooms(newRoom, excludeRoom: transitionRoom))
                {
                    Debug.LogWarning($"[LevelGenerator] ⚠️ Sala '{roomPrefab.name}' causaria sobreposição com sala existente. Par descartado.");
                    Destroy(newRoom);
                    Destroy(transitionRoom);
                    continue;
                }

                transSaida.isOccupied = true;
                roomEntrada.isOccupied = true;

                // Registra bounds de ambas no sistema anti-sobreposição
                RegisterRoomBounds(transitionRoom);
                RegisterRoomBounds(newRoom);

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
                Debug.Log($"[LevelGenerator] ✅ Sala '{prefabName}' adicionada (#{roomCount}). Saídas abertas: {openOutputs.Count}");
            }
            else
            {
                // Sem sala compatível — mantém a transição no mapa e registra a saída dela como beco
                openOutputs.Add(transSaida);
                Debug.LogWarning($"[LevelGenerator] ⚠️ Nenhum prefab de mainRoom compatível com tag='{transSaida.connectionTag}'. " +
                                 $"A transição ficou no mapa e sua saída virou beco.");
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
    // ALINHAMENTO
    // =========================================================

    /// <summary>
    /// Alinha a sala que contém socketB de modo que socketB coincida com socketA
    /// no plano XZ, com os forwards opostos (as salas se "encaixam" pela boca dos sockets).
    ///
    /// O eixo Y é IGNORADO no delta de posição: todas as salas ficam ancoradas em
    /// roomFloorY independentemente da altura local dos sockets no prefab.
    /// Isso evita que diferenças de pivot entre modelos façam uma sala flutuar ou afundar.
    /// </summary>
    void AlignRooms(Transform socketA, Transform socketB)
    {
        if (socketA == null || socketB == null)
        {
            Debug.LogError("[LevelGenerator] AlignRooms: um dos sockets é null!");
            return;
        }

        Transform roomA = socketA.root;
        Transform roomB = socketB.root;

        // 1. Rotação: faz o forward de socketB apontar contra o forward de socketA
        Quaternion targetRotation = Quaternion.LookRotation(-socketA.forward, Vector3.up);
        Quaternion correctionRotation = targetRotation * Quaternion.Inverse(socketB.rotation);
        roomB.rotation = correctionRotation * roomB.rotation;

        // 2. Posição XZ: alinha os sockets horizontalmente
        Vector3 delta = socketA.position - socketB.position;
        roomB.position += new Vector3(delta.x, 0f, delta.z);

        // 3. Posição Y: iguala diretamente o Y de socketB ao Y de socketA.
        //
        //    ABORDAGEM ANTERIOR (bugada): definia roomB.position.y = socketA.y
        //    Isso ignorava o offset local do socket dentro do prefab, causando
        //    acúmulo de erro a cada conexão (cada sala afundava um pouco mais).
        //
        //    ABORDAGEM CORRETA: como todos os ConnectionPoints são posicionados
        //    rentes ao chão nos prefabs, igualar o Y dos sockets é equivalente
        //    a igualar os pisos — independente de onde o pivot/root do prefab esteja.
        float deltaY = socketA.position.y - socketB.position.y;
        roomB.position += new Vector3(0f, deltaY, 0f);
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

    // =========================================================
    // ANTI-SOBREPOSIÇÃO
    // =========================================================

    /// <summary>
    /// Calcula o Bounds combinado de todos os Renderers da sala.
    /// Retorna Bounds zerado se não houver renderers (sala invisível/vazia).
    /// </summary>
    Bounds GetRoomBounds(GameObject room)
    {
        Renderer[] renderers = room.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return new Bounds();

        Bounds combined = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            combined.Encapsulate(renderers[i].bounds);
        return combined;
    }

    /// <summary>Registra os bounds da sala como área ocupada para checagens futuras.</summary>
    void RegisterRoomBounds(GameObject room)
    {
        Bounds b = GetRoomBounds(room);
        // Só registra se tem volume real (evita entrar com bounds nulo)
        if (b.size.sqrMagnitude > 0f)
            placedRoomBounds.Add((room, b));
    }

    /// <summary>
    /// Retorna true se os bounds da sala se sobrepõem com alguma sala já confirmada.
    /// O overlapTolerance encolhe o bounds antes de testar, permitindo que paredes
    /// encostem sem disparar um falso positivo.
    ///
    /// excludeRoom: ignora este GameObject na checagem (usado para não rejeitar
    /// salas que encostam em seu vizinho direto por design).
    /// </summary>
    bool HasOverlapWithExistingRooms(GameObject room, GameObject excludeRoom = null)
    {
        Bounds b = GetRoomBounds(room);
        if (b.size.sqrMagnitude == 0f) return false;

        // Encolhe para aceitar paredes tocando, mas rejeitar penetração real
        b.Expand(-overlapTolerance);

        foreach (var entry in placedRoomBounds)
        {
            // Pula o vizinho direto — ele encosta por design, não é colisão real
            if (excludeRoom != null && entry.room == excludeRoom) continue;
            if (b.Intersects(entry.bounds)) return true;
        }
        return false;
    }
}