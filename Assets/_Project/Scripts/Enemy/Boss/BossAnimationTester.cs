using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Ferramenta de Testes e Debug do Boss Cromático (Bioma 1).
/// 100% Controlado via Botões Personalizados no Inspector do Unity!
/// </summary>
public class BossAnimationTester : MonoBehaviour
{
    [Header("1. Controle de Modo de Teste")]
    [Tooltip("Se FALSO (padrão), o Boss desativa ataques automáticos e fica em modo Sandbox paradinho para inspeção. Se VERDADEIRO, o Boss te ataca normalmente.")]
    public bool enableBossAI = false;

    [Header("2. Referências")]
    public BossController bossController;
    public Animator animator;

    private NavMeshAgent navAgent;
    private bool lastAIState = true;

    private void Awake()
    {
        FindReferences();
    }

    private void OnValidate()
    {
        FindReferences();
    }

    private void FindReferences()
    {
        if (bossController == null) bossController = GetComponent<BossController>() ?? GetComponentInParent<BossController>();
        if (bossController != null && animator == null) animator = bossController.animator;
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (navAgent == null) navAgent = GetComponent<NavMeshAgent>() ?? GetComponentInParent<NavMeshAgent>();
    }

    private void Start()
    {
        FindReferences();
        ApplyAIMode(true);
    }

    private void Update()
    {
        if (lastAIState != enableBossAI)
        {
            ApplyAIMode(false);
        }

        // No modo sandbox, garante que o NavMesh continue congelado e não ande
        if (!enableBossAI && navAgent != null && navAgent.isOnNavMesh)
        {
            navAgent.isStopped = true;
            navAgent.velocity = Vector3.zero;
        }
    }

    /// <summary>
    /// Aplica o estado de IA (Pausa perseguição e magias automáticas no modo Sandbox)
    /// </summary>
    public void ApplyAIMode(bool force = false)
    {
        FindReferences();
        lastAIState = enableBossAI;

        if (bossController != null)
        {
            bossController.OverrideMovement = !enableBossAI;
        }

        // Desativa apenas os scripts autônomos de fases e magias automáticas (mantendo AnimationEvents e Animator ativos!)
        var phase1 = GetComponent<BossPhase1_MestreDoSolo>() ?? GetComponentInChildren<BossPhase1_MestreDoSolo>();
        if (phase1 != null) { phase1.StopAllCoroutines(); phase1.enabled = enableBossAI; }

        var spawner = GetComponent<BossPhase1_MobSpawner>() ?? GetComponentInChildren<BossPhase1_MobSpawner>();
        if (spawner != null) { spawner.StopAllCoroutines(); spawner.enabled = enableBossAI; }

        var phase2 = GetComponent<BossPhase2_Refraction>() ?? GetComponentInChildren<BossPhase2_Refraction>();
        if (phase2 != null) { phase2.StopAllCoroutines(); phase2.enabled = enableBossAI; }

        var puddles = GetComponent<AcidPuddleSpawner>() ?? GetComponentInChildren<AcidPuddleSpawner>();
        if (puddles != null) { puddles.StopAllCoroutines(); puddles.enabled = enableBossAI; }

        if (navAgent != null && navAgent.isOnNavMesh)
        {
            navAgent.isStopped = !enableBossAI;
            if (!enableBossAI) navAgent.velocity = Vector3.zero;
        }

        if (animator != null)
        {
            animator.enabled = true;
            if (!enableBossAI) animator.SetBool("IsWalking", false);
        }

        Debug.Log(enableBossAI 
            ? "[BossAnimationTester] ⚔️ IA do Boss ATIVADA (Combate Normal)." 
            : "[BossAnimationTester] ⏸️ IA do Boss PAUSADA (Modo Sandbox de Testes).");
    }

    // =====================================================
    // CONTROLE DE FASES (DISPARADO VIA INSPECTOR)
    // =====================================================

    public void SetPhase(BossController.BossState newPhase)
    {
        FindReferences();

        int phaseInt = 1;
        if (newPhase == BossController.BossState.Phase2) phaseInt = 2;
        else if (newPhase == BossController.BossState.Phase3) phaseInt = 3;

        Debug.Log($"[BossAnimationTester] 🔄 Alterando Fase para: {newPhase} (Fase {phaseInt})");
        BossEvents.RaisePhaseChanged(phaseInt);

        if (newPhase == BossController.BossState.Phase1) PlayAnimationTrigger("PowerUp");
        else if (newPhase == BossController.BossState.Phase2) PlayAnimationTrigger("bossSpell");
        else if (newPhase == BossController.BossState.Phase3) PlayAnimationTrigger("PowerUp");
    }

    // =====================================================
    // DISPARO DE ANIMAÇÕES (DISPARADO VIA INSPECTOR)
    // =====================================================

    public void PlayAnimationTrigger(string triggerName)
    {
        FindReferences();

        if (animator == null)
        {
            Debug.LogWarning("[BossAnimationTester] ⚠️ Animator não encontrado! Verifique a hierarquia do Boss.");
            return;
        }

        animator.enabled = true;
        animator.SetBool("IsWalking", false);

        // Reseta triggers conhecidos para evitar colisões
        string[] allTriggers = new string[] { "bossSwipe", "bossPunch", "bossJumpAttack", "bossStomp", "bossSpell", "PowerUp", "SimpleCast", "BossSpellWide", "Stunned", "Die" };
        foreach (var t in allTriggers)
        {
            try { animator.ResetTrigger(t); } catch { }
        }

        // Dispara o Trigger solicitado
        animator.SetTrigger(triggerName);

        if (triggerName.Equals("PowerUp", System.StringComparison.OrdinalIgnoreCase) && bossController != null)
        {
            bossController.TriggerPowerUP(2.5f);
            return;
        }

        // Fallbacks de busca de State no Animator (com variações de caixa e prefixos)
        string[] candidates = new string[] 
        { 
            triggerName, 
            "Boss" + char.ToUpper(triggerName[0]) + triggerName.Substring(1),
            triggerName.Replace("boss", "").Replace("Boss", ""),
            char.ToUpper(triggerName[0]) + triggerName.Substring(1)
        };

        foreach (var name in candidates)
        {
            if (animator.HasState(0, Animator.StringToHash(name)))
            {
                animator.Play(name, 0, 0f);
                break;
            }
        }

        Debug.Log($"[BossAnimationTester] 🎬 Animação disparada com sucesso: {triggerName}");
    }

    public void PlayIdle()
    {
        FindReferences();
        if (animator != null)
        {
            animator.SetBool("IsWalking", false);
            if (animator.HasState(0, Animator.StringToHash("Idle"))) animator.Play("Idle", 0, 0f);
            else if (animator.HasState(0, Animator.StringToHash("bossIdle"))) animator.Play("bossIdle", 0, 0f);
        }
    }

    // =====================================================
    // DISPARO DE VFX & CAMERA SHAKE (DISPARADO VIA INSPECTOR)
    // =====================================================

    public void TriggerVFXStomp()
    {
        FindReferences();
        if (bossController != null && bossController.vfxStompPrefab != null)
        {
            Instantiate(bossController.vfxStompPrefab, transform.position, Quaternion.identity);
            Debug.Log("[BossAnimationTester] 💥 Instanciou VFX de Pisada (Stomp)");
        }
    }

    public void TriggerVFXJumpAttack()
    {
        FindReferences();
        if (bossController != null && bossController.vfxJumpAttackPrefab != null)
        {
            Instantiate(bossController.vfxJumpAttackPrefab, transform.position, Quaternion.identity);
            Debug.Log("[BossAnimationTester] 💥 Instanciou VFX de Salto Esmagador (Jump)");
        }
    }

    public void TriggerCameraShake()
    {
        BossController.TriggerCameraShake(0.45f, 0.40f);
        Debug.Log("[BossAnimationTester] 📳 Disparou Camera Shake do Boss");
    }

    public void TriggerSuperAtaqueEspinhos()
    {
        FindReferences();
        if (bossController != null)
        {
            bossController.PerformBossSpellWide();
        }
        else
        {
            PlayAnimationTrigger("BossSpellWide");
        }
    }

    public void TriggerSharpBlurTeleport()
    {
        FindReferences();
        if (bossController != null)
        {
            bossController.TriggerSharpBlurTeleport();
        }
        else
        {
            Debug.LogWarning("[BossAnimationTester] ⚠️ BossController não encontrado para testar o Teleporte!");
        }
    }
}

// =====================================================
// CUSTOM INSPECTOR EDITOR (DESENHA BOTÕES NO UNITY)
// =====================================================
#if UNITY_EDITOR
[CustomEditor(typeof(BossAnimationTester))]
public class BossAnimationTesterEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        BossAnimationTester tester = (BossAnimationTester)target;

        EditorGUILayout.Space(15);
        EditorGUILayout.LabelField("🎮 BOTÕES DE CONTROLE DO BOSS", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Clique nos botões abaixo durante o Play Mode para testar animações, fases e efeitos no Inspector!", MessageType.Info);

        EditorGUILayout.Space(10);

        // 1. ALTERNAR MODO DE IA
        GUI.backgroundColor = tester.enableBossAI ? new Color(1f, 0.4f, 0.4f) : new Color(0.4f, 0.9f, 0.4f);
        if (GUILayout.Button(tester.enableBossAI ? "⚔️ DESATIVAR IA (IR PARA MODO SANDBOX PASSIVO)" : "▶️ ATIVAR IA (MODO COMBATE AGRESSIVO)", GUILayout.Height(35)))
        {
            tester.enableBossAI = !tester.enableBossAI;
            tester.ApplyAIMode();
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space(15);
        EditorGUILayout.LabelField("⚙️ ALTERNAR FASES DO BOSS", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Fase 1 (Solo)", GUILayout.Height(30)))
        {
            tester.SetPhase(BossController.BossState.Phase1);
        }
        if (GUILayout.Button("Fase 2 (Refração)", GUILayout.Height(30)))
        {
            tester.SetPhase(BossController.BossState.Phase2);
        }
        if (GUILayout.Button("Fase 3 (Raízes)", GUILayout.Height(30)))
        {
            tester.SetPhase(BossController.BossState.Phase3);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(15);
        EditorGUILayout.LabelField("🎬 DISPARAR ANIMAÇÕES", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("⚔️ Swipe (Varredura)", GUILayout.Height(28)))
        {
            tester.PlayAnimationTrigger("bossSwipe");
        }
        if (GUILayout.Button("👊 Punch (Soco)", GUILayout.Height(28)))
        {
            tester.PlayAnimationTrigger("bossPunch");
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("🦘 Jump Attack (Salto)", GUILayout.Height(28)))
        {
            tester.PlayAnimationTrigger("bossJumpAttack");
        }
        if (GUILayout.Button("🦶 Stomp (Pisada)", GUILayout.Height(28)))
        {
            tester.PlayAnimationTrigger("bossStomp");
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("🪄 Spell Cast (Magia)", GUILayout.Height(28)))
        {
            tester.PlayAnimationTrigger("bossSpell");
        }
        if (GUILayout.Button("⚡ PowerUp (Energização)", GUILayout.Height(28)))
        {
            tester.PlayAnimationTrigger("PowerUp");
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("💫 Stunned (Atordoado)", GUILayout.Height(28)))
        {
            tester.PlayAnimationTrigger("Stunned");
        }
        if (GUILayout.Button("💀 Morrer (Death)", GUILayout.Height(28)))
        {
            tester.PlayAnimationTrigger("Die");
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(15);
        EditorGUILayout.LabelField("💥 TESTAR VFX E MAGIAS ESPECIAIS", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("💥 VFX Pisada (Stomp)", GUILayout.Height(28)))
        {
            tester.TriggerVFXStomp();
        }
        if (GUILayout.Button("💥 VFX Salto Esmagador", GUILayout.Height(28)))
        {
            tester.TriggerVFXJumpAttack();
        }
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("🌊 Super Ataque: Espinhos 360° (BossSpellWide)", GUILayout.Height(28)))
        {
            tester.TriggerSuperAtaqueEspinhos();
        }

        GUI.backgroundColor = new Color(0.75f, 0.4f, 1f);
        if (GUILayout.Button("⚡ TESTAR: Teleporte Estilo SharpBlur (Holograma + Warp + Golpe)", GUILayout.Height(32)))
        {
            tester.TriggerSharpBlurTeleport();
        }
        GUI.backgroundColor = Color.white;

        if (GUILayout.Button("📳 Tremor de Câmera (Camera Shake)", GUILayout.Height(30)))
        {
            tester.TriggerCameraShake();
        }

        EditorGUILayout.Space(10);
    }
}
#endif
