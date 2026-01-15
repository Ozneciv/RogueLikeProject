using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PrimaryAttackKnife : MonoBehaviour
{
    // --- MUDANÇA 1: Variável que indica se a janela de dano está aberta ---
    public bool isHitboxActive { get; private set; } = false;

    // Mantemos isAttacking apenas para lógica de Combo, não mais para travar movimento
    public bool isAttacking { get; private set; } 

    [Header("Estado da Arma")]
    public bool hasWeapon = false; 

    [Header("Required Components")]
    public Animator animator;
    public Rigidbody playerRb;
    public PlayerHealth playerHealth;

    [Header("Weapon Hitbox")]
    public Collider handHitbox;
    private Collider equippedWeaponHitbox;
    private Collider currentHitbox;

    [Header("Attack Stats")]
    public float currentRange;
    public float defaultRange = 2f;
    public float daggerRange = 5f;
    public float swordRange = 7f;

    [Header("VFX")]
    public GameObject hitImpactPrefab; 

    [Header("Settings")]
    public float attackAnimationSpeed = 1.0f;
    
    // REMOVIDO: public float movementLockDuration; -> Não usaremos mais tempo fixo

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
    }

    private void Update()
    {
        if ((Input.GetKeyDown(KeyCode.Q) || Input.GetMouseButtonDown(0)) && canAttack && hasWeapon)
        {
            if (comboResetCoroutine != null) StopCoroutine(comboResetCoroutine);
            PerformNextAttack();
        }
    }

    private void PerformNextAttack()
    {
        // --- MUDANÇA 2: Não chamamos mais a rotina de travar movimento aqui ---
        // O travamento será controlado EXCLUSIVAMENTE pelos Animation Events
        
        isAttacking = true; // Apenas para saber que estamos num combo
        canAttack = false;
        comboStep++;
        
        animator.SetInteger("ComboStep", comboStep);
        animator.SetTrigger("Attack");

        // (Sem AddForce para não deslizar)

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

            if (playerHealth != null)
                finalDamage = Mathf.RoundToInt(baseDamage * playerHealth.damageMultiplier);
            
            DummyHealth enemy = enemyCollider.GetComponent<DummyHealth>();
            if (enemy != null) enemy.TakeDamage(finalDamage);

            if (hitImpactPrefab != null)
            {
                Vector3 hitPoint = enemyCollider.ClosestPoint(transform.position + Vector3.up);
                GameObject hitVFX = Instantiate(hitImpactPrefab, hitPoint, Quaternion.identity);
                Destroy(hitVFX, 2f);
            }
        }
    }
    
    // --- EVENTOS DE ANIMAÇÃO (A Mágica acontece aqui) ---

    // Chamado na Animation quando o golpe começa a valer (janela de dano)
    public void EnableHitbox()
    {
        isHitboxActive = true; // <--- AGORA O PLAYER VAI DESACELERAR
        enemiesHitInThisAttack.Clear();
        if (currentHitbox != null) currentHitbox.enabled = true;
    }

    // Chamado na Animation quando o golpe termina
    public void DisableHitbox()
    {
        isHitboxActive = false; // <--- AGORA O PLAYER VOLTA A CORRER
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
        isHitboxActive = false; // Segurança
        comboStep = 0;
        animator.SetInteger("ComboStep", 0);
        canAttack = true;
        if (comboResetCoroutine != null) StopCoroutine(comboResetCoroutine);
    }
    
    private IEnumerator ResetComboAfterTime()
    {
        yield return new WaitForSeconds(comboResetTime);
        ResetCombo();
    }

    // Equip Logic (Mantido igual)
    public void EquipDefaultWeapon() { currentDamages = defaultDamages; currentRange = defaultRange; currentHitbox = handHitbox; if (equippedWeaponHitbox != null) equippedWeaponHitbox.enabled = false; }
    public void EquipDaggerWeapon(Collider daggerHitbox) { currentDamages = daggerDamages; currentRange = daggerRange; equippedWeaponHitbox = daggerHitbox; currentHitbox = equippedWeaponHitbox; if (handHitbox != null) handHitbox.enabled = false; hasWeapon = true; }
    public void EquipSwordWeapon(Collider swordHitbox) { currentDamages = swordDamages; currentRange = swordRange; equippedWeaponHitbox = swordHitbox; currentHitbox = equippedWeaponHitbox; if (handHitbox != null) handHitbox.enabled = false; hasWeapon = true; }
}