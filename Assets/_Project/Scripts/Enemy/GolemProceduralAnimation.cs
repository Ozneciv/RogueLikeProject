using UnityEngine;
using System.Collections;

/// <summary>
/// Controla as animações do Golem de forma 100% procedural via script.
/// Simula inércia, passos pesados ("pés colados no chão"), torso inclinado e giros 360.
/// </summary>
public class GolemProceduralAnimation : MonoBehaviour
{
    public enum GolemAttackStyle
    {
        Rotator,      // Gira os braços no próprio ombro/soquete
        Helicopter    // Levanta os braços horizontalmente e orbita 360 ao redor do corpo
    }

    [Header("Referências de Ossos (Quadril/Pernas)")]
    [Tooltip("Osso do quadril (Hips). Se nulo, tentará auto-detectar.")]
    public Transform hipsBone;
    [Tooltip("Osso da coluna/tronco (Spine). Usado para inclinar o torso sem inclinar as pernas.")]
    public Transform spineBone;
    [Tooltip("Perna superior esquerda (Coxa). Se nulo, tentará auto-detectar.")]
    public Transform leftLeg;
    [Tooltip("Perna inferior esquerda (Canela/Pé). Se nulo, tentará auto-detectar.")]
    public Transform leftCalf;
    [Tooltip("Pé esquerdo. Usado para manter a base plana no chão. Se nulo, tentará auto-detectar.")]
    public Transform leftFoot;
    [Tooltip("Perna superior direita (Coxa). Se nulo, tentará auto-detectar.")]
    public Transform rightLeg;
    [Tooltip("Perna inferior direita (Canela/Pé). Se nulo, tentará auto-detectar.")]
    public Transform rightCalf;
    [Tooltip("Pé direito. Usado para manter a base plana no chão. Se nulo, tentará auto-detectar.")]
    public Transform rightFoot;

    [Header("Referências de Ossos (Braços/Dedos - Inércia e Punhos)")]
    [Tooltip("Braço (ou Ombro) esquerdo. Para girar em torno do corpo (helicóptero), arraste o OMbro correspondente aqui.")]
    public Transform leftArm;
    [Tooltip("Braço (ou Ombro) direito. Para girar em torno do corpo (helicóptero), arraste o OMbro correspondente aqui.")]
    public Transform rightArm;
    [Tooltip("Dedo/Mão esquerda (para fechar o punho). Se nulo, tentará auto-detectar.")]
    public Transform leftFinger;
    [Tooltip("Dedo/Mão direita (para fechar o punho). Se nulo, tentará auto-detectar.")]
    public Transform rightFinger;

    [Header("Estilo de Ataque Melee")]
    [Tooltip("Escolha o estilo de animação de ataque básico do Golem.")]
    public GolemAttackStyle attackStyle = GolemAttackStyle.Helicopter;

    [Header("Configurações de Caminhada (Passos Pesados)")]
    [Tooltip("Velocidade do ciclo de passos (Valores baixos = mais lento/pesado).")]
    public float walkCycleSpeed = 3.0f;
    [Tooltip("Percentual do ciclo em que o pé fica colado no chão. Valores altos (ex: 0.75) dão a sensação de peso/dificuldade ao levantar.")]
    [Range(0.5f, 0.9f)]
    public float footGroundDurationRatio = 0.7f;
    [Tooltip("Curva de esforço para descolar o pé do chão. Valores maiores (ex: 2.5) fazem o início da subida ser lento/pesado e a descida mais rápida/impactante.")]
    [Range(1.0f, 5.0f)]
    public float stepLiftCurvePower = 2.5f;
    [Tooltip("Ângulo de balanço da coxa (para frente e para trás).")]
    public float swingAngle = 12f;
    [Tooltip("Ângulo de dobra do joelho/canela.")]
    public float liftAngle = 8f;
    [Tooltip("Distância que o quadril afunda e sobe a cada passo.")]
    public float bodyBounceAmount = 0.08f;
    [Tooltip("Ângulo de inclinação lateral do quadril (peso mudando de lado).")]
    public float bodySwayAngle = 4f;
    [Tooltip("Inclinação padrão da Coluna (Spine) para frente (Golem corcunda, sem afetar as pernas).")]
    public float defaultBodyLeanAngle = 15f;
    [Tooltip("Suavidade na transição de parado/movimento (Valores menores = mais pesado).")]
    public float transitionSmoothness = 4f;

    [Header("Configurações dos Pés")]
    [Tooltip("Ângulo de inclinação da ponta do pé para cima ao dar o passo (evita arrastar).")]
    public float footLiftAngle = 10f;

    [Header("Configurações dos Braços (Arrastados com Inércia)")]
    [Tooltip("Ângulo máximo de balanço dos braços durante a caminhada.")]
    public float armSwingAngle = 15f;
    [Tooltip("Atraso (fase) do balanço dos braços em relação às pernas. Cria a sensação de arrasto.")]
    public float armPhaseDelay = 0.3f;
    [Tooltip("Suavidade da inércia dos braços (valores menores = mais pesado/atrasado).")]
    public float armInertiaFactor = 1.8f;
    [Tooltip("Eixo local de rotação para o balanço dos braços.")]
    public Vector3 armRotationAxis = new Vector3(1, 0, 0); // Geralmente X

    [Header("Configurações dos Braços em Idle (Parado)")]
    [Tooltip("Velocidade do balanço dos braços quando parado (Idle).")]
    public float idleArmSpeed = 2.0f;
    [Tooltip("Ângulo máximo de balanço dos braços quando parado (Idle).")]
    public float idleArmAngle = 4.0f;

    [Header("Configurações dos Dedos (Punhos Cerrados)")]
    [Tooltip("Ângulo que o dedo rotaciona para fechar o punho.")]
    public float fingerCloseAngle = 70f;
    [Tooltip("Eixo local em que os dedos giram para fechar (dobrar).")]
    public Vector3 fingerRotationAxis = new Vector3(0, 0, 1); // Geralmente Z ou X para fechar os dedos

    [Header("Eixos de Rotação (Ajuste se as articulações girarem errado)")]
    [Tooltip("Eixo local para girar as coxas para frente e trás.")]
    public Vector3 hipRotationAxis = new Vector3(1, 0, 0); // Geralmente X
    [Tooltip("Eixo local para dobrar os joelhos/canelas.")]
    public Vector3 calfRotationAxis = new Vector3(1, 0, 0); // Geralmente X
    [Tooltip("Eixo local para inclinação lateral (peso) do corpo.")]
    public Vector3 bodySwayAxis = new Vector3(0, 0, 1); // Geralmente Z

    [Header("Correções de Espelhamento (Rigging)")]
    [Tooltip("Inverte o sentido de movimento da perna esquerda.")]
    public bool invertLeftSwing = false;
    [Tooltip("Inverte o sentido de movimento da perna direita.")]
    public bool invertRightSwing = false;
    [Tooltip("Inverte o sentido de dobra do joelho esquerdo.")]
    public bool invertLeftKnee = false;
    [Tooltip("Inverte o sentido de dobra do joelho direito.")]
    public bool invertRightKnee = false;
    [Tooltip("Inverte a dobra do dedo esquerdo para fechar na direção certa.")]
    public bool invertLeftFinger = false;
    [Tooltip("Inverte a dobra do dedo direito para fechar na direção certa.")]
    public bool invertRightFinger = false;

    [Header("Ataque - Estilo Helicopter (Órbita 360)") ]
    [Tooltip("Ângulo em que os braços sobem horizontalmente para alinhar com os ombros no wind-up.")]
    public float armLiftAngle = 70f;
    [Tooltip("Eixo local usado para levantar os braços (geralmente Z).")]
    public Vector3 armLiftAxis = new Vector3(0, 0, 1);

    [Header("Ataque - Configurações Gerais (Slam)")]
    [Tooltip("Ângulo máximo que o tronco inclina para trás antes de atacar.")]
    public float attackLeanBackAngle = -35f;
    [Tooltip("Ângulo máximo que o tronco avança ao golpear.")]
    public float attackSlamForwardAngle = 40f;
    [Tooltip("Eixo de rotação para a inclinação de ataque.")]
    public Vector3 attackLeanAxis = new Vector3(1, 0, 0); // Geralmente X
    [Tooltip("Eixo local em que os braços vão girar (Usado no estilo Rotator).")]
    public Vector3 attackArmSpinAxis = new Vector3(0, 1, 0); // Y gira horizontalmente
    [Tooltip("Número de giros completos que o braço realiza no ataque Rotator.")]
    public int rotatorSpinsCount = 2;
    [Tooltip("Ângulo (em graus) que os braços inclinam para dentro durante o ataque Rotator.")]
    public float rotatorConvergeAngle = 20f;
    [Tooltip("Ângulo (em graus) que os braços inclinam para baixo durante o ataque Rotator.")]
    public float rotatorPitchAngle = 30f;
    [Tooltip("Duração total da animação de ataque (deve condizer com o windup da IA).")]
    public float attackDuration = 1.0f;

    [Header("Animação de Cast do Stun (Pisando Forte)")]
    [Tooltip("Altura máxima que o Golem sobe ao pular/preparar o stun.")]
    public float stunRiseHeight = 0.4f;
    [Tooltip("Ângulo que as coxas dobram para cima no ar durante a preparação.")]
    public float stunLegFoldAngle = 30f;
    [Tooltip("Duração total da telegrafagem do stun (pulo e impacto).")]
    public float stunCastDuration = 1.5f;

    // Rotações, posições e escalas iniciais salvas
    private Quaternion initialHipsRotation;
    private Vector3 initialHipsPosition;
    private Quaternion initialSpineRotation;
    private Quaternion initialLeftLegRotation;
    private Quaternion initialLeftCalfRotation;
    private Quaternion initialLeftFootRotation;
    private Quaternion initialRightLegRotation;
    private Quaternion initialRightCalfRotation;
    private Quaternion initialRightFootRotation;
    private Quaternion initialLeftArmRotation;
    private Vector3 initialLeftArmPosition;
    private Quaternion initialRightArmRotation;
    private Vector3 initialRightArmPosition;
    private Quaternion initialLeftFingerRotation;
    private Quaternion initialRightFingerRotation;
    private Quaternion initialParentLRotationRelativeToRoot;
    private Quaternion initialParentRRotationRelativeToRoot;


    // Timers e fatores de controle
    private float animationTime = 0f;
    private float currentMovementFactor = 0f;
    private Vector3 lastPosition;
    private Golem_AI golemAI;
    private DummyHealth health;
    private Rigidbody rb;
    private Transform playerTransform;

    private float attackTimer = 0f;
    private float stunTimer = 0f;

    /// <summary>
    /// Multiplicador de velocidade de movimento que deve ser respeitado pela IA física.
    /// Mantido para compatibilidade com o script de IA, sempre retorna 1f (sem pausas).
    /// </summary>
    public float MovementSpeedMultiplier => 1f;

    void Start()
    {
        golemAI = GetComponent<Golem_AI>();
        rb = GetComponent<Rigidbody>();
        health = GetComponent<DummyHealth>();
        lastPosition = transform.position;

        // Encontra o player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }

        // Auto-detecta os ossos se o usuário deixou em branco
        AutoDetectBones();

        // Salva as poses originais (T-pose ou pose padrão do FBX)
        SaveInitialPoses();
    }

    void AutoDetectBones()
    {
        Transform[] allChildren = GetComponentsInChildren<Transform>();

        foreach (Transform t in allChildren)
        {
            string nameLower = t.name.ToLower();

            // Evita pegar pontas vazias (ends)
            if (nameLower.Contains("end")) continue;

            if (hipsBone == null && (nameLower == "hips" || nameLower == "pelvis" || nameLower == "body" || nameLower == "corpo_principal"))
            {
                hipsBone = t;
            }
            else if (spineBone == null && (nameLower == "spine" || nameLower == "torso" || nameLower == "coluna" || nameLower == "spine_01"))
            {
                spineBone = t;
            }
            else if (leftLeg == null && (nameLower == "leftleg" || nameLower == "l.leg" || nameLower == "l_leg" || nameLower == "left_leg" || nameLower == "coxa.l"))
            {
                leftLeg = t;
            }
            else if (leftCalf == null && (nameLower == "leftcalf" || nameLower == "l.calf" || nameLower == "l_calf" || nameLower == "left_calf" || nameLower == "canela.l"))
            {
                leftCalf = t;
            }
            else if (leftFoot == null && (nameLower == "leftfoot" || nameLower == "l.foot" || nameLower == "l_foot" || nameLower == "left_foot" || nameLower == "pé.l" || nameLower == "pe.l"))
            {
                leftFoot = t;
            }
            else if (rightLeg == null && (nameLower == "rightleg" || nameLower == "r.leg" || nameLower == "r_leg" || nameLower == "right_leg" || nameLower == "coxa.r"))
            {
                rightLeg = t;
            }
            else if (rightCalf == null && (nameLower == "rightcalf" || nameLower == "r.calf" || nameLower == "r_calf" || nameLower == "right_calf" || nameLower == "canela.r"))
            {
                rightCalf = t;
            }
            else if (rightFoot == null && (nameLower == "rightfoot" || nameLower == "r.foot" || nameLower == "r_foot" || nameLower == "right_foot" || nameLower == "pé.r" || nameLower == "pe.r"))
            {
                rightFoot = t;
            }
            else if (leftArm == null && (nameLower == "leftarm" || nameLower == "l.arm" || nameLower == "l_arm" || nameLower == "left_arm" || nameLower == "braço.l"))
            {
                leftArm = t;
            }
            else if (rightArm == null && (nameLower == "rightarm" || nameLower == "r.arm" || nameLower == "r_arm" || nameLower == "right_arm" || nameLower == "braço.r"))
            {
                rightArm = t;
            }
            else if (leftFinger == null && (nameLower == "leftfinger" || nameLower == "l.finger" || nameLower == "l_finger" || nameLower == "left_finger" || nameLower == "dedo.l"))
            {
                leftFinger = t;
            }
            else if (rightFinger == null && (nameLower == "rightfinger" || nameLower == "r.finger" || nameLower == "r_finger" || nameLower == "right_finger" || nameLower == "dedo.r"))
            {
                rightFinger = t;
            }
        }

        Debug.Log($"[GOLEM_ANIMATION] Auto-detecção finalizada: Hips={hipsBone?.name}, Spine={spineBone?.name}, L_Leg={leftLeg?.name}, L_Calf={leftCalf?.name}, L_Foot={leftFoot?.name}, R_Leg={rightLeg?.name}, R_Calf={rightCalf?.name}, R_Foot={rightFoot?.name}");
    }

    void SaveInitialPoses()
    {
        if (hipsBone != null)
        {
            initialHipsRotation = hipsBone.localRotation;
            initialHipsPosition = hipsBone.localPosition;
        }
        if (spineBone != null)
        {
            initialSpineRotation = spineBone.localRotation;
        }
        if (leftLeg != null) initialLeftLegRotation = leftLeg.localRotation;
        if (leftCalf != null) initialLeftCalfRotation = leftCalf.localRotation;
        if (leftFoot != null) initialLeftFootRotation = leftFoot.localRotation;
        if (rightLeg != null) initialRightLegRotation = rightLeg.localRotation;
        if (rightCalf != null) initialRightCalfRotation = rightCalf.localRotation;
        if (rightFoot != null) initialRightFootRotation = rightFoot.localRotation;
        if (leftArm != null)
        {
            initialLeftArmRotation = leftArm.localRotation;
            initialLeftArmPosition = leftArm.localPosition;
            Transform parentL = leftArm.parent != null ? leftArm.parent : transform;
            initialParentLRotationRelativeToRoot = Quaternion.Inverse(transform.rotation) * parentL.rotation;
        }
        if (rightArm != null)
        {
            initialRightArmRotation = rightArm.localRotation;
            initialRightArmPosition = rightArm.localPosition;
            Transform parentR = rightArm.parent != null ? rightArm.parent : transform;
            initialParentRRotationRelativeToRoot = Quaternion.Inverse(transform.rotation) * parentR.rotation;
        }
        if (leftFinger != null) initialLeftFingerRotation = leftFinger.localRotation;
        if (rightFinger != null) initialRightFingerRotation = rightFinger.localRotation;
    }

    /// <summary>
    /// Distorce o tempo da animação para que o pé permaneça mais tempo no chão (groundRatio)
    /// e faça o movimento de levantamento/avanço mais rápido, gerando sensação de peso.
    /// </summary>
    private float GetWarpedTime(float rawTime, float groundRatio)
    {
        float period = 2f * Mathf.PI;
        float cycle = rawTime % period;
        if (cycle < 0) cycle += period;
        
        float x = cycle / period;
        float airRatio = 1f - groundRatio;
        float warpedX = 0f;
        
        if (x < airRatio)
        {
            // Fase Aérea (menor duração real): Mapeia o tempo curto para a primeira metade do ciclo senoidal (0 a PI)
            float tAir = x / airRatio;
            // Aplica a curva de esforço para "descolar" o pé: início lento/pesado, aceleração rápida
            tAir = Mathf.Pow(tAir, Mathf.Max(1.0f, stepLiftCurvePower));
            warpedX = tAir * 0.5f;
        }
        else
        {
            // Fase Terrestre (maior duração real): Mapeia o tempo longo para a segunda metade do ciclo senoidal (PI a 2PI)
            warpedX = 0.5f + ((x - airRatio) / groundRatio) * 0.5f;
        }
        
        float completedCycles = Mathf.Floor(rawTime / period);
        return (completedCycles + warpedX) * period;
    }

    void Update()
    {
        // 1. Calcula a velocidade real de deslocamento
        Vector3 displacement = transform.position - lastPosition;
        displacement.y = 0; // Ignora altura
        float speed = displacement.magnitude / Time.deltaTime;
        lastPosition = transform.position;

        if (rb != null)
        {
            Vector3 localVel = transform.InverseTransformDirection(rb.linearVelocity);
            localVel.y = 0;
            speed = Mathf.Max(speed, localVel.magnitude);
        }

        // 2. Verifica estados de combate da IA
        bool isAttacking = golemAI != null && golemAI.IsAttacking;
        bool isCastingStun = golemAI != null && golemAI.IsCastingStun;

        // 3. Gerencia e executa as diferentes animações procedurais
        if (isCastingStun)
        {
            if (rb != null) rb.constraints = RigidbodyConstraints.FreezeRotation;
            // Executa o pisão/salto do Stun (Prioridade 1)
            AnimateStunCast();
        }
        else if (isAttacking)
        {
            if (rb != null) rb.constraints = RigidbodyConstraints.FreezeRotation;
            // Executa a investida/cabeçada do Ataque Corporal com giro 360 orbital dos braços (Prioridade 2)
            AnimateMeleeAttack();
        }
        else
        {
            // Reseta timers de combate
            attackTimer = 0f;
            stunTimer = 0f;

            // Restaura as constraints padrões de rotação/física
            if (rb != null && rb.constraints != RigidbodyConstraints.FreezeRotation)
            {
                rb.constraints = RigidbodyConstraints.FreezeRotation;
            }

            // Executa a Caminhada Procedural (Idle ou Caminhando)
            bool isTryingToMove = false;
            if (playerTransform == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) playerTransform = player.transform;
            }

            if (golemAI != null)
            {
                // Se a IA está ativada e o jogador está fora do meleeRange e ele está vivo
                DummyHealth golemHealth = GetComponent<DummyHealth>();
                bool isAlive = golemHealth == null || golemHealth.CurrentHealth > 0;
                if (isAlive && golemAI.IsActivated && playerTransform != null)
                {
                    float distToPlayer = Vector3.Distance(transform.position, playerTransform.position);
                    if (distToPlayer > golemAI.meleeRange)
                    {
                        isTryingToMove = true;
                    }
                }
            }
            else
            {
                // Fallback caso não haja Golem_AI anexado (ex: para testes)
                isTryingToMove = speed > 0.1f;
            }

            float targetFactor = isTryingToMove ? 1f : 0f;
            currentMovementFactor = Mathf.Lerp(currentMovementFactor, targetFactor, transitionSmoothness * Time.deltaTime);

            if (isTryingToMove)
            {
                // Avança o tempo do ciclo de passos baseado na velocidade
                animationTime += Time.deltaTime * walkCycleSpeed * Mathf.Clamp(speed / 2f, 0.5f, 2f);
            }
            else
            {
                // Retorna suavemente o ciclo de animação para a pose em pé (idle)
                animationTime = Mathf.Lerp(animationTime, 0f, transitionSmoothness * Time.deltaTime);
            }

            AnimateWalkCycle();
        }
    }

    void AnimateWalkCycle()
    {
        // Limita a proporção do pé no chão entre 50% e 90%
        float groundRatio = Mathf.Clamp(footGroundDurationRatio, 0.5f, 0.9f);

        // Calcula o tempo distorcido (warped time) de forma independente para cada perna
        // A perna direita está defasada em PI em relação à esquerda
        float warpedTimeLeft = GetWarpedTime(animationTime, groundRatio);
        float warpedTimeRight = GetWarpedTime(animationTime + Mathf.PI, groundRatio);

        // Ondas senoidais baseadas no tempo distorcido
        float waveLeft = Mathf.Sin(warpedTimeLeft);
        float waveRight = Mathf.Sin(warpedTimeRight);

        // 1. Rotação das Coxas (Balanço frente/trás)
        float leftSwing = waveLeft * swingAngle * currentMovementFactor;
        float rightSwing = waveRight * swingAngle * currentMovementFactor;

        if (invertLeftSwing) leftSwing = -leftSwing;
        if (invertRightSwing) rightSwing = -rightSwing;

        Quaternion leftTargetRot = initialLeftLegRotation * Quaternion.Euler(hipRotationAxis * leftSwing);
        Quaternion rightTargetRot = initialRightLegRotation * Quaternion.Euler(hipRotationAxis * rightSwing);

        // 2. Rotação das Canelas/Joelhos (Dobra apenas na fase aérea, quando o pé está subindo)
        float leftKneeBend = 0f;
        if (waveLeft > 0f) leftKneeBend = waveLeft * liftAngle * currentMovementFactor;

        float rightKneeBend = 0f;
        if (waveRight > 0f) rightKneeBend = waveRight * liftAngle * currentMovementFactor;

        if (invertLeftKnee) leftKneeBend = -leftKneeBend;
        if (invertRightKnee) rightKneeBend = -rightKneeBend;

        Quaternion leftCalfTargetRot = initialLeftCalfRotation * Quaternion.Euler(calfRotationAxis * leftKneeBend);
        Quaternion rightCalfTargetRot = initialRightCalfRotation * Quaternion.Euler(calfRotationAxis * rightKneeBend);

        // 3. Movimentação do Hips e Spine (Torso)
        // O corpo afunda e balança acompanhando o ritmo das pernas
        float bounce = Mathf.Abs(Mathf.Sin(warpedTimeLeft)) * -bodyBounceAmount * currentMovementFactor;
        Vector3 targetHipsPos = initialHipsPosition + new Vector3(0, bounce, 0);
        float sway = waveLeft * bodySwayAngle * currentMovementFactor;
        Quaternion targetHipsRot = initialHipsRotation * Quaternion.Euler(bodySwayAxis * sway);

        // Spine (Coluna): Inclinação corcunda de repouso
        Quaternion targetSpineRot = initialSpineRotation * Quaternion.Euler(attackLeanAxis * defaultBodyLeanAngle);

        // 5. Dedos relaxados
        if (leftFinger != null) leftFinger.localRotation = Quaternion.Slerp(leftFinger.localRotation, initialLeftFingerRotation, 10f * Time.deltaTime);
        if (rightFinger != null) rightFinger.localRotation = Quaternion.Slerp(rightFinger.localRotation, initialRightFingerRotation, 10f * Time.deltaTime);

        // 6. Rotação dos Pés (Mantendo a rotação padrão do rig para evitar deformidades no mesh)
        if (leftFoot != null)
        {
            leftFoot.localRotation = initialLeftFootRotation;
        }

        if (rightFoot != null)
        {
            rightFoot.localRotation = initialRightFootRotation;
        }

        // 7. Aplica as rotações e posições do Hips, Spine e Pernas
        if (hipsBone != null)
        {
            hipsBone.localPosition = Vector3.Lerp(hipsBone.localPosition, targetHipsPos, transitionSmoothness * Time.deltaTime);
            hipsBone.localRotation = Quaternion.Slerp(hipsBone.localRotation, targetHipsRot, transitionSmoothness * Time.deltaTime);
        }
        if (spineBone != null)
        {
            spineBone.localRotation = Quaternion.Slerp(spineBone.localRotation, targetSpineRot, transitionSmoothness * Time.deltaTime);
        }
        if (leftLeg != null) leftLeg.localRotation = Quaternion.Slerp(leftLeg.localRotation, leftTargetRot, transitionSmoothness * 2f * Time.deltaTime);
        if (rightLeg != null) rightLeg.localRotation = Quaternion.Slerp(rightLeg.localRotation, rightTargetRot, transitionSmoothness * 2f * Time.deltaTime);
        if (leftCalf != null) leftCalf.localRotation = Quaternion.Slerp(leftCalf.localRotation, leftCalfTargetRot, transitionSmoothness * 2f * Time.deltaTime);
        if (rightCalf != null) rightCalf.localRotation = Quaternion.Slerp(rightCalf.localRotation, rightCalfTargetRot, transitionSmoothness * 2f * Time.deltaTime);

        // 8. Rotação dos Braços (Momento/Inércia) - Calculado e aplicado DEPOIS de atualizar os quadris/coluna
        float leftArmSwing = Mathf.Sin(warpedTimeLeft - armPhaseDelay) * armSwingAngle * currentMovementFactor;
        float rightArmSwing = Mathf.Sin(warpedTimeLeft - armPhaseDelay + Mathf.PI) * armSwingAngle * currentMovementFactor;
        float armOutwardSway = Mathf.Abs(Mathf.Sin(warpedTimeLeft - armPhaseDelay)) * 4f * currentMovementFactor;

        // Balanço do braço (idle + caminhando)
        float leftArmX = leftArmSwing;
        float rightArmX = rightArmSwing;

        // Adiciona um balanço suave e leve de idle quando parado (mesma frequência dos dois lados, simulando respiração)
        float idleSway = Mathf.Sin(Time.time * idleArmSpeed) * idleArmAngle * (1f - currentMovementFactor);
        leftArmX += idleSway;
        rightArmX += idleSway;

        Quaternion leftArmTargetRot = initialLeftArmRotation;
        if (leftArm != null)
        {
            Transform parentL = leftArm.parent != null ? leftArm.parent : transform;
            Quaternion parentWorldDefaultL = transform.rotation * initialParentLRotationRelativeToRoot;
            Quaternion baseRotationL = parentWorldDefaultL * initialLeftArmRotation;
            Quaternion worldOffsetL = Quaternion.AngleAxis(leftArmX, transform.right) * Quaternion.AngleAxis(-armOutwardSway, transform.forward);
            Quaternion targetWorldL = worldOffsetL * baseRotationL;
            leftArmTargetRot = Quaternion.Inverse(parentL.rotation) * targetWorldL;
        }

        Quaternion rightArmTargetRot = initialRightArmRotation;
        if (rightArm != null)
        {
            Transform parentR = rightArm.parent != null ? rightArm.parent : transform;
            Quaternion parentWorldDefaultR = transform.rotation * initialParentRRotationRelativeToRoot;
            Quaternion baseRotationR = parentWorldDefaultR * initialRightArmRotation;
            Quaternion worldOffsetR = Quaternion.AngleAxis(rightArmX, transform.right) * Quaternion.AngleAxis(armOutwardSway, transform.forward);
            Quaternion targetWorldR = worldOffsetR * baseRotationR;
            rightArmTargetRot = Quaternion.Inverse(parentR.rotation) * targetWorldR;
        }

        // Aplica as rotações dos braços com uma taxa de interpolação dinâmica:
        // No idle (currentMovementFactor = 0) usa o armInertiaFactor suave.
        // Ao caminhar (currentMovementFactor = 1) usa uma taxa rápida (15f) para cancelar o rebolado do quadril/tronco.
        float activeInertia = Mathf.Lerp(armInertiaFactor, 15f, currentMovementFactor);

        if (leftArm != null)
        {
            leftArm.localPosition = Vector3.Lerp(leftArm.localPosition, initialLeftArmPosition, transitionSmoothness * Time.deltaTime);
            leftArm.localRotation = Quaternion.Slerp(leftArm.localRotation, leftArmTargetRot, activeInertia * Time.deltaTime);
        }
        if (rightArm != null)
        {
            rightArm.localPosition = Vector3.Lerp(rightArm.localPosition, initialRightArmPosition, transitionSmoothness * Time.deltaTime);
            rightArm.localRotation = Quaternion.Slerp(rightArm.localRotation, rightArmTargetRot, activeInertia * Time.deltaTime);
        }
    }

    void AnimateMeleeAttack()
    {
        attackTimer += Time.deltaTime;
        float tNorm = Mathf.Clamp01(attackTimer / attackDuration);

        float leanAngle = 0f;
        float bodyDrop = 0f;
        
        Vector3 targetLeftPos = initialLeftArmPosition;
        Quaternion targetLeftRot = initialLeftArmRotation;
        Vector3 targetRightPos = initialRightArmPosition;
        Quaternion targetRightRot = initialRightArmRotation;

        float fingerAngle = 0f;

        if (attackStyle == GolemAttackStyle.Helicopter)
        {
            // 1. Wind-up (0% a 40%): Torso inclina para trás, braços sobem horizontalmente (T-pose) e fecha as mãos
            if (tNorm < 0.4f)
            {
                float subT = tNorm / 0.4f;
                leanAngle = Mathf.Lerp(defaultBodyLeanAngle, attackLeanBackAngle, subT);
                bodyDrop = Mathf.Lerp(0f, -0.02f, subT);
                
                float liftL = Mathf.Lerp(0f, -armLiftAngle, subT);
                float liftR = Mathf.Lerp(0f, armLiftAngle, subT);
                targetLeftRot = initialLeftArmRotation * Quaternion.Euler(armLiftAxis * liftL);
                targetRightRot = initialRightArmRotation * Quaternion.Euler(armLiftAxis * liftR);

                fingerAngle = Mathf.Lerp(0f, fingerCloseAngle, subT);
            }
            // 2. Golpe (40% a 80%): Torso avança rápido e os braços LEVANTADOS realizam a órbita 360 ao redor do corpo
            else if (tNorm >= 0.4f && tNorm < 0.8f)
            {
                float subT = (tNorm - 0.4f) / 0.4f;
                float easeOutT = Mathf.Sin(subT * Mathf.PI * 0.5f);
                leanAngle = Mathf.Lerp(attackLeanBackAngle, attackSlamForwardAngle, easeOutT);
                bodyDrop = Mathf.Lerp(-0.02f, -0.1f, easeOutT);
                
                float spinAngle = subT * 360f;
                Quaternion orbitRot = Quaternion.Euler(0, spinAngle, 0);

                Quaternion raisedRotL = initialLeftArmRotation * Quaternion.Euler(armLiftAxis * -armLiftAngle);
                Quaternion raisedRotR = initialRightArmRotation * Quaternion.Euler(armLiftAxis * armLiftAngle);

                targetLeftPos = orbitRot * initialLeftArmPosition;
                targetLeftRot = orbitRot * raisedRotL;

                targetRightPos = orbitRot * initialRightArmPosition;
                targetRightRot = orbitRot * raisedRotR;

                fingerAngle = fingerCloseAngle;
            }
            // 3. Recuperação (80% a 100%): Corpo volta ao normal, braços descem e voltam para o lado do corpo
            else
            {
                float subT = (tNorm - 0.8f) / 0.2f;
                leanAngle = Mathf.Lerp(attackSlamForwardAngle, defaultBodyLeanAngle, subT);
                bodyDrop = Mathf.Lerp(-0.1f, 0f, subT);
                
                targetLeftPos = Vector3.Lerp(Quaternion.Euler(0, 360f, 0) * initialLeftArmPosition, initialLeftArmPosition, subT);
                targetLeftRot = Quaternion.Slerp(Quaternion.Euler(0, 360f, 0) * (initialLeftArmRotation * Quaternion.Euler(armLiftAxis * -armLiftAngle)), initialLeftArmRotation, subT);

                targetRightPos = Vector3.Lerp(Quaternion.Euler(0, 360f, 0) * initialRightArmPosition, initialRightArmPosition, subT);
                targetRightRot = Quaternion.Slerp(Quaternion.Euler(0, 360f, 0) * (initialRightArmRotation * Quaternion.Euler(armLiftAxis * armLiftAngle)), initialRightArmRotation, subT);

                fingerAngle = Mathf.Lerp(fingerCloseAngle, 0f, subT);
            }
        }
        else
        {
            // --- ESTILO ROTATOR (GIRO NO PRÓPRIO EIXO DO OMBRO) ---
            float armSpinAngle = 0f;
            // Sem buff: apenas 180 graus (meio giro). Com buff: 2 giros completos (720 graus).
            float totalDegrees = (health != null && health.isBuffed) ? 720f : 180f;
            float convergeT = 0f;

            if (tNorm < 0.4f)
            {
                float subT = tNorm / 0.4f;
                leanAngle = Mathf.Lerp(defaultBodyLeanAngle, attackLeanBackAngle, subT);
                bodyDrop = Mathf.Lerp(0f, -0.02f, subT);
                armSpinAngle = Mathf.Lerp(0f, -45f, subT);
                fingerAngle = Mathf.Lerp(0f, fingerCloseAngle, subT);
                convergeT = subT;
            }
            else if (tNorm >= 0.4f && tNorm < 0.8f)
            {
                float subT = (tNorm - 0.4f) / 0.4f;
                float easeOutT = Mathf.Sin(subT * Mathf.PI * 0.5f);
                leanAngle = Mathf.Lerp(attackLeanBackAngle, attackSlamForwardAngle, easeOutT);
                bodyDrop = Mathf.Lerp(-0.02f, -0.1f, easeOutT);
                armSpinAngle = Mathf.Lerp(-45f, -45f + totalDegrees, subT);
                fingerAngle = fingerCloseAngle;
                convergeT = 1f;
            }
            else
            {
                float subT = (tNorm - 0.8f) / 0.2f;
                leanAngle = Mathf.Lerp(attackSlamForwardAngle, defaultBodyLeanAngle, subT);
                bodyDrop = Mathf.Lerp(-0.1f, 0f, subT);
                float endAngle = -45f + totalDegrees;
                armSpinAngle = Mathf.Lerp(endAngle, endAngle + 45f, subT);
                fingerAngle = Mathf.Lerp(fingerCloseAngle, 0f, subT);
                convergeT = 1f - subT;
            }

            Quaternion spinRot = Quaternion.AngleAxis(armSpinAngle, attackArmSpinAxis);
            Quaternion spinRotRight = Quaternion.AngleAxis(-armSpinAngle, attackArmSpinAxis); // Inverte o sentido do giro para o braço direito

            // Braço Esquerdo: Corrigido os sinais (invertidos) para inclinar para baixo e para dentro
            Quaternion leftConvergeRot = Quaternion.Euler(-rotatorPitchAngle * convergeT, -rotatorConvergeAngle * convergeT, 0f);
            targetLeftPos = initialLeftArmPosition;
            targetLeftRot = leftConvergeRot * spinRot * initialLeftArmRotation;

            // Braço Direito: Inclina para baixo (X negativo) e para dentro (Y negativo) no espaço do tronco, com giro invertido
            Quaternion rightConvergeRot = Quaternion.Euler(-rotatorPitchAngle * convergeT, -rotatorConvergeAngle * convergeT, 0f);
            targetRightPos = initialRightArmPosition;
            targetRightRot = rightConvergeRot * spinRotRight * initialRightArmRotation;
        }

        // Aplica inclinação na Coluna (Spine), mantendo os pés no chão
        if (spineBone != null)
        {
            Quaternion targetSpineRot = initialSpineRotation * Quaternion.Euler(attackLeanAxis * leanAngle);
            spineBone.localRotation = Quaternion.Slerp(spineBone.localRotation, targetSpineRot, 12f * Time.deltaTime);
        }

        // Hips apenas afunda verticalmente de forma suave, sem rotacionar
        if (hipsBone != null)
        {
            Vector3 targetHipsPos = initialHipsPosition + new Vector3(0, bodyDrop, 0);
            hipsBone.localPosition = Vector3.Lerp(hipsBone.localPosition, targetHipsPos, 12f * Time.deltaTime);
            hipsBone.localRotation = Quaternion.Slerp(hipsBone.localRotation, initialHipsRotation, 10f * Time.deltaTime);
        }

        // Aplica posições e rotações orbitais/rotacionais aos braços
        if (leftArm != null)
        {
            Transform parentL = leftArm.parent != null ? leftArm.parent : transform;
            Quaternion parentWorldDefaultL = transform.rotation * initialParentLRotationRelativeToRoot;
            Quaternion desiredWorldL = parentWorldDefaultL * targetLeftRot;
            Quaternion finalLocalRotL = Quaternion.Inverse(parentL.rotation) * desiredWorldL;

            leftArm.localPosition = Vector3.Lerp(leftArm.localPosition, targetLeftPos, 15f * Time.deltaTime);
            leftArm.localRotation = Quaternion.Slerp(leftArm.localRotation, finalLocalRotL, 15f * Time.deltaTime);
        }
        if (rightArm != null)
        {
            Transform parentR = rightArm.parent != null ? rightArm.parent : transform;
            Quaternion parentWorldDefaultR = transform.rotation * initialParentRRotationRelativeToRoot;
            Quaternion desiredWorldR = parentWorldDefaultR * targetRightRot;
            Quaternion finalLocalRotR = Quaternion.Inverse(parentR.rotation) * desiredWorldR;

            rightArm.localPosition = Vector3.Lerp(rightArm.localPosition, targetRightPos, 15f * Time.deltaTime);
            rightArm.localRotation = Quaternion.Slerp(rightArm.localRotation, finalLocalRotR, 15f * Time.deltaTime);
        }

        // Aplica o fechamento do punho
        if (leftFinger != null)
        {
            float angle = invertLeftFinger ? -fingerAngle : fingerAngle;
            leftFinger.localRotation = Quaternion.Slerp(leftFinger.localRotation, initialLeftFingerRotation * Quaternion.Euler(fingerRotationAxis * angle), 15f * Time.deltaTime);
        }
        if (rightFinger != null)
        {
            float angle = invertRightFinger ? -fingerAngle : fingerAngle;
            rightFinger.localRotation = Quaternion.Slerp(rightFinger.localRotation, initialRightFingerRotation * Quaternion.Euler(fingerRotationAxis * angle), 15f * Time.deltaTime);
        }

        // Mantém as pernas e pés retos e plantados
        Quaternion straightLeg = initialLeftLegRotation;
        Quaternion straightCalf = initialLeftCalfRotation;

        if (leftLeg != null) leftLeg.localRotation = Quaternion.Slerp(leftLeg.localRotation, straightLeg, 10f * Time.deltaTime);
        if (rightLeg != null) rightLeg.localRotation = Quaternion.Slerp(rightLeg.localRotation, straightLeg, 10f * Time.deltaTime);
        if (leftCalf != null) leftCalf.localRotation = Quaternion.Slerp(leftCalf.localRotation, straightCalf, 10f * Time.deltaTime);
        if (rightCalf != null) rightCalf.localRotation = Quaternion.Slerp(rightCalf.localRotation, straightCalf, 10f * Time.deltaTime);

        // Garante a rotação padrão dos pés para evitar deformidades no mesh
        if (leftFoot != null)
        {
            leftFoot.localRotation = initialLeftFootRotation;
        }
        if (rightFoot != null)
        {
            rightFoot.localRotation = initialRightFootRotation;
        }
    }

    void AnimateStunCast()
    {
        stunTimer += Time.deltaTime;
        float tNorm = Mathf.Clamp01(stunTimer / stunCastDuration);

        float heightOffset = 0f;
        float legFold = 0f;
        float armAngle = 0f;
        float fingerAngle = 0f;

        // Curva do Pisão/Salto:
        // 1. Preparação (0% a 70% do tempo): Flexiona joelhos, sobe no ar, braços abrem/sobem e fecha punhos.
        if (tNorm < 0.7f)
        {
            float subT = tNorm / 0.7f;
            heightOffset = Mathf.Lerp(0f, stunRiseHeight, subT);
            legFold = Mathf.Lerp(0f, stunLegFoldAngle, subT);
            armAngle = Mathf.Lerp(0f, 35f, subT);
            fingerAngle = Mathf.Lerp(0f, fingerCloseAngle, subT); // Fecha a mão ao saltar
        }
        // 2. Queda e Impacto (70% a 85% do tempo): Desce muito rápido e amassa. Punhos fechados.
        else if (tNorm >= 0.7f && tNorm < 0.85f)
        {
            float subT = (tNorm - 0.7f) / 0.15f;
            float easeInT = Mathf.Pow(subT, 2f);
            heightOffset = Mathf.Lerp(stunRiseHeight, -0.2f, easeInT);
            legFold = Mathf.Lerp(stunLegFoldAngle, -5f, easeInT);
            armAngle = Mathf.Lerp(35f, -40f, easeInT);
            fingerAngle = fingerCloseAngle; // Mantém fechado para o impacto
        }
        // 3. Estabilização (85% a 100% do tempo): Recupera postura de pé. Punhos abrem.
        else
        {
            float subT = (tNorm - 0.85f) / 0.15f;
            heightOffset = Mathf.Lerp(-0.2f, 0f, subT);
            legFold = Mathf.Lerp(-5f, 0f, subT);
            fingerAngle = Mathf.Lerp(fingerCloseAngle, 0f, subT); // Abre as mãos
            
            if (subT < 0.5f)
            {
                armAngle = Mathf.Lerp(-40f, 15f, subT / 0.5f);
            }
            else
            {
                armAngle = Mathf.Lerp(15f, 0f, (subT - 0.5f) / 0.5f);
            }
        }

        // Aplica altura no quadril (Hips)
        if (hipsBone != null)
        {
            Vector3 targetHipsPos = initialHipsPosition + new Vector3(0, heightOffset, 0);
            hipsBone.localPosition = Vector3.Lerp(hipsBone.localPosition, targetHipsPos, 20f * Time.deltaTime);
            hipsBone.localRotation = Quaternion.Slerp(hipsBone.localRotation, initialHipsRotation, 10f * Time.deltaTime);
        }

        // Mantém a inclinação padrão de repouso na coluna (Spine)
        if (spineBone != null)
        {
            Quaternion targetSpineRot = initialSpineRotation * Quaternion.Euler(attackLeanAxis * defaultBodyLeanAngle);
            spineBone.localRotation = Quaternion.Slerp(spineBone.localRotation, targetSpineRot, 10f * Time.deltaTime);
        }

        // Retorna as posições dos braços à pose original
        if (leftArm != null)
        {
            Quaternion localTargetL = initialLeftArmRotation * Quaternion.Euler(armRotationAxis * -armAngle);
            Transform parentL = leftArm.parent != null ? leftArm.parent : transform;
            Quaternion parentWorldDefaultL = transform.rotation * initialParentLRotationRelativeToRoot;
            Quaternion desiredWorldL = parentWorldDefaultL * localTargetL;
            Quaternion leftArmTargetRot = Quaternion.Inverse(parentL.rotation) * desiredWorldL;

            leftArm.localPosition = Vector3.Lerp(leftArm.localPosition, initialLeftArmPosition, 15f * Time.deltaTime);
            leftArm.localRotation = Quaternion.Slerp(leftArm.localRotation, leftArmTargetRot, 15f * Time.deltaTime);
        }
        if (rightArm != null)
        {
            Quaternion localTargetR = initialRightArmRotation * Quaternion.Euler(armRotationAxis * armAngle);
            Transform parentR = rightArm.parent != null ? rightArm.parent : transform;
            Quaternion parentWorldDefaultR = transform.rotation * initialParentRRotationRelativeToRoot;
            Quaternion desiredWorldR = parentWorldDefaultR * localTargetR;
            Quaternion rightArmTargetRot = Quaternion.Inverse(parentR.rotation) * desiredWorldR;

            rightArm.localPosition = Vector3.Lerp(rightArm.localPosition, initialRightArmPosition, 15f * Time.deltaTime);
            rightArm.localRotation = Quaternion.Slerp(rightArm.localRotation, rightArmTargetRot, 15f * Time.deltaTime);
        }

        // Aplica o fechamento do punho
        if (leftFinger != null)
        {
            float angle = invertLeftFinger ? -fingerAngle : fingerAngle;
            leftFinger.localRotation = Quaternion.Slerp(leftFinger.localRotation, initialLeftFingerRotation * Quaternion.Euler(fingerRotationAxis * angle), 15f * Time.deltaTime);
        }
        if (rightFinger != null)
        {
            float angle = invertRightFinger ? -fingerAngle : fingerAngle;
            rightFinger.localRotation = Quaternion.Slerp(rightFinger.localRotation, initialRightFingerRotation * Quaternion.Euler(fingerRotationAxis * angle), 15f * Time.deltaTime);
        }

        // Aplica dobra nas pernas
        Quaternion targetLegL = initialLeftLegRotation * Quaternion.Euler(hipRotationAxis * (invertLeftSwing ? -legFold : legFold));
        Quaternion targetLegR = initialRightLegRotation * Quaternion.Euler(hipRotationAxis * (invertRightSwing ? -legFold : legFold));
        Quaternion targetCalfL = initialLeftCalfRotation * Quaternion.Euler(calfRotationAxis * (invertLeftKnee ? -legFold : legFold));
        Quaternion targetCalfR = initialRightCalfRotation * Quaternion.Euler(calfRotationAxis * (invertRightKnee ? -legFold : legFold));

        if (leftLeg != null) leftLeg.localRotation = Quaternion.Slerp(leftLeg.localRotation, targetLegL, 15f * Time.deltaTime);
        if (rightLeg != null) rightLeg.localRotation = Quaternion.Slerp(rightLeg.localRotation, targetLegR, 15f * Time.deltaTime);
        if (leftCalf != null) leftCalf.localRotation = Quaternion.Slerp(leftCalf.localRotation, targetCalfL, 15f * Time.deltaTime);
        if (rightCalf != null) rightCalf.localRotation = Quaternion.Slerp(rightCalf.localRotation, targetCalfR, 15f * Time.deltaTime);

        // Mantém a rotação padrão dos pés para evitar deformidades
        if (leftFoot != null)
        {
            leftFoot.localRotation = initialLeftFootRotation;
        }
        if (rightFoot != null)
        {
            rightFoot.localRotation = initialRightFootRotation;
        }
    }
}