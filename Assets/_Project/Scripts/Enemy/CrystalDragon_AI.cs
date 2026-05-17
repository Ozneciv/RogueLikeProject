using UnityEngine;
using System.Collections;

/// <summary>
/// IA do Crystal Dragon — patrulha no chão, ataca com spikes, tail sweep e crash heavy dash.
/// Usa DummyHealth para vida e PlayerHealth para causar dano.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(DummyHealth))]
public class CrystalDragon_AI : MonoBehaviour
{
    private Transform playerTransform;
    private DummyHealth health;
    private Rigidbody rb;

    [Header("Ativação")]
    public float activationDistance = 20f;
    private bool isActivated = false;

    [Header("Patrulha")]
    public float patrolRadius = 6f;
    public float patrolPointTime = 4f;
    private Vector3 patrolTarget;
    private float patrolTimer = 0f;
    private Vector3 startPosition;

    [Header("Movimento")]
    public float groundSpeed = 5f;
    public float rotationSpeed = 5f;
    public float dashSpeed = 8f;
    public float flightCooldown = 8f;
    private float flightCooldownTimer = 0f;

    [Header("Ataques")]
    public float spikeRange = 8f;
    public float tailRange = 3f;
    public float crashTriggerDistance = 4f;
    [Tooltip("Distância a partir da qual o dragão inicia o comportamento de voo (aproximação)")]
    public float flightStartDistance = 6f;
    [Header("Voo")]
    [Tooltip("Distância para iniciar o voo (m)")]
    public float flightTriggerDistance = 4f;
    [Tooltip("Tempo de subida até atingir o voo (segundos)")]
    public float flightAscendTime = 0.6f;
    [Tooltip("Tempo de hang/espera no ar antes do ataque (segundos)")]
    public float flightHangTime = 0.6f;
    [Tooltip("Tempo de descida/aterrissagem (segundos)")]
    public float flightDescendTime = 0.4f;
    [Tooltip("Velocidade vertical de subida (apenas para sensação de movimento)")]
    public float flightAscendSpeed = 6f;
    [Tooltip("Raio do ataque aéreo que aplica stun ao jogador")]
    public float flightAttackRange = 3f;
    [Tooltip("Duração do stun aplicado ao jogador (segundos)")]
    public float flightStunDuration = 2f;
    [Tooltip("Altura alvo em relação ao player para atingir o pico do voo (metros)")]
    public float flightPeakHeight = 2f;
    [Tooltip("Máxima distância horizontal (m) que o dragão pode deslocar-se durante o voo para posicionar-se acima do player")]
    public float flightMaxHorizontalMove = 2f;
    [Header("Tática")]
    [Tooltip("Distância que o dragão tenta manter para usar Crystal Spikes (m)")]
    public float preferredSpikeDistance = 6f;
    [Tooltip("Velocidade de recuo/posição quando mantendo distância (multiplicador)")]
    public float kiteSpeedMultiplier = 1.0f;
    [Header("Orbit")]
    [Tooltip("Velocidade de órbita em graus por segundo")]
    public float orbitSpeed = 45f;
    [Tooltip("Intervalo em segundos para possivel mudança de direção de órbita")]
    public float orbitChangeInterval = 4f;

    private float orbitAngle = 0f;
    private int orbitDirection = 1;
    private float orbitTimer = 0f;
    [Tooltip("Velocidade linear (m/s) usada ao orbitar ao redor do player")]
    public float orbitLinearSpeed = 3f;
    [Tooltip("Tolerância radial (m) para considerar-se na distância preferida e então orbitar tangencialmente")]
    public float orbitRadialTolerance = 0.6f;
    [Header("Animator")]
    [Tooltip("Animator opcional do modelo para triggers de voo/crash")]
    public Animator modelAnimator;
    public string flightAnimTrigger = "Fly";
    public string crashAnimTrigger = "Crash";
    [Tooltip("Prefab do telegraph visual exibido no chão antes do crash (opcional)")]
    public GameObject crashTelegraphPrefab;
    [Tooltip("Tempo em segundos do telegraph antes do crash")]
    public float crashTelegraphTime = 0.5f;
    public float spikeCooldown = 5f;
    [Header("Spike Rate")]
    [Tooltip("Cooldown mínimo entre volleys quando na distância ideal (segundos)")]
    public float spikeCooldownMin = 2f;
    [Tooltip("Cooldown máximo entre volleys quando fora da distância ideal (segundos)")]
    public float spikeCooldownMax = 5f;
    public float tailCooldown = 2f;
    public float chargeTime = 1.2f;
    public float recoveryTime = 1.5f;
    public int spikeDamage = 8;
    public int tailDamage = 15;
    public int crashDamage = 15;
    public int spikeCount = 3;
    public float spikeInterval = 0.15f;
    public float crashUpwardForce = 3f;
    public float crashForwardForce = 20f;
    public float tailHitRadius = 2.5f;

    [Header("Ataque frontal")]
    [Tooltip("Origem das crystal spikes. Se null, usa a posição do próprio inimigo.")]
    public Transform spikeOrigin;
    public float spikeConeAngle = 35f;

    [Header("Crystal Spike Projectile")]
    [Tooltip("Prefab do projétil lançado pelo Crystal Dragon.")]
    public GameObject crystalSpikeProjectilePrefab;
    public float spikeProjectileSpeed = 8f;
    public float spikeProjectileLifetime = 4f;
    public float spikeProjectileVerticalOffset = 0.4f;
    [Tooltip("Número de projéteis disparados em cada volley (forma de cone).")]
    public int spikeProjectilesPerVolley = 3;
    [Tooltip("Ângulo total em graus do cone (os projéteis são distribuídos igualmente).")]
    public float spikeVolleySpreadAngle = 20f;

    [Header("Model Offset")]
    [Tooltip("Offset de rotação local aplicado ao modelo filho para corrigir orientação importada (em Euler degrees)")]
    public Vector3 modelLocalEulerOffset = new Vector3(0f, 180f, 0f);
    [Tooltip("Se true, o offset será aplicado no Start() mesmo que o modelo já tenha rotação local configurada. Se false, preserva a rotação manual definida no prefab.")]
    public bool forceApplyModelOffset = true;

    [Header("Model World Alignment")]
    [Tooltip("Se true, o modelo visual será alinhado no mundo para manter a cabeça para cima enquanto a raiz gira.")]
    public bool preserveModelWorldUpright = true;
    [Tooltip("Euler offsets em world space aplicados ao modelo durante o alinhamento.")]
    public Vector3 modelWorldEulerOffset = new Vector3(-90f, 0f, 0f);

    private Transform modelTransform;
    private Quaternion desiredModelLocalRotation;
    private Quaternion lastLoggedModelLocalRotation;
    private float currentYaw = 0f;

    // Para restaurar constraints após crash
    private RigidbodyConstraints previousConstraints = RigidbodyConstraints.None;

    private float spikeTimer = 0f;
    private float tailTimer = 0f;
    private bool isCharging = false;
    private bool isAirDashing = false;
    private bool isFlying = false;
    private bool isRecovering = false;
    // Which subsystem is causing the charging state (used to allow flight to interrupt spikes)
    private bool isChargingForSpikes = false;
    private bool isChargingForTail = false;
    private bool isChargingForCrash = false;
    private Vector3 crashTarget;
    // Ensure the initial state allows the first flight without cooldown
    private bool hasFlownOnce = false;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        health = GetComponent<DummyHealth>();
        startPosition = transform.position;
        patrolTarget = GetRandomPatrolPoint();

        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
        else
        {
            Debug.LogError("CrystalDragon_AI: Player não encontrado! Verifique a tag 'Player'.");
        }

        if (spikeOrigin == null)
        {
            spikeOrigin = transform;
        }

        // Init orbit angle/direction
        if (playerTransform != null)
        {
            Vector3 offset = transform.position - playerTransform.position;
            orbitAngle = Mathf.Atan2(offset.x, offset.z) * Mathf.Rad2Deg;
            orbitDirection = (Random.value > 0.5f) ? 1 : -1;
            orbitTimer = Random.Range(1f, orbitChangeInterval);
        }

        currentYaw = transform.eulerAngles.y;

        // Corrige a orientação do modelo filho importado (se existir)
        modelTransform = transform.Find("blue_eyes_alt_dragon");
        if (modelTransform == null)
        {
            var rend = GetComponentInChildren<Renderer>();
            if (rend != null) modelTransform = rend.transform;
        }

        if (modelTransform != null)
        {
            if (forceApplyModelOffset || modelTransform.localEulerAngles == Vector3.zero)
            {
                desiredModelLocalRotation = Quaternion.Euler(modelLocalEulerOffset);
                modelTransform.localRotation = desiredModelLocalRotation;
                lastLoggedModelLocalRotation = modelTransform.localRotation;
                Debug.Log("[CRYSTAL DRAGON] Applied model local Euler offset: " + modelLocalEulerOffset);
            }
            else
            {
                Debug.Log("[CRYSTAL DRAGON] Preserving manual model local rotation: " + modelTransform.localEulerAngles);
            }
        }
    }

    private void LateUpdate()
    {
        if (modelTransform == null) return;

        if (forceApplyModelOffset)
        {
            if (preserveModelWorldUpright)
            {
                Quaternion targetRotation;
                if (modelTransform == transform)
                {
                    targetRotation = Quaternion.Euler(modelWorldEulerOffset.x, currentYaw + modelWorldEulerOffset.y, modelWorldEulerOffset.z);
                    if (transform.rotation != targetRotation)
                        transform.rotation = targetRotation;
                }
                else
                {
                    targetRotation = Quaternion.Euler(modelWorldEulerOffset);
                    if (modelTransform.localRotation != targetRotation)
                        modelTransform.localRotation = targetRotation;
                }
            }
            else
            {
                if (modelTransform.localRotation != desiredModelLocalRotation)
                {
                    modelTransform.localRotation = desiredModelLocalRotation;
                }
            }
        }
        else
        {
            if (modelTransform.localRotation != lastLoggedModelLocalRotation)
            {
                Debug.LogWarning("[CRYSTAL DRAGON] Model local rotation changed at runtime: " + modelTransform.localEulerAngles);
                lastLoggedModelLocalRotation = modelTransform.localRotation;
            }
        }
    }

    private void Update()
    {
        if (playerTransform == null) return;
        if (health != null && health.CurrentHealth <= 0) return;

        if (spikeTimer > 0f) spikeTimer -= Time.deltaTime;
        if (tailTimer > 0f) tailTimer -= Time.deltaTime;
        // Only decrement flight cooldown after we've performed the first flight
        if (hasFlownOnce && flightCooldownTimer > 0f) flightCooldownTimer -= Time.deltaTime;

        // Diagnostic: if player is within flight trigger range but flight doesn't start, log the blocking reason
        if (playerTransform != null)
        {
            float _diagDist = Vector3.Distance(transform.position, playerTransform.position);
            if (_diagDist <= flightStartDistance + 0.5f)
            {
                if (isCharging || isAirDashing || isRecovering)
                {
                    Debug.Log("[CRYSTAL DRAGON] Flight blocked: busy state -> isCharging=" + isCharging + " isAirDashing=" + isAirDashing + " isRecovering=" + isRecovering);
                }
                else if (flightCooldownTimer > 0f)
                {
                    Debug.Log("[CRYSTAL DRAGON] Flight blocked: cooldown remaining=" + flightCooldownTimer.ToString("F2") + "s");
                }
            }
        }

        if (!isActivated)
        {
            float dist = Vector3.Distance(transform.position, playerTransform.position);
            if (dist <= activationDistance)
            {
                isActivated = true;
                Debug.Log("[CRYSTAL DRAGON] Ativado! Player detectado a " + dist.ToString("F1") + "m");

                EnemyIdentity id = GetComponent<EnemyIdentity>() ?? GetComponentInChildren<EnemyIdentity>() ?? GetComponentInParent<EnemyIdentity>();
                if (id != null && BestiarioManager.instancia != null)
                {
                    BestiarioManager.instancia.Registrar(id);
                }
            }
            return;
        }

        if (isCharging || isAirDashing || isRecovering) return;

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        if (distanceToPlayer <= tailRange && tailTimer <= 0f && IsPlayerAtSideOrRear())
        {
            StartCoroutine(PerformTailSweep());
            return;
        }

        // Allow the first flight even if cooldown timer would otherwise block it
        if (!isFlying && distanceToPlayer <= flightTriggerDistance && (flightCooldownTimer <= 0f || !hasFlownOnce) && CanStartFlight())
        {
            StartCoroutine(PerformFlightStun());
            return;
        }

        if (distanceToPlayer <= spikeRange && spikeTimer <= 0f)
        {
            StartCoroutine(PerformCrystalSpikes());
            return;
        }
    }

    private void FixedUpdate()
    {
        if (!isActivated || isCharging || isAirDashing || isRecovering || isFlying) return;
        if (health != null && health.CurrentHealth <= 0) return;

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        if (distanceToPlayer <= activationDistance)
        {
            // If player is within spike range, try to maintain preferred distance (kiting)
            if (distanceToPlayer <= spikeRange)
            {
                // Orbit / strafe behavior while within spike range
                // Update orbit direction timer
                orbitTimer -= Time.deltaTime;
                if (orbitTimer <= 0f)
                {
                    orbitDirection = (Random.value > 0.5f) ? 1 : -1;
                    orbitTimer = orbitChangeInterval;
                }

                // Advance orbit angle
                orbitAngle += orbitSpeed * orbitDirection * Time.deltaTime;

                // Compute desired orbit position at preferred distance
                Vector3 toPlayer = transform.position - playerTransform.position;
                Vector3 desiredPos = playerTransform.position + Quaternion.Euler(0f, orbitAngle, 0f) * Vector3.forward * preferredSpikeDistance;

                // Radial difference (positive = we're farther than preferred)
                float radialDiff = toPlayer.magnitude - preferredSpikeDistance;

                // If we're outside the radial tolerance, approach/retreat radially
                // SEU FIXEDUPDATE CORRIGIDO:
                if (Mathf.Abs(radialDiff) > orbitRadialTolerance)
                {
                    // CORREÇÃO: O vetor agora aponta do Dragão para o Player para aproximar corretamente
                    Vector3 radialDirToPlayer = (playerTransform.position - transform.position).normalized; 
                    
                    // Se o dragão estiver longe demais, ele vai em direção ao player. 
                    // Se estiver perto demais (radialDiff negativo), ele se afasta até a preferredSpikeDistance.
                    Vector3 radialTarget = playerTransform.position - radialDirToPlayer * preferredSpikeDistance;
                    
                    MoveTowards(radialTarget, groundSpeed * kiteSpeedMultiplier);
                }
                else
                {
                    // We're close to preferred radius: orbit tangentially smoothly
                    Vector3 radialNorm = toPlayer.normalized;
                    Vector3 tangential = Vector3.Cross(Vector3.up, radialNorm) * orbitDirection; // right/left around player
                    Vector3 tangentialVel = tangential.normalized * orbitLinearSpeed;

                    // Small radial correction to keep stable distance
                    Vector3 radialCorrection = Vector3.zero;
                    if (Mathf.Abs(radialDiff) > 0.05f)
                        radialCorrection = radialNorm * (radialDiff * -0.5f);

                    Vector3 desiredVelocity = tangentialVel + radialCorrection;
                    desiredVelocity.y = 0f;

                    // Smooth velocity change to avoid jitter
                    rb.linearVelocity = Vector3.Lerp(new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z), desiredVelocity, 0.16f) + new Vector3(0f, rb.linearVelocity.y, 0f);
                }
            }
            else
            {
                // If spike is on cooldown, don't rush onto the player: try to move to preferred spike distance
                if (spikeTimer > 0f)
                {
                    Vector3 radialDir = (transform.position - playerTransform.position).normalized;
                    Vector3 radialTarget = playerTransform.position + radialDir * preferredSpikeDistance;
                    MoveTowards(radialTarget, groundSpeed * 0.6f);
                }
                else
                {
                    // Approach to get within spike range
                    MoveTowards(playerTransform.position, groundSpeed);
                }
            }
        }
        else
        {
            Patrol();
        }
    }

    private void MoveTowards(Vector3 targetPosition, float speed)
    {
        Vector3 direction = (targetPosition - transform.position).normalized;
        direction.y = 0f;

        if (direction != Vector3.zero)
        {
            currentYaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            Quaternion targetRotation = Quaternion.Euler(0f, currentYaw, 0f);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        Vector3 velocity = direction * speed;
        velocity.y = rb.linearVelocity.y;
        rb.linearVelocity = velocity;
    }

    private void Patrol()
    {
        patrolTimer -= Time.deltaTime;
        if (patrolTimer <= 0f || Vector3.Distance(transform.position, patrolTarget) < 1f)
        {
            patrolTarget = GetRandomPatrolPoint();
            patrolTimer = patrolPointTime;
        }
        MoveTowards(patrolTarget, groundSpeed * 0.6f);
    }

    private Vector3 GetRandomPatrolPoint()
    {
        Vector2 randomCircle = Random.insideUnitCircle * patrolRadius;
        Vector3 point = startPosition + new Vector3(randomCircle.x, 0f, randomCircle.y);
        return point;
    }

    private bool IsPlayerAtSideOrRear()
    {
        Vector3 toPlayer = (playerTransform.position - transform.position).normalized;
        toPlayer.y = 0f;
        float forwardDot = Vector3.Dot(transform.forward, toPlayer);
        return forwardDot < 0.5f;
    }

    private IEnumerator PerformCrystalSpikes()
    {
        isCharging = true;
        isChargingForSpikes = true;
        // dynamic cooldown based on distance to preferred spike distance
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        float closeness = 0f;
        if (preferredSpikeDistance > 0f)
        {
            closeness = 1f - Mathf.Clamp01(Mathf.Abs(distanceToPlayer - preferredSpikeDistance) / preferredSpikeDistance);
        }
        float dynamicCooldown = Mathf.Lerp(spikeCooldownMax, spikeCooldownMin, closeness);
        spikeTimer = dynamicCooldown;

        Debug.Log("[CRYSTAL DRAGON] Crystal Spikes! Preparando disparo frontal.");
        rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);

        // Pequena telegraph
        yield return new WaitForSeconds(0.4f);

        Vector3 spawnPosition = spikeOrigin != null ? spikeOrigin.position : transform.position + transform.forward * 1.2f + Vector3.up * spikeProjectileVerticalOffset;
        if (spikeOrigin == null) spawnPosition += Vector3.up * spikeProjectileVerticalOffset;

        // Spawn a single volley of N projectiles distributed in a cone
        Vector3 baseTarget = playerTransform != null ? playerTransform.position : (spawnPosition + transform.forward * 10f);
        baseTarget.y = spawnPosition.y;
        Vector3 baseDir = (baseTarget - spawnPosition).normalized;
        if (baseDir == Vector3.zero) baseDir = transform.forward;

        int nProjectiles = Mathf.Max(1, spikeProjectilesPerVolley);
        float half = spikeVolleySpreadAngle * 0.5f;

        for (int j = 0; j < nProjectiles; j++)
        {
            float t = (nProjectiles == 1) ? 0f : ((float)j / (nProjectiles - 1));
            float angle = Mathf.Lerp(-half, half, t);
            Vector3 dir = Quaternion.Euler(0f, angle, 0f) * baseDir;

            GameObject projectileObj = null;
            if (crystalSpikeProjectilePrefab != null)
            {
                projectileObj = Instantiate(crystalSpikeProjectilePrefab, spawnPosition, Quaternion.LookRotation(dir));
            }
            else
            {
                // Fallback visual: esfera pequena para representar projétil
                projectileObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                projectileObj.transform.position = spawnPosition;
                projectileObj.transform.rotation = Quaternion.LookRotation(dir);
                projectileObj.transform.localScale = Vector3.one * 0.22f;
                Collider col = projectileObj.GetComponent<Collider>();
                if (col != null) col.isTrigger = false;
                Rigidbody rbp = projectileObj.AddComponent<Rigidbody>();
                rbp.useGravity = false;

                // Apply crystal-like material color (purple/pink crystal)
                Renderer r = projectileObj.GetComponent<Renderer>();
                if (r != null)
                {
                    Material mat = new Material(Shader.Find("Standard"));
                    Color baseColor = new Color(0.46f, 0.29f, 0.62f); // purple
                    mat.color = baseColor;
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", baseColor * 0.6f);
                    r.material = mat;
                }
            }

            // If prefab has a renderer, tint it to the crystal color so it matches the VFX image
            if (projectileObj != null)
            {
                Renderer existingRend = projectileObj.GetComponentInChildren<Renderer>();
                if (existingRend != null)
                {
                    Material mat2 = new Material(Shader.Find("Standard"));
                    Color tint = new Color(0.46f, 0.29f, 0.62f);
                    mat2.color = tint;
                    mat2.EnableKeyword("_EMISSION");
                    mat2.SetColor("_EmissionColor", tint * 0.6f);
                    existingRend.material = mat2;
                }
            }

            // Attach or configure projectile behaviour
            CrystalSpikeProjectile spikeProjectile = projectileObj.GetComponent<CrystalSpikeProjectile>();
            if (spikeProjectile == null)
            {
                spikeProjectile = projectileObj.AddComponent<CrystalSpikeProjectile>();
            }

            spikeProjectile.owner = gameObject;
            spikeProjectile.damage = spikeDamage;
            spikeProjectile.Launch(dir, spikeProjectileSpeed, spikeProjectileLifetime);
        }

        // Single telegraph delay after volley
        yield return new WaitForSeconds(spikeInterval);

        isCharging = false;
        isChargingForSpikes = false;
    }

    private bool IsPlayerInFrontCone()
    {
        Vector3 from = spikeOrigin != null ? spikeOrigin.position : transform.position;
        Vector3 toPlayer = (playerTransform.position - from).normalized;
        toPlayer.y = 0f;
        float angle = Vector3.Angle(transform.forward, toPlayer);
        float distance = Vector3.Distance(from, playerTransform.position);
        return angle <= spikeConeAngle && distance <= spikeRange;
    }

    private IEnumerator PerformTailSweep()
    {
        isCharging = true;
        isChargingForTail = true;
        tailTimer = tailCooldown;

        Debug.Log("[CRYSTAL DRAGON] Tail Sweep! Girando e batendo nas laterais.");
        rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);

        yield return new WaitForSeconds(0.25f);

        Collider[] hits = Physics.OverlapSphere(transform.position, tailHitRadius);
        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                PlayerHealth playerHealth = hit.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(tailDamage, gameObject);
                    Debug.Log("[CRYSTAL DRAGON] Tail Sweep acertou o player! Dano: " + tailDamage);
                }
            }
        }

        yield return new WaitForSeconds(0.25f);
        isCharging = false;
        isChargingForTail = false;
    }

    private IEnumerator PerformCrashCharge()
    {
        isCharging = true;
        isChargingForCrash = true;
        rb.linearVelocity = Vector3.zero;
        Debug.Log("[CRYSTAL DRAGON] Charging Heavy Crash...");

        yield return new WaitForSeconds(chargeTime);

        crashTarget = new Vector3(playerTransform.position.x, transform.position.y, playerTransform.position.z);
        Vector3 dashDirection = (crashTarget - transform.position).normalized;
        dashDirection.y = 0f;
        if (dashDirection == Vector3.zero) dashDirection = transform.forward;

        isCharging = false;
        isChargingForCrash = false;
        isAirDashing = true;

        // Store and enforce freeze rotation to avoid tumbling during crash
        previousConstraints = rb.constraints;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.angularVelocity = Vector3.zero;

        Vector3 crashVelocity = dashDirection * crashForwardForce + Vector3.up * crashUpwardForce;
        rb.linearVelocity = crashVelocity;

        Debug.Log("[CRYSTAL DRAGON] Heavy Crash lançado! Constraints set to FreezeRotation.");
        yield return null;
    }

 private IEnumerator PerformFlightStun()
{
    if (playerTransform == null) yield break;

    isCharging = true;
    isFlying = true;

    if (health != null) health.isInvulnerable = true;

    hasFlownOnce = true;
    Debug.Log("[CRYSTAL DRAGON] Flight takeoff: ascending to peak above player.");

    if (modelAnimator != null && !string.IsNullOrEmpty(flightAnimTrigger)) 
        modelAnimator.SetTrigger(flightAnimTrigger);

    // Subida
    Vector3 startPos = transform.position;
    Vector3 currentXZ = new Vector3(transform.position.x, 0f, transform.position.z);
    Vector3 playerXZ = new Vector3(playerTransform.position.x, 0f, playerTransform.position.z);
    Vector3 horizontalOffset = playerXZ - currentXZ;
    horizontalOffset = Vector3.ClampMagnitude(horizontalOffset, flightMaxHorizontalMove);
    Vector3 peakPos = new Vector3(currentXZ.x + horizontalOffset.x, playerTransform.position.y + flightPeakHeight, currentXZ.z + horizontalOffset.z);
    
    float elapsed = 0f;
    while (elapsed < flightAscendTime)
    {
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / flightAscendTime);
        Vector3 next = Vector3.Lerp(startPos, peakPos, t);
        rb.MovePosition(next);
        yield return null;
    }

    rb.MovePosition(peakPos);
    yield return new WaitForSeconds(flightHangTime);

    if (modelAnimator != null && !string.IsNullOrEmpty(crashAnimTrigger)) 
        modelAnimator.SetTrigger(crashAnimTrigger);

    if (crashTelegraphPrefab != null && playerTransform != null)
    {
        Vector3 telePos = new Vector3(playerTransform.position.x, playerTransform.position.y + 0.05f, playerTransform.position.z);
        GameObject tele = Instantiate(crashTelegraphPrefab, telePos, Quaternion.Euler(90f, 0f, 0f));
        tele.transform.localScale = Vector3.one * 1f;
        Destroy(tele, crashTelegraphTime + 0.1f);
    }

    yield return new WaitForSeconds(crashTelegraphTime);

    // Transição estrita para o AirDash
    isCharging = false;
    isChargingForSpikes = false;
    isChargingForTail = false;
    isChargingForCrash = false;
    isAirDashing = true;

    previousConstraints = rb.constraints;
    rb.constraints = RigidbodyConstraints.FreezeRotation;
    rb.angularVelocity = Vector3.zero;

    Vector3 target = new Vector3(playerTransform.position.x, playerTransform.position.y, playerTransform.position.z);
    Vector3 dashDirection = (target - transform.position).normalized;
    dashDirection.y = Mathf.Min(dashDirection.y, -0.3f);

    Vector3 crashVelocity = dashDirection * crashForwardForce + Vector3.down * crashUpwardForce;
    rb.linearVelocity = crashVelocity;

    Debug.Log("[CRYSTAL DRAGON] Performing airborne heavy crash towards player.");

    // CORREÇÃO: Failsafe caso o OnCollisionEnter falhe por problemas de malha/Trigger da Unity
    yield return new WaitForSeconds(1.5f);
    if (isAirDashing)
    {
        Debug.LogWarning("[CRYSTAL DRAGON] Failsafe ativado: Colisão não detectada no tempo limite. Forçando recuperação.");
        StartCoroutine(CrashRecovery());
    }
    }
    private IEnumerator CrashRecovery()
    {
        // 1. Libera imediatamente o estado de colisão e voo
        isAirDashing = false; 
        isFlying = false; 
        flightCooldownTimer = flightCooldown;

        Debug.Log("[CRYSTAL DRAGON] Crash complete. Recuperando...");
        rb.linearVelocity = Vector3.zero; // Para o deslize completamente

        // Espera o tempo de tontura/recuperação do dragão
        yield return new WaitForSeconds(recoveryTime);

        // 2. CORREÇÃO CRÍTICA: Desliga TODAS as variáveis de carregamento e estado
        isRecovering = false;
        isCharging = false; 
        isChargingForSpikes = false;
        isChargingForTail = false;
        isChargingForCrash = false;

        // 3. Restaura a física original de rotação
        rb.constraints = previousConstraints != RigidbodyConstraints.None ? previousConstraints : RigidbodyConstraints.FreezeRotation;
        
        if (health != null) health.isInvulnerable = false;
        Debug.Log("[CRYSTAL DRAGON] Recovery finished. Estado resetado com sucesso! Retornando para órbita.");
    }

    private void OnCollisionEnter(Collision collision)
{
    // Se ele já colidiu ou não está no meio do Dash, ignora para não reiniciar o cooldown à toa
    if (!isAirDashing || isRecovering) return; 

    Debug.Log("[CRYSTAL DRAGON] Collision detected with: " + collision.collider.name);

    if (collision.collider.CompareTag("Player"))
    {
        PlayerHealth playerHealth = collision.collider.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(crashDamage);
            Debug.Log("[CRYSTAL DRAGON] Heavy Crash acertou o player!");
        }
    }

    // Para o processo do Failsafe do voo e inicia a recuperação imediatamente
    StopAllCoroutines(); 
    StartCoroutine(CrashRecovery());
}

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, activationDistance);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, spikeRange);

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, tailRange);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, crashTriggerDistance);
    }

    private bool CanStartFlight()
    {
        // Can't start flight if currently air dashing or recovering
        if (isAirDashing || isRecovering) return false;

        // If not charging anything, flight can start
        if (!isCharging) return true;

        // If charging, only allow flight to interrupt spike charging
        if (isChargingForSpikes && !isChargingForTail && !isChargingForCrash) return true;

        return false;
    }
}
