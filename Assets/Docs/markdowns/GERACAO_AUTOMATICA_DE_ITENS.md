# 🤖 Entendendo a Automação de Drops e ItemData

Este documento explica de forma direta como o nosso sistema de Loots (Drops de Inimigos) está integrado estruturalmente à *UI de Infusão* do Player. Se você é um Programador ou de Game Design, leia com atenção.

## 1. O Problema Inicial
No nosso projeto, nós tínhamos dois mundos isolados:
- **Mundo A:** Os PDFs ("Melhorias dos Itens") e o GDD do Isaac, definindo que o Golem no Tier 1 daria *"Armadura Máxima"*, que a Aranha T2 daria *"Velocidade de Ataque"*, etc.
- **Mundo B:** A Unity cobrando que os itens de fato existissem no jogo (os Prefabs que caem no chão) **e**, mais importante, que a *UI de Infusão do Lojista* conseguisse ler matematicamente **o que** aquele item faz!

## 2. A Solução: A Trindade de Configuração

Nós resolvemos isso com uma pipeline em 3 etapas que gera automaticamente os itens. Ninguém precisa desenhar 32 ícones na mão!

### O Cérebro (`DropDataConfig.cs`)
Este script atua como o Banco de Dados puro. Ele é a ponte do nosso PDF. Ele literalmente traduziu "Armadura Máxima" do Golem para C# (`attributes = new List<string> { "MaxArmor" }`).

### Os Drops no Chão (`AutoDropGenerator.cs`)
Ele roda com os dados do Cérebro. Ao ser executado via botão no Editor da Unity, ele gera 32 caixinhas/esferas coloridíssimas e joga nos `EnemyDrops.cs` dos nossos inimigos na aba de LootPools. Os itens caem dos bichos!

### Os Efeitos no Player (`ItemDataAssetGenerator.cs` & `.asset`)
Um Prefab caindo no chão não tem status. Por isso criamos o **ItemData**. 
Nós programamos um Botão de Editor que cria **32 ScriptableObjects independentes**, mapeando 1 por 1 cada drop a uma classe legível para o Inventário de Infusão.
Sem ele, o Player coleteva um Item que o jogo não sabia a quantia de poder que dava.

---

## ⚖️ 3. O Fator Game Design: Como Balancear os Itens?

O gerador já cumpriu o papel mais brutal, criando os 32 itens de uma vez. E ele jogou valores fictícios e "Mudos" (ex: tudo dá `10` de Dano ou `0.05%` de Velocidade, visto que o Isaac só deu o "Nome" da Passiva, mas não a quantia numérica final).

**Sua Missão como Designer agora é testar o Gameplay!**
1. Vá até a pasta fixada de Items: `Assets/_Project/Items_and_Crafting/Items/`.
2. Clique em qualquer arquivo `.asset` (os quadrados com símbolo da Unity). Exemplo: `spider_leg_t1`.
3. Olhe para o lado **Direito da Unity (aba Inspector)**.
4. Lá você verá uma lista bonitinha chamada `Item Attributes`.
5. Modifique os valores o quanto quiser (ex: `value = 2.5` - Multiplicador ligado `checked` = Dando `+2.5%`).

Sempre que a sua build quebrar ou mudarmos algo radical no GDD, a gente só altera o `DropDataConfig` e roda nosso super menu de Gerar Tudo Novamente, reestabelecendo a economia perfeita com um clique!
