using UnityEngine;
using System.Collections;

/// <summary>
/// IA do Crystal Watcher (Vigia de Cristal) — Inimigo estático que dispara um laser giratório
/// Inspirado no Brimstone do Hades.
/// 
/// COMO FUNCIONA:
/// 1. O inimigo fica parado no chão (estático)
/// 2. Quando o player se aproxima, ele começa a "carregar" (efeito visual)
/// 3. Após o carregamento, um laser sai do cristal e gira seguindo o player
/// 4. O laser sempre gira pelo caminho mais curto até o player
/// 5. Se o player dashar pro outro lado, o laser TEM QUE INVERTER a rotação
/// 6. O laser causa dano contínuo se tocar no player
/// 7. Após alguns segundos, o laser desliga e o ciclo recomeça
/// 
/// COMPONENTES NECESSÁRIOS no GameObject:
/// - DummyHealth (vida, barra de HP, morte e drops)
/// - EnemyDrops (sistema de loot)
/// - Collider (para o player poder atacar)
/// - Rigidbody (kinematic, pois é estático)
/// </summary>
[RequireComponent(typeof(DummyHealth))]
public class CrystalWatcher_AI : MonoBehaviour
{
    // =============================================
    // REFERÊNCIAS (preenchidas automaticamente)
    // =============================================
    private Transform playerTransform;   // Referência ao player
    private DummyHealth health;          // Componente de vida
    private CrystalWatcherVFX vfx;       // Efeitos visuais de partículas

    // =============================================
    // ATIVAÇÃO — quando o inimigo "acorda"
    // =============================================
    [Header("Ativação")]
    [Tooltip("Distância em que o inimigo detecta o player e começa a atacar")]
    public float activationDistance = 20f;
    private bool isActivated = false;    // Guardamos se já foi ativado

    // =============================================
    // CICLO DE ATAQUE — tempos do ciclo
    // =============================================
    [Header("Ciclo de Ataque")]
    [Tooltip("Tempo de carregamento antes do laser disparar (em segundos)")]
    public float chargeTime = 0.5f;

    [Tooltip("Duração do laser ativo (em segundos)")]
    public float fireDuration = 4.0f;

    [Tooltip("Pausa entre ciclos de ataque (em segundos)")]
    public float cooldownTime = 2.0f;

    // =============================================
    // LASER — configurações do laser
    // =============================================
    [Header("Laser")]
    [Tooltip("Dano causado por cada tick do laser")]
    public int laserDamage = 15;

    [Tooltip("Intervalo entre ticks de dano (em segundos). Menor = mais dano por segundo")]
    public float damageTickRate = 0.3f;

    [Tooltip("Velocidade de rotação do laser em graus por segundo")]
    public float rotationSpeed = 30f;

    [Tooltip("Alcance máximo do laser")]
    public float laserRange = 20f;

    [Tooltip("Largura do laser para detecção de colisão")]
    public float laserWidth = 0.5f;

    // =============================================
    // BUFF (quando Crystal Tuner está conectado)
    // =============================================
    [Header("Buff")]
    private bool isBuffed = false;
    private float originalChargeTime;
    private float originalRotationSpeed;

    // =============================================
    // ESTADO INTERNO — controle do ciclo
    // =============================================
    // Enum = lista de estados possíveis (como um semáforo: vermelho, amarelo, verde)
    private enum State
    {
        Idle,       // Dormindo, esperando o player se aproximar
        Charging,   // Carregando energia antes de disparar
        Firing,     // Laser ativo, girando e causando dano
        Cooldown    // Descansando entre ataques
    }
    private State currentState = State.Idle;

    // Controle do laser visual
    private LineRenderer laserLine;           // O componente que desenha a linha do laser
    private float currentLaserAngle = 0f;     // Ângulo atual do laser (em graus, no plano XZ)
    private float damageTimer = 0f;           // Timer para controlar ticks de dano
    private Renderer modelRenderer;            // Renderer do modelo 3D (para achar o centro)

    // =============================================
    // START — Inicialização (roda uma vez quando o jogo começa)
    // =============================================
    void Start()
    {
        // Pega o componente de vida que está no mesmo GameObject
        health = GetComponent<DummyHealth>();

        // Encontra o player na cena pela tag "Player"
        // IMPORTANTE: o player precisa ter a tag "Player" no Unity!
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
        else
        {
            Debug.LogError("[WATCHER] Player não encontrado! Verifique a tag 'Player'.");
        }

        // Cria o LineRenderer para o laser (o componente que desenha linhas na tela)
        SetupLaserVisual();

        // Busca o Renderer do modelo 3D filho para encontrar o centro exato
        modelRenderer = GetComponentInChildren<Renderer>();

        // Configura VFX de partículas (adiciona se não existir)
        vfx = GetComponent<CrystalWatcherVFX>();
        if (vfx == null)
        {
            vfx = gameObject.AddComponent<CrystalWatcherVFX>();
        }
    }

    // =============================================
    // UPDATE — Roda TODO FRAME (60x por segundo)
    // =============================================
    void Update()
    {
        // Se não achou o player, não faz nada
        if (playerTransform == null) return;

        // Se o inimigo morreu, não faz nada
        if (health != null && health.CurrentHealth <= 0)
        {
            // Desliga o laser ao morrer
            if (laserLine != null) laserLine.enabled = false;
            return;
        }

        // -------- MÁQUINA DE ESTADOS --------
        // Dependendo do estado atual, executa uma lógica diferente
        switch (currentState)
        {
            case State.Idle:
                HandleIdle();
                break;

            case State.Charging:
                // Charging é controlado pela Coroutine, não faz nada aqui
                break;

            case State.Firing:
                HandleFiring();
                break;

            case State.Cooldown:
                // Cooldown é controlado pela Coroutine, não faz nada aqui
                break;
        }
    }

    // =============================================
    // ESTADO: IDLE (dormindo)
    // =============================================
    void HandleIdle()
    {
        // Calcula a distância entre o inimigo e o player
        float distToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        // Se o player está perto o suficiente, ATIVA!
        if (distToPlayer < activationDistance)
        {
            isActivated = true;
            Debug.Log("[WATCHER] Ativado! Player detectado a " + distToPlayer.ToString("F1") + "m");

            // Inicia o ciclo de ataque (Coroutine = função que pode "pausar" e esperar)
            StartCoroutine(AttackCycle());
        }
    }

    // =============================================
    // CICLO DE ATAQUE (Coroutine)
    // =============================================
    // Coroutine é como uma receita de bolo:
    // "Faça X, espere 2 segundos, faça Y, espere mais 3 segundos..."
    // Sem coroutine, tudo rodaria instantaneamente sem esperas.
    IEnumerator AttackCycle()
    {
        // Loop infinito — o ciclo repete até o inimigo morrer
        while (health != null && health.CurrentHealth > 0)
        {
            // --- FASE 1: CARREGAMENTO ---
            currentState = State.Charging;
            Debug.Log("[WATCHER] Carregando laser...");

            // Aponta o laser na direção do player ANTES de carregar
            Vector3 dirToPlayer = (playerTransform.position - transform.position);
            dirToPlayer.y = 0; // Ignora altura (laser é horizontal)
            currentLaserAngle = Mathf.Atan2(dirToPlayer.x, dirToPlayer.z) * Mathf.Rad2Deg;

            // Gira o modelo para encarar o player durante o carregamento
            transform.rotation = Quaternion.Euler(0, currentLaserAngle, 0);

            // Mostra o laser fraquinho durante o carregamento (feedback visual)
            ShowLaserCharging(true);

            // Ativa partículas de carregamento
            if (vfx != null)
            {
                vfx.StartChargeEffect();
                vfx.SetAmbientIntensity(3f); // Aura mais intensa
            }

            // Espera o tempo de carregamento
            yield return new WaitForSeconds(chargeTime);

            // --- FASE 2: DISPARANDO ---
            currentState = State.Firing;
            damageTimer = 0f;
            Debug.Log("[WATCHER] LASER ATIVO!");

            // Para carregamento e ativa partículas do laser
            if (vfx != null)
            {
                vfx.StopChargeEffect();
                vfx.StartLaserEffect(currentLaserAngle);
                vfx.SetAmbientIntensity(5f); // Aura bem intensa durante disparo
            }

            // Mostra o laser com força total
            ShowLaserFiring(true);

            // O laser fica ativo por 'fireDuration' segundos
            // A lógica de rotação e dano roda no Update() > HandleFiring()
            yield return new WaitForSeconds(fireDuration);

            // --- FASE 3: DESLIGANDO ---
            ShowLaserFiring(false);
            if (vfx != null)
            {
                vfx.StopLaserEffect();
                vfx.SetAmbientIntensity(1f); // Aura volta ao normal
            }
            Debug.Log("[WATCHER] Laser desligado. Descansando...");

            // --- FASE 4: COOLDOWN ---
            currentState = State.Cooldown;
            yield return new WaitForSeconds(cooldownTime);

            // Verifica se o player ainda está no range
            // Se saiu, volta pro Idle
            float dist = Vector3.Distance(transform.position, playerTransform.position);
            if (dist > activationDistance * 1.5f)
            {
                currentState = State.Idle;
                isActivated = false;
                Debug.Log("[WATCHER] Player saiu do range. Voltando a dormir.");
                yield break; // Sai da Coroutine
            }
        }
    }

    // =============================================
    // ESTADO: FIRING (laser ativo)
    // =============================================
    void HandleFiring()
    {
        // 1. CALCULA PARA ONDE O LASER DEVERIA APONTAR (direção do player)
        Vector3 dirToPlayer = (playerTransform.position - transform.position);
        dirToPlayer.y = 0; // Laser no plano horizontal

        // Converte a direção em ângulo (0-360 graus)
        float targetAngle = Mathf.Atan2(dirToPlayer.x, dirToPlayer.z) * Mathf.Rad2Deg;

        // 2. CALCULA A DIFERENÇA DE ÂNGULO (caminho mais curto)
        // Mathf.DeltaAngle retorna um valor entre -180 e 180
        // Positivo = girar horário, Negativo = girar anti-horário
        // ISSO é o que faz o laser inverter quando o player dá dash pro outro lado!
        float angleDifference = Mathf.DeltaAngle(currentLaserAngle, targetAngle);

        // 3. GIRA O LASER na velocidade configurada
        // Mathf.Sign retorna +1 ou -1 dependendo do sinal
        // Isso garante que o laser gira pelo caminho mais curto
        float maxRotation = rotationSpeed * Time.deltaTime;
        if (Mathf.Abs(angleDifference) > maxRotation)
        {
            // Se falta girar mais do que a velocidade permite, gira o máximo possível
            currentLaserAngle += Mathf.Sign(angleDifference) * maxRotation;
        }
        else
        {
            // Se falta pouco, vai direto pro ângulo do player
            currentLaserAngle = targetAngle;
        }

        // 4. GIRA O MODELO DO CRYSTAL WATCHER junto com o laser
        // Quaternion.Euler cria uma rotação a partir de ângulos
        // Só rotacionamos no eixo Y (horizontal), mantendo X e Z em 0
        transform.rotation = Quaternion.Euler(0, currentLaserAngle, 0);

        // 5. ATUALIZA DIREÇÃO DAS PARTÍCULAS DO LASER
        if (vfx != null) vfx.UpdateLaserDirection(currentLaserAngle);

        // 6. ATUALIZA A POSIÇÃO VISUAL DO LASER
        UpdateLaserVisual();

        // 6. VERIFICA SE O LASER ESTÁ ACERTANDO O PLAYER
        CheckLaserHit();
    }

    // =============================================
    // DETECÇÃO DE COLISÃO DO LASER
    // =============================================
    void CheckLaserHit()
    {
        // Timer de dano — só causa dano a cada 'damageTickRate' segundos
        damageTimer += Time.deltaTime;
        if (damageTimer < damageTickRate) return;

        // Calcula a direção do laser baseado no ângulo atual
        Vector3 laserDirection = AngleToDirection(currentLaserAngle);
        Vector3 origin = GetLaserOrigin(); // Um pouco acima do chão

        // SphereCast = como um Raycast mas com "espessura"
        // Imagina jogar uma bolinha invisível na direção do laser
        // Se ela bater em algo, retorna true
        RaycastHit hit;
        if (Physics.SphereCast(origin, laserWidth, laserDirection, out hit, laserRange))
        {
            // Verifica se acertou o player
            if (hit.collider.CompareTag("Player"))
            {
                PlayerHealth playerHealth = hit.collider.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    int finalDamage = laserDamage;
                    
                    // Se está buffado pelo Crystal Tuner, dano aumenta
                    if (isBuffed) finalDamage = Mathf.RoundToInt(finalDamage * 1.5f);
                    
                    playerHealth.TakeDamage(finalDamage, gameObject);
                    damageTimer = 0f; // Reseta o timer
                    Debug.Log("[WATCHER] Laser acertou o player! Dano: " + finalDamage);
                }
            }
        }
    }

    // =============================================
    // VISUAL DO LASER (LineRenderer)
    // =============================================
    
    // Duas camadas de laser: núcleo interno + brilho externo
    private LineRenderer laserGlow;  // Camada externa (brilho roxo suave)

    /// <summary>
    /// Configura os LineRenderers que desenham o laser na tela
    /// Usamos DUAS linhas sobrepostas para criar efeito de brilho:
    /// - laserLine = núcleo interno (fino, branco-roxo, brilhante)
    /// - laserGlow = brilho externo (grosso, roxo transparente)
    /// </summary>
    void SetupLaserVisual()
    {
        // === CAMADA 1: BRILHO EXTERNO (glow) ===
        GameObject glowObj = new GameObject("LaserGlow");
        glowObj.transform.SetParent(transform);
        glowObj.transform.localPosition = Vector3.zero;

        laserGlow = glowObj.AddComponent<LineRenderer>();
        laserGlow.positionCount = 2;
        laserGlow.startWidth = 0.8f;      // Mais grosso que o núcleo
        laserGlow.endWidth = 0.4f;
        laserGlow.useWorldSpace = true;
        laserGlow.numCapVertices = 8;
        laserGlow.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        Shader glowShader = Shader.Find("Sprites/Default");
        if (glowShader == null) glowShader = Shader.Find("UI/Default");
        Material glowMat = new Material(glowShader);
        laserGlow.material = glowMat;

        // Gradiente roxo suave e transparente
        Gradient glowGradient = new Gradient();
        glowGradient.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(new Color(0.6f, 0.1f, 0.9f), 0.0f),   // Roxo vibrante
                new GradientColorKey(new Color(0.4f, 0.0f, 0.7f), 0.5f),   // Roxo escuro
                new GradientColorKey(new Color(0.6f, 0.1f, 0.9f), 1.0f),   // Roxo vibrante
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(0.4f, 0.0f),   // Semi-transparente
                new GradientAlphaKey(0.25f, 0.5f),
                new GradientAlphaKey(0.05f, 1.0f),   // Quase invisível na ponta
            }
        );
        laserGlow.colorGradient = glowGradient;
        laserGlow.enabled = false;

        // === CAMADA 2: NÚCLEO INTERNO (core) ===
        GameObject laserObj = new GameObject("LaserCore");
        laserObj.transform.SetParent(transform);
        laserObj.transform.localPosition = Vector3.zero;

        laserLine = laserObj.AddComponent<LineRenderer>();
        laserLine.positionCount = 2;
        laserLine.startWidth = 0.15f;     // Fino e concentrado
        laserLine.endWidth = 0.08f;
        laserLine.useWorldSpace = true;
        laserLine.numCapVertices = 8;
        laserLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        Shader coreShader = Shader.Find("Sprites/Default");
        if (coreShader == null) coreShader = Shader.Find("UI/Default");
        Material coreMat = new Material(coreShader);
        laserLine.material = coreMat;

        // Gradiente do núcleo: branco → roxo claro → roxo
        Gradient coreGradient = new Gradient();
        coreGradient.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(new Color(0.9f, 0.7f, 1.0f), 0.0f),   // Branco-roxo (brilhante)
                new GradientColorKey(new Color(0.8f, 0.4f, 1.0f), 0.3f),   // Roxo claro
                new GradientColorKey(new Color(0.6f, 0.1f, 0.9f), 1.0f),   // Roxo puro
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(1.0f, 0.0f),
                new GradientAlphaKey(0.9f, 0.5f),
                new GradientAlphaKey(0.5f, 1.0f),
            }
        );
        laserLine.colorGradient = coreGradient;
        laserLine.enabled = false;
    }

    /// <summary>
    /// Mostra o laser fraquinho durante o carregamento (preview roxo fino)
    /// </summary>
    void ShowLaserCharging(bool show)
    {
        if (laserLine == null) return;
        laserLine.enabled = show;
        if (laserGlow != null) laserGlow.enabled = false; // Sem glow durante carregamento
        if (show)
        {
            // Laser bem fininho e transparente — só um "fio" roxo de aviso
            laserLine.startWidth = 0.04f;
            laserLine.endWidth = 0.02f;
            UpdateLaserVisual();
        }
    }

    /// <summary>
    /// Mostra/esconde o laser com força total (núcleo + glow)
    /// </summary>
    void ShowLaserFiring(bool show)
    {
        if (laserLine == null) return;
        laserLine.enabled = show;
        if (laserGlow != null) laserGlow.enabled = show;
        if (show)
        {
            // Tamanhos iniciais — vão pulsar no UpdateLaserVisual
            laserLine.startWidth = 0.15f;
            laserLine.endWidth = 0.08f;
            laserGlow.startWidth = 0.8f;
            laserGlow.endWidth = 0.4f;
        }
    }

    /// <summary>
    /// Atualiza as posições e efeito pulsante do laser todo frame
    /// </summary>
    void UpdateLaserVisual()
    {
        if (laserLine == null || !laserLine.enabled) return;

        Vector3 origin = GetLaserOrigin();
        Vector3 direction = AngleToDirection(currentLaserAngle);
        Vector3 endPoint = origin + direction * laserRange;

        // Se o laser bater em algo (parede, etc.), encurta
        RaycastHit hit;
        if (Physics.Raycast(origin, direction, out hit, laserRange))
        {
            if (!hit.collider.CompareTag("Player"))
            {
                endPoint = hit.point;
            }
        }

        // Posiciona ambas as linhas
        laserLine.SetPosition(0, origin);
        laserLine.SetPosition(1, endPoint);

        if (laserGlow != null && laserGlow.enabled)
        {
            laserGlow.SetPosition(0, origin);
            laserGlow.SetPosition(1, endPoint);

            // === EFEITO DE PULSAÇÃO ===
            // Faz o laser "respirar" — oscila a largura suavemente
            // Mathf.Sin com Time.time cria uma onda que vai e volta
            float pulse = Mathf.Sin(Time.time * 8f) * 0.15f; // Oscila ±0.15
            laserLine.startWidth = 0.15f + pulse * 0.3f;
            laserGlow.startWidth = 0.8f + pulse;
            laserGlow.endWidth = 0.4f + pulse * 0.5f;
        }
    }

    // =============================================
    // FUNÇÕES AUXILIARES
    // =============================================

    /// <summary>
    /// Retorna o centro exato do modelo 3D.
    /// Usa o bounds.center do Renderer, que dá o ponto central da malha visível.
    /// </summary>
    Vector3 GetLaserOrigin()
    {
        if (modelRenderer != null)
            return modelRenderer.bounds.center;
        return transform.position;
    }

    /// <summary>
    /// Converte um ângulo em graus para uma direção no plano XZ
    /// Exemplo: 0° = frente (Z+), 90° = direita (X+), 180° = trás (Z-)
    /// </summary>
    Vector3 AngleToDirection(float angleDegrees)
    {
        float rad = angleDegrees * Mathf.Deg2Rad;
        return new Vector3(Mathf.Sin(rad), 0, Mathf.Cos(rad));
    }

    // =============================================
    // SISTEMA DE BUFF (Crystal Tuner)
    // =============================================
    
    /// <summary>
    /// Chamado pelo Crystal Tuner quando conecta/desconecta
    /// Quando buffado: carrega mais rápido e laser gira mais rápido
    /// </summary>
    public void SetBuff(bool active)
    {
        if (active && !isBuffed)
        {
            isBuffed = true;
            originalChargeTime = chargeTime;
            originalRotationSpeed = rotationSpeed;

            chargeTime *= 0.5f;         // Carrega 2x mais rápido
            rotationSpeed *= 1.5f;      // Laser gira 50% mais rápido

            Debug.Log("[WATCHER] BUFFADO pelo Crystal Tuner!");
        }
        else if (!active && isBuffed)
        {
            isBuffed = false;
            chargeTime = originalChargeTime;
            rotationSpeed = originalRotationSpeed;

            Debug.Log("[WATCHER] Buff removido.");
        }
    }

    // =============================================
    // GIZMOS — Visualização no Editor (só aparece no Scene view, não no jogo)
    // =============================================
    void OnDrawGizmosSelected()
    {
        // Círculo amarelo = range de ativação
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, activationDistance);

        // Linha vermelha = direção atual do laser
        Gizmos.color = Color.red;
        Vector3 dir = AngleToDirection(currentLaserAngle);
        Gizmos.DrawRay(GetLaserOrigin(), dir * laserRange);

        // Círculo do alcance do laser
        Gizmos.color = new Color(1f, 0.3f, 0.1f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, laserRange);
    }
}
