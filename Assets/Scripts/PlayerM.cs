using UnityEngine;

public class PlayerM : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 5f;
    public float sprintSpeed = 10f;
    public float rotationSpeed = 10f;

    [Header("Dash")]
    public float dashSpeed = 30f;
    public float dashDuration = 0.2f;
    public float dashCd = 1.5f;
    public int maxDashes = 2;
    private int dashesLeft;
    private float dashCdTimer;
    public Transform groundCheck;
    public LayerMask whatIsGround;

    private bool grounded;

    [Header("Animation")]
    public Animator animator;
    public float sprintAnimationSpeedMultiplier = 0.8f;

    private Rigidbody rb;
    private Vector3 moveDirection;
    private float currentSpeed;

    private PrimaryAttackKnife attackScript;

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

        attackScript = GetComponentInChildren<PrimaryAttackKnife>();
        if (attackScript == null)
        {
            Debug.LogError("PrimaryAttackKnife não encontrado.");
        }
    }

    private void Update()
    {
        grounded = Physics.CheckSphere(groundCheck.position, 0.2f, whatIsGround);

        MyInput();
        LookAtMoveDirection();

        if (animator != null)
        {
            if (attackScript != null && animator.GetBool("isSAKnife"))
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
        MovePlayer();
    }

    private void MyInput()
    {
        float horizontalInput = Input.GetAxisRaw("Horizontal");
        float verticalInput = Input.GetAxisRaw("Vertical");

        if (Input.GetKey(KeyCode.LeftShift))
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
    public void RechargeDashes(int amount)
    {
        dashesLeft += amount;
        if (dashesLeft > maxDashes)
        {
            dashesLeft = maxDashes;
        }
    }

}