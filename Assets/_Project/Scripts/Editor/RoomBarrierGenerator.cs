using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Editor Tool: gera automaticamente GameObjects de barreira (BoxCollider sólido)
/// em cada ConnectionPoint dos prefabs que têm RoomController.
///
/// USO:
///   Menu Unity → EPTA Tools → Generate Room Barriers
///
/// O QUE FAZ:
///   1. Encontra todos os prefabs com RoomController (salas de combate).
///   2. Para cada ConnectionPoint filho, cria um filho "Barrier_[nome]" com:
///        - BoxCollider sólido (isTrigger = false) para bloquear o player.
///        - MeshRenderer de um quad semi-transparente (vermelho durante combate,
///          invisível quando desbloqueado — via RoomController.doors[]).
///   3. Adiciona todos os barriers ao array doors[] do RoomController.
///   4. Não recria barriers que já existam (idempotente).
///
/// DIMENSÕES DA BARREIRA:
///   Largura (X): barrierWidth  — cobre o vão da passagem
///   Altura  (Y): barrierHeight — alto o suficiente para bloquear
///   Profund.(Z): 0.4 unidades  — parede fina
///
/// IMPORTANTE:
///   As barreiras começam INATIVAS (SetActive(false)) via RoomController.UnlockDoors().
///   Elas só se tornam ativas quando o player entra na sala (LockDoors).
/// </summary>
public class RoomBarrierGenerator : EditorWindow
{
    // =========================================================
    // CONFIGURAÇÕES
    // =========================================================

    private static float barrierWidth  = 12f;  // largura da barreira (eixo X local)
    private static float barrierHeight = 5f;   // altura da barreira (eixo Y)
    private static string barrierTag   = "Untagged";
    private static string barrierLayer = "Default";

    // =========================================================
    // MENU
    // =========================================================

    [MenuItem("EPTA Tools/Generate Room Barriers")]
    public static void GenerateBarriers()
    {
        int prefabsProcessed = 0;
        int barriersCreated = 0;
        int barriersSkipped = 0;

        string[] allPrefabGUIDs = AssetDatabase.FindAssets("t:Prefab");

        foreach (string guid in allPrefabGUIDs)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefabAsset == null) continue;

            // Só processa prefabs com RoomController não-safeRoom
            RoomController rc = prefabAsset.GetComponentInChildren<RoomController>(true);
            if (rc == null || rc.isSafeRoom) continue;

            using (var scope = new PrefabUtility.EditPrefabContentsScope(path))
            {
                GameObject root = scope.prefabContentsRoot;
                RoomController editRC = root.GetComponentInChildren<RoomController>(true);
                ConnectionPoint[] cps = root.GetComponentsInChildren<ConnectionPoint>(true);

                if (cps.Length == 0) continue;

                List<GameObject> doorList = new List<GameObject>();
                if (editRC.doors != null)
                    doorList.AddRange(editRC.doors);

                foreach (ConnectionPoint cp in cps)
                {
                    string barrierName = $"Barrier_{cp.gameObject.name}";

                    // Remove barreira anterior se existir (garante que recriar sempre aplica as novas configs)
                    Transform existing = cp.transform.Find(barrierName);
                    if (existing != null)
                    {
                        if (doorList.Contains(existing.gameObject))
                            doorList.Remove(existing.gameObject);
                        DestroyImmediate(existing.gameObject);
                        barriersSkipped++; // conta como "substituída"
                    }

                    // Cria o GameObject da barreira como filho do ConnectionPoint
                    GameObject barrier = new GameObject(barrierName);
                    barrier.transform.SetParent(cp.transform, false);

                    // Ancora a barreira ao CHÃO (Y=0 mundo) independente da altura do CP.
                    // Sem isso, se o ConnectionPoint estiver elevado, a barreira flutua no ar.
                    barrier.transform.localPosition = Vector3.zero;
                    barrier.transform.localRotation = Quaternion.identity;

                    // Converte Y=0 mundo para local do CP para posicionar no chão
                    float worldFloorY = 0f; // chão sempre em Y=0
                    float cpWorldY = cp.transform.position.y;
                    float localYOffset = worldFloorY - cpWorldY; // offset para descer até o chão

                    // BoxCollider sólido (isTrigger = false) ancorado ao chão
                    BoxCollider col = barrier.AddComponent<BoxCollider>();
                    col.isTrigger = false;
                    col.size = new Vector3(barrierWidth, barrierHeight, 0.4f);
                    // center.y = metade da altura + offset para que a base fique no chão
                    col.center = new Vector3(0f, (barrierHeight * 0.5f) + localYOffset, 0f);

                    // Começa INATIVO — RoomController.UnlockDoors() já faz SetActive(false)
                    barrier.SetActive(false);

                    doorList.Add(barrier);
                    barriersCreated++;

                    Debug.Log($"[BarrierGen] ✅ Barreira criada: '{barrierName}' em '{path}'");
                }

                // Atualiza o array doors[] do RoomController
                editRC.doors = doorList.ToArray();
                prefabsProcessed++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        string summary =
            $"✅ Barreiras geradas!\n\n" +
            $"Prefabs processados : {prefabsProcessed}\n" +
            $"Barreiras criadas   : {barriersCreated}\n" +
            $"Já existiam (puladas): {barriersSkipped}";

        Debug.Log($"[BarrierGen] {summary}");
        EditorUtility.DisplayDialog("EPTA Tools — Room Barriers", summary, "OK");
    }

    // =========================================================
    // MENU DE CONFIGURAÇÃO
    // =========================================================

    [MenuItem("EPTA Tools/Configure Barrier Dimensions")]
    public static void OpenSettings()
    {
        GetWindow<RoomBarrierGenerator>("Barrier Settings");
    }

    private void OnGUI()
    {
        GUILayout.Label("Dimensões das Barreiras", EditorStyles.boldLabel);
        barrierWidth  = EditorGUILayout.FloatField("Largura (X)", barrierWidth);
        barrierHeight = EditorGUILayout.FloatField("Altura (Y)",  barrierHeight);

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "Largura: deve cobrir o vão da passagem entre salas.\n" +
            "Altura: deve ser alta o suficiente para o player não pular por cima.\n\n" +
            "Ajuste e clique em 'Gerar Barreiras'.",
            MessageType.Info);

        EditorGUILayout.Space();
        if (GUILayout.Button("Gerar Barreiras", GUILayout.Height(40)))
            GenerateBarriers();
    }
}
