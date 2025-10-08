using System.Collections;
using UnityEngine;

public class PrimaryAttackKnife : MonoBehaviour
{
    public Animator animator;

    // Variáveis para controle de dano e alcance
    [Header("Attack Stats")]
    public int currentDamage;
    public float currentRange;
    public Rigidbody playerRb;
    public int defaultDamage = 10;
    public float defaultRange = 2f;
    public int daggerDamage = 30;
    public float daggerRange = 5f;

    [Header("Attack Settings")]
    public float comboDelay = 0.5f; // Tempo máximo entre os cliques para um combo
    public float attackRange = 2f; // Alcance do ataque
    public LayerMask enemyLayer;

    private float lastClickTime;
    private int clickCount = 0;
    private bool isAttacking = false;

    private void Start()
    {
        // Define os valores iniciais do ataque
        currentDamage = defaultDamage;
        currentRange = defaultRange;
    }

    private void Update()
    {
        // Reseta o contador de cliques se o tempo de combo acabar
        if (Time.time - lastClickTime > comboDelay)
        {
            clickCount = 0;
        }

        if (Input.GetKeyDown(KeyCode.Q))
        {
            lastClickTime = Time.time;
            clickCount++;
            
            // Exibe a contagem de cliques no console
            Debug.Log("Cliques no combo: " + clickCount);

            if (!isAttacking)
            {
                StartCoroutine(PerformSingleAttack());
            }
        }
    }

    private IEnumerator PerformSingleAttack()
    {
        isAttacking = true;
        animator.SetBool("isSAKnife", true);

        // Espera 1 segundo para forçar a parada da animação
        yield return new WaitForSeconds(1.0f);

        // Força a parada da animação
        animator.SetBool("isSAKnife", false);
        isAttacking = false;
    }

    // Função que causa dano, chamada no momento do ataque
    public void CauseDamage()
    {
        Collider[] enemiesInRange = Physics.OverlapSphere(transform.position, currentRange, enemyLayer);
        foreach (Collider enemy in enemiesInRange)
        {
            if (enemy.gameObject.GetComponent<DummyHealth>() != null)
            {
                enemy.gameObject.GetComponent<DummyHealth>().TakeDamage(currentDamage);
            }
        }
    }

    // Função pública para que o Player_WeaponManager possa atualizar os valores
    public void EquipWeapon(int damage, float range)
    {
        currentDamage = damage;
        currentRange = range;
    }

    // Esta função foi removida, pois a lógica de combo será no Update()
    // private IEnumerator PerformSingleAttack() {}

    // Esta função foi removida, pois a lógica de combo será no Update()
    // public void EndAttack() {}
}