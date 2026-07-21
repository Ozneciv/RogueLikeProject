# 🎮 GAME DESIGN DOCUMENT (GDD) — BIOMA CRISTALIZADO

**Projeto:** RogueLikeProject  
**Última Atualização:** 21/07/2026  
**Status do Jogo:** Versão 1.4 — Balanceamento Oficial Sincronizado

---

## 🛡️ 1. Atributos do Jogador (Player Balance)

| Atributo | Valor Oficial | Descrição / Mecânica |
| :--- | :---: | :--- |
| **HP Máximo (Health)** | **100 HP** | Barra de vida física do personagem. |
| **Armadura Máxima (Armor)** | **300 AP** | **Escudo regenerável.** Total de **400 EHP** (*Effective HP*). |
| **Taxa de Regeneração de Armadura** | **5 AP / seg** | Inicia após 4.0 segundos sem receber dano. |
| **Dano Base Multiplier** | **1.0x** | Multiplicador inicial limpo. |
| **Chance de Acerto Crítico** | **5.0%** | Chance padrão inicial de causar dano crítico (1.5x). |
| **Negação de Dano / Esquiva** | **0.0%** | Sem mitigação passiva inicial. |

---

## ⚔️ 2. Armas do Jogador (Combos & Danos)

### 🗡️ Adaga (Dagger) — Ataques Rápidos
* **Velocidade de Ataque:** 1.0x (Padrão)
* **Tempo de Reset de Combo:** 1.2 segundos

| Hit do Combo | Dano Base | Dano com Crítico (1.5x) |
| :---: | :---: | :---: |
| **Hit 1** | **30** | 45 |
| **Hit 2** | **35** | 52 |
| **Hit 3 (Final)** | **40** | 60 |
| **TOTAL COMBO** | **105 Dano** | **157 Dano** |

### 🪓 Machado (Axe) — Ataques Pesados
* **Velocidade de Ataque:** 0.85x (Pesado)
* **Tempo de Reset de Combo:** 1.8 segundos

| Hit do Combo | Dano Base | Dano com Crítico (1.5x) |
| :---: | :---: | :---: |
| **Hit 1** | **40** | 60 |
| **Hit 2** | **45** | 67 |
| **Hit 3** | **55** | 82 |
| **Hit 4 (Final)** | **55** | 82 |
| **TOTAL COMBO** | **195 Dano** | **291 Dano** |

---

## 👾 3. Tabela Geral de Inimigos (HP, Dano & Drop Base)

| Inimigo | Categoria (Pontos) | HP Base | Dano Base Ataques | Drop Essência Base ($d$) |
| :--- | :---: | :---: | :--- | :---: |
| **Aranha** | Mob Menor (1 pt) | **35 HP** | 25 (Pulo) | **2** |
| **SharpBlur** | Mob Menor (1 pt) | **70 HP** | 20 (Melee) / 15 (Dash) | **2** |
| **Goblin** | Atirador (2 pts) | **95 HP** | 30 (Bomba) / 15 (Picareta) | **4** |
| **Cristaldrag (Dragão)** | Atirador (2 pts) | **75 HP** | 8 (Spike) / 16 (Tail) / 20 (Charge) | **4** |
| **Crystal Watcher** | Atirador (2 pts) | **80 HP** | 20 (Tick) | **4** |
| **Crystal Tuner** | Suporte (3 pts) | **55 HP** | Buffs de Área | **4** |
| **Cristalus** | Suporte (3 pts) | **45 HP** | Invocação / Apoio | **4** |
| **Golem** | Tanque (4 pts) | **220 HP** | 30 (Melee) + Stun | **8** |
| **Pedra Mágica** | Tanque (4 pts) | **195 HP** | 40 (Skybeam) | **8** |
| **Shard Swarm (Gen 0)** | Tanque (4 pts) | **180 HP** | 25 (Contato) | **8** |
| **Totem** | Tanque (4 pts) | **150 HP** | Disparos de Suporte | **8** |
| **Geobionte (Bismutado)** | Elite (10 pts) | **250 HP** | 40 (Sweep) | **20** |
| **Geobionte (Sentinela)** | Elite (10 pts) | **400 HP** | 50 (Slam) | **20** |

---

## 📈 4. Fórmulas de Escalonamento Procedural

### 📈 Escalonamento de HP dos Inimigos por Sala:
$$HP(n) = HP_{base} \times (1 + 0.03 \times (n - 1))$$
*A cada sala avançada $n$, a vida de todos os inimigos aumenta **+3%**.*

### 💎 Escalonamento de Drop de Essências por Sala:
$$E(n) = d \times (1 + 0.05 \times n)$$
*A essência dropada por cada inimigo aumenta **+5% por sala**.*

### 🎯 Orçamento de Spawn por Sala (Difficulty Budget):
$$P(n) = 5 + 0.65 \times n$$
* **Salas 1 e 2:** Limitadas a **1 onda leve** (2 a 3 mobs menores) para curva de aprendizado suave.

---

## 🎒 5. Tabela de Drops de Itens & Raridades

* **Chance Global de Drop de Item:** 30% por inimigo derrotado.
* **Tiers de Raridade:** Tier 1 (Comum - Peso 60), Tier 2 (Raro - Peso 30), Tier 3 (Épico - Peso 10), Tier 4 (Lendário - Peso 3).
