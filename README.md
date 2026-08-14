# Living Plerotus
A 3D action-survival roguelite built in Unity centered on tactical adaptation, resource management, and dynamic weapon crafting.
## Visão Geral
Em *Living Plerotus*, o jogador assume o papel de um engenheiro civil preso em um planeta alienígena hospedeiro e consciente. Sem treinamento militar ou armas de fogo convencionais, o protagonista deve utilizar suas ferramentas de trabalho de campo e modificar seu arsenal fundindo minérios e estruturas biológicas extraídas das criaturas nativas.
O ecossistema do planeta atua como um sistema imunológico inteligente. Quando o jogador é derrotado, o solo tenta assimilá-lo, mas falha em digerir a biologia humana. Esse processo de indigestão reconstrói o engenheiro e o expele de volta à zona esterilizada da base, reiniciando o ciclo de exploração.
## Mecânicas Principais
### Ciclo de Infusão e Economia
- **Essência da Vida:** Moeda universal obtida ao derrotar criaturas, utilizada para realizar infusões e negociar com o Mercador.
- **Partes do Corpo:** Componentes biológicos de quatro graus de raridade (Comum, Incomum, Raro e Lendário) coletados durante as explorações.
- **Inflação Progressiva:** Cada infusão realizada aumenta o peso total acumulado, escalonando o custo de Essência para as próximas melhorias e exigindo decisões estratégicas de balanceamento.
### Sistema de Combate e Combos
- **Janelas de Ataque e Input Buffering:** Combate corpo a corpo com sequências telegrafadas, cancelamento tático e janela de recuperação (*recovery frames*).
- **Proteção contra Spam:** Finalizadores de combo desativam o buffer de entrada para evitar ataques involuntários e exigir precisão de tempo.
- **Atributos Ofensivos e Defensivos:** Suporte a velocidade de ataque, alcance, acerto crítico, repulsão (*knockback*), ricochete, perfuração, regeneração de escudo e imunidade temporária (*i-frames*).
### Inventário e Bolsa Sintética
- **Preservação de Recursos:** Equipamentos expostos ao solo são dissolvidos pela biologia do planeta após a morte. Apenas os recursos armazenados dentro da bolsa sintética mantida hermeticamente fechada são preservados entre as rodadas e transferidos para a base.
### O Mercador
- **Trocas de Sangue:** Sacrifício permanente de vida máxima em troca de Essência imediata.
- **Cirurgia de Remoção:** Extração de partes acopladas à arma para reduzir a taxa acumulada de inflação econômica.
- **Cartas de Maldição:** Concessão de poderes passivos de alto impacto acompanhados de penalidades severas.
## Bestiário e Inimigos
- **Inimigos Menores e Atiradores:** Aranha (ataques em salto e recuo tático), Goblin (arremesso de bombas cristalinas e golpes de picareta).
- **Tanques e Suporte:** Golem (ataques rotacionais pesados e ondas de choque em área), Sintonizador de Cristal (unidade voadora que concede amplificações a até três aliados simultaneamente).
- **Elites:** Pedra Mágica, Vigia de Cristal, Enxame de Cristais, Dragão de Cristal, Geobionte (Bismutado) e SharpBlur.
- **Chefe de Bioma:** Boss Cromático (guardião de fronteira cristalino com três fases distintas de combate, mecanismos de invisibilidade por refração e erupções em 360 graus).
## Arquitetura Técnica
- **Engine:** Unity (Universal Render Pipeline - URP)
- **Linguagem:** C#
- **Sistemas Visuais:** TextMeshPro, Toony Colors Pro, Cartoon FX Remaster, KinoBloom
## Estrutura do Repositório
```text
Assets/
├── Docs/               # Documentação técnica e especificações
├── GameAssets/         # Recursos de jogo e modelos 3D
├── _Project/
