using UnityEngine;
using System.Collections;

public class PrimaryAttackKnife : MonoBehaviour
{
    [Header("Required Components")]
    public Animator animator;
    public Rigidbody playerRb;

    [Header("Attack Stats")]
    public float currentRange;
    public float defaultRange = 2f;
    public float daggerRange = 5f;

    // --- MUDANÇA 1: Arrays de Dano ---
    // Agora temos listas de dano para cada tipo de arma.
    [Header("Weapon Damages")]
    public int[] defaultDamages = { 10, 15, 30 };
    public int[] daggerDamages = { 25, 35, 60 }; // Exemplo de dano para a adaga
    private int[] currentDamages; // O dano que está ativo no momento

    [Header("Attack Settings")]
    public LayerMask enemyLayer;
    public float[] attackLungeForces = { 2f, 2f, 8f };

    [Header("Combo Settings")]
    public float comboResetTime = 1.2f;
    private int comboStep = 0;
    private bool canAttack = true;
    private Coroutine comboResetCoroutine;

    private void Start()
    {
        // Começa com a arma padrão (mãos vazias)
        EquipDefaultWeapon();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q) && canAttack)
        {
            if (comboResetCoroutine != null)
            {
                StopCoroutine(comboResetCoroutine);
            }
            PerformNextAttack();
        }
    }

    private void PerformNextAttack()
    {
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

    // --- MUDANÇA 2: Lógica de Dano no CauseDamage ---
    public void CauseDamage()
    {
        // Garante que temos um golpe válido e um array de dano configurado
        if (comboStep > 0 && comboStep <= currentDamages.Length)
        {
            // Pega o dano correto da lista baseado no golpe atual
            int damageToDeal = currentDamages[comboStep - 1];

            Collider[] enemiesInRange = Physics.OverlapSphere(transform.position, currentRange, enemyLayer);
            foreach (Collider enemy in enemiesInRange)
            {
                DummyHealth enemyHealth = enemy.gameObject.GetComponent<DummyHealth>();
                if (enemyHealth != null)
                {
                    enemyHealth.TakeDamage(damageToDeal);
                }
            }
            Debug.Log("Ataque " + comboStep + " causou " + damageToDeal + " de dano.");
        }
    }

    public void OpenAttackWindow()
    {
        canAttack = true;
    }



    public void ResetCombo()
    {
        Debug.Log("Combo resetado.");
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

    // --- MUDANÇA 3: Funções para Trocar de Arma ---
    public void EquipDefaultWeapon()
    {
        currentDamages = defaultDamages;
        currentRange = defaultRange;
        Debug.Log("Arma Padrão Equipada.");
    }

    public void EquipDaggerWeapon()
    {
        currentDamages = daggerDamages;
        currentRange = daggerRange;
        Debug.Log("Adaga Equipada.");
    }
}