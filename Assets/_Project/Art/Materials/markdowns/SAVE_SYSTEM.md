# 💾 Sistema de Save, Bolsa Sintética e Inventários

## Visão Geral

O jogo possui **dois espaços de armazenamento de itens** com comportamentos distintos:

| Espaço | Script | Persiste entre runs? | Chave |
|---|---|---|---|
| **Inventário de Run** | `PlayerInventory.cs` | ❌ Limpo na morte | `returnsToBase = false` |
| **Bolsa Sintética** | `SaveManager` + `SyntheticBagUI` | ✅ Salvo em disco | `returnsToBase = true` |

---

## 🗂️ Arquivos Envolvidos

| Script | Pasta | Responsabilidade |
|---|---|---|
| `SaveManager.cs` | `Scripts/Save/` | Leitura/escrita de JSON, cache de dados persistentes |
| `SyntheticBagUI.cs` | `Scripts/Inventory/` | UI da Bolsa Sintética (tecla B) |
| `PlayerInventory.cs` | `Scripts/Player/` | Inventário temporário de run |
| `InventoryUI.cs` | `Scripts/Inventory/` | UI do inventário de run (tecla Tab) |
| `ItemPickup.cs` | `Scripts/Rest/` | Roteamento de coleta por `returnsToBase` |
| `CraftingManager.cs` | `Scripts/Crafting/` | Lê ingredientes das duas fontes |

---

## 🔁 Fluxo de Dados

```
Item coletado (ItemPickup.cs)
       │
       ├─ returnsToBase == true  ──▶ SaveManager.AddResourceToBase()
       │                                    │
       │                                    ▼
       │                             CachedData.baseResources
       │                                    │
       │                             OnBaseResourcesChanged event
       │                                    │
       │                             SyntheticBagUI.RefreshUI()
       │
       └─ returnsToBase == false ──▶ PlayerInventory.AddItem()
                                            │
                                            ▼
                                     InventoryUI atualiza
```

---

## 📂 Arquivo de Save

**Caminho:**
```
%AppData%\..\LocalLow\DefaultCompany\RogueLProject\player_progress.json
```

**Estrutura JSON:**
```json
{
    "inventoryMaxSlots": 10,
    "baseResources": [
        { "itemId": "po_de_cristal", "quantity": 5 }
    ],
    "purchasedUpgradeIndices": [0, 2],
    "craftedEquipmentIds": ["Equip_Amulet"],
    "equippedEquipmentIds": ["Equip_Amulet"]
}
```

---

## 💀 Save na Morte do Player

Chamado por `PlayerHealth.Die()`:

```
PlayerHealth.Die()
    └── SaveManager.OnPlayerDied(player)
            ├── Remove itens de run (returnsToBase == false) do PlayerInventory
            └── SavePersistentData() → grava JSON em disco
```

**O que é PRESERVADO após a morte:**
- `baseResources` (Bolsa Sintética)
- `purchasedUpgradeIndices` (upgrades comprados)
- `craftedEquipmentIds` / `equippedEquipmentIds`
- `inventoryMaxSlots`

**O que é DESCARTADO após a morte:**
- Todos os itens do `PlayerInventory` com `returnsToBase == false`

---

## 🚪 Save no Fechamento do Jogo

Automático via `OnApplicationQuit()` no `SaveManager`:

```csharp
void OnApplicationQuit()
{
    SavePersistentData();
}
```

---

## 🔄 Carregamento (Load)

Chamado por `GameManager.RegisterPlayer()` ao registrar o player na cena:

```
GameManager.RegisterPlayer(player)
    └── SaveManager.LoadPersistentData(player)
            ├── Lê JSON do disco → preenche _cachedData
            ├── Restaura PlayerInventory.SetMaxSlots()
            ├── Restaura PlayerUpgrades.BuyUpgrade() para cada índice salvo
            └── OnBaseResourcesChanged?.Invoke() → SyntheticBagUI.RefreshUI()
```

---

## 🎒 Bolsa Sintética (SyntheticBagUI)

- Toggle: **tecla B**
- Singleton `DontDestroyOnLoad`
- Subscreve `SaveManager.OnBaseResourcesChanged` no `OnEnable()`
- Atualiza automaticamente sempre que um item é adicionado/removido via `SaveManager`

**Métodos do SaveManager para a Bolsa Sintética:**

| Método | Descrição |
|---|---|
| `AddResourceToBase(itemId, amount)` | Adiciona item + dispara evento |
| `RemoveResourceFromBase(itemId, amount)` | Remove item + dispara evento |
| `GetBaseResourceCount(itemId)` | Retorna quantidade atual |
| `GetAllBaseResources()` | Retorna lista completa |

---

## 🛠️ Crafting com Múltiplas Fontes

O `CraftingManager` verifica ingredientes nas **duas fontes** antes de craftar:

```
CanCraft() / Craft()
    ├── GetTotalIngredientCount(itemId)
    │       ├── SaveManager.GetBaseResourceCount(itemId)  ← Bolsa Sintética
    │       └── PlayerInventory.GetItemCount(itemId)      ← Inventário de Run
    │
    └── Consumo (ordem de prioridade):
            1. Inventário de Run primeiro
            2. Bolsa Sintética com o restante
```

---

## ⚙️ Setup na Cena

Os seguintes GameObjects precisam existir na cena inicial (ex: `Eptinho`):

| GameObject | Componente | Observação |
|---|---|---|
| `SaveManager` | `SaveManager.cs` | DontDestroyOnLoad |
| `GameManager` | `GameManager.cs` | DontDestroyOnLoad |
| `SyntBag` | `SyntheticBagUI.cs` | DontDestroyOnLoad, UI ativa/inativa |
| `Player` | `PlayerInventory.cs` | Registrado via `GameManager.RegisterPlayer()` |

---

## 🐛 Problemas Comuns

| Problema | Causa | Solução |
|---|---|---|
| Bolsa Sintética não atualiza após load | `LoadPersistentData` não disparava `OnBaseResourcesChanged` | Corrigido: evento é invocado ao final do load |
| Item some sem ir a lugar nenhum | `ItemPickup` não tinha `return` nos guards de null | Corrigido: guards com `return` adicionados |
| Crafting ignora itens do inventário de run | `CraftingManager` só lia `SaveManager` | Corrigido: dual-source com `GetTotalIngredientCount` |
| SaveManager não encontrado | Objeto não existia na cena inicial | Adicionar `SaveManager` GameObject + componente na cena `Eptinho` |
