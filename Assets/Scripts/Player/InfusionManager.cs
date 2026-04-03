using UnityEngine;

/// <summary>
/// Motor central de Upgrades (Infusão e Reciclagem).
/// Fica no objeto Player (junto com o PlayerInventory e os Status base).
/// </summary>
public class InfusionManager : MonoBehaviour
{
    private PlayerInventory inventory;
    private PlayerAttributesOffensive offensiveStats;
    private PlayerAttributesDefensive defensiveStats;
    private PlayerHealth healthStats;
    private PlayerEssence essenceWallet;

    void Start()
    {
        inventory = GetComponent<PlayerInventory>();
        
        // PegaInChildren pois as vezes esses scripts ficam no modelo 3D do player ("Astronaut")
        offensiveStats = GetComponentInChildren<PlayerAttributesOffensive>();
        defensiveStats = GetComponentInChildren<PlayerAttributesDefensive>();
        
        healthStats = GetComponent<PlayerHealth>();
        essenceWallet = GetComponent<PlayerEssence>();

        // Diagnóstico para garantir que não falta nada
        if (inventory == null || offensiveStats == null || defensiveStats == null || healthStats == null || essenceWallet == null)
        {
            Debug.LogWarning("[INFUSION MANAGER] Faltando componentes no Player! Verifique se todos os scripts de status estão adicionados no mesmo objeto.");
        }
    }

    /// <summary>
    /// Recicla o item, ganhando a essência configurada no ItemData e removendo o item da mochila.
    /// </summary>
    public bool RecycleItem(string itemId)
    {
        if (ItemDatabase.Instance == null) return false;
        
        ItemData data = ItemDatabase.Instance.GetItemData(itemId);
        if (data == null) return false;

        // Verifica se tem o item no inventário antes de destruir
        if (inventory.HasItem(itemId, 1))
        {
            // Dá essência
            if (essenceWallet != null)
                essenceWallet.AddEssence(data.recycleEssenceValue);
            
            // Remove 1 do inventário
            inventory.RemoveItem(itemId, 1);
            
            Debug.Log($"[INFUSÃO] Item Reciclado: {data.itemName} -> +{data.recycleEssenceValue} Essências");
            return true;
        }

        return false;
    }

    /// <summary>
    /// Infunde o item no corpo, ganhando TODOS os atributos descritos no "ItemData" permanentemente e consumindo o item do inventário.
    /// </summary>
    public bool InfuseItem(string itemId)
    {
        if (ItemDatabase.Instance == null) return false;
        
        ItemData data = ItemDatabase.Instance.GetItemData(itemId);
        if (data == null) return false;

        if (inventory.HasItem(itemId, 1))
        {
            // TENTA PAGAR O CUSTO PRIMEIRO!
            if (essenceWallet != null)
            {
                if (!essenceWallet.SpendEssence(data.infusionEssenceCost))
                {
                    Debug.Log($"[INFUSÃO] Bloqueado! Você precisa de {data.infusionEssenceCost} Essências para infundir {data.itemName}");
                    return false; // Falhou na compra, vaza fora
                }
            }

            // Roda o loop em todos os buffs que o Game Designer cadastrou no Unity
            foreach (var buff in data.itemAttributes)
            {
                ApplyAttribute(buff);
            }

            // Consome 1 item do inventário
            inventory.RemoveItem(itemId, 1);

            Debug.Log($"[INFUSÃO] Sucesso! Item Infundido: {data.itemName}");
            return true;
        }
        
        return false;
    }

    /// <summary>
    /// Roteador: Descobre de quem é esse atributo e manda pro script correto
    /// </summary>
    private void ApplyAttribute(ItemAttributeParam buff)
    {
        // Pega o nome exato do Enum em formato de Texto (String) para casar perfeitamente com os seus ModifyAttributes
        string attrName = buff.attributeType.ToString();

        switch (buff.attributeType)
        {
            // ======= OFENSIVOS =======
            case AttributeType.BaseDamageMultiplier:
            case AttributeType.AttackSpeedMelee:
            case AttributeType.CritChance:
            case AttributeType.CritMultiplier:
            case AttributeType.Knockback:
            case AttributeType.WeaponRangeMelee:
            case AttributeType.WeaponRangeProjectile:
            case AttributeType.Piercing:
            case AttributeType.BounceChance:
            case AttributeType.BounceCount:
            case AttributeType.MultiShotChance:
            case AttributeType.Spread:
                if (offensiveStats != null)
                    offensiveStats.ModifyAttribute(attrName, buff.value, buff.isMultiplier);
                break;
            
            // ======= DEFENSIVOS & MOBILIDADE =======
            case AttributeType.ArmorRegen:
            case AttributeType.DodgeChance:
            case AttributeType.DamageNegation:
            case AttributeType.Thorns:
            case AttributeType.SpeedMultiplier:
            case AttributeType.DashCooldownMultiplier:
            case AttributeType.DashCounts:
            case AttributeType.DashInvulnerability:
                if (defensiveStats != null)
                    defensiveStats.ModifyAttribute(attrName, buff.value, buff.isMultiplier);
                break;

            // ======= VIDA & ARMADURA =======
            case AttributeType.MaxHealth:
            case AttributeType.MaxArmor:
                if (healthStats != null)
                    healthStats.ModifyAttribute(attrName, buff.value, buff.isMultiplier);
                break;
        }
    }
}
