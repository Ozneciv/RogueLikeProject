using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PrimaryAttackKnife : MonoBehaviour
{
    // --- Variável que indica se a janela de dano está aberta ---
    public bool isHitboxActive { get; private set; } = false;
    public bool isAttacking { get; private set; }

    [Header("Estado da Arma")]
    public bool hasWeapon = false;

    [Header("Required Components")]
    public Animator animator;
    public Rigidbody playerRb;
    public PlayerHealth playerHealth;
    private PlayerAttributesOffensive playerAttributes;

    [Header("Weapon Hitbox")]
    public Collider handHitbox;
    private Collider equippedWeaponHitbox;
    private Collider currentHitbox;

    // Armazenar tamanhos originais para escalar com weaponRangeMelee
    private Vector3 originalHandHitboxSize;
    private Vector3 originalWeaponHitboxSize;
    private Vector3 currentOriginalSize;
    private float lastAppliedWeaponRange = 1f;

    [Header("Attack Stats")]
    public float currentRange;
    public float defaultRange = 2f;
    public float daggerRange = 5f;
    public float swordRange = 7f;
    public float axeRange = 7.5f; 

    [Header("VFX")]
    [Tooltip("Arraste aqui suas variações de VFX (o original e o Slash). O script vai sortear um deles a cada hit!")]
    public GameObject[] hitImpactVariations;
    // (Mantive o antigo escondido só para não dar erro se alguma outra coisa puxar ele)
    [HideInInspector] public GameObject hitImpactPrefab; 

    [Header("Settings")]
    public float attackAnimationSpeed = 1.0f;
    public float defaultAttackSpeed = 1.0f; // Para resetar a velocidade padrão
    public float axeAttackSpeed = 0.85f; // Velocidade reduzida e pesada para o machado

    [Header("Combo Speed Modifiers (Fine Tuning)")]
    [Tooltip("Multiplicador de velocidade para o Hit 1")]
    public float hit1SpeedMultiplier = 1.0f;
    [Tooltip("Multiplicador de velocidade para o Hit 2")]
    public float hit2SpeedMultiplier = 0.7f; // Reduzido por padrão!
    [Tooltip("Multiplicador de velocidade para o Hit 3")]
    public float hit3SpeedMultiplier = 1.0f;
    [Tooltip("Multiplicador de velocidade para o Hit 4")]
    public float hit4SpeedMultiplier = 1.0f;

    [Header("Weapon Damages")]
    public int[] defaultDamages = { 20, 25, 35 };
    public int[] daggerDamages = { 30, 35, 40 }; // Conforme Hp_Dano_Inimigos.pdf (Combo total = 105)
    public int[] swordDamages = { 35, 45, 60 };
    public int[] axeDamages = { 40, 45, 55, 55 }; // Conforme Hp_Dano_Inimigos.pdf (Combo total = 195)
    private int[] currentDamages;

    [Header("Combo Settings")]
    [Tooltip("Tempo limite do combo com as mãos vazias")]
    public float defaultComboResetTime = 1.2f;
    [Tooltip("Tempo limite do combo com a Adaga")]
    public float daggerComboResetTime = 1.2f;
    [Tooltip("Tempo limite do combo com a Espada")]
    public float swordComboResetTime = 1.2f;
    [Tooltip("Tempo limite do combo com o Machado")]
    public float axeComboResetTime = 1.8f;

    [HideInInspector]
    public float comboResetTime = 1.2f;
    private int comboStep = 0;

    [Header("Backup Animation Timing Settings (When no Animation Events exist)")]
    [Tooltip("Delay (em segundos) para ativar o colisor de dano da Adaga")]
    public float daggerHitDelay = 0.15f;
    [Tooltip("Duração (em segundos) que o colisor da Adaga fica ativo")]
    public float daggerHitDuration = 0.2f;

    [Tooltip("Delay (em segundos) para ativar o colisor de dano do Machado")]
    public float axeHitDelay = 0.35f;
    [Tooltip("Duração (em segundos) que o colisor do Machado fica ativo")]
    public float axeHitDuration = 0.3f;

    [Tooltip("Delay (em segundos) para ativar o colisor de dano padrão")]
    public float defaultHitDelay = 0.15f;
    [Tooltip("Duração (em segundos) que o colisor padrão fica ativo")]
    public float defaultHitDuration = 0.2f;

    private float currentHitDelay = 0.15f;
    private float currentHitDuration = 0.2f;
    private bool canAttack = true;
    public bool CanAttack => canAttack;
    private bool hasBufferedAttack = false;
    private Coroutine comboResetCoroutine;
    private Coroutine backupAttackCoroutine;
    private bool eventFiredEnableHitbox = false;
    private bool eventFiredDisableHitbox = false;
    private bool eventFiredOpenWindow = false;
    private List<Collider> enemiesHitInThisAttack;

    private void Start()
    {
        defaultAttackSpeed = attackAnimationSpeed; // Salva a velocidade customizada do Inspector (como a da Adaga) antes de qualquer troca
        enemiesHitInThisAttack = new List<Collider>();
        EquipDefaultWeapon();
        
        // Permite ataque por padrão com a mão/arma equipada
        hasWeapon = true;
        isAttacking = false;
        isHitboxActive = false;

        // Buscar PlayerAttributesOffensive
        playerAttributes = GetComponent<PlayerAttributesOffensive>();
        if (playerAttributes == null)
        {
            Debug.LogError("❌ PrimaryAttackKnife: PlayerAttributesOffensive NÃO ENCONTRADO!");
            Debug.LogError("   → Adicione PlayerAttributesOffensive ao GameObject 'astronaut'!");
        }
        else
        {
            Debug.Log("✅ PrimaryAttackKnife: PlayerAttributesOffensive encontrado!");
        }
    }

    private void Update()
    {
        try
        {
            // Pressione L para imprimir diagnósticos de ataque no console do Unity
            if (Input.GetKeyDown(KeyCode.L))
            {
                Debug.LogWarning($"=== DIAGNÓSTICO DE ATAQUE ===");
                Debug.LogWarning($"[PrimaryAttackKnife] Script Ativo: {enabled}, Objeto: {gameObject.name}, Ativo na Hierarquia: {gameObject.activeInHierarchy}");
                Debug.LogWarning($"[PrimaryAttackKnife] Animator associado: {(animator != null ? animator.name : "null")}, Animator Ativo: {(animator != null ? animator.isActiveAndEnabled.ToString() : "false")}");
                Debug.LogWarning($"[PrimaryAttackKnife] Controller: {(animator != null && animator.runtimeAnimatorController != null ? animator.runtimeAnimatorController.name : "null")}");
                Debug.LogWarning($"[PrimaryAttackKnife] canAttack: {canAttack}, hasWeapon: {hasWeapon}, isAttacking: {isAttacking}");
                Debug.LogWarning($"[PrimaryAttackKnife] hitbox: {(currentHitbox != null ? currentHitbox.name : "null")}, hitboxEnabled: {(currentHitbox != null ? currentHitbox.enabled.ToString() : "false")}");
            }

            // Aplicar weapon range scale dinamicamente
            if (playerAttributes != null && currentHitbox != null)
            {
                if (Mathf.Abs(playerAttributes.weaponRangeMelee - lastAppliedWeaponRange) > 0.01f)
                {
                    ApplyWeaponRangeScale();
                    lastAppliedWeaponRange = playerAttributes.weaponRangeMelee;
                }
            }

            // Manter attack speed durante combo (incluindo os multiplicadores específicos de cada passo do combo do machado)
            if (isAttacking && playerAttributes != null && animator != null && animator.isActiveAndEnabled)
            {
                float stepSpeedMult = 1.0f;
                
                // Verifica se a arma equipada é o Machado
                Player_WeaponManager wm = GetComponent<Player_WeaponManager>();
                bool isAxe = false;
                if (wm != null && wm.rightHand != null && wm.rightHand.childCount > 0)
                {
                    WeaponOffset offsetData = wm.rightHand.GetChild(0).GetComponent<WeaponOffset>();
                    if (offsetData != null && offsetData.weaponType == WeaponType.Axe)
                    {
                        isAxe = true;
                    }
                }

                if (isAxe)
                {
                    if (comboStep == 1) stepSpeedMult = hit1SpeedMultiplier;
                    else if (comboStep == 2) stepSpeedMult = hit2SpeedMultiplier;
                    else if (comboStep == 3) stepSpeedMult = hit3SpeedMultiplier;
                    else if (comboStep == 4) stepSpeedMult = hit4SpeedMultiplier;
                }

                float targetSpeed = attackAnimationSpeed * playerAttributes.attackSpeedMelee * stepSpeedMult;
                if (Mathf.Abs(animator.speed - targetSpeed) > 0.01f)
                {
                    animator.speed = targetSpeed;
                }
            }

            if (Input.GetKeyDown(KeyCode.Q) || Input.GetMouseButtonDown(0))
            {
                // Impede ataque se qualquer janela de menu/inventário/console estiver aberta
                if (IsAnyUIOpen())
                {
                    return;
                }

                if (hasWeapon)
                {
                    if (canAttack)
                    {
                        if (comboResetCoroutine != null) StopCoroutine(comboResetCoroutine);
                        PerformNextAttack();
                    }
                    else if (isAttacking)
                    {
                        // Só permitimos bufferizar se NÃO estivermos no último hit do combo.
                        // O último golpe não deve enfileirar o reinício do combo (golpe 1) automaticamente.
                        int maxComboSteps = (currentDamages != null) ? currentDamages.Length : 3;
                        if (comboStep < maxComboSteps)
                        {
                            hasBufferedAttack = true;
                            Debug.Log("[PrimaryAttackKnife] Input buffered for next combo step.");
                        }
                    }
                }
            }
        }
        catch (System.Exception)
        {
            // Evita crashar o loop de update se referências estiverem se restabelecendo
        }
    }

    private bool IsAnyUIOpen()
    {
        if (CheatConsole.IsOpen) return true;
        if (InventoryUI.Instance != null && InventoryUI.Instance.IsOpen()) return true;
        if (SyntheticBagUI.Instance != null && SyntheticBagUI.Instance.IsOpen()) return true;
        if (CraftingUI.Instance != null && CraftingUI.Instance.IsOpen()) return true;
        if (EptinhoMenuController.instancia != null && EptinhoMenuController.instancia.IsOpen()) return true;
        if (MerchantUIController.Instance != null && MerchantUIController.Instance.IsUiOpen()) return true;
        return false;
    }

    private void PerformNextAttack()
    {
        try
        {
            // Garante que o animator seja dinamicamente recuperado do modelo ativo ou do WeaponManager se estiver nulo ou desativado
            Player_WeaponManager wmRef = GetComponent<Player_WeaponManager>() ?? GetComponentInParent<Player_WeaponManager>();
            if (wmRef != null && wmRef.playerAnimator != null && wmRef.playerAnimator.isActiveAndEnabled)
            {
                animator = wmRef.playerAnimator;
            }
            else if (animator == null || !animator.isActiveAndEnabled)
            {
                animator = GetComponentInChildren<Animator>(false) ?? GetComponentInParent<Animator>();
            }

            if (animator == null || !animator.isActiveAndEnabled)
            {
                Debug.LogError("[PrimaryAttackKnife] Critical: Active Animator not found or disabled! Aborting attack sequence.");
                canAttack = true;
                isAttacking = false;
                return;
            }

            // Garante que o colisor esteja desativado ao iniciar um novo golpe para evitar hitboxes fantasmas residuais
            isHitboxActive = false;
            if (currentHitbox != null) currentHitbox.enabled = false;

            isAttacking = true;
            canAttack = false;
            comboStep++;

            // Loop do combo de volta para o primeiro hit se passar do limite de ataques
            int maxComboSteps = (currentDamages != null) ? currentDamages.Length : 3;
            if (comboStep > maxComboSteps)
            {
                comboStep = 1;
            }

            animator.ResetTrigger("Attack");
            animator.SetInteger("ComboStep", comboStep);
            animator.SetTrigger("Attack");

            // --- LUNGE FORWARD FOR ATTACKS ---
            if (playerRb == null) playerRb = GetComponent<Rigidbody>();
            if (playerRb != null)
            {
                float lungeForce = 3.5f; // lunge padrão
                Player_WeaponManager wm = GetComponent<Player_WeaponManager>();
                if (wm != null && wm.rightHand != null && wm.rightHand.childCount > 0)
                {
                    WeaponOffset offsetData = wm.rightHand.GetChild(0).GetComponent<WeaponOffset>();
                    if (offsetData != null)
                    {
                        if (offsetData.weaponType == WeaponType.Axe)
                        {
                            lungeForce = 7.5f; // Machado lunge mais forte e pesado
                        }
                        else if (offsetData.weaponType == WeaponType.Dagger)
                        {
                            lungeForce = 5f; // Adaga lunge médio rápido
                        }
                    }
                }

                // Aplica impulso na direção frontal do jogador
                Vector3 lungeDir = transform.forward;
                playerRb.linearVelocity = new Vector3(lungeDir.x * lungeForce, playerRb.linearVelocity.y, lungeDir.z * lungeForce);
                Debug.Log($"[PrimaryAttackKnife] Lunge aplicado com força {lungeForce} na direção {lungeDir}");
            }

            // Aplicar Attack Speed multiplicada pela velocidade específica de cada passo do combo (apenas para o Machado)
            if (playerAttributes != null)
            {
                float stepSpeedMult = 1.0f;
                
                // Verifica se a arma equipada é o Machado
                Player_WeaponManager wm = GetComponent<Player_WeaponManager>();
                bool isAxe = false;
                if (wm != null && wm.rightHand != null && wm.rightHand.childCount > 0)
                {
                    WeaponOffset offsetData = wm.rightHand.GetChild(0).GetComponent<WeaponOffset>();
                    if (offsetData != null && offsetData.weaponType == WeaponType.Axe)
                    {
                        isAxe = true;
                    }
                }

                if (isAxe)
                {
                    if (comboStep == 1) stepSpeedMult = hit1SpeedMultiplier;
                    else if (comboStep == 2) stepSpeedMult = hit2SpeedMultiplier;
                    else if (comboStep == 3) stepSpeedMult = hit3SpeedMultiplier;
                    else if (comboStep == 4) stepSpeedMult = hit4SpeedMultiplier;
                }

                animator.speed = attackAnimationSpeed * playerAttributes.attackSpeedMelee * stepSpeedMult;
            }

            if (comboResetCoroutine != null) StopCoroutine(comboResetCoroutine);
            comboResetCoroutine = StartCoroutine(ResetComboAfterTime());

            // Inicia a corrotina de backup caso a animação do modelo não tenha eventos configurados (ex: Goku/custom)
            if (backupAttackCoroutine != null) StopCoroutine(backupAttackCoroutine);
            backupAttackCoroutine = StartCoroutine(BackupAttackSequenceCoroutine());
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[PrimaryAttackKnife] Error during PerformNextAttack: {ex.Message}");
            canAttack = true;
            isAttacking = false;
        }
    }

    private System.Collections.IEnumerator BackupAttackSequenceCoroutine()
    {
        eventFiredEnableHitbox = false;
        eventFiredDisableHitbox = false;
        eventFiredOpenWindow = false;

        // Fator de escala da velocidade de ataque (quanto mais rápido o ataque, menor o delay)
        float speedMultiplier = 1f;
        if (playerAttributes != null)
        {
            speedMultiplier = attackAnimationSpeed * playerAttributes.attackSpeedMelee;
        }
        // Evitar divisão por zero ou velocidades negativas
        if (speedMultiplier <= 0f) speedMultiplier = 1f;

        // 1. Aguarda para ativar o colisor de dano (ajustável no Inspector)
        yield return new WaitForSeconds(currentHitDelay / speedMultiplier);
        if (!eventFiredEnableHitbox)
        {
            EnableHitbox();
        }

        // 2. Aguarda a duração ativa do golpe (ajustável no Inspector)
        yield return new WaitForSeconds(currentHitDuration / speedMultiplier);
        if (!eventFiredDisableHitbox)
        {
            DisableHitbox();
        }

        // 3. Libera o próximo clique de ataque (por volta de 0.1s após desativar o hitbox)
        yield return new WaitForSeconds(0.1f / speedMultiplier);
        if (!eventFiredOpenWindow)
        {
            OpenAttackWindow();
        }
    }

    public void RegisterHit(Collider enemyCollider)
    {
        // Busca DummyHealth ou ShardSwarmHealth: primeiro no próprio collider, depois no pai
        DummyHealth enemy = enemyCollider.GetComponent<DummyHealth>()
                         ?? enemyCollider.GetComponentInParent<DummyHealth>();

        ShardSwarmHealth swarmEnemy = enemyCollider.GetComponent<ShardSwarmHealth>()
                                   ?? enemyCollider.GetComponentInParent<ShardSwarmHealth>();

        if (enemy == null && swarmEnemy == null) return;

        // Anti-hit-duplo: usa o Collider do objeto raiz de vida
        Collider rootCollider;
        if (enemy != null)
            rootCollider = enemy.GetComponent<Collider>() ?? enemyCollider;
        else
            rootCollider = swarmEnemy.GetComponent<Collider>() ?? enemyCollider;

        if (enemiesHitInThisAttack.Contains(rootCollider)) return;
        enemiesHitInThisAttack.Add(rootCollider);

        if (comboStep > 0 && comboStep <= currentDamages.Length)
        {
            int baseDamage = currentDamages[comboStep - 1];
            int finalDamage = baseDamage;
            bool isCritical = false;

            // Aplicar multiplicador de dano BASE
            if (playerAttributes != null)
                finalDamage = Mathf.RoundToInt(baseDamage * playerAttributes.baseDamageMultiplier);

            // Aplicar multiplicador de dano do PlayerHealth
            if (playerHealth != null)
                finalDamage = Mathf.RoundToInt(finalDamage * playerHealth.damageMultiplier);

            // Calcular crítico
            if (playerAttributes != null)
            {
                float critRoll = Random.Range(0f, 100f);
                if (critRoll < playerAttributes.critChance)
                {
                    finalDamage = Mathf.RoundToInt(finalDamage * playerAttributes.critMultiplier);
                    isCritical = true;
                }
            }

            // Aplica dano no componente correto
            if (enemy != null)
            {
                enemy.TakeDamage(finalDamage, isCritical);
                if (playerAttributes != null && playerAttributes.slowOnHit > 0f)
                {
                    enemy.ApplySlow(playerAttributes.slowOnHit, 3.0f);
                }
            }
            else if (swarmEnemy != null)
            {
                swarmEnemy.TakeDamage(finalDamage, isCritical);
                if (playerAttributes != null && playerAttributes.slowOnHit > 0f)
                {
                    swarmEnemy.ApplySlow(playerAttributes.slowOnHit, 3.0f);
                }
            }

            // Aplicar Knockback — o Rigidbody pode estar no pai (ex: ShardSwarm)
            if (playerAttributes != null)
            {
                Rigidbody enemyRb = enemyCollider.GetComponent<Rigidbody>()
                                 ?? enemyCollider.GetComponentInParent<Rigidbody>();
                if (enemyRb != null)
                {
                    Vector3 knockbackDirection = (enemyCollider.transform.position - transform.position).normalized;
                    knockbackDirection.y = 0;
                    float knockbackForce = playerAttributes.knockback * 10f;
                    enemyRb.AddForce(knockbackDirection * knockbackForce, ForceMode.Impulse);
                }
            }

            // Escolhe o VFX: tenta pegar do array de variações, se estiver vazio, usa o antigo
            GameObject vfxToSpawn = hitImpactPrefab;
            if (hitImpactVariations != null && hitImpactVariations.Length > 0)
            {
                vfxToSpawn = hitImpactVariations[Random.Range(0, hitImpactVariations.Length)];
            }

            if (vfxToSpawn != null)
            {
                Vector3 hitPoint = enemyCollider.ClosestPoint(transform.position + Vector3.up);
                GameObject hitVFX = Instantiate(vfxToSpawn, hitPoint, Quaternion.identity);
                Destroy(hitVFX, 2f);
            }
        }
    }

    // Eventos de Animação (ou chamados pela corrotina de backup)
    public void EnableHitbox()
    {
        eventFiredEnableHitbox = true;
        isHitboxActive = true;
        enemiesHitInThisAttack.Clear();

        if (currentHitbox != null)
        {
            currentHitbox.isTrigger = true;
            currentHitbox.enabled = true;

            WeaponHitbox hb = currentHitbox.GetComponent<WeaponHitbox>();
            if (hb == null)
            {
                hb = currentHitbox.gameObject.AddComponent<WeaponHitbox>();
            }
            hb.primaryAttackScript = this;
        }
    }

    public void DisableHitbox()
    {
        eventFiredDisableHitbox = true;
        isHitboxActive = false;
        if (currentHitbox != null) currentHitbox.enabled = false;
    }

    public void OpenAttackWindow()
    {
        eventFiredOpenWindow = true;
        
        if (hasBufferedAttack)
        {
            hasBufferedAttack = false;
            Debug.Log("[PrimaryAttackKnife] Consumindo ataque buffered.");
            if (comboResetCoroutine != null) StopCoroutine(comboResetCoroutine);
            PerformNextAttack();
        }
        else
        {
            canAttack = true;
        }
    }

    // Reset Combo
    public void ResetCombo()
    {
        // Se a janela de ataque estiver fechada (canAttack == false), significa que o jogador
        // já emendou o próximo golpe do combo e a nova animação está iniciando/fazendo blend.
        // Ignoramos este reset para evitar que eventos de animação antigos cortem o combo ativo.
        if (!canAttack) return;

        isAttacking = false;
        isHitboxActive = false;
        hasBufferedAttack = false;
        comboStep = 0;
        animator.SetInteger("ComboStep", 0);
        canAttack = true;

        if (currentHitbox != null) currentHitbox.enabled = false;

        // Resetar velocidade da animação
        if (animator != null)
        {
            animator.speed = 1f;
        }

        if (comboResetCoroutine != null) StopCoroutine(comboResetCoroutine);
    }

    private IEnumerator ResetComboAfterTime()
    {
        yield return new WaitForSeconds(comboResetTime);
        ResetCombo();
    }

    // Aplicar escala de weapon range ao collider
    private void ApplyWeaponRangeScale()
    {
        try
        {
            if (currentHitbox == null || playerAttributes == null) return;

            BoxCollider boxCollider = currentHitbox as BoxCollider;
            if (boxCollider != null)
            {
                // Escalar apenas o eixo Y (para frente da arma) - como alongar uma adaga em espada
                Vector3 newSize = new Vector3(
                    currentOriginalSize.x,  // Largura: mantém original
                    currentOriginalSize.y * playerAttributes.weaponRangeMelee,  // Comprimento: escala
                    currentOriginalSize.z   // Profundidade: mantém original
                );
                boxCollider.size = newSize;
                Debug.Log($"🎯 Weapon Range aplicado! Size Y: {currentOriginalSize.y:F2} → {newSize.y:F2} ({playerAttributes.weaponRangeMelee}x)");
            }
        }
        catch (System.Exception)
        {
        }
    }

    // Equip Logic
    public void EquipDefaultWeapon()
    {
        currentDamages = defaultDamages;
        currentRange = defaultRange;
        attackAnimationSpeed = defaultAttackSpeed; // Retorna à velocidade padrão
        comboResetTime = defaultComboResetTime;
        currentHitDelay = defaultHitDelay;
        currentHitDuration = defaultHitDuration;
        currentHitbox = handHitbox;

        if (handHitbox != null)
        {
            BoxCollider boxCollider = handHitbox as BoxCollider;
            if (boxCollider != null)
            {
                originalHandHitboxSize = boxCollider.size;
                currentOriginalSize = originalHandHitboxSize;
            }
        }

        if (equippedWeaponHitbox != null) equippedWeaponHitbox.enabled = false;
    }

    public void EquipDaggerWeapon(Collider daggerHitbox)
    {
        currentDamages = daggerDamages;
        currentRange = daggerRange;
        attackAnimationSpeed = defaultAttackSpeed; // Retorna à velocidade padrão
        comboResetTime = daggerComboResetTime;
        currentHitDelay = daggerHitDelay;
        currentHitDuration = daggerHitDuration;
        equippedWeaponHitbox = daggerHitbox;
        currentHitbox = equippedWeaponHitbox;

        if (equippedWeaponHitbox != null)
        {
            BoxCollider boxCollider = equippedWeaponHitbox as BoxCollider;
            if (boxCollider != null)
            {
                originalWeaponHitboxSize = boxCollider.size;
                currentOriginalSize = originalWeaponHitboxSize;
            }
        }

        if (handHitbox != null) handHitbox.enabled = false;
        hasWeapon = true;

        ApplyWeaponRangeScale();
    }

    public void EquipSwordWeapon(Collider swordHitbox)
    {
        currentDamages = swordDamages;
        currentRange = swordRange;
        attackAnimationSpeed = defaultAttackSpeed; // Retorna à velocidade padrão
        comboResetTime = swordComboResetTime;
        currentHitDelay = defaultHitDelay; // Usa padrão para espada
        currentHitDuration = defaultHitDuration;
        equippedWeaponHitbox = swordHitbox;
        currentHitbox = equippedWeaponHitbox;

        if (equippedWeaponHitbox != null)
        {
            BoxCollider boxCollider = equippedWeaponHitbox as BoxCollider;
            if (boxCollider != null)
            {
                originalWeaponHitboxSize = boxCollider.size;
                currentOriginalSize = originalWeaponHitboxSize;
            }
        }

        if (handHitbox != null) handHitbox.enabled = false;
        hasWeapon = true;

        ApplyWeaponRangeScale();
    }


    public void EquipAxeWeapon(Collider axeHitbox)
    {
        // Garante que o array tenha pelo menos 4 elementos no runtime (sobrescrevendo serializações antigas de 3 elementos da Unity)
        if (axeDamages == null || axeDamages.Length < 4)
        {
            axeDamages = new int[] { 45, 60, 110, 150 };
            Debug.LogWarning("[PrimaryAttackKnife] Corrigido tamanho de axeDamages em runtime para 4 elementos para liberar o quarto combo.");
        }

        currentDamages = axeDamages; 
        currentRange = axeRange; 
        attackAnimationSpeed = axeAttackSpeed; 
        comboResetTime = axeComboResetTime;
        currentHitDelay = axeHitDelay;
        currentHitDuration = axeHitDuration;
        
        equippedWeaponHitbox = axeHitbox;
        currentHitbox = equippedWeaponHitbox;

        if (equippedWeaponHitbox != null)
        {
            BoxCollider boxCollider = equippedWeaponHitbox as BoxCollider;
            if (boxCollider != null)
            {
                originalWeaponHitboxSize = boxCollider.size;
                currentOriginalSize = originalWeaponHitboxSize;
            }
        }

        if (handHitbox != null) handHitbox.enabled = false;
        hasWeapon = true;

        ApplyWeaponRangeScale();
    }
}