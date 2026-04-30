# 🆕 Como Criar Novos Itens no Inventário

Guia passo a passo para adicionar novos itens ao sistema de inventário.

---

## Passo 1 — Criar o ScriptableObject do Item

1. Na aba **Project**, navegue até `Assets/Data/Items/` (ou crie essa pasta)
2. Clique com botão direito → **Create** → **Inventory** → **Item Data**
3. Renomeie o arquivo para algo descritivo (ex: `SpiderSilk`, `GolemChip`)

---

## Passo 2 — Preencher os Dados no Inspector

Selecione o `ItemData` que acabou de criar e preencha no **Inspector**:

| Campo | O que colocar | Exemplo |
|-------|---------------|---------|
| **Item Id** | ID único, igual ao do CharacteristicItemPickup | `spider_silk` |
| **Item Name** | Nome de exibição | `Teia de Aranha` |
| **Description** | Texto do tooltip | `Seda resistente produzida pela aranha` |
| **Icon** | Sprite do ícone (arraste da pasta de sprites) | *(pode deixar vazio por enquanto)* |
| **Tier** | Raridade: Common, Uncommon, Rare ou Legendary | `Common` |
| **Enemy Source** | Inimigo que dropa esse item | `Spider` |

> ⚠️ **IMPORTANTE**: O **Item Id** precisa ser **idêntico** ao campo `itemId` do `CharacteristicItemPickup` no prefab do drop do inimigo. Se não bater, o item aparece no inventário mas sem ícone/nome.

---

## Passo 3 — Registrar no ItemDatabase

1. Na **Hierarchy**, selecione o GameObject que tem o componente `ItemDatabase`
2. No Inspector, na lista **All Items**, clique no **"+"**
3. Arraste o ScriptableObject que você criou para o novo slot

```
┌─ ItemDatabase (Script) ──────────────┐
│                                       │
│  All Items                            │
│  ┌───────────────────────────────┐   │
│  │  Element 0: SpiderSilk        │   │
│  │  Element 1: GolemChip         │   │
│  │  Element 2: ShardSplinter     │   │
│  │  Element 3: SeuNovoItem  ← +  │   │
│  └───────────────────────────────┘   │
│                                       │
└───────────────────────────────────────┘
```

---

## Passo 4 — Configurar o Drop no Inimigo

Se o inimigo ainda não dropa esse item:

1. Selecione o **prefab do inimigo** na aba Project
2. Verifique se tem o componente `EnemyDrops`
3. No campo **Characteristic Item Prefab**, arraste o prefab do pickup
4. O prefab do pickup precisa ter o componente `CharacteristicItemPickup` com o mesmo `itemId`

```
┌─ Prefab do Pickup ───────────────────┐
│                                       │
│  CharacteristicItemPickup (Script)    │
│  ┌───────────────────────────────┐   │
│  │  Item Id:    spider_silk       │   │  ← Igual ao ItemData
│  │  Item Name:  Teia de Aranha    │   │
│  │  Item Description: ...         │   │
│  └───────────────────────────────┘   │
│                                       │
│  SphereCollider (Is Trigger ✓)        │
│  Rigidbody                            │
│                                       │
└───────────────────────────────────────┘
```

---

## Resumo Rápido

```
1. Project → Create → Inventory → Item Data
2. Preencher: itemId, nome, descrição, ícone, tier
3. Arrastar para a lista "All Items" do ItemDatabase
4. Garantir que o prefab de drop tem o mesmo itemId
```

---

## Referência de Tiers

| Tier | Nome | Cor da Borda |
|------|------|-------------|
| Common | Comum | ⬜ Branco/Cinza |
| Uncommon | Incomum | 🟩 Verde |
| Rare | Raro | 🟦 Azul |
| Legendary | Lendário | 🟨 Dourado |

---

## Referência de Itens Existentes (GDD)

| Item Id | Nome | Inimigo | Tier |
|---------|------|---------|------|
| `spider_leg` | Pata de Aranha | Spider | Common |
| `spider_silk_gland` | Glândula de Teia | Spider | Uncommon |
| `spider_fang` | Presa Venenosa | Spider | Rare |
| `spider_queen_eye` | Olho da Matriarca | Spider | Legendary |
| `golem_chip` | Lasca de Pedra | Golem | Common |
| `golem_plate` | Placa de Rocha | Golem | Uncommon |
| `golem_granite_heart` | Coração de Granito | Golem | Rare |
| `golem_tectonic_core` | Núcleo Tectônico | Golem | Legendary |
| `shard_splinter` | Estilhaço Cristalino | Shard Swarm | Common |
| `shard_resonant` | Fragmento Ressonante | Shard Swarm | Uncommon |
| `shard_prism` | Prisma Harmônico | Shard Swarm | Rare |
| `shard_nexus` | Nexus do Enxame | Shard Swarm | Legendary |
| `magic_dust` | Pó Arcano | MagicStone | Common |
| `magic_rune` | Runa Instável | MagicStone | Uncommon |
| `magic_crystal` | Cristal Canalizado | MagicStone | Rare |
| `magic_primordial` | Essência Primordial | MagicStone | Legendary |
| `tuner_shard` | Estilha Sintonizada | Crystal Tuner | Common |
| `tuner_lens` | Lente Ressonante | Crystal Tuner | Uncommon |
| `tuner_amplifier` | Amplificador Cristalino | Crystal Tuner | Rare |
| `tuner_neural_net` | Rede Neural Cristalina | Crystal Tuner | Legendary |
