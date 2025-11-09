using UnityEngine;

public class PlayerM : MonoBehaviour
{
    [Header("Referências")]
    public DashM dashScript;
    public PrimaryAttackKnife attackScript;

    [Header("Movement")]
    public float walkSpeed = 5f;
    public float sprintSpeed = 10f;
    public float rotationSpeed = 10f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public LayerMask whatIsGround;
    private bool grounded;

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
    }

    private void Update()
    {
        grounded = Physics.CheckSphere(groundCheck.position, 0.2f, whatIsGround);

        MyInput();
        LookAtMoveDirection();
        UpdateAnimations();
    }

    private void FixedUpdate()
    {
        // Pausa o movimento se um dash estiver acontecendo
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

        // Condição para correr: só é possível se NÃO estiver atacando
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

    private void UpdateAnimations()
    {
        if (animator != null)
        {
            if (attackScript != null && attackScript.isAttacking)
            {
                // Garante que a velocidade da animação de ataque não seja alterada
                animator.speed = 1f;
            }
            else
            {
                float speedMagnitude = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z).magnitude;
                if (speedMagnitude > 0.1f)
                {
                    animator.SetFloat("Speed", 1);
                    if (currentSpeed > walkSpeed) // Checa se está correndo
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
}