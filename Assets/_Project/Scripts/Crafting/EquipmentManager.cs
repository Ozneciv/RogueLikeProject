using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Gerencia melhorias equipáveis craftadas pelo jogador.
/// Singleton — persiste entre cenas via DontDestroyOnLoad.
///
/// RESPONSABILIDADES:
///   • Manter a lista de melhorias craftadas (persistidas via SaveManager)
///   • Equipar/Desequipar melhorias, aplicando/removendo efeitos
///   • Disparar eventos para a UI se atualizar
///
/// EFEITOS SUPORTADOS:
///   • InventorySlotExpansion — modifica PlayerInventory.MaxSlots
///   • MaxHealthBoost        — modifica PlayerHealth via ModifyAttribute
///   • MaxArmorBoost         — modifica PlayerHealth via ModifyAttribute
///   • SpeedBoost            — modifica PlayerAttributesDefensive via ModifyAttribute
///   • DamageBoost           — modifica PlayerAttributesOffensive via ModifyAttribute
///   • CritChanceBoost       — modifica PlayerAttributesOffensive via ModifyAttribute
///   • ArmorRegenBoost       — modifica PlayerAttributesDefensive via ModifyAttribute
///
/// DEPENDÊNCIAS:
///   - SaveManager.instance      (persistência)
///   - GameManager.instance      (acesso ao player)
/// </summary>
public class EquipmentManager : MonoBehaviour
{
    public static EquipmentManager Instance { get; private set; }

    [Header("Definições de Equipamento")]
    [Tooltip("Arraste todos os ScriptableObjects de EquipmentData aqui")]
    public List<EquipmentData> allEquipmentDefinitions = new List<EquipmentData>();

    // Dicionário para busca rápida por ID
    private Dictionary<string, EquipmentData> equipmentLookup = new Dictionary<string, EquipmentData>();

    // ─── EVENTOS ─────────────────────────────────────────────────────────────

    /// <summary>Disparado quando qualquer equipamento é equipado/desequipado.</summary>
    public static event Action OnEquipmentStateChanged;

    // ─── CICLO DE VIDA ────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        BuildLookup();
    }

    void Start()
    {
        // Garante que todas as definições no Resources sejam carregadas e mescladas
        EquipmentData[] loaded = Resources.LoadAll<EquipmentData>("");
        if (loaded.Length > 0)
        {
            foreach (var equip in loaded)
            {
                if (equip != null && !allEquipmentDefinitions.Contains(equip))
                {
                    allEquipmentDefinitions.Add(equip);
                }
            }
            BuildLookup();
            Debug.Log($"[EQUIPMENT] Mesclou {loaded.Length} definições do Resources. Total: {allEquipmentDefinitions.Count}");
        }

        // Re-aplica efeitos de equipamentos equipados (após carregar o save)
        ReapplyAllEquippedEffects();

        Debug.Log($"[EQUIPMENT] EquipmentManager inicializado com {allEquipmentDefinitions.Count} definições.");
    }

    void BuildLookup()
    {
        equipmentLookup.Clear();
        foreach (var equip in allEquipmentDefinitions)
        {
            if (equip == null || string.IsNullOrEmpty(equip.equipmentId)) continue;
            if (!equipmentLookup.ContainsKey(equip.equipmentId))
                equipmentLookup[equip.equipmentId] = equip;
        }
    }

    // ─── API PRINCIPAL ────────────────────────────────────────────────────────

    /// <summary>
    /// Equipa uma melhoria. Aplica o efeito ao jogador.
    /// Retorna true se equipado com sucesso.
    /// </summary>
    public bool Equip(string equipmentId)
    {
        if (SaveManager.instance == null) return false;

        // Verifica se o jogador possui essa melhoria
        if (!IsOwned(equipmentId))
        {
            Debug.LogWarning($"[EQUIPMENT] Jogador não possui: {equipmentId}");
            return false;
        }

        // Verifica se já está equipado
        if (IsEquipped(equipmentId))
        {
            Debug.Log($"[EQUIPMENT] Já está equipado: {equipmentId}");
            return false;
        }

        // Aplica o efeito
        EquipmentData data = GetEquipmentData(equipmentId);
        if (data == null)
        {
            Debug.LogWarning($"[EQUIPMENT] EquipmentData não encontrado: {equipmentId}");
            return false;
        }

        ApplyEffect(data);

        // Salva estado
        SaveManager.instance.SetEquipmentEquipped(equipmentId, true);
        SaveManager.instance.SavePersistentData();

        OnEquipmentStateChanged?.Invoke();

        Debug.Log($"[EQUIPMENT] ✓ Equipado: {data.equipmentName} ({data.effectType} +{data.effectValue})");
        return true;
    }

    /// <summary>
    /// Desequipa uma melhoria. Remove o efeito do jogador.
    /// Retorna true se desequipado com sucesso.
    /// </summary>
    public bool Unequip(string equipmentId)
    {
        if (SaveManager.instance == null) return false;

        if (!IsEquipped(equipmentId))
        {
            Debug.Log($"[EQUIPMENT] Não está equipado: {equipmentId}");
            return false;
        }

        EquipmentData data = GetEquipmentData(equipmentId);
        if (data == null) return false;

        RemoveEffect(data);

        SaveManager.instance.SetEquipmentEquipped(equipmentId, false);
        SaveManager.instance.SavePersistentData();

        OnEquipmentStateChanged?.Invoke();

        Debug.Log($"[EQUIPMENT] ✗ Desequipado: {data.equipmentName}");
        return true;
    }

    /// <summary>
    /// Verifica se o jogador possui esta melhoria (foi craftada).
    /// </summary>
    public bool IsOwned(string equipmentId)
    {
        if (SaveManager.instance == null) return false;
        return SaveManager.instance.GetCraftedEquipmentCount(equipmentId) > 0;
    }

    /// <summary>
    /// Verifica se a melhoria está equipada.
    /// </summary>
    public bool IsEquipped(string equipmentId)
    {
        if (SaveManager.instance == null) return false;
        return SaveManager.instance.IsEquipmentEquipped(equipmentId);
    }

    /// <summary>
    /// Retorna o EquipmentData pelo ID.
    /// </summary>
    public EquipmentData GetEquipmentData(string equipmentId)
    {
        if (string.IsNullOrEmpty(equipmentId)) return null;
        return equipmentLookup.TryGetValue(equipmentId, out var data) ? data : null;
    }

    /// <summary>
    /// Retorna todas as melhorias que o jogador possui (craftou).
    /// </summary>
    public List<EquipmentData> GetOwnedEquipment()
    {
        List<EquipmentData> owned = new List<EquipmentData>();
        if (SaveManager.instance == null) return owned;

        List<string> craftedIds = SaveManager.instance.GetAllCraftedEquipmentIds();
        HashSet<string> added = new HashSet<string>();

        foreach (string id in craftedIds)
        {
            if (added.Contains(id)) continue;
            EquipmentData data = GetEquipmentData(id);
            if (data != null)
            {
                owned.Add(data);
                added.Add(id);
            }
        }

        return owned;
    }

    // ─── APLICAÇÃO DE EFEITOS ────────────────────────────────────────────────

    /// <summary>
    /// Re-aplica todos os efeitos dos equipamentos marcados como equipados.
    /// Chamado após carregar o save para restaurar o estado.
    /// </summary>
    public void ReapplyAllEquippedEffects()
    {
        if (SaveManager.instance == null) return;

        List<string> equippedIds = SaveManager.instance.GetAllEquippedEquipmentIds();
        foreach (string id in equippedIds)
        {
            EquipmentData data = GetEquipmentData(id);
            if (data != null)
            {
                ApplyEffect(data);
                Debug.Log($"[EQUIPMENT] Re-aplicado efeito: {data.equipmentName}");
            }
        }
    }

    /// <summary>
    /// Aplica o efeito de uma melhoria ao jogador.
    /// </summary>
    private void ApplyEffect(EquipmentData data)
    {
        GameObject player = GetPlayer();
        if (player == null) return;

        switch (data.effectType)
        {
            case EquipmentEffectType.InventorySlotExpansion:
                PlayerInventory inv = player.GetComponent<PlayerInventory>();
                if (inv != null)
                    inv.IncreaseMaxSlots(Mathf.RoundToInt(data.effectValue));
                break;

            case EquipmentEffectType.MaxHealthBoost:
                PlayerHealth health = player.GetComponent<PlayerHealth>();
                if (health != null)
                    health.ModifyAttribute("MaxHealth", data.effectValue, false);
                break;

            case EquipmentEffectType.MaxArmorBoost:
                PlayerHealth armor = player.GetComponent<PlayerHealth>();
                if (armor != null)
                    armor.ModifyAttribute("MaxArmor", data.effectValue, false);
                break;

            case EquipmentEffectType.SpeedBoost:
                PlayerAttributesDefensive defStats = player.GetComponentInChildren<PlayerAttributesDefensive>();
                if (defStats != null)
                    defStats.ModifyAttribute("SpeedMultiplier", data.effectValue, true);
                break;

            case EquipmentEffectType.DamageBoost:
                PlayerAttributesOffensive offStats = player.GetComponentInChildren<PlayerAttributesOffensive>();
                if (offStats != null)
                    offStats.ModifyAttribute("BaseDamageMultiplier", data.effectValue, true);
                break;

            case EquipmentEffectType.CritChanceBoost:
                PlayerAttributesOffensive critStats = player.GetComponentInChildren<PlayerAttributesOffensive>();
                if (critStats != null)
                    critStats.ModifyAttribute("CritChance", data.effectValue, false);
                break;

            case EquipmentEffectType.ArmorRegenBoost:
                PlayerAttributesDefensive regenStats = player.GetComponentInChildren<PlayerAttributesDefensive>();
                if (regenStats != null)
                    regenStats.ModifyAttribute("ArmorRegen", data.effectValue, false);
                break;

            case EquipmentEffectType.RangeBoost:
                PlayerAttributesOffensive rangeStats = player.GetComponentInChildren<PlayerAttributesOffensive>();
                if (rangeStats != null)
                {
                    rangeStats.ModifyAttribute("WeaponRangeMelee", data.effectValue, true);
                    rangeStats.ModifyAttribute("WeaponRangeProjectile", data.effectValue, true);
                }
                break;

            case EquipmentEffectType.DodgeChanceBoost:
                PlayerAttributesDefensive dodgeStats = player.GetComponentInChildren<PlayerAttributesDefensive>();
                if (dodgeStats != null)
                    dodgeStats.ModifyAttribute("DodgeChance", data.effectValue, false);
                break;

            case EquipmentEffectType.CritMultiplierBoost:
                PlayerAttributesOffensive critMultStats = player.GetComponentInChildren<PlayerAttributesOffensive>();
                if (critMultStats != null)
                    critMultStats.ModifyAttribute("CritMultiplier", data.effectValue, false);
                break;

            case EquipmentEffectType.KnockbackBoost:
                PlayerAttributesOffensive knockStats = player.GetComponentInChildren<PlayerAttributesOffensive>();
                if (knockStats != null)
                    knockStats.ModifyAttribute("Knockback", data.effectValue, false);
                break;

            case EquipmentEffectType.AttackSpeedBoost:
                PlayerAttributesOffensive atkSpeedStats = player.GetComponentInChildren<PlayerAttributesOffensive>();
                if (atkSpeedStats != null)
                    atkSpeedStats.ModifyAttribute("AttackSpeedMelee", data.effectValue, true);
                break;

            case EquipmentEffectType.HealthRegenBoost:
                PlayerAttributesDefensive hpRegenStats = player.GetComponentInChildren<PlayerAttributesDefensive>();
                if (hpRegenStats != null)
                    hpRegenStats.ModifyAttribute("HealthRegen", data.effectValue, false);
                break;

            case EquipmentEffectType.DashCooldownBoost:
                PlayerAttributesDefensive dashCDStats = player.GetComponentInChildren<PlayerAttributesDefensive>();
                if (dashCDStats != null)
                    dashCDStats.ModifyAttribute("DashCooldownMultiplier", data.effectValue, true);
                break;

            case EquipmentEffectType.DashCountsBoost:
                PlayerAttributesDefensive dashCountStats = player.GetComponentInChildren<PlayerAttributesDefensive>();
                if (dashCountStats != null)
                    dashCountStats.ModifyAttribute("DashCounts", data.effectValue, false);
                break;

            case EquipmentEffectType.DashInvulnerabilityBoost:
                PlayerAttributesDefensive dashInvStats = player.GetComponentInChildren<PlayerAttributesDefensive>();
                if (dashInvStats != null)
                    dashInvStats.ModifyAttribute("DashInvulnerability", data.effectValue, false);
                break;
        }
    }

    /// <summary>
    /// Remove o efeito de uma melhoria do jogador (inverte a aplicação).
    /// </summary>
    private void RemoveEffect(EquipmentData data)
    {
        GameObject player = GetPlayer();
        if (player == null) return;

        switch (data.effectType)
        {
            case EquipmentEffectType.InventorySlotExpansion:
                PlayerInventory inv = player.GetComponent<PlayerInventory>();
                if (inv != null)
                {
                    int newMax = Mathf.Max(1, inv.MaxSlots - Mathf.RoundToInt(data.effectValue));
                    inv.SetMaxSlots(newMax);
                }
                break;

            case EquipmentEffectType.MaxHealthBoost:
                PlayerHealth health = player.GetComponent<PlayerHealth>();
                if (health != null)
                    health.ModifyAttribute("MaxHealth", -data.effectValue, false);
                break;

            case EquipmentEffectType.MaxArmorBoost:
                PlayerHealth armor = player.GetComponent<PlayerHealth>();
                if (armor != null)
                    armor.ModifyAttribute("MaxArmor", -data.effectValue, false);
                break;

            case EquipmentEffectType.SpeedBoost:
                PlayerAttributesDefensive defStats = player.GetComponentInChildren<PlayerAttributesDefensive>();
                if (defStats != null)
                    defStats.ModifyAttribute("SpeedMultiplier", 1f / data.effectValue, true);
                break;

            case EquipmentEffectType.DamageBoost:
                PlayerAttributesOffensive offStats = player.GetComponentInChildren<PlayerAttributesOffensive>();
                if (offStats != null)
                    offStats.ModifyAttribute("BaseDamageMultiplier", 1f / data.effectValue, true);
                break;

            case EquipmentEffectType.CritChanceBoost:
                PlayerAttributesOffensive critStats = player.GetComponentInChildren<PlayerAttributesOffensive>();
                if (critStats != null)
                    critStats.ModifyAttribute("CritChance", -data.effectValue, false);
                break;

            case EquipmentEffectType.ArmorRegenBoost:
                PlayerAttributesDefensive regenStats = player.GetComponentInChildren<PlayerAttributesDefensive>();
                if (regenStats != null)
                    regenStats.ModifyAttribute("ArmorRegen", -data.effectValue, false);
                break;

            case EquipmentEffectType.RangeBoost:
                PlayerAttributesOffensive rangeStats = player.GetComponentInChildren<PlayerAttributesOffensive>();
                if (rangeStats != null)
                {
                    rangeStats.ModifyAttribute("WeaponRangeMelee", 1f / data.effectValue, true);
                    rangeStats.ModifyAttribute("WeaponRangeProjectile", 1f / data.effectValue, true);
                }
                break;

            case EquipmentEffectType.DodgeChanceBoost:
                PlayerAttributesDefensive dodgeStats = player.GetComponentInChildren<PlayerAttributesDefensive>();
                if (dodgeStats != null)
                    dodgeStats.ModifyAttribute("DodgeChance", -data.effectValue, false);
                break;

            case EquipmentEffectType.CritMultiplierBoost:
                PlayerAttributesOffensive critMultStats = player.GetComponentInChildren<PlayerAttributesOffensive>();
                if (critMultStats != null)
                    critMultStats.ModifyAttribute("CritMultiplier", -data.effectValue, false);
                break;

            case EquipmentEffectType.KnockbackBoost:
                PlayerAttributesOffensive knockStats = player.GetComponentInChildren<PlayerAttributesOffensive>();
                if (knockStats != null)
                    knockStats.ModifyAttribute("Knockback", -data.effectValue, false);
                break;

            case EquipmentEffectType.AttackSpeedBoost:
                PlayerAttributesOffensive atkSpeedStats = player.GetComponentInChildren<PlayerAttributesOffensive>();
                if (atkSpeedStats != null)
                    atkSpeedStats.ModifyAttribute("AttackSpeedMelee", 1f / data.effectValue, true);
                break;

            case EquipmentEffectType.HealthRegenBoost:
                PlayerAttributesDefensive hpRegenStats = player.GetComponentInChildren<PlayerAttributesDefensive>();
                if (hpRegenStats != null)
                    hpRegenStats.ModifyAttribute("HealthRegen", -data.effectValue, false);
                break;

            case EquipmentEffectType.DashCooldownBoost:
                PlayerAttributesDefensive dashCDStats = player.GetComponentInChildren<PlayerAttributesDefensive>();
                if (dashCDStats != null)
                    dashCDStats.ModifyAttribute("DashCooldownMultiplier", 1f / data.effectValue, true);
                break;

            case EquipmentEffectType.DashCountsBoost:
                PlayerAttributesDefensive dashCountStats = player.GetComponentInChildren<PlayerAttributesDefensive>();
                if (dashCountStats != null)
                    dashCountStats.ModifyAttribute("DashCounts", -data.effectValue, false);
                break;

            case EquipmentEffectType.DashInvulnerabilityBoost:
                PlayerAttributesDefensive dashInvStats = player.GetComponentInChildren<PlayerAttributesDefensive>();
                if (dashInvStats != null)
                    dashInvStats.ModifyAttribute("DashInvulnerability", -data.effectValue, false);
                break;
        }
    }

    /// <summary>
    /// Helper: encontra o GameObject do player via GameManager.
    /// </summary>
    private GameObject GetPlayer()
    {
        if (GameManager.instance != null && GameManager.instance.currentPlayer != null)
            return GameManager.instance.currentPlayer;

        Debug.LogWarning("[EQUIPMENT] Player não encontrado via GameManager.");
        return null;
    }
}
