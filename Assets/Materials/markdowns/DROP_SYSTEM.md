# 💎 Sistema de Drops de Inimigos

## Visão Geral

Sistema de drops onde inimigos deixam **Essência** (sempre garantida) e **Itens Característicos** (chance aleatória).

---

## 📦 Scripts do Sistema

| Script | Descrição |
|--------|-----------|
| `EnemyDrops.cs` | Componente do inimigo que configura drops |
| `EssencePickup.cs` | Pickup de essência com atração magnética |
| `CharacteristicItemPickup.cs` | Pickup de item único do mob |
| `PlayerEssence.cs` | Contador de essência do player |
| `PlayerInventory.cs` | Inventário de itens coletados |

---

## 🎮 Como Implementar

### 1. Configurar o Player

Adicione ao GameObject do Player:

```
Player
├── PlayerEssence.cs    ← Contador de essência
└── PlayerInventory.cs  ← Inventário de itens
```

### 2. Criar Prefab de Essência

1. Crie um GameObject (esfera pequena)
2. Adicione:
   - `EssencePickup.cs`
   - `SphereCollider` (Is Trigger = ✅)
   - `Rigidbody` (Use Gravity = ✅)
3. Salve como prefab: `Assets/Prefabs/EssencePickup.prefab`

**Configurações sugeridas:**
| Campo | Valor |
|-------|-------|
| Essence Value | 10 |
| Attract Distance | 3 |
| Attract Speed | 8 |
| Lifetime | 30 |

### 3. Criar Prefab de Item Característico

1. Crie um GameObject (cubo ou modelo 3D)
2. Adicione:
   - `CharacteristicItemPickup.cs`
   - `SphereCollider` (Is Trigger = ✅)
   - `Rigidbody` (Use Gravity = ✅, ou false para flutuar)
3. Configure o `Item ID` único
4. Salve como prefab (ex: `SpiderSilkPickup.prefab`)

### 4. Configurar Inimigos

Em cada inimigo com `DummyHealth`:

1. Adicione `EnemyDrops.cs`
2. Arraste o prefab de essência
3. Arraste o prefab do item característico
4. Configure quantidade e chance

---

## 🕷️ Itens por Inimigo

| Inimigo | Item ID | Nome | Buff Futuro |
|---------|---------|------|-------------|
| Spider | `spider_silk` | Teia de Aranha | +Velocidade de ataque |
| Golem | `golem_core` | Núcleo de Pedra | +Defesa |
| Shard Swarm | `crystal_shard` | Fragmento Cristalino | +Dano mágico |
| MagicStone | `magic_essence` | Essência Mágica | +Regeneração |
| Crystal Tuner | `tuner_fragment` | Fragmento Sintonizador | +Alcance |

---

## ⚙️ Variáveis do EnemyDrops

```csharp
[Header("Essência (Sempre Dropa)")]
public GameObject essencePrefab;    // Prefab da essência
public int essenceAmount = 10;      // Quantidade base
public int essenceVariation = 3;    // Variação +/-

[Header("Item Característico (Chance)")]
public GameObject characteristicItemPrefab;  // Prefab do item
public float itemDropChance = 0.3f;          // 30% de chance
public int itemAmount = 1;                   // Quantidade

[Header("Spawn Settings")]
public float spawnHeight = 0.5f;    // Altura do spawn
public float spawnRadius = 0.5f;    // Dispersão
public float launchForce = 3f;      // Impulso para cima
```

---

## 🔧 Integração Automática

O `DummyHealth.Die()` já chama automaticamente:

```csharp
EnemyDrops drops = GetComponent<EnemyDrops>();
if (drops != null)
{
    drops.OnDeath();
}
```

Não precisa fazer nada extra, só adicionar o componente!

---

## 📊 Exemplo de Configuração

### Spider (Drop Fácil)
| Campo | Valor |
|-------|-------|
| Essence Amount | 8 |
| Essence Variation | 2 |
| Item Drop Chance | 0.25 (25%) |

### Golem (Drop Difícil)
| Campo | Valor |
|-------|-------|
| Essence Amount | 25 |
| Essence Variation | 5 |
| Item Drop Chance | 0.40 (40%) |

### Shard Swarm (Drop Médio)
| Campo | Valor |
|-------|-------|
| Essence Amount | 15 |
| Essence Variation | 3 |
| Item Drop Chance | 0.30 (30%) |

---

## ✅ Checklist de Implementação

- [ ] Adicionar `PlayerEssence` ao Player
- [ ] Adicionar `PlayerInventory` ao Player
- [ ] Criar prefab de Essência
- [ ] Criar prefabs de itens para cada mob
- [ ] Adicionar `EnemyDrops` a cada inimigo
- [ ] Testar drops
- [ ] (Opcional) Criar UI de essência
- [ ] (Opcional) Criar sistema de crafting com itens
