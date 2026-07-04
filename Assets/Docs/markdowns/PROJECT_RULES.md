# Project Rules

Este documento resume as regras de implementação para novos inimigos e sistemas de combate no projeto.

## Regras Gerais
- Reuse a arquitetura já existente. Não crie novos sistemas de dano, vida ou movimento.
- Antes de adicionar ou alterar um inimigo, inspecione os scripts atuais em `Assets/_Project/Scripts/Enemy/`.
- Use as mesmas convenções dos inimigos existentes: `Spider_AI.cs`, `Golem_AI.cs`, `CrystalWatcher_AI.cs`, `ShardSwarm_AI.cs`.

## Vida e Dano
- O sistema de vida existente para inimigos é `DummyHealth`.
- Para causar dano a um inimigo, use `DummyHealth.TakeDamage(int damage, bool isCritical = false)`.
- Não crie uma nova interface de dano (`IDamageable`) ou novo sistema de saúde, a menos que já exista no repositório.
- Sempre use `RequireComponent(typeof(DummyHealth))` em scripts de inimigo que precisam receber dano.

## Buffs e Status
- Para alterações de status ou buffs, use a lógica existente de `DummyHealth.SetBuffedStatus(bool)` e os componentes já presentes no projeto.
- Não crie temporizadores personalizados para aplicar ou remover buffs/debuffs.
- Siga os exemplos do projeto para aplicar status:
  - `CrystalTuner.cs` (buff em aliados)
  - `TotemSpawner.cs` (buff interno ligado/desligado)
  - `MagicStone.cs` (estado de buff variável)

## Movimento e AI
- Reuse os sistemas de movimento/ataque existentes em vez de criar novos controles de física.
- Não substitua o uso de Rigidbody/Collider se o inimigo pode ser implementado com o padrão existente.

## Animações
- Crie e conecte `Animator Controller` apenas quando o modelo tiver animações.
- Use o padrão de `Animator` já existente no projeto para transições de Idle/Walk/Attack/Die.

## Dicas importantes
- Se um arquivo ou funcionalidade não existir no projeto atual, não invente novas dependências.
- Quando estiver em dúvida, copie o padrão de um inimigo já implementado e ajuste para o novo caso.
