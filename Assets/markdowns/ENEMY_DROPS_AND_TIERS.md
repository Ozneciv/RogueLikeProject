# ⚔️ Sistema de Drops & Tiers — Relação Completa (GDD 3.7.2 & 3.8.4)

## Visão Geral

Cada inimigo dropa **Essência da Vida** (moeda, 100% garantido) e tem chance probabilística de dropar **Partes do Corpo** (loot). As Partes do Corpo possuem **Tiers de raridade** que definem a força dos atributos concedidos ao infundir na arma.

---

## 📊 Tabela de Tiers de Raridade

| Tier | Nome | Cor | Chance Base | Multiplicador de Atributo | Slots de Atributo |
|------|------|-----|-------------|---------------------------|-------------------|
| T1 | Comum | ⬜ Branco | ~60% | x1.0 | 1 |
| T2 | Incomum | 🟢 Verde | ~25% | x1.5 | 1 |
| T3 | Raro | 🔵 Azul | ~10% | x2.0 | 1-2 |
| T4 | Épico | 🟣 Roxo | ~4% | x3.0 | 2 |
| T5 | Lendário | 🟡 Dourado | ~1% | x4.0+ | 2-3 (+ chance de efeito mecânico) |

> [!NOTE]
> O atributo **Luck (Drops)** do jogador (GDD 3.4) aumenta a probabilidade global de drops E a chance de virem com Tiers superiores.

---

## 🕷️ Drops por Inimigo — Bioma Cristalizado

### 1. Spider (Aranha)
| Propriedade | Valor |
|---|---|
| **HP** | 40 |
| **Dano** | 10 |
| **Velocidade** | 6 |
| **Dificuldade** | ⭐⭐ |
| **Essência** | 6-10 (base 8, variação ±2) |
| **Chance de Drop** | 25% |
| **Parte do Corpo** | 🕸️ **Teia de Aranha** (`spider_silk`) |

#### Atributos por Tier — Teia de Aranha

| Tier | Atributo Principal | Valor | Atributo Secundário (se houver) |
|------|-------------------|-------|---------------------------------|
| T1 | Attack Speed (Melee) | +3% | — |
| T2 | Attack Speed (Melee) | +5% | — |
| T3 | Attack Speed (Melee) | +7% | Dodge +2% |
| T4 | Attack Speed (Melee) | +10% | Dodge +4% |
| T5 | Attack Speed (Melee) | +14% | Dodge +6% · Efeito: **Fio Infinito** (cada acerto consecutivo +0.1% dano, reseta ao tomar dano) |

---

### 2. Golem
| Propriedade | Valor |
|---|---|
| **HP** | 150 |
| **Dano** | 35 |
| **Velocidade** | 2 |
| **Dificuldade** | ⭐⭐⭐ |
| **Essência** | 20-30 (base 25, variação ±5) |
| **Chance de Drop** | 40% |
| **Parte do Corpo** | 🪨 **Núcleo de Pedra** (`golem_core`) |

#### Atributos por Tier — Núcleo de Pedra

| Tier | Atributo Principal | Valor | Atributo Secundário (se houver) |
|------|-------------------|-------|---------------------------------|
| T1 | Damage Negation | +3% | — |
| T2 | Damage Negation | +5% | — |
| T3 | Damage Negation | +7% | Armor Regen +5% |
| T4 | Damage Negation | +10% | Armor Regen +8% |
| T5 | Damage Negation | +14% | Armor Regen +12% · Efeito: **Thorns** (devolve 15% do dano recebido por contato) |

---

### 3. Shard Swarm (Enxame de Fragmentos)
| Propriedade | Valor |
|---|---|
| **HP** | 60 (dividido em 4 fragmentos) |
| **Dano** | 8-20 (por fragmento / combinado) |
| **Velocidade** | 5 |
| **Dificuldade** | ⭐⭐⭐ |
| **Essência** | 12-18 (base 15, variação ±3) |
| **Chance de Drop** | 30% |
| **Parte do Corpo** | 💎 **Fragmento Cristalino** (`crystal_shard`) |

#### Atributos por Tier — Fragmento Cristalino

| Tier | Atributo Principal | Valor | Atributo Secundário (se houver) |
|------|-------------------|-------|---------------------------------|
| T1 | Crit Chance | +2% | — |
| T2 | Crit Chance | +4% | — |
| T3 | Crit Chance | +6% | Crit Multiplier +0.10x |
| T4 | Crit Chance | +8% | Crit Multiplier +0.18x |
| T5 | Crit Chance | +12% | Crit Multiplier +0.25x · Efeito: **Vampirismo de Essência** (chance de dropar Essência extra ao matar com Crítico) |

---

### 4. MagicStone (Pedra Mágica)
| Propriedade | Valor |
|---|---|
| **HP** | 80 |
| **Dano** | 25 |
| **Velocidade** | 4 |
| **Dificuldade** | ⭐⭐⭐ |
| **Essência** | 15-25 (base 20, variação ±5) |
| **Chance de Drop** | 35% |
| **Parte do Corpo** | ✨ **Essência Mágica** (`magic_essence`) |

#### Atributos por Tier — Essência Mágica

| Tier | Atributo Principal | Valor | Atributo Secundário (se houver) |
|------|-------------------|-------|---------------------------------|
| T1 | Ult Charge with Kills | +3% redução CD | — |
| T2 | Ult Charge with Kills | +5% redução CD | — |
| T3 | Ult Charge with Kills | +7% redução CD | Ult Damage +5% |
| T4 | Ult Charge with Kills | +10% redução CD | Ult Damage +8% |
| T5 | Ult Charge with Kills | +14% redução CD | Ult Damage +12% · Efeito: **Toxina Viral** (acertos aplicam veneno que se espalha se o alvo morrer infectado) |

---

### 5. Crystal Tuner (Sintonizador Cristalino)
| Propriedade | Valor |
|---|---|
| **HP** | ~100 (estimado — líder local) |
| **Dano** | ~20 |
| **Velocidade** | 3 |
| **Dificuldade** | ⭐⭐⭐⭐ (buffa aliados) |
| **Essência** | 25-35 (base 30, variação ±5) |
| **Chance de Drop** | 45% |
| **Parte do Corpo** | 🔮 **Fragmento Sintonizador** (`tuner_fragment`) |

#### Atributos por Tier — Fragmento Sintonizador

| Tier | Atributo Principal | Valor | Atributo Secundário (se houver) |
|------|-------------------|-------|---------------------------------|
| T1 | Weapon Range | +5% | — |
| T2 | Weapon Range | +8% | — |
| T3 | Weapon Range | +12% | Magnet +10% |
| T4 | Weapon Range | +16% | Magnet +15% |
| T5 | Weapon Range | +20% | Magnet +20% · Efeito: **Bounce** (+1 ricochete em projéteis / +1 Piercing em melee) |

---

## 🔄 Relação: Dificuldade do Inimigo → Qualidade do Drop

```
Dificuldade do Inimigo     Essência     Chance Drop     Chance Tier Alto
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Spider (⭐⭐)                Baixa        25%             Baixa
Shard Swarm (⭐⭐⭐)          Média        30%             Média
MagicStone (⭐⭐⭐)           Média-Alta   35%             Média
Golem (⭐⭐⭐)                Alta         40%             Média-Alta
Crystal Tuner (⭐⭐⭐⭐)       Muito Alta   45%             Alta
```

> [!IMPORTANT]
> Quanto mais difícil o inimigo, **mais Essência** ele dropa E **maior a chance** de dropar uma Parte do Corpo de **Tier superior**. Isso incentiva o jogador a enfrentar inimigos mais perigosos ao invés de farmar Spiders.

---

## 🧬 Tipos de Melhorias nas Infusões (GDD 3.8.4)

### Numéricas (Stat Boost Direto)

| Categoria | Atributos Possíveis |
|-----------|-------------------|
| **Ofensivo** | Attack Speed, Crit Chance, Crit Multiplier, Weapon Range, Knockback, Piercing, MultiShot Chance, Spread |
| **Defensivo** | Damage Negation, Dodge, Armor Regen, Thorns |
| **Mobilidade** | Speed, Dash Cooldown, Dash Counts, Dash Invulnerability |
| **Economia** | Luck (Essence), Luck (Drops), Magnet |
| **ULT** | Cooldown Reduction, Ult Charge with Kills, Ult Radius, Ult Damage, Ult Effect Duration, Ult Buff Potency |

### Mecânicas (Efeitos Especiais — T4/T5)

| Efeito | Descrição | Melhor com |
|--------|-----------|------------|
| **Fio Infinito** | Cada acerto consecutivo +0.1% dano (acumula ∞, reseta ao tomar dano). HUD mostra stacks. | Alta Attack Speed (Spider) |
| **Toxina Viral** | Acertos aplicam veneno que se espalha para inimigos próximos se o alvo morrer infectado. | Dano em área / DoT builds |
| **Vampirismo de Essência** | Chance de dropar Essência extra ao matar com Crítico. | Alta Crit Chance (Shard Swarm) |
| **Thorns** | Dano devolvido ao atacante ao ser atingido por contato. | Alta Armor / Tanque (Golem) |
| **Bounce/Piercing** | Projéteis ricocheteiam ou golpes atravessam inimigos. | Longo alcance / Crowd control (Crystal Tuner) |

---

## 💰 Custo de Infusão por Tier

| Infusão # | Custo Base | Com Item T1 | Com Item T3 | Com Item T5 |
|-----------|-----------|-------------|-------------|-------------|
| 1ª | 50 | Vale a pena | Ótimo | Excelente |
| 2ª | 100 | Aceitável | Ótimo | Excelente |
| 3ª | 200 | Cuidado | Bom | Excelente |
| 5ª | 400 | ❌ Evitar | Aceitável | Excelente |
| 10ª | 1000 | ❌ Nunca | ❌ Evitar | Vale considerar |

> [!TIP]
> **Estratégia ideal:** Nas primeiras infusões (baratas), aceite T1/T2. Nas tardias (caras), espere T3+ ou compre do Mercador.

---

## ✅ Resumo da Decisão Estratégica do Jogador

```
Matar Inimigo → Coleta Essência (sempre)
                    ↓
              Drop de Parte? (RNG)
                    ↓
              Qual o Tier? (T1-T5)
                    ↓
        ┌─── Infundir agora? (gasta Essência, ganha poder)
        │         ↓
        │    Overcharge? (gasta MUITO mais, buff +40%)
        │
        └─── Guardar/Descartar? (economizar para Tier melhor ou Mercador)
```
