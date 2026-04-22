using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerM : MonoBehaviour
{
    [Header("Referências")]
    public DashM dashScript;
    public PrimaryAttackKnife attackScript;

    [Header("Movement Settings")]
    public float walkSpeed = 5f;
    public float sprintSpeed = 10f;
    public float rotationSpeed = 10f;
    public float acceleration = 15f; 

    [Header("Hitbox Window Settings (O Ajuste Fino)")]
    [Tooltip("Velocidade FÍSICA durante o impacto. 0 = O boneco para no lugar. 5 = O boneco desliza.")]
    public float hitboxMoveSpeed = 0f; 

    [Tooltip("Rotação durante o impacto. 0 = Não vira. 2 = Vira devagar.")]
    public float hitboxRotationSpeed = 0f;

    [Tooltip("Velocidade da ANIMAÇÃO durante o impacto. 1 = Normal. 0.1 = Câmera Lenta (Matrix).")]
    public float hitboxAnimSpeed = 1f; // <--- A NOVA VARIÁVEL QUE FALTAVA

    [Header("Ground Check")]
    public Transform groundCheck;
    public LayerMask whatIsGround;
    public float groundRadius = 0.2f;
    private bool grounded;

    [Header("Animation")]
    public Animator animator;

    private Rigidbody rb;
    private PlayerAttributesDefensive playerAttributes;
    private Vector3 moveDirection;
    private float targetSpeed;
    private float currentRotationSpeed;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        if (attackScript == null) attackScript = GetComponent<PrimaryAttackKnife>();
        
        // Buscar PlayerAttributesDefensive
        playerAttributes = GetComponent<PlayerAttributesDefensive>();
        if (playerAttributes == null)
        {
            Debug.LogWarning("PlayerM: PlayerAttributesDefensive não encontrado! Speed multiplier não será aplicado.");
        }
    }

    private void Update()
    {
        if (groundCheck != null) grounded = Physics.CheckSphere(groundCheck.position, groundRadius, whatIsGround);

        MyInput();
        LookAtMoveDirection();
        UpdateAnimations();
    }

    private void FixedUpdate()
    {
        if (dashScript != null && dashScript.isDashing) return;
        MovePlayer();
    }

    private void MyInput()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        moveDirection = new Vector3(horizontal, 0, vertical).normalized;

        // Verifica se estamos na janela crítica de dano (EnableHitbox -> DisableHitbox)
        bool inDamageWindow = attackScript != null && attackScript.isHitboxActive;

        if (inDamageWindow)
        {
            // Usa as velocidades configuradas para o momento do impacto
            targetSpeed = hitboxMoveSpeed; 
            currentRotationSpeed = hitboxRotationSpeed;
        }
        else
        {
            // Fora do impacto, usa a velocidade normal de corrida
            // Aplicar Speed Multiplier do PlayerAttributesDefensive
            if (playerAttributes != null)
            {
                targetSpeed = sprintSpeed * playerAttributes.speedMultiplier;
            }
            else
            {
                targetSpeed = sprintSpeed;
            }
            currentRotationSpeed = rotationSpeed;
        }
    }

    private void MovePlayer()
    {
        bool inDamageWindow = attackScript != null && attackScript.isHitboxActive;
        
        Vector3 targetVelocity = moveDirection * targetSpeed;
        
        // Se der erro no Unity antigo, troque linearVelocity por velocity
        Vector3 currentVelocity = rb.linearVelocity; 

        if (inDamageWindow)
        {
            // Aplicação direta (sem inércia) para controle preciso durante o golpe
            rb.linearVelocity = new Vector3(targetVelocity.x, currentVelocity.y, targetVelocity.z);
        }
        else
        {
            // Movimento suave normal
            Vector3 newVelocity = new Vector3(targetVelocity.x, currentVelocity.y, targetVelocity.z);
            rb.linearVelocity = Vector3.Lerp(currentVelocity, newVelocity, acceleration * Time.fixedDeltaTime);
        }
    }

    private void LookAtMoveDirection()
    {
        if (moveDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, currentRotationSpeed * Time.deltaTime);
        }
    }

    private void UpdateAnimations()
    {
        if (animator == null) return;
        
        bool inDamageWindow = attackScript != null && attackScript.isHitboxActive;

        // --- LÓGICA DE VELOCIDADE DA ANIMAÇÃO ---
        if (inDamageWindow)
        {
            // AQUI ESTÁ A MUDANÇA: O código agora obedece o valor do Inspector
            animator.speed = hitboxAnimSpeed; 

            // Se você quer que as pernas parem de mexer visualmente se o moveSpeed for 0:
            if (hitboxMoveSpeed < 0.1f)
            {
                animator.SetFloat("Speed", 0f);
            }
        }
        else
        {
            // Velocidade normal fora do impacto
            animator.speed = 1f; 

            // Lógica normal de pernas correndo/paradas
            float velocityMagnitude = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z).magnitude;
            animator.SetFloat("Speed", velocityMagnitude > 0.1f ? 1f : 0f);
        }
    }
}