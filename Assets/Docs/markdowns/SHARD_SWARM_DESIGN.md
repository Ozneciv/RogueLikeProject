# 💎 Shard Swarm (Estrela de Fragmentos) - GDD & Especificação de Design

## 1. Visão Geral

O **Shard Swarm** (apelidado de **Estrela**) é um inimigo ranged/estratégico formado por um **Núcleo Central (CORE)** e **5 Espinhos de Cristal Flutuantes** dispostos em simetria radial. É uma entidade quântico-cristalina territorial que ataca à distância com projéteis ioiô energizados e se defende com uma barreira de retaliação elétrica.

---

## 🎨 2. Design Visual & Anatomia

### Aparência & Estrutura
- **Núcleo (CORE)**: Cubo/Cristal central translúcido que flutua suavemente e gira em torno do próprio eixo ($200^\circ/\text{s}$).
- **Espinhos (Pontas)**: 5 pontas de cristal dispostas ao redor do Core em órbita tridimensional.
- **Barra de Vida (`HealthBar_Canvas`)**: Canvas de UI acoplado com o componente `FaceCamera`, mantendo a barra de vida perfeitamente reta, estável e encarando a câmera sem girar com o modelo.
- **Escudo Holográfico (`Escudo`)**: Esfera de holograma acoplada ao prefab (`Shader Graphs/hologramShader`) que aciona um **Fade In (0.12s) e Fade Out (0.23s)** suave ao receber impacto enquanto estiver no modo protegido.

---

## 🌊 3. Navegação Orgânica 2D (Sem Circundamento Repetitivo)

Diferente de inimigos como o *Crystal Tuner* que circundam o jogador em órbitas mecânicas, o Shard Swarm navega usando um algoritmo de **Movimentação Orgânica 2D**:

- **Eixo Longitudinal (Frente/Trás)**: Combina **Perlin Noise 2D** com oscilações senoidais (`SineWaveFrequency = 1.2`, `SineWaveAmplitude = 3.5`), mantendo o mob flutuando dinamicamente entre **7.0m e 11.0m** de distância do jogador.
- **Eixo Lateral (Strafe Orgânico)**: Aplica ruído Perlin com cosseno suave (`SideDriftAmplitude = 3.0`), realizando desvios laterais imprevisíveis para a esquerda e direita sem nunca formar um círculo mecânico.
- **Desincronização de Enxame**: Cada instância calcula sementes de ruído únicas (`randomSeedX`, `randomSeedY`), garantindo que múltiplos Shard Swarms na mesma sala se movam com personalidades totalmente independentes.

---

## ⚔️ 4. Mecânicas de Combate: Escudo de Espinhos vs. Núcleo Exposto

### 🛡️ A. Forma Unida (Escudo de Espinhos Fechado)
Quando o Shard Swarm está com as 5 pontas encaixadas ao redor do Core:

| Mecânica | Efeito / Valor | Descrição |
| :--- | :---: | :--- |
| **Armadura do Escudo** | **50% Redução de Dano** (`0.5x`) | O escudo espinhoso absorve metade de todo o dano recebido. |
| **Retaliação de Contato** | **8 Dano Físico + Knockback (6f)** | Tentar encostar ou bater de perto (< 1.3m) causa retaliação instantânea e empurrão. |
| **Status Eletrocutado** | **50% Slow + 5 Dano/s (3s)** | O contato com o escudo aplica o debuff elétrico por 3 segundos. |
| **Feedback de Impacto** | **Pulso de Holograma (Fade In/Out)** | O mesh `Escudo` pulsa suavemente para indicar o bloqueio. |

---

### 🎯 B. Ataque Telegrafado & Disparo Ioiô
O ataque principal é dividido em fases claras de **Risco x Recompensa**:

1. **Wind-Up Amplo (Telegrafo Visual)**:
   - A Estrela para, encara o jogador e expande suas 5 pontas para **$2.2\times$ de amplitude**.
   - Segura essa pose bem aberta por **0.85s**, telegrafando claramente o ataque para o jogador poder se esquivar.
2. **Disparo dos Projéteis (60m/s)**:
   - Os projéteis voam a $60\text{ m/s}$ até a distância máxima de $25\text{ m}$, causando **Dano Físico Direto (10 HP)** ao atingir o jogador.
3. **Pausa Congelada no Ar (1.2s)**:
   - Ao atingir o alcance máximo, todos os projéteis ficam congelados/vibrando no ar por **1.2s** antes do retorno.
4. **Retorno Sequencial Aleatório**:
   - Os espinhos retornam **um a um em ordem aleatória** (embaralhamento Fisher-Yates com intervalo de 0.35s).
   - Ao chegarem na órbita, executam o **Giro de Reencaixe (Wheel Spin $360^\circ$ a $360^\circ/\text{s}$)** ao redor do pivô exato do CORE.

---

### 💥 C. Janela de Vulnerabilidade Crítica (Núcleo Exposto)
Durante o disparo e retorno dos espinhos (*Disparando / Reagrupando*):

- **Núcleo Nú e Indefeso**: O Core perde a proteção dos espinhos. O contato direto com o Core passa a ser 100% seguro (sem dano de retaliação nem knockback).
- **Dano Crítico (+50% Bônus)**: Qualquer ataque que atingir o Core nu causa **1.5x Dano Crítico** (disparando textos flutuantes de Dano Crítico).
- **Perímetro Seguro (3.0m)**: Não são gerados rastros elétricos a menos de 3.0m do Core, mantendo a zona de punição corporal totalmente limpa e segura.
- **Destruição das Pontas**: Ao derrotar o Core, todas as 5 pontas (mesmo que estejam desanexadas no ar) são **destruídas instantaneamente**, sem deixar restos na cena.

---

## ⚡ 5. Descargas Elétricas 3D no Ar (`ElectricTrailVFX`)

- **Efeito Visual**: Filamento 3D ionizado e serrilhado de $0.35\text{ m}$ gerado atrás das pontas em voo. Possui Additive Glow ciano neon e ruído elétrico (*flicker*) a cada $50\text{ ms}$.
- **Duração**: Permanece ativo no espaço tridimensional por **3.0 segundos** antes de dissipar.
- **Debuff de Eletrocussão**:
  - Pisar ou cruzar o rastro aplica **50% de redução na velocidade de movimento do player (`PlayerM`)** e **5 Dano/s**.
  - **Renovação de Timer**: Sempre que o jogador pisa no rastro novamente, o timer de 3 segundos do status de eletrocussão é **100% restaurado**.

---

## ⚙️ 6. Tabela de Parâmetros Recomendados (`ShardSwarm_AI.cs`)

| Parâmetro | Valor Padrão | Descrição |
| :--- | :---: | :--- |
| `moveSpeed` | `3.5` | Velocidade de movimentação orgânica flutuante |
| `sineWaveFrequency` | `1.2` | Frequência da onda de avanço/recuo |
| `sineWaveAmplitude` | `3.5` | Amplitude da variação de distância longitudinal |
| `sideDriftAmplitude` | `3.0` | Amplitude dos desvios de strafe lateral |
| `projectileSpeed` | `60.0` | Velocidade de voo dos projéteis ($m/s$) |
| `maxProjectileDistance` | `25.0` | Alcance máximo do disparo ($m$) |
| `windUpExpansionMultiplier` | `2.2` | Amplitude de abertura do telegrafo antes de atirar |
| `windUpHoldDuration` | `0.85` | Duração da pausa de aviso com a estrela aberta ($s$) |
| `spikeAirHoverTime` | `1.2` | Tempo congelado pairando no ar antes do retorno ($s$) |
| `spikeReturnInterval` | `0.35` | Intervalo entre a chamada de retorno de cada pino ($s$) |
| `wheelSpinSpeedDegreesPerSec` | `360.0` | Velocidade de rotação do giro de reencaixe ($^\circ/s$) |
| `wheelSpinPeriodicInterval` | `30.0` | Intervalo para o Giro de Roda acontecer por estética ($s$) |
| `trailDuration` | `3.0` | Duração de permanência da descarga elétrica no ar ($s$) |
| `spikeShieldHazardRadius` | `1.3` | Raio da zona de retaliação dos espinhos fechados ($m$) |
| `spikeShieldDamageMultiplier` | `0.5` | Multiplicador de dano sofrido com escudo (50% armadura) |
| `exposedCoreDamageMultiplier` | `1.5` | Multiplicador de dano sofrido com Core nu (1.5x Crítico) |
