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
    private float originalHandHitboxRadius;
    private float originalWeaponHitboxRadius;
    private float currentOriginalRadius;
    private float lastAppliedWeaponRange = 1f;

    [Header("Attack Stats")]
    public float currentRange;
    public float defaultRange = 2f;
    public float daggerRange = 5f;
    public float swordRange = 7f;

    [Header("VFX")]
    public GameObject hitImpactPrefab;

    [Header("Settings")]
    public float attackAnimationSpeed = 1.0f;

    [Header("Weapon Damages")]
    public int[] defaultDamages = { 10, 15, 30 };
    public int[] daggerDamages = { 25, 35, 60 };
    public int[] swordDamages = { 30, 40, 75 };
    private int[] currentDamages;

    [Header("Combo Settings")]
    public float comboResetTime = 1.2f;
    private int comboStep = 0;
    private bool canAttack = true;
    private Coroutine comboResetCoroutine;
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
        if (isAttacking && playerAttributes != null && animator != null)
        {
            float targetSpeed = attackAnimationSpeed * playerAttributes.attackSpeedMelee;
            if (Mathf.Abs(animator.speed - targetSpeed) > 0.01f)
            {
                animator.speed = targetSpeed;
            }
        }
        
        if ((Input.GetKeyDown(KeyCode.Q) || Input.GetMouseButtonDown(0)) && canAttack && hasWeapon)
        {
            if (comboResetCoroutine != null) StopCoroutine(comboResetCoroutine);
            PerformNextAttack();
        }
    }

    private void PerformNextAttack()
    {
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

        comboResetCoroutine = StartCoroutine(ResetComboAfterTime());
    }

    public void RegisterHit(Collider enemyCollider)
    {
        if (enemiesHitInThisAttack.Contains(enemyCollider)) return;
        enemiesHitInThisAttack.Add(enemyCollider);

        if (comboStep > 0 && comboStep <= currentDamages.Length)
        {
            int baseDamage = currentDamages[comboStep - 1];
            int finalDamage = baseDamage;
            bool isCritical = false;
            
            // Aplicar multiplicador de dano BASE (afeta críticos)
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
                    Debug.Log($"💥 CRÍTICO! Dano: {finalDamage} ({playerAttributes.critMultiplier}x)");
                }
            }

            DummyHealth enemy = enemyCollider.GetComponent<DummyHealth>();
            if (enemy != null) enemy.TakeDamage(finalDamage, isCritical);

            // Aplicar Knockback
            if (playerAttributes != null)
            {
                Rigidbody enemyRb = enemyCollider.GetComponent<Rigidbody>();
                if (enemyRb != null)
                {
                    Vector3 knockbackDirection = (enemyCollider.transform.position - transform.position).normalized;
                    knockbackDirection.y = 0;
                    float knockbackForce = playerAttributes.knockback * 100f;
                    enemyRb.AddForce(knockbackDirection * knockbackForce, ForceMode.Impulse);
                    Debug.Log($"🔨 KNOCKBACK! Força: {knockbackForce}");
                }
            }

            if (hitImpactPrefab != null)
            {
                Vector3 hitPoint = enemyCollider.ClosestPoint(transform.position + Vector3.up);
                GameObject hitVFX = Instantiate(hitImpactPrefab, hitPoint, Quaternion.identity);
                Destroy(hitVFX, 2f);
            }
        }
    }
    
    // Eventos de Animação
    public void EnableHitbox()
    {
        isHitboxActive = true;
        enemiesHitInThisAttack.Clear();
        if (currentHitbox != null) currentHitbox.enabled = true;
    }

    public void DisableHitbox()
    {
        isHitboxActive = false;
        if (currentHitbox != null) currentHitbox.enabled = false;
    }

    public void OpenAttackWindow()
    {
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
        if (currentHitbox == null || playerAttributes == null) return;
        
        SphereCollider sphereCollider = currentHitbox as SphereCollider;
        if (sphereCollider != null)
        {
            float newRadius = currentOriginalRadius * playerAttributes.weaponRangeMelee;
            sphereCollider.radius = newRadius;
            Debug.Log($"🎯 Weapon Range aplicado! Raio: {currentOriginalRadius:F2} → {newRadius:F2} ({playerAttributes.weaponRangeMelee}x)");
        }
    }

    // Equip Logic
    public void EquipDefaultWeapon()
    {
        currentDamages = defaultDamages;
        currentRange = defaultRange;
        currentHitbox = handHitbox;
        
        if (handHitbox != null)
        {
            SphereCollider sphereCollider = handHitbox as SphereCollider;
            if (sphereCollider != null)
            {
                originalHandHitboxRadius = sphereCollider.radius;
                currentOriginalRadius = originalHandHitboxRadius;
            }
        }
        
        if (equippedWeaponHitbox != null) equippedWeaponHitbox.enabled = false;
    }

    public void EquipDaggerWeapon(Collider daggerHitbox)
    {
        currentDamages = daggerDamages;
        currentRange = daggerRange;
        equippedWeaponHitbox = daggerHitbox;
        currentHitbox = equippedWeaponHitbox;
        
        if (equippedWeaponHitbox != null)
        {
            SphereCollider sphereCollider = equippedWeaponHitbox as SphereCollider;
            if (sphereCollider != null)
            {
                originalWeaponHitboxRadius = sphereCollider.radius;
                currentOriginalRadius = originalWeaponHitboxRadius;
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
        equippedWeaponHitbox = swordHitbox;
        currentHitbox = equippedWeaponHitbox;
        
        if (equippedWeaponHitbox != null)
        {
            SphereCollider sphereCollider = equippedWeaponHitbox as SphereCollider;
            if (sphereCollider != null)
            {
                originalWeaponHitboxRadius = sphereCollider.radius;
                currentOriginalRadius = originalWeaponHitboxRadius;
            }
        }
        
        if (handHitbox != null) handHitbox.enabled = false;
        hasWeapon = true;
        
        ApplyWeaponRangeScale();
    }
}