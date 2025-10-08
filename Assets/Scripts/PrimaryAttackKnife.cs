using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PrimaryAttackKnife : MonoBehaviour
{
    // --- MUDANÇA 1: Variável de Estado de Ataque ---
    // Esta variável será 'true' quando um combo estiver ativo. Outros scripts podem lê-la.
    public bool isAttacking { get; private set; }

    [Header("Required Components")]
    public Animator animator;
    public Rigidbody playerRb;

    // ... (O resto do script continua igual) ...
    [Header("Weapon Hitbox")]
    public Collider handHitbox;
    private Collider equippedWeaponHitbox;
    private Collider currentHitbox;

    [Header("Attack Stats")]
    public float currentRange;
    public float defaultRange = 2f;
    public float daggerRange = 5f;

    [Header("Weapon Damages")]
    public int[] defaultDamages = { 10, 15, 30 };
    public int[] daggerDamages = { 25, 35, 60 };
    private int[] currentDamages;

    [Header("Attack Settings")]
    public LayerMask enemyLayer;
    public float[] attackLungeForces = { 2f, 2f, 8f };

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
        isAttacking = false; // Garante que começa como 'false'
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q) && canAttack)
        {
            if (comboResetCoroutine != null) StopCoroutine(comboResetCoroutine);
            PerformNextAttack();
        }
    }

    private void PerformNextAttack()
    {
        // --- MUDANÇA 2: Ativar o Estado de Ataque ---
        isAttacking = true;

        canAttack = false;
        comboStep++;
        animator.SetInteger("ComboStep", comboStep);
        animator.SetTrigger("Attack");

        if (playerRb != null && comboStep <= attackLungeForces.Length)
        {
            float forceToApply = attackLungeForces[comboStep - 1];
            playerRb.AddForce(transform.forward * forceToApply, ForceMode.Impulse);
        }
        comboResetCoroutine = StartCoroutine(ResetComboAfterTime());
    }

    public void EnableHitbox()
    {
        enemiesHitInThisAttack.Clear();
        if (currentHitbox != null) currentHitbox.enabled = true;
    }

    public void DisableHitbox()
    {
        if (currentHitbox != null) currentHitbox.enabled = false;
    }

    public void RegisterHit(Collider enemyCollider)
    {
        if (enemiesHitInThisAttack.Contains(enemyCollider)) return;
        enemiesHitInThisAttack.Add(enemyCollider);

        if (comboStep > 0 && comboStep <= currentDamages.Length)
        {
            int damageToDeal = currentDamages[comboStep - 1];
            enemyCollider.GetComponent<DummyHealth>().TakeDamage(damageToDeal);
            Debug.Log("ACERTOU com " + currentHitbox.name + "! Ataque " + comboStep + " causou " + damageToDeal + " de dano em " + enemyCollider.name);
        }
    }

    public void OpenAttackWindow() { canAttack = true; }
    
    public void ResetCombo()
    {
        // --- MUDANÇA 3: Desativar o Estado de Ataque ---
        isAttacking = false;

        // Debug.Log("Combo resetado.");
        comboStep = 0;
        animator.SetInteger("ComboStep", 0);
        canAttack = true;
        if (comboResetCoroutine != null)
        {
            StopCoroutine(comboResetCoroutine);
            comboResetCoroutine = null;
        }
    }

    private IEnumerator ResetComboAfterTime()
    {
        yield return new WaitForSeconds(comboResetTime);
        ResetCombo();
    }
    
    public void EquipDefaultWeapon()
    {
        currentDamages = defaultDamages;
        currentRange = defaultRange;
        currentHitbox = handHitbox;

        if (equippedWeaponHitbox != null) equippedWeaponHitbox.enabled = false;
    }

    public void EquipDaggerWeapon(Collider daggerHitbox)
    {
        currentDamages = daggerDamages;
        currentRange = daggerRange;
        equippedWeaponHitbox = daggerHitbox;
        currentHitbox = equippedWeaponHitbox;

        if (handHitbox != null) handHitbox.enabled = false;
    }
}