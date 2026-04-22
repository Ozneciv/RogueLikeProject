using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Helper para exportar a tabela de drops em diferentes formatos
/// Para fácil preenchimento manual ou integração
/// </summary>
public class DropDataExporter : Editor
{
    [MenuItem("Tools/RogueLike/Exportar Drops em Formato CSV")]
    public static void ExportAsCSV()
    {
        var config = ScriptableObject.CreateInstance<DropDataConfig>();
        config.GenerateDefaultData();

        var csv = new System.Text.StringBuilder();
        csv.AppendLine("Item ID,Item Name,Enemy,Tier,Attributes,Essence Cost");

        foreach (var item in config.allItems)
        {
            string attrs = string.Join("|", item.attributes);
            csv.AppendLine($"\"{item.itemId}\",\"{item.itemName}\",\"{item.enemyName}\",{item.tier},\"{attrs}\",{item.essenceCost}");
        }

        string path = "Assets/DROPS_DATA.csv";
        File.WriteAllText(path, csv.ToString());
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("✅ Exportado!",
            $"Tabela exportada para:\n{path}\n\nAbra em Excel 📊",
            "OK");

        Debug.Log($"[EXPORT] CSV salvo em {path}");

        DestroyImmediate(config);
    }

    [MenuItem("Tools/RogueLike/Gerar Prefab Template de Item")]
    public static void GenerateItemTemplate()
    {
        // Cria um GameObject template
        GameObject template = new GameObject("ItemTemplate_T1");

        // Adiciona o componente CharacteristicItemPickup
        var pickup = template.AddComponent<CharacteristicItemPickup>();
        pickup.itemId = "template_item_t1";
        pickup.itemName = "Item Template (T1)";
        pickup.itemDescription = "Descreva o efeito aqui";
        pickup.rotateSpeed = 45f;
        pickup.bobSpeed = 2f;
        pickup.bobHeight = 0.2f;
        pickup.pickupDelay = 0.5f;
        pickup.lifetime = 60f;

        // Adiciona SphereCollider (trigger)
        var collider = template.AddComponent<SphereCollider>();
        collider.radius = 0.5f;
        collider.isTrigger = true;

        // Adiciona Rigidbody
        var rb = template.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = true;

        // Adiciona visual (cubo simples)
        var visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visual.transform.SetParent(template.transform);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);

        // Remove o collider do cubo
        DestroyImmediate(visual.GetComponent<Collider>());
        DestroyImmediate(visual.GetComponent<Rigidbody>());

        // Salva como prefab
        string prefabPath = "Assets/Prefabs/Items/ItemTemplate_T1.prefab";
        Directory.CreateDirectory(Path.GetDirectoryName(prefabPath));

        PrefabUtility.SaveAsPrefabAsset(template, prefabPath);
        DestroyImmediate(template);

        EditorUtility.DisplayDialog("✅ Template Criado!",
            $"Prefab salvo em:\n{prefabPath}\n\n" +
            $"Duplique ele 31 vezes e mude:\n" +
            $"- Item ID\n" +
            $"- Item Name\n" +
            $"- Description",
            "OK");

        Debug.Log($"[TEMPLATE] Salvo em {prefabPath}");
    }

    [MenuItem("Tools/RogueLike/Listar Todos os Items (Copiar)")]
    public static void ListAllItemsForCopy()
    {
        var config = ScriptableObject.CreateInstance<DropDataConfig>();
        config.GenerateDefaultData();

        var txt = new System.Text.StringBuilder();
        txt.AppendLine("LISTA COMPLETA DE 32 ITEMS - COPIE E COLE NO SEU EDITOR:\n");

        string currentEnemy = "";
        foreach (var item in config.allItems)
        {
            if (item.enemyName != currentEnemy)
            {
                currentEnemy = item.enemyName;
                txt.AppendLine($"\n═══ {currentEnemy.ToUpper()} ═══");
            }

            txt.AppendLine($"ID: {item.itemId}");
            txt.AppendLine($"   Name: {item.itemName}");
            txt.AppendLine($"   Tier: {item.tier}");
            txt.AppendLine($"   Attrs: {string.Join(", ", item.attributes)}");
            txt.AppendLine();
        }

        string path = "Assets/DROPS_LIST.txt";
        File.WriteAllText(path, txt.ToString());
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("✅ Lista Gerada!",
            $"Arquivo criado:\n{path}\n\n" +
            $"Abra e copie para referência rápida! 📋",
            "OK");

        Debug.Log($"[LIST] Salvo em {path}");
        DestroyImmediate(config);
    }
}
