using UnityEngine;

/// <summary>
/// Script de Teste Showcase no Inspector para a Estrela (Shard Swarm).
/// 100% controlado via Inspector da Unity (sem teclas de teclado).
/// 
/// Como usar:
///   1. Selecione o objeto ShardSwarm na cena.
///   2. No Inspector, clique com o BOTÃO DIREITO sobre o título deste script
///      (ou use as opções do cabeçalho) para disparar qualquer animação na hora!
/// </summary>
public class ShardSwarmAnimationTester : MonoBehaviour
{
    public enum AnimationStateToTest
    {
        IdleFloat,
        WheelSpin,
        AreaWindUp,
        IoyoDisparo,
        ElectricPulse
    }

    [Header("--- CONTROLES DO INSPETOR ---")]
    [Tooltip("Escolha uma animação e marque 'Disparar Animação Selecionada' abaixo.")]
    public AnimationStateToTest selectAnimation;
    
    [Tooltip("Marque esta caixinha no Inspector para disparar a animação selecionada acima!")]
    public bool triggerSelectedAnimationNow = false;

    [Header("--- Modos de Segurança ---")]
    [Tooltip("Desativa ataques automáticos da IA se ativado.")]
    public bool disableAutoAttacks = false;
    [Tooltip("Desativa todo o dano ao jogador enquanto testa.")]
    public bool disableDamageToPlayer = false;

    private ShardSwarm_AI aiScript;

    void Awake()
    {
        aiScript = GetComponent<ShardSwarm_AI>();
        if (aiScript == null) aiScript = GetComponentInChildren<ShardSwarm_AI>();

        if (aiScript != null)
        {
            if (disableAutoAttacks) aiScript.autoAttackEnabled = false;
            if (disableDamageToPlayer)
            {
                aiScript.projectileDamage = 0;
                aiScript.electricPulseDamage = 0;
                aiScript.trailDamagePerTick = 0;
            }
        }
    }

    void Update()
    {
        if (aiScript == null)
        {
            aiScript = GetComponent<ShardSwarm_AI>();
            if (aiScript == null) aiScript = GetComponentInChildren<ShardSwarm_AI>();
            if (aiScript == null) aiScript = GetComponentInParent<ShardSwarm_AI>();
        }
        if (aiScript == null) return;

        if (disableAutoAttacks) aiScript.autoAttackEnabled = false;
        else aiScript.autoAttackEnabled = true;
        if (disableDamageToPlayer)
        {
            aiScript.projectileDamage = 0;
            aiScript.electricPulseDamage = 0;
            aiScript.trailDamagePerTick = 0;
        }

        // Dispara quando a caixinha do Inspector for marcada
        if (triggerSelectedAnimationNow)
        {
            triggerSelectedAnimationNow = false;
            ExecuteSelectedAnimation();
        }

        // Atalhos opcionais no teclado para praticidade (1 a 5)
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) TriggerFormaUnida();
        if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) TriggerWheelSpin();
        if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)) TriggerAreaExpansion();
        if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4)) TriggerDisparo();
        if (Input.GetKeyDown(KeyCode.Alpha5) || Input.GetKeyDown(KeyCode.Keypad5)) TriggerElectricPulse();
    }

    public void ExecuteSelectedAnimation()
    {
        switch (selectAnimation)
        {
            case AnimationStateToTest.IdleFloat:
                TriggerFormaUnida();
                break;
            case AnimationStateToTest.WheelSpin:
                TriggerWheelSpin();
                break;
            case AnimationStateToTest.AreaWindUp:
                TriggerAreaExpansion();
                break;
            case AnimationStateToTest.IoyoDisparo:
                TriggerDisparo();
                break;
            case AnimationStateToTest.ElectricPulse:
                TriggerElectricPulse();
                break;
        }
    }

    // --- MÉTODOS DO CONTEXT MENU (CLIQUE COM BOTÃO DIREITO NO NOME DO SCRIPT NO INSPECTOR) ---

    [ContextMenu("► TESTAR: 1. Giro de Roda (Wheel Spin 360°)")]
    public void TriggerWheelSpin()
    {
        if (aiScript == null) return;
        aiScript.StopAllCoroutines();
        aiScript.ResetSpikesToHome();
        aiScript.StartCoroutine(aiScript.WheelSpinRoutine());
        Debug.Log("⚙️ [INSPECTOR SHOWCASE] Animação de Giro de Roda (Wheel Spin) ativada!");
    }

    [ContextMenu("► TESTAR: 2. Formação de Área (Wind-Up)")]
    public void TriggerAreaExpansion()
    {
        if (aiScript == null) return;
        aiScript.StopAllCoroutines();
        aiScript.ResetSpikesToHome();
        aiScript.StartCoroutine(aiScript.WindUpOnlyRoutine());
        Debug.Log("🛡️ [INSPECTOR SHOWCASE] Formação de Expansão de Área ativada!");
    }

    [ContextMenu("► TESTAR: 3. Disparo Ioiô com Cadência")]
    public void TriggerDisparo()
    {
        if (aiScript == null) return;
        aiScript.StopAllCoroutines();
        aiScript.ResetSpikesToHome();
        aiScript.StartCoroutine(aiScript.LaunchSpikesRoutine());
        Debug.Log("🎯 [INSPECTOR SHOWCASE] Disparo Ioiô ativado!");
    }

    [ContextMenu("► TESTAR: 4. Pulso Elétrico (Descarga)")]
    public void TriggerElectricPulse()
    {
        if (aiScript == null) return;
        aiScript.StopAllCoroutines();
        aiScript.ResetSpikesToHome();
        aiScript.TriggerElectricPulse();
        Debug.Log("⚡ [INSPECTOR SHOWCASE] Descarga de Pulso Elétrico ativada!");
    }

    [ContextMenu("► TESTAR: 5. Forma Unida (Idle Float)")]
    public void TriggerFormaUnida()
    {
        if (aiScript == null) return;
        aiScript.StopAllCoroutines();
        aiScript.ResetSpikesToHome();
        aiScript.currentState = ShardSwarm_AI.SwarmState.FormaUnida;
        Debug.Log("🌟 [INSPECTOR SHOWCASE] Modo Flutuação Idle restaurado!");
    }
}
