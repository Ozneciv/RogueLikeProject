using UnityEngine;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Gerencia os dois setores de persistência do jogo.
///
/// ─── SETOR 1 — PROGRESSÃO PERMANENTE ─────────────────────────────────────────
///   SavePersistentData()   Serializa PersistentSaveData em JSON e grava em disco.
///                          Chamado automaticamente pelo OnApplicationQuit.
///
///   LoadPersistentData()   Lê o JSON do disco e aplica ao player atual.
///                          Chamado pelo GameManager logo após RegisterPlayer().
///
/// ─── SETOR 2 — LIMPEZA DE RUN (morte) ────────────────────────────────────────
///   OnPlayerDied()         Remove do PlayerInventory todos os itens de run
///                          (ItemData.returnsToBase == false). Itens de base
///                          (returnsToBase == true) são mantidos intactos.
///                          Chamado por PlayerHealth.Die().
///
/// DEPENDÊNCIAS:
///   - GameManager.instance.currentPlayer  (player persistente)
///   - ItemDatabase.Instance               (lookup de ItemData)
///   - PlayerInventory, PlayerUpgrades     (no GameObject do player)
/// </summary>
public class SaveManager : MonoBehaviour
{
    public static SaveManager instance;

    private const string SaveFileName = "player_progress.json";
    private string SaveFilePath => Path.Combine(Application.persistentDataPath, SaveFileName);

    // ─── CICLO DE VIDA ────────────────────────────────────────────────────────

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Salva automaticamente ao fechar o aplicativo.
    /// </summary>
    void OnApplicationQuit()
    {
        SavePersistentData();
    }

    // ─── SETOR 1 — PROGRESSÃO PERMANENTE ─────────────────────────────────────

    /// <summary>
    /// Serializa a progressão permanente do jogador (itens de base, slots e upgrades
    /// comprados) em um arquivo JSON em Application.persistentDataPath.
    ///
    /// O QUE SALVA:
    ///   • inventoryMaxSlots           — total de slots de inventário expandidos
    ///   • baseResources               — itens com ItemData.returnsToBase == true
    ///   • purchasedUpgradeIndices     — índices dos upgrades comprados (botão desativado)
    /// </summary>
    public void SavePersistentData()
    {
        GameObject player = GameManager.instance?.currentPlayer;
        if (player == null)
        {
            Debug.LogWarning("[SAVE] Nenhum player registrado. Nada foi salvo.");
            return;
        }

        PersistentSaveData data = new PersistentSaveData();

        PlayerInventory inventory = player.GetComponent<PlayerInventory>();
        if (inventory != null)
        {
            data.inventoryMaxSlots = inventory.MaxSlots;

            foreach (var kvp in inventory.GetAllItems())
            {
                ItemData itemData = ItemDatabase.Instance?.GetItemData(kvp.Key);
                if (itemData != null && itemData.returnsToBase)
                    data.baseResources.Add(new ItemSaveEntry(kvp.Key, kvp.Value));
            }
        }

        PlayerUpgrades upgrades = player.GetComponent<PlayerUpgrades>();
        if (upgrades != null)
        {
            for (int i = 0; i < upgrades.upgrades.Count; i++)
            {
                var upg = upgrades.upgrades[i];
                if (upg.button != null && !upg.button.interactable)
                    data.purchasedUpgradeIndices.Add(i);
            }
        }

        string json = JsonUtility.ToJson(data, prettyPrint: true);
        File.WriteAllText(SaveFilePath, json);
        Debug.Log($"[SAVE] Progressão salva → {SaveFilePath} | " +
                  $"{data.baseResources.Count} recursos | " +
                  $"{data.purchasedUpgradeIndices.Count} upgrades");
    }

    /// <summary>
    /// Lê o arquivo JSON do disco e aplica os dados ao player informado.
    /// Chamado pelo GameManager após registrar o player (RegisterPlayer).
    ///
    /// O QUE RESTAURA:
    ///   • Capacidade máxima de slots do inventário
    ///   • Itens de base (returnsToBase == true) de volta ao inventário
    ///   • Re-aplica upgrades comprados (repassa para PlayerUpgrades.BuyUpgrade)
    /// </summary>
    public void LoadPersistentData(GameObject player)
    {
        if (!File.Exists(SaveFilePath))
        {
            Debug.Log("[SAVE] Arquivo de save não encontrado. Jogador inicia do zero.");
            return;
        }

        string json = File.ReadAllText(SaveFilePath);
        PersistentSaveData data = JsonUtility.FromJson<PersistentSaveData>(json);
        if (data == null)
        {
            Debug.LogWarning("[SAVE] Falha ao desserializar o save. Arquivo pode estar corrompido.");
            return;
        }

        PlayerInventory inventory = player.GetComponent<PlayerInventory>();
        if (inventory != null)
        {
            inventory.SetMaxSlots(data.inventoryMaxSlots);

            foreach (var entry in data.baseResources)
                inventory.AddItem(entry.itemId, entry.quantity);
        }

        PlayerUpgrades upgrades = player.GetComponent<PlayerUpgrades>();
        if (upgrades != null)
        {
            foreach (int idx in data.purchasedUpgradeIndices)
                upgrades.BuyUpgrade(idx);
        }

        Debug.Log($"[SAVE] Progressão carregada | " +
                  $"{data.baseResources.Count} recursos de base | " +
                  $"{data.purchasedUpgradeIndices.Count} upgrades re-aplicados");
    }

    // ─── SETOR 2 — LIMPEZA DE RUN (morte) ────────────────────────────────────

    /// <summary>
    /// Limpa o inventário de infusão do jogador após a morte.
    ///
    /// COMPORTAMENTO:
    ///   • Remove todos os itens onde ItemData.returnsToBase == false
    ///     (itens comuns de run — coletados durante a dungeon para infusão).
    ///   • Mantém todos os itens onde ItemData.returnsToBase == true
    ///     (recursos permanentes usados para upgrades na base).
    ///   • Itens sem registro no ItemDatabase são descartados por segurança.
    ///
    /// Chamado por PlayerHealth.Die() antes da transição de cena.
    /// </summary>
    public void OnPlayerDied(GameObject player)
    {
        if (player == null) return;

        PlayerInventory inventory = player.GetComponent<PlayerInventory>();
        if (inventory == null) return;

        List<string> toDiscard = new List<string>();

        foreach (var kvp in inventory.GetAllItems())
        {
            ItemData itemData = ItemDatabase.Instance?.GetItemData(kvp.Key);
            bool isBaseResource = itemData != null && itemData.returnsToBase;

            if (!isBaseResource)
                toDiscard.Add(kvp.Key);
        }

        foreach (string id in toDiscard)
            inventory.RemoveItem(id, inventory.GetItemCount(id));

        Debug.Log($"[SAVE] Morte: {toDiscard.Count} item(s) de run descartado(s). Recursos de base preservados.");
    }
}
