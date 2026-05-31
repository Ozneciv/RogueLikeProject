# Contexto do Script: IA do Dragão Comportamento (Unity)

Você deve agir como um desenvolvedor Unity sênior especialista em IA e Gameplay. Preciso de um script em C# para controlar o comportamento de um dragão (mob comum) utilizando uma Máquina de Estados (State Machine) simples. O script deve ser limpo, performático (evitando excesso de buscas no `Update`) e usar a física/transform do Unity de forma inteligente.

---

## 1. Configurações e Variáveis Principais

### **Alvo**

* `Transform player` (identificado via Tag `"Player"` no `Start` ou injetado).

### **Movimentação**

* `float velocidadeVoo`: Velocidade de deslocamento.
* `float alturaFixa`: Altura ideal de voo (onde o player consegue alcançá-lo com ataques corpo a corpo/projéteis padrão).
* `float distanciaIdeal = 3.0f`: O "Range de Spike" de 3 metros que o dragão tenta manter.

### **Ataques**

* `Transform pointDisparo`: Ponto de origem dos projéteis.
* `GameObject projectilePrefab`: Prefab do projétil.
* `float cooldownAtaque`: Tempo entre os ataques de projétil.
* `float raioTail = 2.0f`: Alcance do ataque de 360º (Tail).

---

## 2. Estados da IA (Enum)

```csharp
public enum EstadoDragao
{
    AjustarDistancia, // Tentando ficar exatamente a 3 metros do player
    Fugir,            // Se o player chegar perto demais (menos de 3 metros)
    AtacarProjetil,   // Parado, disparando os 3 projéteis
    AtaqueTail        // Ataque de emergência 360º se o player flanquear por trás
}
```

---

# 3. Regras de Negócio e Comportamento

## Voo e Altura

O dragão deve manter sempre a sua posição `Y` travada na `alturaFixa` em relação ao chão ou diretamente calculada sobre o `Y` do Player, garantindo que ele plane na altura exata onde o player consiga acertá-lo.

---

## Máquina de Estados no Update / FixedUpdate

### Verificação de Flanco (Rabada de Emergência)

Antes de qualquer ação:

* calcular a distância;
* calcular o ângulo entre o dragão e o player.

Se:

* o player estiver muito próximo (dentro de `raioRabada`);
* e o ângulo (usando `Vector3.Dot` ou `Vector3.Angle`) indicar que o player está atrás do dragão;

Então:

* transicionar imediatamente para o estado `AtaqueRabada`.

---

## Manutenção de Distância (Range de 3m)

### Aproximação

Se a distância horizontal for maior que `3.5f`:

* o dragão se move em direção ao player até atingir a zona de 3 metros.

### Recuo

Se a distância horizontal for menor que `2.8f`:

* o dragão entra em estado de `Fugir`,
* movendo-se na direção oposta ao player para reestabelecer o range de 3 metros.

---

## Ataque de Projétil

Quando o dragão estiver estabilizado no range de ~3 metros e o cooldown permitir:

1. ele para de se mover;
2. executa uma Corrotina;
3. dispara 3 projéteis sequenciais na direção do player;
4. utiliza um leve delay entre os disparos;
5. reseta o cooldown;
6. volta a monitorar a distância.

---

# 4. Estrutura do Código Esperada

## Movimentação

* Use `Vector3.MoveTowards` ou `Rigidbody.MovePosition` para movimentação suave.
* Manter sempre o eixo `Y` corrigido.

---

## Ataque de Projétil

* Use uma `Coroutine` para o disparo dos 3 projéteis.
* Não travar o loop principal.

---

## Ataque de Rabada

O ataque de rabada deve:

* acionar um gatilho de animação (`Animator.SetTrigger`);
* aplicar dano em área (`Physics.OverlapSphere`).

---

# Objetivo Final

Gerar o código C# completo:

* documentado;
* performático;
* pronto para ser anexado ao `GameObject` do Dragão;
* utilizando boas práticas de arquitetura para IA simples em Unity.
