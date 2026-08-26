using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor utility that adds the "CFXR3 LightGlow A (Loop)" effect as a child
/// of every prefab inside Assets/_Project/Items_and_Crafting/Drops.
/// Accessible via the top menu: Tools > Add Glow To Drops.
/// </summary>
public static class AddGlowToDrops
{
    private const string DropsFolderPath = "Assets/_Project/Items_and_Crafting/Drops";
    private const string EffectPrefabPath = "Assets/_Project/VFX/Texture Packs/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Light/CFXR3 LightGlow A (Loop).prefab";

    [MenuItem("Tools/Add Glow To Drops")]
    public static void Execute()
    {
        // Load the effect prefab
        GameObject effectPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EffectPrefabPath);
        if (effectPrefab == null)
        {
            Debug.LogError($"[AddGlowToDrops] Prefab do efeito não encontrado em: {EffectPrefabPath}");
            return;
        }

        // Find all prefab assets in the Drops folder
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { DropsFolderPath });

        if (guids.Length == 0)
        {
            Debug.LogWarning($"[AddGlowToDrops] Nenhum prefab encontrado em: {DropsFolderPath}");
            return;
        }

        int addedCount = 0;
        int skippedCount = 0;

        foreach (string guid in guids)
        {
            string prefabPath = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);

            if (prefabRoot == null)
            {
                Debug.LogWarning($"[AddGlowToDrops] Falha ao carregar prefab: {prefabPath}");
                continue;
            }

            // Check if the effect already exists as a child to avoid duplicates
            bool alreadyHasGlow = false;
            foreach (Transform child in prefabRoot.transform)
            {
                if (child.name.Contains("CFXR3 LightGlow A"))
                {
                    alreadyHasGlow = true;
                    break;
                }
            }

            if (alreadyHasGlow)
            {
                Debug.Log($"[AddGlowToDrops] Efeito já presente, pulando: {prefabPath}");
                PrefabUtility.UnloadPrefabContents(prefabRoot);
                skippedCount++;
                continue;
            }

            // Instantiate the effect and parent it
            GameObject effectInstance = (GameObject)PrefabUtility.InstantiatePrefab(effectPrefab, prefabRoot.transform);
            effectInstance.transform.localPosition = Vector3.zero;

            // Save changes back to the prefab asset
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
            PrefabUtility.UnloadPrefabContents(prefabRoot);

            addedCount++;
            Debug.Log($"[AddGlowToDrops] Efeito adicionado com sucesso em: {prefabPath}");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[AddGlowToDrops] Concluído! {addedCount} prefabs modificados, {skippedCount} já possuíam o efeito.");
    }
}
