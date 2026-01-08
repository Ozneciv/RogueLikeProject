using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PrimaryAttackKnife : MonoBehaviour
{
    // Variável para que outros scripts saibam se um ataque está em andamento
    public bool isAttacking { get; private set; }

    [Header("Required Components")]
    public Animator animator;
    public Rigidbody playerRb;
    public PlayerHealth playerHealth; // Referência ao script de vida para pegar o multiplicador de dano

    [Header("Weapon Hitbox")]
    public Collider handHitbox;
    private Collider equippedWeaponHitbox;
    private Collider currentHitbox;

    [Header("Attack Stats")]
    public float currentRange;
    public float defaultRange = 2f;
    public float daggerRange = 5f;
    public float swordRange = 7f;

    [Header("VFX (Efeitos Visuais)")]
    public GameObject hitImpactPrefab; // Mantemos o efeito de partícula!

    [Header("Animation Settings")]
    [Tooltip("Multiplicador de velocidade da animação de ataque. 1 = Normal, 2 = Dobro da velocidade.")]
    public float attackAnimationSpeed = 1.0f;

    [Header("Weapon Damages")]
    public int[] defaultDamages = { 10, 15, 30 };
    public int[] daggerDamages = { 25, 35, 60 };
    public int[] swordDamages = { 30, 40, 75 };
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
        isAttacking = false;
    }

    private void Update()
    {
        // Inicia o combo com a tecla 'Q' OU o botão esquerdo do mouse
        if ((Input.GetKeyDown(KeyCode.Q) || Input.GetMouseButtonDown(0)) && canAttack)
        {
            if (comboResetCoroutine != null) StopCoroutine(comboResetCoroutine);
            PerformNextAttack();
        }
    }

    private void PerformNextAttack()
    {
        isAttacking = true; // Avisa outros scripts que um ataque começou
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

    public void RegisterHit(Collider enemyCollider)
    {
        if (enemiesHitInThisAttack.Contains(enemyCollider)) return;
        enemiesHitInThisAttack.Add(enemyCollider);

        if (comboStep > 0 && comboStep <= currentDamages.Length)
        {
            int baseDamage = currentDamages[comboStep - 1];
            int finalDamage = baseDamage;

            // Aplica o multiplicador de dano dos pactos do Mercador
            if (playerHealth != null)
            {
                finalDamage = Mathf.RoundToInt(baseDamage * playerHealth.damageMultiplier);
            }
            
            // Causa o dano
            enemyCollider.GetComponent<DummyHealth>().TakeDamage(finalDamage);

            // --- HIT IMPACT (Visual) ---
            if (hitImpactPrefab != null)
            {
                // Calcula o ponto no colisor do inimigo mais próximo do jogador
                Vector3 hitPoint = enemyCollider.ClosestPoint(transform.position + Vector3.up);

                // Instancia o efeito visual
                GameObject hitVFX = Instantiate(hitImpactPrefab, hitPoint, Quaternion.identity);

                // Destroi o efeito após 2 segundos
                Destroy(hitVFX, 2f);
            }
            // ---------------------------


            Debug.Log("ACERTOU com " + currentHitbox.name + "! Dano final: " + finalDamage);
        }
    }
    
    public void ResetCombo()
    {
        isAttacking = false; // Avisa outros scripts que o ataque terminou
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
    
    // --- Funções chamadas por Animation Events ---
    public void EnableHitbox()
    {
        enemiesHitInThisAttack.Clear();
        if (currentHitbox != null) currentHitbox.enabled = true;
    }

    public void DisableHitbox()
    {
        if (currentHitbox != null) currentHitbox.enabled = false;
    }

    public void OpenAttackWindow()
    {
        canAttack = true;
    }
    
    // --- Funções para Trocar de Arma ---
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

    public void EquipSwordWeapon(Collider swordHitbox)
    {
        currentDamages = swordDamages;
        currentRange = swordRange;
        equippedWeaponHitbox = swordHitbox;
        currentHitbox = equippedWeaponHitbox;
        if (handHitbox != null) handHitbox.enabled = false;
    }
}