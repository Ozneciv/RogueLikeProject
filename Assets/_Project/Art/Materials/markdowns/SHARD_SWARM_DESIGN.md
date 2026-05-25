# 💎 Shard Swarm (Enxame de Fragmentos)

## Visão Geral

O **Shard Swarm** é uma criatura coletiva formada por múltiplos fragmentos de cristal que flutuam e agem como uma única entidade. São manifestações defensivas do planeta, criadas quando áreas importantes são ameaçadas.

---

## 🎨 Design Visual

### Aparência
- **Composição**: 3-5 fragmentos de cristal flutuantes
- **Tamanho**: Cada fragmento tem ~30cm, enxame total ~2m de diâmetro
- **Cor**: Cristal translúcido com núcleo brilhante (azul/roxo)
- **Conexão**: Fios de energia visíveis conectando os fragmentos
- **Movimento**: Flutuam em padrão orbital ao redor de um centro invisível

### Efeitos Visuais
| Estado | Efeito |
|--------|--------|
| Idle | Brilho suave pulsante, movimento lento |
| Agressivo | Brilho intenso, rotação acelerada |
| Atacando | Trail de luz ao mover, som de cristal |
| Dividindo | Flash de luz, fragmentos se separam |
| Morrendo | Estilhaça em partículas menores |

### Sons
- **Idle**: Zumbido cristalino baixo (como sino de vento)
- **Ataque**: Som agudo de cristal cortando o ar
- **Hit**: Som de vidro rachando
- **Morte**: Estilhaçar melodioso

---

## 📖 Lore & Simbiose

### Origem
Os Shard Swarms são fragmentos do próprio planeta que ganharam consciência coletiva. Quando uma área sagrada ou um depósito de cristal é ameaçado, o planeta "desperta" esses fragmentos para defender sua integridade.

### Comportamento Natural
- **Territoriais**: Patrulham áreas com alta concentração de cristais
- **Comunicação**: Compartilham informações através da rede cristalina do planeta
- **Simbiose**: Alimentam-se da energia ambiente, devolvendo-a purificada ao solo
- **Ciclo de vida**: Eventualmente retornam ao planeta, fundindo-se novamente à terra

### Hierarquia
```
Consciência do Planeta
        ↓
   Crystal Tuner (líder local)
        ↓
   Shard Swarm (sentinelas)
        ↓
   Fragmentos individuais
```

---

## ⚔️ Mecânicas de Combate

### Stats Base

| Atributo | Valor | Notas |
|----------|-------|-------|
| HP Total | 60 | Dividido entre fragmentos |
| HP por Fragmento | ~15 | 4 fragmentos = 60 HP |
| Velocidade | 5.0 | Mais rápido que player |
| Dano por Hit | 8 | Dano por fragmento |
| Dano Combinado | 20 | Ataque em grupo |

### Habilidades

#### 1. **Swarm Attack (Ataque em Enxame)**
- **Tipo**: Ataque básico
- **Mecânica**: Todos os fragmentos voam em direção ao player
- **Dano**: 5 por fragmento que acerta
- **Cooldown**: 2 segundos

#### 2. **Split (Divisão)**
- **Tipo**: Reação passiva
- **Trigger**: Ao receber dano pesado (>20 HP de uma vez)
- **Mecânica**: Fragmentos se separam, cada um vira uma unidade independente
- **Duração**: 5 segundos separados, depois reagrupam

#### 3. **Crystal Reform (Reagrupamento)**
- **Tipo**: Passiva
- **Mecânica**: Fragmentos separados se reagrupam após tempo
- **Bônus**: Ao reagrupar, recupera 10% do HP máximo
- **Condição**: Só funciona se ao menos 2 fragmentos estão vivos

#### 4. **Resonance Burst (Explosão Ressonante)** - Ao Morrer
- **Tipo**: On-Death
- **Mecânica**: Último fragmento explode em área
- **Dano**: 15 em raio de 3m
- **Visual**: Flash de luz + partículas

---

## 🎯 Padrão de Comportamento (FSM)

```
┌─────────────────────────────────────────────────────┐
│                     PATROL                          │
│  (Flutua em área, procura player)                   │
└─────────────────┬───────────────────────────────────┘
                  │ Player detectado (15m)
                  ▼
┌─────────────────────────────────────────────────────┐
│                    APPROACH                         │
│  (Voa em direção ao player, mantém formação)        │
└─────────────────┬───────────────────────────────────┘
                  │ Distância < 5m
                  ▼
┌─────────────────────────────────────────────────────┐
│                SWARM_ATTACK                         │
│  (Fragmentos atacam em sequência ou juntos)         │
└────────┬────────────────────────────┬───────────────┘
         │ Dano pesado recebido       │ Ataque completo
         ▼                            ▼
┌─────────────────┐          ┌────────────────────────┐
│     SPLIT       │          │       RETREAT          │
│ (Separa em 4)   │          │  (Recua 8m, reagrupa)  │
└────────┬────────┘          └───────────┬────────────┘
         │ 5 segundos                    │
         ▼                               │
┌─────────────────┐                      │
│    REFORM       │◄─────────────────────┘
│ (Reagrupa)      │
└────────┬────────┘
         │
         ▼
    [APPROACH]
```

---

## 🎮 Estratégias do Jogador

### Como Derrotar
1. **Hit and Run**: Atacar e recuar antes do contra-ataque
2. **Ataques em Área**: Eliminar múltiplos fragmentos de uma vez
3. **Priorizar Reagrupamento**: Matar fragmentos antes de reformarem
4. **Evitar Explosão**: Afastar-se quando restar 1 fragmento

### Counters
| Estratégia do Player | Efetividade |
|---------------------|-------------|
| Ataques rápidos | ⭐⭐⭐ Alta - Mata fragmentos antes de reagrupar |
| Ataques AoE | ⭐⭐⭐⭐ Muito Alta - Mata vários de uma vez |
| Dash através | ⭐⭐ Média - Evita dano mas não elimina |
| Focar um fragmento | ⭐ Baixa - Outros continuam atacando |

---

## 🔧 Implementação Técnica

### Componentes Necessários

```
ShardSwarm (GameObject Pai)
├── ShardSwarm_AI.cs (Script principal)
├── Rigidbody (UseGravity = false)
├── SphereCollider (Trigger, para detecção)
│
├── Shard_1 (Fragmento filho)
│   ├── MeshRenderer (cristal)
│   ├── SphereCollider (hitbox)
│   └── TrailRenderer (rastro visual)
│
├── Shard_2
├── Shard_3
└── Shard_4
```

### Variáveis Principais

```csharp
[Header("Fragmentos")]
public int shardCount = 4;
public int hpPerShard = 15;
public float orbitRadius = 1.5f;
public float orbitSpeed = 2f;

[Header("Movimento")]
public float moveSpeed = 5f;
public float detectionRange = 15f;
public float attackRange = 5f;

[Header("Ataque")]
public float attackCooldown = 2f;
public int damagePerShard = 5;
public int combinedDamage = 20;

[Header("Split")]
public float splitThreshold = 20f; // Dano para triggerar split
public float splitDuration = 5f;
public float reformHealPercent = 0.1f;

[Header("Morte")]
public float deathExplosionRadius = 3f;
public int deathExplosionDamage = 15;
```

---

## 📊 Balanceamento

### Comparação com Outros Inimigos

| Inimigo | HP | Dano | Velocidade | Dificuldade |
|---------|----|----- |------------|-------------|
| Spider | 40 | 10 | 6 | ⭐⭐ |
| Golem | 150 | 35 | 2 | ⭐⭐⭐ |
| MagicStone | 80 | 25 | 4 | ⭐⭐⭐ |
| **Shard Swarm** | 60 | 8-20 | 5 | ⭐⭐⭐ |

### Sinergias com Crystal Tuner
- **Quando buffado pelo Tuner**: Fragmentos ganham +50% de dano
- **Prioridade de conexão**: Alta (mesma "família" cristalina)
- **Visual buffado**: Conexões de energia ficam douradas

---

## ✅ Checklist de Implementação

- [ ] Criar modelo 3D dos fragmentos
- [ ] Script ShardSwarm_AI.cs
- [ ] Sistema de fragmentos individuais
- [ ] Lógica de Split/Reform
- [ ] Efeitos visuais (trail, brilho, conexões)
- [ ] Efeitos sonoros
- [ ] Integração com Crystal Tuner (buff)
- [ ] Explosão ao morrer
- [ ] Testes de balanceamento
- [ ] Spawn no Level Generator
