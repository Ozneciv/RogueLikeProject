using UnityEngine;

public class PlayerM : MonoBehaviour
{
    // --- MUDANÇA 1: Referências aos outros scripts ---
    public DashM dashScript; // Arraste o componente DashM aqui
    public PrimaryAttackKnife attackScript; // Arraste o componente PrimaryAttackKnife aqui

    [Header("Movement")]
    public float walkSpeed = 5f;
    public float sprintSpeed = 10f;
    public float rotationSpeed = 10f;

    // ... (O resto das suas variáveis de movimento continua igual) ...
    [Header("Animation")]
    public Animator animator;
    public float sprintAnimationSpeedMultiplier = 0.8f;

    private Rigidbody rb;
    private Vector3 moveDirection;
    private float currentSpeed;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("Rigidbody não encontrado. Adicione um Rigidbody ao jogador.");
        }
        rb.freezeRotation = true;

        animator = GetComponentInChildren<Animator>();
        if (animator == null)
        {
            Debug.LogError("Animator não encontrado.");
        }
    }

    private void Update()
    {
        MyInput();
        LookAtMoveDirection();

        if (animator != null)
        {
            // ... (A sua lógica de animação continua a mesma) ...
            if (attackScript != null && attackScript.isAttacking)
            {
                animator.speed = 1f;
            }
            else
            {
                float speedMagnitude = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z).magnitude;
                if (speedMagnitude > 0.1f)
                {
                    animator.SetFloat("Speed", 1);
                    if (Input.GetKey(KeyCode.LeftShift))
                    {
                        animator.speed = (sprintSpeed / walkSpeed) * sprintAnimationSpeedMultiplier;
                    }
                    else
                    {
                        animator.speed = 1f;
                    }
                }
                else
                {
                    animator.SetFloat("Speed", 0);
                }
            }
        }
    }

    private void FixedUpdate()
    {
        // Se um dash estiver acontecendo, esta função para aqui.
        if (dashScript != null && dashScript.isDashing)
        {
            return;
        }
        MovePlayer();
    }

    private void MyInput()
    {
        float horizontalInput = Input.GetAxisRaw("Horizontal");
        float verticalInput = Input.GetAxisRaw("Vertical");

        // --- MUDANÇA 2: Condição para Correr ---
        // Agora só é possível correr se NÃO estiver atacando.
        if (Input.GetKey(KeyCode.LeftShift) && (attackScript == null || !attackScript.isAttacking))
        {
            currentSpeed = sprintSpeed;
        }
        else
        {
            currentSpeed = walkSpeed;
        }

        moveDirection = new Vector3(horizontalInput, 0, verticalInput).normalized;
    }

    private void MovePlayer()
    {
        rb.linearVelocity = new Vector3(moveDirection.x * currentSpeed, rb.linearVelocity.y, moveDirection.z * currentSpeed);
    }

    private void LookAtMoveDirection()
    {
        if (moveDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }
        // Adicione esta função dentro da classe PlayerM

    public void Die()
    {

    Debug.Log("O jogador morreu!");

    if (animator != null)
    {
        // NOVO: Força a animação a ser "in-loco", ignorando o movimento embutido nela.
        animator.applyRootMotion = true; 
        animator.SetTrigger("Death");
    }

    if (rb != null)
    {
        rb.linearVelocity = Vector3.zero;
        // COMENTADO: Deixamos a gravidade agir para o corpo cair realisticamente.
        // rb.isKinematic = true; 
    }

    // Desativa os scripts de controle para que o jogador não possa mais se mover ou atacar
    if (dashScript != null) dashScript.enabled = false;
    if (attackScript != null) attackScript.enabled = false;
    this.enabled = false; 

    // COMENTADO: Mantemos o collider PRINCIPAL para que o corpo não atravesse o chão.
    // Ver a solução avançada abaixo.
    // GetComponent<Collider>().enabled = false;
}
}