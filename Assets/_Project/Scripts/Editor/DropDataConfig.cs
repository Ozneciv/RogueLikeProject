using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// TABELA CENTRAL DE DROPS
/// Define todos os 32 items (8 inimigos × 4 tiers)
/// Com seus atributos e efeitos especiais T4
/// </summary>
[System.Serializable]
public class DropItemConfig
{
    public string itemId;
    public string itemName;
    public string enemyName;
    public int tier; // 1, 2, 3, 4
    public List<string> attributes = new List<string>();
    public float essenceCost = 100f;
}

public class DropDataConfig : ScriptableObject
{
    public List<DropItemConfig> allItems = new List<DropItemConfig>();

    public void GenerateDefaultData()
    {
        allItems.Clear();

        // ════════════════════════════════════════════════════════════════
        // 🪨 GOLEM (8 items: T1-T4)
        // ════════════════════════════════════════════════════════════════

        // Golem T1
        allItems.Add(new DropItemConfig
        {
            itemId = "golem_chip_t1",
            itemName = "Lasca de Pedra (T1)",
            enemyName = "Golem",
            tier = 1,
            attributes = new List<string> { "MaxArmor" },
            essenceCost = 60f
        });

        // Golem T2
        allItems.Add(new DropItemConfig
        {
            itemId = "golem_plate_t2",
            itemName = "Placa de Rocha (T2)",
            enemyName = "Golem",
            tier = 2,
            attributes = new List<string> { "MaxArmor", "ArmorRegen" },
            essenceCost = 180f
        });

        // Golem T3
        allItems.Add(new DropItemConfig
        {
            itemId = "golem_core_t3",
            itemName = "Núcleo de Pedra (T3)",
            enemyName = "Golem",
            tier = 3,
            attributes = new List<string> { "MaxArmor", "ArmorRegen", "Knockback" },
            essenceCost = 300f
        });

        // Golem T4 - P2: Invoca armadura de pedra espessa ao cair abaixo de 30% vida
        allItems.Add(new DropItemConfig
        {
            itemId = "golem_heart_t4",
            itemName = "Coração de Granito (T4)",
            enemyName = "Golem",
            tier = 4,
            attributes = new List<string> { "MaxArmor", "ArmorRegen", "Knockback", "Special_GolemHeart" },
            essenceCost = 420f
        });

        // ════════════════════════════════════════════════════════════════
        // 🕷️ ARANHA (8 items: T1-T4)
        // ════════════════════════════════════════════════════════════════

        // Aranha T1
        allItems.Add(new DropItemConfig
        {
            itemId = "spider_leg_t1",
            itemName = "Pata de Aranha (T1)",
            enemyName = "Spider",
            tier = 1,
            attributes = new List<string> { "AttackSpeedMelee" },
            essenceCost = 60f
        });

        // Aranha T2
        allItems.Add(new DropItemConfig
        {
            itemId = "spider_silk_t2",
            itemName = "Glândula de Teia (T2)",
            enemyName = "Spider",
            tier = 2,
            attributes = new List<string> { "AttackSpeedMelee", "DashCooldownMultiplier" },
            essenceCost = 180f
        });

        // Aranha T3
        allItems.Add(new DropItemConfig
        {
            itemId = "spider_fang_t3",
            itemName = "Presa Venenosa (T3)",
            enemyName = "Spider",
            tier = 3,
            attributes = new List<string> { "AttackSpeedMelee", "DashCooldownMultiplier", "DodgeChance" },
            essenceCost = 300f
        });

        // Aranha T4 - Dash deixa teia venenosa, ganho attack speed por 3s
        allItems.Add(new DropItemConfig
        {
            itemId = "spider_egg_t4",
            itemName = "Ovo de Aranha (T4)",
            enemyName = "Spider",
            tier = 4,
            attributes = new List<string> { "AttackSpeedMelee", "DashCooldownMultiplier", "DodgeChance", "Special_SpiderVenom" },
            essenceCost = 420f
        });

        // ════════════════════════════════════════════════════════════════
        // 🟢 GOBLIN (8 items: T1-T4)
        // ════════════════════════════════════════════════════════════════

        // Goblin T1
        allItems.Add(new DropItemConfig
        {
            itemId = "goblin_coin_t1",
            itemName = "Moeda Mágica (T1)",
            enemyName = "Goblin",
            tier = 1,
            attributes = new List<string> { "SpeedMultiplier" },
            essenceCost = 60f
        });

        // Goblin T2
        allItems.Add(new DropItemConfig
        {
            itemId = "goblin_trinket_t2",
            itemName = "Trinado Goblin (T2)",
            enemyName = "Goblin",
            tier = 2,
            attributes = new List<string> { "SpeedMultiplier", "CritChance" },
            essenceCost = 180f
        });

        // Goblin T3
        allItems.Add(new DropItemConfig
        {
            itemId = "goblin_amulet_t3",
            itemName = "Amuleto Goblin (T3)",
            enemyName = "Goblin",
            tier = 3,
            attributes = new List<string> { "SpeedMultiplier", "CritChance", "BaseDamageMultiplier" },
            essenceCost = 300f
        });

        // Goblin T4 - Bombas explodem passivamente
        allItems.Add(new DropItemConfig
        {
            itemId = "goblin_bomb_t4",
            itemName = "Bomba Goblin (T4)",
            enemyName = "Goblin",
            tier = 4,
            attributes = new List<string> { "SpeedMultiplier", "CritChance", "BaseDamageMultiplier", "Special_GoblinBomb" },
            essenceCost = 420f
        });

        // ════════════════════════════════════════════════════════════════
        // 🔮 CRYSTAL TUNER (8 items: T1-T4)
        // ════════════════════════════════════════════════════════════════

        // Crystal Tuner T1
        allItems.Add(new DropItemConfig
        {
            itemId = "tuner_shard_t1",
            itemName = "Estilha Sintonizada (T1)",
            enemyName = "Crystal Tuner",
            tier = 1,
            attributes = new List<string> { "CritChance" },
            essenceCost = 60f
        });

        // Crystal Tuner T2
        allItems.Add(new DropItemConfig
        {
            itemId = "tuner_lens_t2",
            itemName = "Lente Ressonante (T2)",
            enemyName = "Crystal Tuner",
            tier = 2,
            attributes = new List<string> { "CritChance", "CritMultiplier" },
            essenceCost = 180f
        });

        // Crystal Tuner T3
        allItems.Add(new DropItemConfig
        {
            itemId = "tuner_prism_t3",
            itemName = "Prisma Sintonizador (T3)",
            enemyName = "Crystal Tuner",
            tier = 3,
            attributes = new List<string> { "CritChance", "CritMultiplier", "SpeedMultiplier" },
            essenceCost = 300f
        });

        // Crystal Tuner T4 - Tomar dano ativa "Adrenalina"
        allItems.Add(new DropItemConfig
        {
            itemId = "tuner_matrix_t4",
            itemName = "Matriz Sintonizadora (T4)",
            enemyName = "Crystal Tuner",
            tier = 4,
            attributes = new List<string> { "CritChance", "CritMultiplier", "SpeedMultiplier", "Special_TunerAdrenaline" },
            essenceCost = 420f
        });

        // ════════════════════════════════════════════════════════════════
        // 💎 SHARD SWARM (8 items: T1-T4)
        // ════════════════════════════════════════════════════════════════

        // Shard Swarm T1
        allItems.Add(new DropItemConfig
        {
            itemId = "shard_splinter_t1",
            itemName = "Estilhaço Cristalino (T1)",
            enemyName = "Shard Swarm",
            tier = 1,
            attributes = new List<string> { "MaxHealth" },
            essenceCost = 60f
        });

        // Shard Swarm T2 (nota: documento diz "2.5. SHARD SWARM" vazio, vou preencher)
        allItems.Add(new DropItemConfig
        {
            itemId = "shard_resonant_t2",
            itemName = "Fragmento Ressonante (T2)",
            enemyName = "Shard Swarm",
            tier = 2,
            attributes = new List<string> { "MaxHealth", "CritMultiplier" },
            essenceCost = 180f
        });

        // Shard Swarm T3
        allItems.Add(new DropItemConfig
        {
            itemId = "shard_refract_t3",
            itemName = "Fragmento Refrator (T3)",
            enemyName = "Shard Swarm",
            tier = 3,
            attributes = new List<string> { "MaxHealth", "CritMultiplier", "Thorns" },
            essenceCost = 300f
        });

        // Shard Swarm T4 - 3 cristais orbitam, dão dano e destroem 1º projétil
        allItems.Add(new DropItemConfig
        {
            itemId = "shard_prismatic_t4",
            itemName = "Fragmento Prismático (T4)",
            enemyName = "Shard Swarm",
            tier = 4,
            attributes = new List<string> { "MaxHealth", "CritMultiplier", "Thorns", "Special_ShardOrbit" },
            essenceCost = 420f
        });

        // ════════════════════════════════════════════════════════════════
        // 🗿 TOTEM (8 items: T1-T4)
        // ════════════════════════════════════════════════════════════════

        // Totem T1
        allItems.Add(new DropItemConfig
        {
            itemId = "totem_stone_t1",
            itemName = "Pedra Totem (T1)",
            enemyName = "Totem",
            tier = 1,
            attributes = new List<string> { "MaxHealth" },
            essenceCost = 60f
        });

        // Totem T2
        allItems.Add(new DropItemConfig
        {
            itemId = "totem_carved_t2",
            itemName = "Totem Esculpido (T2)",
            enemyName = "Totem",
            tier = 2,
            attributes = new List<string> { "MaxHealth", "MaxHealth" }, // Regen aparentemente está como 2x MaxHealth ou usar outro atributo
            essenceCost = 180f
        });

        // Totem T3
        allItems.Add(new DropItemConfig
        {
            itemId = "totem_ancient_t3",
            itemName = "Totem Ancestral (T3)",
            enemyName = "Totem",
            tier = 3,
            attributes = new List<string> { "MaxHealth", "MaxHealth", "DamageNegation" },
            essenceCost = 300f
        });

        // Totem T4 - Ficar imóvel >1.5s = se torna totem, -50% dano, lança caveiras
        allItems.Add(new DropItemConfig
        {
            itemId = "totem_monolith_t4",
            itemName = "Monolito Totem (T4)",
            enemyName = "Totem",
            tier = 4,
            attributes = new List<string> { "MaxHealth", "MaxHealth", "DamageNegation", "Special_TotemForm" },
            essenceCost = 420f
        });

        // ════════════════════════════════════════════════════════════════
        // 👁️ CRYSTAL WATCHER (8 items: T1-T4)
        // ════════════════════════════════════════════════════════════════

        // Crystal Watcher T1
        allItems.Add(new DropItemConfig
        {
            itemId = "watcher_lens_t1",
            itemName = "Lente Vigilante (T1)",
            enemyName = "Crystal Watcher",
            tier = 1,
            attributes = new List<string> { "WeaponRangeProjectile" },
            essenceCost = 60f
        });

        // Crystal Watcher T2
        allItems.Add(new DropItemConfig
        {
            itemId = "watcher_eye_t2",
            itemName = "Olho Vigilante (T2)",
            enemyName = "Crystal Watcher",
            tier = 2,
            attributes = new List<string> { "WeaponRangeProjectile", "Piercing" },
            essenceCost = 180f
        });

        // Crystal Watcher T3
        allItems.Add(new DropItemConfig
        {
            itemId = "watcher_sight_t3",
            itemName = "Visão Vigilante (T3)",
            enemyName = "Crystal Watcher",
            tier = 3,
            attributes = new List<string> { "WeaponRangeProjectile", "Piercing", "CritChance" },
            essenceCost = 300f
        });

        // Crystal Watcher T4 - Laser 360° a cada 10s
        allItems.Add(new DropItemConfig
        {
            itemId = "watcher_beacon_t4",
            itemName = "Farol Vigilante (T4)",
            enemyName = "Crystal Watcher",
            tier = 4,
            attributes = new List<string> { "WeaponRangeProjectile", "Piercing", "CritChance", "Special_WatcherLaser" },
            essenceCost = 420f
        });

        // ════════════════════════════════════════════════════════════════
        // ✨ MAGIC CRYSTAL (8 items: T1-T4)
        // ════════════════════════════════════════════════════════════════

        // Magic Crystal T1
        allItems.Add(new DropItemConfig
        {
            itemId = "magic_dust_t1",
            itemName = "Pó Arcano (T1)",
            enemyName = "Magic Crystal",
            tier = 1,
            attributes = new List<string> { "BaseDamageMultiplier" },
            essenceCost = 60f
        });

        // Magic Crystal T2
        allItems.Add(new DropItemConfig
        {
            itemId = "magic_rune_t2",
            itemName = "Runa Instável (T2)",
            enemyName = "Magic Crystal",
            tier = 2,
            attributes = new List<string> { "BaseDamageMultiplier", "DashInvulnerability" },
            essenceCost = 180f
        });

        // Magic Crystal T3
        allItems.Add(new DropItemConfig
        {
            itemId = "magic_essence_t3",
            itemName = "Essência Mágica (T3)",
            enemyName = "Magic Crystal",
            tier = 3,
            attributes = new List<string> { "BaseDamageMultiplier", "DashInvulnerability", "DashCounts" },
            essenceCost = 300f
        });

        // Magic Crystal T4 - Skybeam a cada 2s em inimigo aleatório ou Dash = Teleporte + Skybeam
        allItems.Add(new DropItemConfig
        {
            itemId = "magic_catalyst_t4",
            itemName = "Catalisador Mágico (T4)",
            enemyName = "Magic Crystal",
            tier = 4,
            attributes = new List<string> { "BaseDamageMultiplier", "DashInvulnerability", "DashCounts", "Special_MagicSkybeam" },
            essenceCost = 420f
        });

        // ════════════════════════════════════════════════════════════════
        // 💎 GEOBIONTE (1 item: T2)
        // ════════════════════════════════════════════════════════════════
        
        allItems.Add(new DropItemConfig
        {
            itemId = "geobionte_bismuto_t2",
            itemName = "Cristal de Bismuto",
            enemyName = "Geobionte",
            tier = 2,
            attributes = new List<string> { "SlowOnHit" },
            essenceCost = 180f
        });

        // ════════════════════════════════════════════════════════════════
        // 💎 GEOBIONTE SENTINELA (1 item: T4)
        // ════════════════════════════════════════════════════════════════
        
        allItems.Add(new DropItemConfig
        {
            itemId = "sentinel_leg_t4",
            itemName = "Perna de Sentinela",
            enemyName = "Geobionte",
            tier = 4,
            attributes = new List<string> { "Special_SentinelLeg" },
            essenceCost = 420f
        });

        Debug.Log($"[DROP CONFIG] Gerados {allItems.Count} items!");
    }
}
