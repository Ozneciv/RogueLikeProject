using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

public class ItemDataAssetGenerator : Editor
{
    [MenuItem("Tools/RogueLike/⚒️ GERAR ASSET DATAS (ItemData)")]
    public static void GenerateItemDataAssets()
    {
        var config = ScriptableObject.CreateInstance<DropDataConfig>();
        config.GenerateDefaultData();

        string targetFolder = "Assets/_Project/Items_and_Crafting/Items";
        if (!Directory.Exists(targetFolder))
        {
            Directory.CreateDirectory(targetFolder);
            AssetDatabase.Refresh();
        }

        int count = 0;
        foreach (var drop in config.allItems)
        {
            string assetPath = $"{targetFolder}/{drop.itemId}.asset";
            ItemData existing = AssetDatabase.LoadAssetAtPath<ItemData>(assetPath);

            if (existing != null)
            {
                Debug.LogWarning($"[ItemDataAssetGenerator] {drop.itemId}.asset já existe, pulando...");
                continue;
            }

            ItemData newItem = ScriptableObject.CreateInstance<ItemData>();
            newItem.itemId = drop.itemId;
            newItem.itemName = drop.itemName;
            newItem.enemySource = drop.enemyName;
            
            // Map Tier
            newItem.tier = drop.tier switch {
                1 => ItemTier.Common,
                2 => ItemTier.Uncommon,
                3 => ItemTier.Rare,
                4 => ItemTier.Legendary,
                _ => ItemTier.Common
            };

            newItem.infusionEssenceCost = (int)drop.essenceCost;
            newItem.recycleEssenceValue = (int)(drop.essenceCost * 0.4f);

            // Parsing Attributes
            newItem.itemAttributes = new List<ItemAttributeParam>();
            foreach (string attrString in drop.attributes)
            {
                // Especial handling for T4 unique abilities that are not standard attributes
                if (attrString.StartsWith("Special_"))
                {
                    if (attrString == "Special_SentinelLeg")
                    {
                        newItem.description = "Evita morte uma vez a cada 10 min. Se <30% HP, ignora dano. Se >=30% HP, define HP para 30%.";
                    }
                    else
                    {
                        newItem.description = $"Efeito Único T4: {attrString.Replace("Special_", "")}";
                    }
                    continue;
                }

                if (System.Enum.TryParse(attrString, true, out AttributeType parsedType))
                {
                    // Generic placeholder values since PDF didn't have numbers
                    float placeholderValue = 10f; 
                    bool isMultiplier = false;

                    string lower = attrString.ToLower();
                    if (lower.Contains("slow"))
                    {
                        placeholderValue = 0.05f; // Represents 5% slow
                        isMultiplier = false;
                    }
                    else if (lower.Contains("multiplier") || lower.Contains("chance") || lower.Contains("speed") || lower.Contains("regen"))
                    {
                        placeholderValue = 0.05f; // Represents +5%
                        isMultiplier = true;
                    }

                    newItem.itemAttributes.Add(new ItemAttributeParam {
                        attributeType = parsedType,
                        value = placeholderValue,
                        isMultiplier = isMultiplier
                    });
                }
            }

            AssetDatabase.CreateAsset(newItem, assetPath);
            count++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        DestroyImmediate(config);

        EditorUtility.DisplayDialog("Geração Concluída", $"Gerados {count} arquivos ItemData com sucesso em {targetFolder}!\nAbra os arquivos para balancear os números exatos.", "OK");
    }
}
