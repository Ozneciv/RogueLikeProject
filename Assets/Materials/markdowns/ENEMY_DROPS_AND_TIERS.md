# ⚔️ Sistema de Drops & Tiers — Bioma Cristalizado (GDD 3.7.2 & 3.8.4)

## Princípios do GDD

- **Essência da Vida**: Drop **100% garantido**. Quantidade escala com a força do inimigo.
- **Partes do Corpo**: Drop **probabilístico (RNG)**. São "**Não Identificadas**" — o jogador vê apenas o **Nome** e o **Tier** até gastar Essência para infundir.
- **4 Tiers de Raridade**. Cada Tier dropa uma **parte diferente** do mesmo inimigo.
- **Luck (Drops)**: Atributo do jogador que aumenta a chance de drop E a chance de Tiers superiores.

---

## 📊 Tiers de Raridade (GDD 3.7.2)

| Tier | Nome | Cor | Teto com Amplificação | Natureza |
|------|------|-----|----------------------|----------|
| T1 | Comum | ⬜ Branco | +5% máx | Numérica baixa |
| T2 | Incomum | 🟢 Verde | +8% máx | Numérica robusta |
| T3 | Raro | 🔵 Azul | +10% máx | Numérica potente (multi-atributo) |
| T4 | Lendário | 🟡 Dourado | — | **Alteração drástica de gameplay** |

> T4 é **garantido por Guardiões** (chefes). Custo de infusão e amplificação extremamente altos.

---

## 🕷️ Spider (Aranha) — ⭐⭐

| | |
|---|---|
| HP: 40 · Dano: 10 · Vel: 6 | Essência: 6-10 · Chance de Drop: 25% |

**Temática do Loot:** Velocidade, agilidade e veneno. Partes cada vez mais vitais da aranha.

| Tier | Item | ID | Atributo | Valor Base → Amplificado |
|------|------|----|----------|--------------------------|
| T1 | 🦵 Pata de Aranha | `spider_leg` | Attack Speed (Melee) | +2% → +5% |
| T2 | 🕸️ Glândula de Teia | `spider_silk_gland` | Attack Speed (Melee) | +5% → +8% |
| T3 | 🦷 Presa Venenosa | `spider_fang` | Attack Speed +5% · Dodge +3% | → +7% / +5% |
| T4 | 👁️ Olho da Matriarca | `spider_queen_eye` | **Predadora**: Após esquivar com Dash, seu próximo ataque causa 2x dano e aplica Lentidão ao alvo por 2s. | — |

---

## 🪨 Golem — ⭐⭐⭐

| | |
|---|---|
| HP: 150 · Dano: 35 · Vel: 2 | Essência: 20-30 · Chance de Drop: 40% |

**Temática do Loot:** Defesa, resistência e retaliação. Fragmentos cada vez mais densos.

| Tier | Item | ID | Atributo | Valor Base → Amplificado |
|------|------|----|----------|--------------------------|
| T1 | 🪨 Lasca de Pedra | `golem_chip` | Damage Negation | +2% → +5% |
| T2 | 🧱 Placa de Rocha | `golem_plate` | Damage Negation | +5% → +8% |
| T3 | 💎 Coração de Granito | `golem_granite_heart` | Damage Negation +5% · Armor Regen +4% | → +7% / +6% |
| T4 | 🌋 Núcleo Tectônico | `golem_tectonic_core` | **Inabalável**: Ao receber dano, 30% de chance de ignorar completamente o golpe e devolver 200% do dano como onda sísmica em área (3m). Cooldown de 5s. | — |

---

## 💎 Shard Swarm (Enxame de Fragmentos) — ⭐⭐⭐

| | |
|---|---|
| HP: 60 · Dano: 8-20 · Vel: 5 | Essência: 12-18 · Chance de Drop: 30% |

**Temática do Loot:** Precisão, corte e fragmentação. Cristais cada vez mais puros.

| Tier | Item | ID | Atributo | Valor Base → Amplificado |
|------|------|----|----------|--------------------------|
| T1 | 🔹 Estilhaço Cristalino | `shard_splinter` | Crit Chance | +2% → +5% |
| T2 | 💠 Fragmento Ressonante | `shard_resonant` | Crit Chance | +5% → +8% |
| T3 | 🔷 Prisma Harmônico | `shard_prism` | Crit Chance +5% · Crit Multiplier +0.15x | → +7% / +0.20x |
| T4 | ⚡ Nexus do Enxame | `shard_nexus` | **Enxame Parasita**: Ao acertar um Crítico, 3 fragmentos cristalinos orbitam sua arma por 8s. Cada fragmento dispara automaticamente no inimigo mais próximo (50% do dano base). | — |

---

## ✨ MagicStone (Pedra Mágica) — ⭐⭐⭐

| | |
|---|---|
| HP: 80 · Dano: 25 · Vel: 4 | Essência: 15-25 · Chance de Drop: 35% |

**Temática do Loot:** Energia, ULT e regeneração. Runas cada vez mais instáveis.

| Tier | Item | ID | Atributo | Valor Base → Amplificado |
|------|------|----|----------|--------------------------|
| T1 | ✳️ Pó Arcano | `magic_dust` | Ult Charge with Kills | +2% redução CD → +5% |
| T2 | 🔮 Runa Instável | `magic_rune` | Ult Charge with Kills | +5% redução CD → +8% |
| T3 | 💜 Cristal Canalizado | `magic_crystal` | Ult Charge +5% · Ult Damage +4% | → +7% / +6% |
| T4 | 🌀 Essência Primordial | `magic_primordial` | **Sobrecarga Arcana**: Sua ULT não tem mais cooldown fixo. Em vez disso, cada inimigo morto carrega 10% da barra. Ao ativar, a ULT consome TODA a sua Armadura atual, adicionando o valor consumido como dano extra à habilidade. | — |

---

## 🔮 Crystal Tuner (Sintonizador Cristalino) — ⭐⭐⭐⭐

| | |
|---|---|
| HP: ~100 · Dano: ~20 · Vel: 3 | Essência: 25-35 · Chance de Drop: 45% |

**Temática do Loot:** Alcance, ressonância e amplificação. Componentes do sistema nervoso cristalino.

| Tier | Item | ID | Atributo | Valor Base → Amplificado |
|------|------|----|----------|--------------------------|
| T1 | 📡 Estilha Sintonizada | `tuner_shard` | Weapon Range | +2% → +5% |
| T2 | 🔊 Lente Ressonante | `tuner_lens` | Weapon Range | +5% → +8% |
| T3 | 📢 Amplificador Cristalino | `tuner_amplifier` | Weapon Range +5% · Magnet +4% | → +7% / +6% |
| T4 | 🌐 Rede Neural Cristalina | `tuner_neural_net` | **Frequência Dominante**: Seus ataques emitem um pulso sônico que marca inimigos atingidos por 4s. Inimigos marcados recebem +25% de dano de todas as fontes e, ao morrer, transferem a marca para o inimigo mais próximo (em 5m). | — |

---

## 🔄 Relação: Dificuldade → Recompensa

```
Inimigo              Essência   Chance Drop   Tiers Possíveis
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Spider     ⭐⭐        6-10       25%          T1 (60%) T2 (30%) T3 (9%) T4 (1%)
Shard Swarm ⭐⭐⭐      12-18      30%          T1 (50%) T2 (30%) T3 (15%) T4 (5%)
MagicStone ⭐⭐⭐       15-25      35%          T1 (50%) T2 (30%) T3 (15%) T4 (5%)
Golem      ⭐⭐⭐       20-30      40%          T1 (45%) T2 (30%) T3 (18%) T4 (7%)
Crystal Tuner ⭐⭐⭐⭐   25-35      45%          T1 (40%) T2 (30%) T3 (20%) T4 (10%)
Guardião   🏆         50-100     100%         T4 garantido
```

---

## 🧬 Tipos de Melhorias (GDD 3.8.4)

### Numéricas (T1 a T3)

| Categoria | Atributos |
|-----------|-----------|
| **Ofensivo** | Attack Speed, Crit Chance, Crit Multiplier, Weapon Range, Knockback, Piercing, MultiShot, Spread |
| **Defensivo** | Damage Negation, Dodge, Armor Regen, Thorns |
| **Mobilidade** | Speed, Dash Cooldown, Dash Counts, Dash Invulnerability |
| **Economia** | Luck (Essence), Luck (Drops), Magnet |
| **ULT** | CD Reduction, Charge with Kills, Radius, Damage, Duration, Buff Potency |

### Mecânicas — T4 Lendário (Altera Gameplay)

| Inimigo | Efeito T4 | Estilo de Build |
|---------|-----------|-----------------|
| Spider | **Predadora** — Dash → 2x dano + Lentidão | Hit & Run agressivo |
| Golem | **Inabalável** — Chance de negar golpe + retaliação AoE | Tanque retaliador |
| Shard Swarm | **Enxame Parasita** — Crits geram fragmentos autônomos | DPS por Crítico |
| MagicStone | **Sobrecarga Arcana** — ULT sem CD fixo, consome Armadura | ULT-spam / Glass Cannon |
| Crystal Tuner | **Frequência Dominante** — Marca inimigos (+25% dano, propaga) | Controle / Suporte |

---

## 💰 Decisão Estratégica: Infundir ou Esperar?

| Infusão # | Custo | T1 vale? | T2 vale? | T3 vale? | T4 vale? |
|-----------|-------|----------|----------|----------|----------|
| 1ª | 50 | ✅ Sim | ✅ Sim | ✅ Ótimo | ✅ Perfeito |
| 2ª | 100 | ⚠️ Talvez | ✅ Sim | ✅ Ótimo | ✅ Perfeito |
| 3ª | 200 | ❌ Evitar | ⚠️ Talvez | ✅ Sim | ✅ Perfeito |
| 5ª | 400 | ❌ Nunca | ❌ Evitar | ⚠️ Talvez | ✅ Sim |
| 10ª | 1000 | ❌ | ❌ | ❌ Evitar | ✅ Único caso |

> A **Amplificação (Overcharge)** do GDD 3.8.3 permite gastar Essência extra imediatamente após uma infusão para elevar o atributo até o teto do Tier.
