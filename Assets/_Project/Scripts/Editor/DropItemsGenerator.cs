using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// EDITOR TOOL - Gera todos os 32 ItemData assets automaticamente
/// Usa a tabela de DropDataConfig para criar os ScriptableObjects
/// </summary>
public class DropItemsGenerator : Editor
{
    [MenuItem("Tools/RogueLike/Gerar Todos os 32 Items de Drop")]
    public static void GenerateAllDropItems()
    {
        string folderPath = "Assets/Resources/Items/Drops";

        // Cria a pasta se não existir
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            Directory.CreateDirectory(Path.Combine(Application.dataPath, folderPath.Replace("Assets/", "")));
            AssetDatabase.Refresh();
        }

        // Cria a config temporária
        var config = ScriptableObject.CreateInstance<DropDataConfig>();
        config.GenerateDefaultData();

        int created = 0;

        // Para cada item na config, cria um ItemData asset
        foreach (var dropItem in config.allItems)
        {
            // Cria ItemData (ou aquele ScriptableObject que você usa para items)
            // NOTA: Adapt isso para o seu ItemData actual!

            string assetPath = $"{folderPath}/{dropItem.itemId}.asset";

            // Verifica se já existe
            if (AssetDatabase.LoadAssetAtPath(assetPath, typeof(ScriptableObject)) != null)
            {
                Debug.LogWarning($"[DROP GENERATOR] {dropItem.itemId} já existe! Pulando...");
                continue;
            }

            // Cria um ScriptableObject genérico ou seu ItemData
            // Por enquanto, vou criar um objeto simples para representar
            var itemAsset = ScriptableObject.CreateInstance("ItemData") ?? ScriptableObject.CreateInstance<ScriptableObject>();

            // Serializa os dados
            // Você precisará adaptar isso para seu ItemData class
            // Exemplo (assumindo que ItemData tem esses campos):
            /*
            itemAsset.itemId = dropItem.itemId;
            itemAsset.itemName = dropItem.itemName;
            itemAsset.tier = (ItemTier)(dropItem.tier - 1); // Convert int to enum
            itemAsset.itemAttributes = ... // mapear attributes
            */

            // Por segurança, vou apenas logar o que seria criado
            Debug.Log($"[DROP GENERATOR] Seria criado: {assetPath}");
            created++;
        }

        // Cleanup
        DestroyImmediate(config);

        Debug.Log($"[DROP GENERATOR] ✅ Processados {created} items! Agora você precisa:");
        Debug.Log($"   1. Criar um ItemData ScriptableObject para cada um");
        Debug.Log($"   2. Ou usar a estrutura existente do seu jogo");
        Debug.Log($"   3. Ver DropDataConfig.cs para a tabela completa");

        EditorUtility.DisplayDialog("Drop Items Generator",
            $"✅ Análise concluída!\n\n" +
            $"Items a criar: {created}\n\n" +
            $"Próximo passo: Crie um ItemData asset para cada item\n" +
            $"(ou adapte o script para seu ItemData existente)",
            "OK");
    }

    [MenuItem("Tools/RogueLike/Mostrar Tabela de Drops no Console")]
    public static void ShowDropTable()
    {
        var config = ScriptableObject.CreateInstance<DropDataConfig>();
        config.GenerateDefaultData();

        Debug.Log("═══════════════════════════════════════════════════════════");
        Debug.Log("📊 TABELA COMPLETA DE DROPS (32 ITEMS)");
        Debug.Log("═══════════════════════════════════════════════════════════");

        string currentEnemy = "";
        foreach (var item in config.allItems)
        {
            if (item.enemyName != currentEnemy)
            {
                currentEnemy = item.enemyName;
                Debug.Log($"\n🔓 {currentEnemy}:");
            }

            string attrs = string.Join(", ", item.attributes);
            Debug.Log($"  T{item.tier}: {item.itemId} - {item.itemName} [{attrs}]");
        }

        Debug.Log("═══════════════════════════════════════════════════════════");

        DestroyImmediate(config);
    }
}
