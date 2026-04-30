# 🎒 Sistema de Inventário Visual

## Visão Geral

Sistema de inventário em grade (estilo Minecraft) com **10 slots expansíveis**, tooltip de hover e integração com o sistema de drops existente.

---

## 📦 Scripts do Sistema

| Script | Pasta | Descrição |
|--------|-------|-----------|
| `ItemData.cs` | `Scripts/Inventory/` | ScriptableObject que define um item (ícone, nome, tier) |
| `ItemDatabase.cs` | `Scripts/Inventory/` | Singleton de lookup por itemId |
| `InventoryUI.cs` | `Scripts/Inventory/` | Controlador principal da UI + Input (Tab) |
| `InventorySlotUI.cs` | `Scripts/Inventory/` | Componente visual de cada slot |
| `InventoryTooltip.cs` | `Scripts/Inventory/` | Tooltip flutuante ao hover |
| `PlayerInventory.cs` | `Scripts/Player/` | Lógica do inventário (refatorado com slots) |

---

## 🔧 Como Configurar

### 1. Criar os ScriptableObjects de Itens

Para cada item do jogo:

1. No Unity: `Assets > Create > Inventory > Item Data`
2. Configure:
   - **Item Id**: Deve ser idêntico ao `itemId` do `CharacteristicItemPickup` (ex: `spider_silk`)
   - **Item Name**: Nome de exibição (ex: "Teia de Aranha")
   - **Description**: Descrição para o tooltip
   - **Icon**: Sprite do item
   - **Tier**: Common, Uncommon, Rare ou Legendary
   - **Enemy Source**: Nome do inimigo de origem

**Itens do jogo (referência):**

| Item ID | Nome | Inimigo | Tier |
|---------|------|---------|------|
| `spider_leg` | Pata de Aranha | Spider | Common |
| `spider_silk_gland` | Glândula de Teia | Spider | Uncommon |
| `spider_fang` | Presa Venenosa | Spider | Rare |
| `golem_chip` | Lasca de Pedra | Golem | Common |
| `golem_plate` | Placa de Rocha | Golem | Uncommon |
| `golem_granite_heart` | Coração de Granito | Golem | Rare |
| `shard_splinter` | Estilhaço Cristalino | Shard Swarm | Common |
| `shard_resonant` | Fragmento Ressonante | Shard Swarm | Uncommon |
| `magic_dust` | Pó Arcano | MagicStone | Common |
| `magic_rune` | Runa Instável | MagicStone | Uncommon |
| `tuner_shard` | Estilha Sintonizada | Crystal Tuner | Common |
| `tuner_lens` | Lente Ressonante | Crystal Tuner | Uncommon |

### 2. Configurar o ItemDatabase

1. Adicione o componente `ItemDatabase` a um GameObject persistente (ex: GameManager)
2. Arraste **todos** os ScriptableObjects de ItemData para a lista "All Items"

### 3. Configurar o Player

Adicione ao GameObject do Player:
```
Player
├── PlayerInventory.cs   ← Já existe (agora com maxSlots)
└── (outros componentes existentes)
```

### 4. Configurar a UI do Inventário

1. Crie um GameObject na cena com um **Canvas**
2. Adicione o componente `InventoryUI` ao Canvas (ou filho)
3. Pronto! A UI se cria automaticamente

---

## 🎮 Controles

| Tecla | Ação |
|-------|------|
| `Tab` | Abre/fecha o inventário |
| `ESC` | Fecha o inventário |
| Mouse hover | Mostra tooltip do item |
| Botão X | Fecha o inventário |

---

## 📐 Layout da Grade

```
┌──────────────────────────────────────┐
│          INVENTÁRIO (3/10)            │
├──────────────────────────────────────┤
│  ┌────┐  ┌────┐  ┌────┐  ┌────┐  ┌────┐  │
│  │ 🕷️ │  │ 🪨 │  │ 💎 │  │    │  │    │  │
│  │ x5 │  │ x2 │  │ x1 │  │    │  │    │  │
│  └────┘  └────┘  └────┘  └────┘  └────┘  │
│  ┌────┐  ┌────┐  ┌────┐  ┌────┐  ┌────┐  │
│  │    │  │    │  │    │  │    │  │    │  │
│  └────┘  └────┘  └────┘  └────┘  └────┘  │
└──────────────────────────────────────┘
```

- Slots **ocupados**: Ícone + quantidade + borda colorida por Tier
- Slots **vazios**: Fundo escuro com borda sutil
- **Cores dos Tiers**: Branco (Comum) | Verde (Incomum) | Azul (Raro) | Dourado (Lendário)

---

## 🔮 Expansão do Inventário (Uso Futuro)

Para aumentar o número de slots (ex: upgrade na base):

```csharp
// Em qualquer script que tenha referência ao PlayerInventory
PlayerInventory inventory = player.GetComponent<PlayerInventory>();
inventory.IncreaseMaxSlots(5); // Adiciona 5 slots (10 → 15)
```

A UI se reconstrui automaticamente com o novo número de slots.

Métodos disponíveis:
- `IncreaseMaxSlots(int amount)` — Adiciona N slots à capacidade atual
- `SetMaxSlots(int newMax)` — Define diretamente o total de slots

---

## 🔗 Integração com Drops

O fluxo completo funciona automaticamente:

```
Inimigo morre
    → EnemyDrops.OnDeath()
        → Spawna CharacteristicItemPickup
            → Player colide com item
                → CharacteristicItemPickup.CollectItem()
                    → PlayerInventory.AddItem()
                        → Se sucesso: destrói pickup, evento onInventoryChanged
                        → Se cheio: NÃO destrói pickup (fica no chão)
                            → InventoryUI.RefreshUI() atualiza a grade
```

---

## ✅ Checklist

- [ ] Criar ScriptableObjects para cada item do jogo
- [ ] Adicionar sprites/ícones para cada item
- [ ] Adicionar `ItemDatabase` ao GameManager
- [ ] Adicionar `InventoryUI` a um Canvas na cena
- [ ] Testar coleta de itens
- [ ] Testar inventário cheio (item fica no chão)
- [ ] Testar Tab para abrir/fechar
- [ ] Testar tooltip no hover
