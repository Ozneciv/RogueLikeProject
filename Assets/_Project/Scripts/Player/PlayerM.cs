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
        if (attackScript == null) attackScript = GetComponent<PrimaryAttackKnife>() ?? GetComponentInChildren<PrimaryAttackKnife>();
        
        // Busca automática do Animator no Start para evitar ficar nulo
        if (animator == null)
        {
            Player_WeaponManager wm = GetComponent<Player_WeaponManager>() ?? GetComponentInParent<Player_WeaponManager>();
            if (wm != null && wm.playerAnimator != null && wm.playerAnimator.isActiveAndEnabled)
            {
                animator = wm.playerAnimator;
            }
            else
            {
                animator = GetComponentInChildren<Animator>(false) ?? GetComponentInParent<Animator>();
            }
        }

        // Buscar PlayerAttributesDefensive
        playerAttributes = GetComponent<PlayerAttributesDefensive>();
        if (playerAttributes == null)
        {
            Debug.LogWarning("PlayerM: PlayerAttributesDefensive não encontrado! Speed multiplier não será aplicado.");
        }
    }

    private void Update()
    {
        // Pressione K para imprimir diagnósticos de movimentação no console do Unity
        if (Input.GetKeyDown(KeyCode.K))
        {
            Debug.LogWarning($"=== DIAGNÓSTICO DE MOVIMENTAÇÃO ===");
            Debug.LogWarning($"[PlayerM] Script Ativo: {enabled}, Objeto: {gameObject.name}, Ativo na Hierarquia: {gameObject.activeInHierarchy}");
            Debug.LogWarning($"[PlayerM] Animator associado: {(animator != null ? animator.name : "null")}, Animator Ativo: {(animator != null ? animator.isActiveAndEnabled.ToString() : "false")}");
            Debug.LogWarning($"[PlayerM] Controller: {(animator != null && animator.runtimeAnimatorController != null ? animator.runtimeAnimatorController.name : "null")}");
            Debug.LogWarning($"[PlayerM] moveDirection: {moveDirection}, grounded: {grounded}, linearVelocity: {rb.linearVelocity}");
        }

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
            if (moveDirection != Vector3.zero)
            {
                // Aplicação direta (sem inércia) para controle preciso durante o golpe
                rb.linearVelocity = new Vector3(targetVelocity.x, currentVelocity.y, targetVelocity.z);
            }
            else
            {
                // Se não há input, deixa a inércia (do lunge) desacelerar suavemente
                rb.linearVelocity = Vector3.Lerp(currentVelocity, new Vector3(0f, currentVelocity.y, 0f), 5f * Time.fixedDeltaTime);
            }
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
        // Sempre sincroniza com o playerAnimator ativo do Player_WeaponManager se ele existir
        Player_WeaponManager wm = GetComponent<Player_WeaponManager>() ?? GetComponentInParent<Player_WeaponManager>();
        if (wm != null && wm.playerAnimator != null && wm.playerAnimator.isActiveAndEnabled)
        {
            animator = wm.playerAnimator;
        }
        else if (animator == null || !animator.isActiveAndEnabled)
        {
            animator = GetComponentInChildren<Animator>(false) ?? GetComponentInParent<Animator>();
        }

        if (animator == null || !animator.isActiveAndEnabled) return;
        
        try
        {
            // Se o player estiver no meio de um ataque, o script de ataque (PrimaryAttackKnife) assume controle absoluto das velocidades e parâmetros
            if (attackScript != null && attackScript.isAttacking)
            {
                return;
            }

            bool inDamageWindow = attackScript != null && attackScript.isHitboxActive;

            // --- LÓGICA DE VELOCIDADE DA ANIMAÇÃO ---
            if (inDamageWindow)
            {
                animator.speed = hitboxAnimSpeed; 

                if (hitboxMoveSpeed < 0.1f)
                {
                    animator.SetFloat("Speed", 0f);
                }
            }
            else
            {
                animator.speed = 1f; 

                // Lógica normal de pernas correndo/paradas (verifica entrada de movimento E velocidade do Rigidbody)
                float moveMagnitude = moveDirection.sqrMagnitude;
                float velocityMagnitude = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z).magnitude;
                float speedParam = (moveMagnitude > 0.01f || velocityMagnitude > 0.1f) ? 1f : 0f;

                animator.SetFloat("Speed", speedParam);
            }
        }
        catch (System.Exception)
        {
            // Evita crashar o loop se referências estiverem se reestabelecendo
        }
    }
}