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
    public float axeAttackSpeed = 0.6f; // Velocidade reduzida e pesada para o machado

    [Header("Weapon Damages")]
    public int[] defaultDamages = { 10, 15, 30 };
    public int[] daggerDamages = { 25, 35, 60 };
    public int[] swordDamages = { 30, 40, 75 };
    public int[] axeDamages = { 45, 60, 110 }; // Dano pesado de impacto para o Machado
    private int[] currentDamages;

    [Header("Combo Settings")]
    public float comboResetTime = 1.2f;
    private int comboStep = 0;
    private bool canAttack = true;
    private Coroutine comboResetCoroutine;
    private Coroutine backupAttackCoroutine;
    private bool eventFiredEnableHitbox = false;
    private bool eventFiredDisableHitbox = false;
    private bool eventFiredOpenWindow = false;
    private List<Collider> enemiesHitInThisAttack;

    private void Start()
    {
        enemiesHitInThisAttack = new List<Collider>();
        EquipDefaultWeapon();
        hasWeapon = false;
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

            // Manter attack speed durante combo
            if (isAttacking && playerAttributes != null && animator != null && animator.isActiveAndEnabled)
            {
                float targetSpeed = attackAnimationSpeed * playerAttributes.attackSpeedMelee;
                if (Mathf.Abs(animator.speed - targetSpeed) > 0.01f)
                {
                    animator.speed = targetSpeed;
                }
            }

            if ((Input.GetKeyDown(KeyCode.Q) || Input.GetMouseButtonDown(0)) && canAttack)
            {
                if (comboResetCoroutine != null) StopCoroutine(comboResetCoroutine);
                PerformNextAttack();
            }
        }
        catch (System.Exception)
        {
            // Evita crashar o loop de update se referências estiverem se restabelecendo
        }
    }

    private void PerformNextAttack()
    {
        try
        {
            if (animator == null)
            {
                Debug.LogWarning("[PrimaryAttackKnife] Animator is null! Finding animator dynamically...");
                animator = GetComponentInChildren<Animator>() ?? GetComponentInParent<Animator>();
                
                if (animator == null)
                {
                    Debug.LogError("[PrimaryAttackKnife] Critical: Animator not found! Aborting attack sequence.");
                    canAttack = true;
                    isAttacking = false;
                    return;
                }
            }

            isAttacking = true;
            canAttack = false;
            comboStep++;

            animator.SetInteger("ComboStep", comboStep);
            animator.SetTrigger("Attack");

            // Aplicar Attack Speed
            if (playerAttributes != null)
            {
                animator.speed = attackAnimationSpeed * playerAttributes.attackSpeedMelee;
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

        // 1. Aguarda para ativar o colisor de dano (o swing do golpe acontece por volta de 0.15s)
        yield return new WaitForSeconds(0.15f / speedMultiplier);
        if (!eventFiredEnableHitbox)
        {
            EnableHitbox();
        }

        // 2. Aguarda a duração ativa do golpe (por volta de 0.2s)
        yield return new WaitForSeconds(0.2f / speedMultiplier);
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
                enemy.TakeDamage(finalDamage, isCritical);
            else if (swarmEnemy != null)
                swarmEnemy.TakeDamage(finalDamage, isCritical);

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
        if (currentHitbox != null) currentHitbox.enabled = true;
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
        canAttack = true;
    }

    // Reset Combo
    public void ResetCombo()
    {
        isAttacking = false;
        isHitboxActive = false;
        comboStep = 0;
        animator.SetInteger("ComboStep", 0);
        canAttack = true;

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
        currentDamages = axeDamages; 
        currentRange = axeRange; 
        attackAnimationSpeed = axeAttackSpeed; 
        
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