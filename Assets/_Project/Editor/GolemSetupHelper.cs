using UnityEditor;
using UnityEngine;

/// <summary>
/// Helper editor script to automatically attach the GolemProceduralAnimation component to the Golem prefabs.
/// This runs automatically on compilation and can be safely deleted afterwards.
/// </summary>
[InitializeOnLoad]
public class GolemSetupHelper
{
    static GolemSetupHelper()
    {
        EditorApplication.delayCall += RunSetup;
    }

    private static void RunSetup()
    {
        string[] prefabPaths = new string[]
        {
            "Assets/_Project/Enemies/Golem/Golem.prefab",
            "Assets/_Project/Enemies Shortcut/Enemies/Golem.prefab"
        };

        foreach (string path in prefabPaths)
        {
            GameObject golemPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (golemPrefab != null)
            {
                // Open prefab editing session
                GameObject prefabRoot = PrefabUtility.LoadPrefabContents(path);
                
                if (prefabRoot.GetComponent<GolemProceduralAnimation>() == null)
                {
                    prefabRoot.AddComponent<GolemProceduralAnimation>();
                    PrefabUtility.SaveAsPrefabAsset(prefabRoot, path);
                    Debug.Log("[GOLEM_SETUP] Componente GolemProceduralAnimation adicionado e salvo com sucesso em: " + path);
                }
                else
                {
                    Debug.Log("[GOLEM_SETUP] GolemProceduralAnimation já está presente no prefab: " + path);
                }
                
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
            else
            {
                Debug.LogWarning("[GOLEM_SETUP] Prefab não encontrado no caminho: " + path);
            }
        }
    }
}
