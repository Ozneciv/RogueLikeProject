# 🎯 GUIA: Adicionar os 32 Items de Drop

## Status: ✅ **TABELA CRIADA E PRONTA**

Criei a tabela completa em `DropDataConfig.cs` com todos os 32 items mapeados (8 inimigos × 4 tiers).

---

## 📋 **COMO VER A TABELA**

1. No Unity, vá para: **Tools → RogueLike → Mostrar Tabela de Drops no Console**
2. Você vê todos os 32 items listados com seus:
   - Item ID
   - Nome
   - Tier (1-4)
   - Atributos
   - Inimigo de origem

---

## 🚀 **ABORDAGEM MAIS RÁPIDA (15 minutos)**

### **Passo 1: Criar os Prefabs de Items**

Para cada um dos 32 items:

1. **Duplicar** um prefab de item existente (ou criar novo GameObject)
2. **Adicionar** o componente `CharacteristicItemPickup`
3. **Preencher** os campos:
   - `Item Id`: ex. `golem_chip_t1`
   - `Item Name`: ex. "Lasca de Pedra (T1)"
   - `Item Description`: ex. "Aumenta Armadura Máxima"
4. **Salvar** como prefab em `Assets/Prefabs/Items/`

**Para ir RÁPIDO:** Copie a linha do console e cole no Inspector!

---

### **Passo 2: Configurar os Inimigos (EnemyDrops)**

Para cada inimigo (Golem, Aranha, etc):

1. **Selecione** o prefab do inimigo
2. **Encontre** o componente `EnemyDrops`
3. **No campo `lootPool`:**
   - Clique em `+` para adicionar 4 itens (T1, T2, T3, T4)
   - Arraste os 4 prefabs que você criou (ex: golem_chip_t1, golem_plate_t2, etc)
4. **Configure** chance e quantidade se desejar

---

## 📊 **TABELA RÁPIDA PARA COPIAR/COLAR**

```
🪨 GOLEM
  T1: golem_chip_t1 | Lasca de Pedra (T1) | MaxArmor
  T2: golem_plate_t2 | Placa de Rocha (T2) | MaxArmor, ArmorRegen
  T3: golem_core_t3 | Núcleo de Pedra (T3) | MaxArmor, ArmorRegen, Knockback
  T4: golem_heart_t4 | Coração de Granito (T4) | Armadura espessa ao <30% HP

🕷️ ARANHA
  T1: spider_leg_t1 | Pata de Aranha (T1) | AttackSpeedMelee
  T2: spider_silk_t2 | Glândula de Teia (T2) | AttackSpeedMelee, DashCooldown
  T3: spider_fang_t3 | Presa Venenosa (T3) | AttackSpeedMelee, DashCooldown, DodgeChance
  T4: spider_egg_t4 | Ovo de Aranha (T4) | Teia venenosa ao Dash + Attack Speed buff 3s

🟢 GOBLIN
  T1: goblin_coin_t1 | Moeda Mágica (T1) | SpeedMultiplier
  T2: goblin_trinket_t2 | Trinado Goblin (T2) | SpeedMultiplier, CritChance
  T3: goblin_amulet_t3 | Amuleto Goblin (T3) | SpeedMultiplier, CritChance, BaseDamage
  T4: goblin_bomb_t4 | Bomba Goblin (T4) | Bombas explodem passivamente

🔮 CRYSTAL TUNER
  T1: tuner_shard_t1 | Estilha Sintonizada (T1) | CritChance
  T2: tuner_lens_t2 | Lente Ressonante (T2) | CritChance, CritMultiplier
  T3: tuner_prism_t3 | Prisma Sintonizador (T3) | CritChance, CritMultiplier, SpeedMultiplier
  T4: tuner_matrix_t4 | Matriz Sintonizadora (T4) | Adrenalina ao tomar dano

💎 SHARD SWARM
  T1: shard_splinter_t1 | Estilhaço Cristalino (T1) | MaxHealth
  T2: shard_resonant_t2 | Fragmento Ressonante (T2) | MaxHealth, CritMultiplier
  T3: shard_refract_t3 | Fragmento Refrator (T3) | MaxHealth, CritMultiplier, Thorns
  T4: shard_prismatic_t4 | Fragmento Prismático (T4) | 3 cristais orbitam + destroem projétil

🗿 TOTEM
  T1: totem_stone_t1 | Pedra Totem (T1) | MaxHealth
  T2: totem_carved_t2 | Totem Esculpido (T2) | MaxHealth, Regen
  T3: totem_ancient_t3 | Totem Ancestral (T3) | MaxHealth, Regen, DamageNegation
  T4: totem_monolith_t4 | Monolito Totem (T4) | Forma Totem ao imóvel >1.5s (-50% dano)

👁️ CRYSTAL WATCHER
  T1: watcher_lens_t1 | Lente Vigilante (T1) | WeaponRangeProjectile
  T2: watcher_eye_t2 | Olho Vigilante (T2) | WeaponRangeProjectile, Piercing
  T3: watcher_sight_t3 | Visão Vigilante (T3) | WeaponRangeProjectile, Piercing, CritChance
  T4: watcher_beacon_t4 | Farol Vigilante (T4) | Laser 360° a cada 10s

✨ MAGIC CRYSTAL
  T1: magic_dust_t1 | Pó Arcano (T1) | BaseDamageMultiplier
  T2: magic_rune_t2 | Runa Instável (T2) | BaseDamageMultiplier, DashInvulnerability
  T3: magic_essence_t3 | Essência Mágica (T3) | BaseDamageMultiplier, DashInvulnerability, DashCounts
  T4: magic_catalyst_t4 | Catalisador Mágico (T4) | Skybeam a cada 2s em inimigo aleatório
```

---

## ⚡ **ATALHO: Copiar do Console**

1. Abra **Tools → RogueLike → Mostrar Tabela**
2. Copie a saída do Console
3. Cole em um editor de texto
4. Use como referência para criar os prefabs

---

## 🔗 **PRÓXIMOS PASSOS**

Quando terminar de criar os prefabs:

- [ ] 32 prefabs de items criados
- [ ] Todos com `CharacteristicItemPickup` configurado
- [ ] Cada inimigo com `EnemyDrops` apontando para seus 4 tiers
- [ ] Testar drops em uma run

**Depois** você pode:
- Adicionar os efeitos especiais de T4 (scripts separados)
- Criar um sistema de "Infusão" que aplica os buffs
- Adicionar artworks/sprites para cada item

---

## 📁 **ARQUIVOS CRIADOS**

- ✅ `Assets/Scripts/Editor/DropDataConfig.cs` - Tabela de dados
- ✅ `Assets/Scripts/Editor/DropItemsGenerator.cs` - Helper de editor
- ✅ `Assets/markdowns/DROPS_SETUP.md` - Este guia

**Próximo:** Role para cima e clique em **Tools → RogueLike** para ver as opções!
