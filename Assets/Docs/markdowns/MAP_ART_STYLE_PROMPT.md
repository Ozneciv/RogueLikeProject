# Guia de Prompt: Artes de Mapa Iso-2D

Sempre que precisar voltar a gerar novas salas ou props para os mapas do RogueLike e quiser manter EXATAMENTE o mesmo estilo visual, siga este passo a passo:

## 1. A Imagem Âncora (Obrigatório)
**NUNCA** peça uma nova arte de mapa apenas por texto. Sempre faça o upload de uma das artes finais aprovadas (ex: a sala de zigzag sem brilho, ou a barricada matte).
- Se quiser **apenas o estilo** mas um formato novo: Diga explicitamente para não usar a geometria da imagem.
- Se quiser **o mesmo formato**: Envie um esboço (sketch) usando as cores sólidas (verde para raízes, vermelho para cristais) ou um recorte colado.

## 2. O Prompt Mestre (Copie e Cole)

Cole o texto abaixo no chat sempre que for pedir uma variação:

> "A top-down isometric 2D game asset on a pure white background. 
> 
> **ESTRUTURA:** [Escreva aqui o que você quer: Ex: Faça uma sala redonda / Use a imagem anexa como base inalterável do formato / Preencha os buracos desenhados].
> 
> **ESTILO (OBRIGATÓRIO):** Mantenha estritamente o estilo chapado (matte), indie 2D isométrico, com contornos pretos, cores pastéis e SEM efeito de glow/neon brilhante nos cristais. Chão de pedra rachada roxa/índigo, raízes negras e musgo verde-água. A arte DEVE parecer pintada à mão e não um render 3D hiper-realista."

## 3. O que fazer se a IA errar?
Se a IA colocar brilho (glow), esticar o mapa para fora da tela, ou mudar o estilo de pintura:
1. Pare e não tente arrumar apenas por texto ("tira o glow").
2. Pegue a imagem que você gostou da estrutura, abra no Paint, faça as marcações com cores neon onde deve mudar.
3. Envie novamente junto com o Prompt Mestre acima.
