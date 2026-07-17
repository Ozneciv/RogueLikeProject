using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Editor Tool: popula automaticamente as listas de inimigos de todos os
/// RoomController encontrados em prefabs do projeto.
///
/// USO:
///   Menu Unity → EPTA Tools → Populate Room Enemy Pools
///
/// CLASSIFICAÇÃO (baseada nos AIs implementados):
///   Mob Menor  (1 pt)  : Spider, SkullController
///   Atirador   (2 pts) : Goblin, CrystalWatcher
///   Tanque     (4 pts) : Golem, MagicStoneEnemy, Totem
///   Elite      (10 pts): Shard Swarm, CrystalTuner
///
/// Para alterar a classificação, edite os arrays abaixo e rode novamente.
/// </summary>
public class RoomControllerEnemyPopulator : EditorWindow
{
    // =========================================================
    // CLASSIFICAÇÃO DOS INIMIGOS
    // Edite os caminhos aqui se os prefabs mudarem de pasta.
    // =========================================================

    private static readonly string[] MOB_MENOR_PATHS = new[]
    {
        "Assets/_Project/Enemies/Spider/Spider.prefab",
    };

    private static readonly string[] ATIRADOR_PATHS = new[]
    {
        "Assets/_Project/Enemies/Goblin/Goblin.prefab",
        "Assets/_Project/Enemies/CrystalWatcher/CrystalWatcher.prefab",
    };

    private static readonly string[] TANQUE_PATHS = new[]
    {
        "Assets/_Project/Enemies/Golem/Golem.prefab",
        "Assets/_Project/Enemies/MagicStone/MagicStoneEnemy.prefab",
        "Assets/_Project/Enemies/Totem/Totem_3_FBX_low.prefab",
    };

    private static readonly string[] ELITE_PATHS = new[]
    {
        "Assets/_Project/Enemies/SharpBlur/Sh.prefab",
        "Assets/_Project/Enemies/CrystalTunner/CrystalTuner_Root.prefab",
        "Assets/_Project/Enemies/CrystalDragon/crystal_dragon.prefab",
    };

    // =========================================================
    // MENU ENTRY
    // =========================================================

    [MenuItem("EPTA Tools/Populate Room Enemy Pools")]
    public static void PopulateAllRoomControllers()
    {
        // Carrega os prefabs de cada classe
        List<GameObject> mobMenor  = LoadPrefabs(MOB_MENOR_PATHS,  "Mob Menor");
        List<GameObject> atirador  = LoadPrefabs(ATIRADOR_PATHS,   "Atirador");
        List<GameObject> tanque    = LoadPrefabs(TANQUE_PATHS,      "Tanque");
        List<GameObject> elite     = LoadPrefabs(ELITE_PATHS,       "Elite");

        if (mobMenor.Count == 0 && atirador.Count == 0 && tanque.Count == 0 && elite.Count == 0)
        {
            EditorUtility.DisplayDialog(
                "Erro",
                "Nenhum prefab de inimigo foi encontrado nos caminhos configurados.\n\n" +
                "Verifique os caminhos em RoomControllerEnemyPopulator.cs e tente novamente.",
                "OK"
            );
            return;
        }

        // Encontra todos os prefabs que contêm RoomController
        string[] allPrefabGUIDs = AssetDatabase.FindAssets("t:Prefab");
        int roomsUpdated = 0;
        int roomsSkipped = 0;

        foreach (string guid in allPrefabGUIDs)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;

            // Busca RoomController em qualquer nível da hierarquia do prefab
            RoomController[] controllers = prefab.GetComponentsInChildren<RoomController>(includeInactive: true);
            if (controllers.Length == 0) continue;

            bool modified = false;
            foreach (RoomController rc in controllers)
            {
                // Pula Safe Rooms — elas não têm combate
                if (rc.isSafeRoom)
                {
                    roomsSkipped++;
                    continue;
                }

                using (var editScope = new PrefabUtility.EditPrefabContentsScope(path))
                {
                    // Dentro do scope, pegamos a instância editável
                    RoomController[] editControllers = editScope.prefabContentsRoot
                        .GetComponentsInChildren<RoomController>(includeInactive: true);

                    foreach (RoomController editRc in editControllers)
                    {
                        if (editRc.isSafeRoom) continue;

                        editRc.mobMenorPrefabs  = new List<GameObject>(mobMenor);
                        editRc.atiradorPrefabs  = new List<GameObject>(atirador);
                        editRc.tanquePrefabs    = new List<GameObject>(tanque);
                        editRc.elitePrefabs     = new List<GameObject>(elite);
                    }

                    modified = true;
                }

                // Só entra no scope uma vez por prefab (break após modificar)
                break;
            }

            if (modified)
            {
                roomsUpdated++;
                Debug.Log($"[EnemyPopulator] ✅ Prefab atualizado: {path}");
            }
        }

        string summary =
            $"✅ População concluída!\n\n" +
            $"Prefabs de sala atualizados: {roomsUpdated}\n" +
            $"Safe Rooms ignoradas: {roomsSkipped}\n\n" +
            $"Mob Menor : {mobMenor.Count} prefabs\n" +
            $"Atirador  : {atirador.Count} prefabs\n" +
            $"Tanque    : {tanque.Count} prefabs\n" +
            $"Elite     : {elite.Count} prefabs";

        Debug.Log($"[EnemyPopulator] {summary}");
        EditorUtility.DisplayDialog("EPTA Tools — Enemy Pools", summary, "OK");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    // =========================================================
    // HELPER
    // =========================================================

    private static List<GameObject> LoadPrefabs(string[] paths, string className)
    {
        var result = new List<GameObject>();
        foreach (string path in paths)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
            {
                result.Add(prefab);
                Debug.Log($"[EnemyPopulator] {className}: carregado '{prefab.name}' de '{path}'");
            }
            else
            {
                Debug.LogWarning($"[EnemyPopulator] ⚠️ {className}: prefab não encontrado em '{path}'. " +
                                 $"Verifique o caminho em RoomControllerEnemyPopulator.cs.");
            }
        }
        return result;
    }
}
