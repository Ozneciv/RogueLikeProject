# 🐝 Shard Swarm — Concept Document

## Identidade

**Nome:** Shard Swarm (Enxame Cristalino)  
**Tipo:** Inimigo composto — enxame coletivo  
**Inspiração:** Vespas reais + formações cristalinas refratárias  
**Comportamento-chave:** Age como uma entidade só até ser atacado, quando se divide em dois enxames menores, mais rápidos e agressivos.

---

## Aparência Visual

### Forma Geral — Estado "Colmeia"
O enxame reunido forma uma **esfera imperfeita e pulsante**, como um ninho de vespas feito de cristais. Imagine uma bola de vidro estilhaçado flutuando no ar, com fragmentos girando lentamente ao redor de um **núcleo central brilhante**.

- **Silhueta:** Esférica, irregular, com pontas afiadas saindo dos lados — como um ouriço-do-mar feito de cristal.
- **Tamanho reunido:** ~1.5x o tamanho do player. Imponente, mas não gigantesco.
- **Flutuação:** Paira a ~1m do chão, oscilando suavemente pra cima e pra baixo como se respirasse.

### Os Fragmentos (Cristais individuais)
Cada fragmento é um **cristal hexagonal alongado**, similar a um prisma de quartzo, mas com superfície translúcida e iridescente.

- **Formato:** Prismas hexagonais pontiagudos (~15–20cm cada), como estilhaços de uma gema gigante.
- **Material/Textura:** Superfície translúcida com reflexos iridescentes — ao girar, projeta arco-íris sutis no chão, como um prisma de Newton decompondo luz. A cor base é **violeta-azulado**, com reflexos que variam entre **ciano, magenta e dourado** dependendo do ângulo.
- **Movimento:** Os cristais orbitam o núcleo em padrões elípticos irregulares, girando sobre o próprio eixo. Alguns giram rápido, outros lento — sente-se caótico mas hipnotizante, como um enxame de vagalumes.
- **Quantidade:** ~12–16 fragmentos visíveis quando reunido. Após o split, ~6–8 em cada sub-enxame.

### Núcleo Central
No centro da formação existe um **ponto de luz concentrada**, o "coração" do enxame.

- **Aparência:** Uma esfera pequena de energia pura, brilhante como uma estrela anã — cor **branca-azulada no centro**, com halo **violeta** que pulsa ritmicamente.
- **Comportamento:** O núcleo pulsa como uma batida cardíaca (~1 pulso/segundo). Quando o enxame "percebe" o player, o pulso acelera visivelmente.
- **Trail:** Deixa um rastro sutil de partículas luminosas atrás de si ao se mover, como poeira de estrela.

### Efeitos Visuais Ambientais
- **Refração de luz:** Ao passar em frente a fontes de luz, os cristais quebram a luz em pequenos arco-íris projetados no chão e paredes próximas.
- **Zumbido visual:** Os fragmentos emitem um brilho pulsante tênue, como eletricidade estática — pequenos arcos de energia conectam cristais próximos ocasionalmente.
- **Aura:** Uma névoa cristalina sutil (~50% transparência) em volta do enxame, como vapor gelado que sai de gelo seco.

---

## Aparência Pós-Split (Clones menores)

Quando o enxame se divide:

| Propriedade | Enxame Original | Clone (filho) |
|---|---|---|
| **Escala** | 100% | ~70% do original |
| **Velocidade** | Normal | +40% mais rápido |
| **Cor do núcleo** | Branco-azulado | Levemente avermelhado (mais agressivo) |
| **Pulso** | ~1/seg | ~2/seg (mais frenético) |
| **Fragmentos** | 12–16 | 6–8 |
| **HP** | Metade do restante | Metade do restante |
| **Pode dividir?** | Não (já dividiu) | Não |

- Os clones são **menores e visivelmente mais erráticos** — os cristais orbitam mais rápido e mais apertados.
- A cor muda sutilmente de **azul-violeta** para **violeta-avermelhado**, sinalizando agressividade aumentada.
- O rastro de partículas fica mais intenso e caótico.

---

## Comportamento (resumo para referência visual)

1. **Idle:** Flutua suavemente em patrulha, fragmentos orbitando em câmera lenta. Parece pacífico à distância.
2. **Alerta:** Ao detectar o player, os cristais aceleram a órbita e o núcleo pulsa mais rápido. Um zumbido crescente.
3. **Ataque:** Os fragmentos disparam em rajada na direção do player como um enxame de vespas — voam, acertam, e retornam à formação.
4. **Split:** Ao receber dano suficiente, o enxame explode em um flash de luz. Dois enxames menores emergem da explosão, se separando lateralmente. Efeito visual de "mitose" — o núcleo se estica, brilha intensamente, e se parte em dois.
5. **Morte:** Explosão cristalina — fragmentos são ejetados em todas as direções e se dissolvem em partículas luminosas antes de tocar o chão.

---

## Prompt Sugerido para Geração com IA

> **Para o estado reunido:**  
> "A floating swarm of iridescent hexagonal crystal shards orbiting a glowing blue-white core of energy, resembling a crystalline wasp nest suspended in midair. The crystals are translucent violet-blue prisms that refract light into rainbow patterns. Ethereal mist surrounds the formation. Dark fantasy game enemy, 3D render style, dramatic lighting, dark background."

> **Para o estado dividido (dois enxames menores):**  
> "Two smaller swarms of iridescent crystal shards, each orbiting their own reddish-violet energy core. The crystals orbit faster and more erratically than normal. They appear agitated and aggressive, trailing bright particle effects. Dark fantasy game enemy, 3D render, dynamic pose, action scene."

> **Para um fragmento individual:**  
> "A single elongated hexagonal crystal prism, translucent violet-blue with iridescent rainbow reflections on its surface. Glowing faintly with internal energy. Floating, slightly tilted. Clean dark background, 3D game asset style, high detail."
