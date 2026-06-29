using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Ferramenta de setup automático para prefabs de SpawnItems.
///
/// O QUE FAZ:
///   1. Para cada prefab em SpawnItems/Prefabs/, verifica se existe um ItemData em SpawnItems/
///   2. Cria o ItemData (.asset) caso não exista
///   3. Abre o prefab em modo de edição e adiciona ItemPickup + Interactable se faltarem
///   4. Atribui o ItemData ao Interactable
///   5. Salva o prefab
///
/// COMO USAR:
///   Unity Menu → Tools → RogueLike → ⚙️ Setup SpawnItems Prefabs
/// </summary>
public class SpawnItemsSetupTool : Editor
{
    private const string PrefabsFolder = "Assets/_Project/Items_and_Crafting/SpawnItems/Prefabs";
    private const string ItemDataFolder = "Assets/_Project/Items_and_Crafting/SpawnItems";

    // Mapeamento: nome do prefab → (itemId, itemName, tier)
    // Adicione novas entradas aqui se criar mais prefabs no futuro
    private static readonly (string prefabName, string itemId, string itemName, ItemTier tier)[] PrefabDefinitions =
    {
        ("little_frog",   "little_frog",   "Sapo",          ItemTier.Common),
        ("Crystal",       "crystal",       "Cristal",       ItemTier.Common),
        ("Magic Carpet",  "magic_carpet",  "Tapete Mágico", ItemTier.Uncommon),
        ("Planta",        "planta",        "Planta",        ItemTier.Common),
        ("stone_low+",    "stone_low",     "Pedra",         ItemTier.Common),
        ("tinker",        "tinker",        "Tinker",        ItemTier.Uncommon),
    };

    [MenuItem("Tools/RogueLike/⚙️ Setup SpawnItems Prefabs")]
    public static void SetupAllSpawnItemPrefabs()
    {
        int created = 0;
        int updated = 0;
        int skipped = 0;

        foreach (var def in PrefabDefinitions)
        {
            string prefabPath = $"{PrefabsFolder}/{def.prefabName}.prefab";
            string itemDataPath = $"{ItemDataFolder}/{def.itemId}.asset";

            // ── Passo 1: Garante que o ItemData existe ─────────────────────────
            ItemData itemData = AssetDatabase.LoadAssetAtPath<ItemData>(itemDataPath);
            if (itemData == null)
            {
                itemData = ScriptableObject.CreateInstance<ItemData>();
                itemData.itemId   = def.itemId;
                itemData.itemName = def.itemName;
                itemData.tier     = def.tier;
                itemData.recycleEssenceValue  = TierToRecycleValue(def.tier);
                itemData.infusionEssenceCost  = TierToInfusionCost(def.tier);
                itemData.returnsToBase = false;

                AssetDatabase.CreateAsset(itemData, itemDataPath);
                Debug.Log($"[SpawnItemsSetup] ItemData criado: {itemDataPath}");
                created++;
            }

            // ── Passo 2: Abre o prefab para edição ────────────────────────────
            if (!File.Exists(Path.Combine(Application.dataPath, "../", prefabPath)))
            {
                if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null)
                {
                    Debug.LogWarning($"[SpawnItemsSetup] Prefab não encontrado: {prefabPath} — pulando.");
                    skipped++;
                    continue;
                }
            }

            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
            bool dirty = false;

            // ── Passo 3: Adiciona Interactable se não existir ─────────────────
            Interactable interactable = prefabRoot.GetComponent<Interactable>();
            if (interactable == null)
            {
                interactable = prefabRoot.AddComponent<Interactable>();
                dirty = true;
            }

            // ── Passo 4: Atribui o ItemData ao Interactable ───────────────────
            if (interactable.itemData != itemData)
            {
                interactable.itemData = itemData;
                dirty = true;
            }

            // ── Passo 5: Adiciona ItemPickup se não existir ───────────────────
            if (prefabRoot.GetComponent<ItemPickup>() == null)
            {
                prefabRoot.AddComponent<ItemPickup>();
                dirty = true;
            }

            // ── Passo 6: Salva o prefab ───────────────────────────────────────
            if (dirty)
            {
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
                updated++;
                Debug.Log($"[SpawnItemsSetup] Prefab configurado: {def.prefabName}");
            }
            else
            {
                skipped++;
                Debug.Log($"[SpawnItemsSetup] Sem mudanças: {def.prefabName}");
            }

            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Setup Concluído",
            $"SpawnItems configurados!\n\n" +
            $"ItemData criados: {created}\n" +
            $"Prefabs atualizados: {updated}\n" +
            $"Sem mudanças: {skipped}",
            "OK"
        );
    }

    // ── Helpers de valores padrão por tier ────────────────────────────────────

    private static int TierToRecycleValue(ItemTier tier) => tier switch
    {
        ItemTier.Common    => 10,
        ItemTier.Uncommon  => 25,
        ItemTier.Rare      => 60,
        ItemTier.Legendary => 120,
        _                  => 10
    };

    private static int TierToInfusionCost(ItemTier tier) => tier switch
    {
        ItemTier.Common    => 60,
        ItemTier.Uncommon  => 180,
        ItemTier.Rare      => 300,
        ItemTier.Legendary => 420,
        _                  => 60
    };
}
