using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class HomingHazard : MonoBehaviour
{
    public enum HazardState { Emergence, Chasing, Fusing, Exploded }

    [Header("Movimento & Perseguição")]
    [Tooltip("A velocidade com que a caveira persegue o jogador.")]
    public float moveSpeed = 3.5f;

    [Header("Emergência da Cabeça do Totem")]
    [Tooltip("Duração do movimento ascendente ao sair da cabeça do Totem.")]
    public float emergeDuration = 0.5f;
    [Tooltip("Velocidade de subida ao sair do Totem.")]
    public float emergeUpSpeed = 3.0f;

    [Header("Altura e Flutuação")]
    public float maxFlyHeight = 1.6f;
    public float bobAmplitude = 0.18f;
    public float bobSpeed = 2.5f;

    [Header("Bomba de Caveira Goblin (Fusível & Explosão)")]
    [Tooltip("Distância do jogador para a caveira parar e armar o fusível.")]
    public float fuseTriggerDistance = 2.5f;

    [Tooltip("Tempo em segundos que a caveira fica piscando antes de explodir.")]
    public float fuseDuration = 1.0f;

    [Tooltip("Raio da área de dano da explosão.")]
    public float explosionRadius = 3.5f;

    [Tooltip("Dano causado ao jogador na explosão.")]
    public int explosionDamage = 25;

    [Header("Efeitos Visuais de Explosão")]
    public GameObject explosionVFXPrefab;

    private Transform playerTransform;
    private Rigidbody rb;
    private float bobOffset;
    private HazardState currentState = HazardState.Emergence;

    private float stateTimer = 0f;
    private Renderer[] renderers;
    private Color[] originalColors;
    private Vector3 originalScale;
    private DummyHealth health;
    private PulseVisualizer pulseVisualizer;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        bobOffset = Random.Range(0f, Mathf.PI * 2f);
        originalScale = transform.localScale;

        health = GetComponent<DummyHealth>();
        pulseVisualizer = GetComponent<PulseVisualizer>();

        // Garante que os colisores da caveira sejam Triggers para fluir sem prender no Totem
        Collider[] cols = GetComponentsInChildren<Collider>();
        foreach (var c in cols)
        {
            if (c != null) c.isTrigger = true;
        }

        // Cache de renderizadores para piscar durante a fusão
        renderers = GetComponentsInChildren<Renderer>();
        if (renderers != null && renderers.Length > 0)
        {
            originalColors = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null && renderers[i].material != null && renderers[i].material.HasProperty("_Color"))
                {
                    originalColors[i] = renderers[i].material.color;
                }
                else
                {
                    originalColors[i] = Color.white;
                }
            }
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            Transform targetPoint = player.transform.Find("TorsoTarget");
            playerTransform = (targetPoint != null) ? targetPoint : player.transform;
        }

        if (currentState == HazardState.Emergence && stateTimer <= 0f)
        {
            stateTimer = emergeDuration;
        }
    }

    public void InitializeEmergence(Vector3 startPos)
    {
        transform.position = startPos;
        currentState = HazardState.Emergence;
        stateTimer = emergeDuration;
    }

    void Update()
    {
        if (health != null && health.CurrentHealth <= 0 && currentState != HazardState.Exploded)
        {
            Explode();
            return;
        }

        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerTransform = player.transform;
            else return;
        }

        if (currentState == HazardState.Fusing)
        {
            stateTimer -= Time.deltaTime;

            // Pisca rapidamente alternando cor e pulso de tamanho
            float flashSpeed = Mathf.Lerp(30f, 10f, stateTimer / fuseDuration);
            bool isFlash = (Mathf.Sin(Time.time * flashSpeed) > 0);

            if (renderers != null)
            {
                for (int i = 0; i < renderers.Length; i++)
                {
                    if (renderers[i] != null && renderers[i].material != null && renderers[i].material.HasProperty("_Color"))
                    {
                        renderers[i].material.color = isFlash ? Color.red * 1.5f : originalColors[i];
                    }
                }
            }

            // Leve pulsação de escala
            float scalePulse = 1f + 0.15f * Mathf.Sin(Time.time * flashSpeed);
            transform.localScale = originalScale * scalePulse;

            if (stateTimer <= 0f)
            {
                Explode();
            }
        }
    }

    void FixedUpdate()
    {
        if (playerTransform == null || currentState == HazardState.Exploded) return;

        if (currentState == HazardState.Emergence)
        {
            // Emerge da cabeça do Totem subindo suavemente
            rb.linearVelocity = Vector3.up * emergeUpSpeed;
            transform.LookAt(playerTransform);

            stateTimer -= Time.fixedDeltaTime;
            if (stateTimer <= 0f)
            {
                currentState = HazardState.Chasing;
            }
            return;
        }

        if (currentState == HazardState.Chasing)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

            // Chegou perto do jogador? Para e arma o fusível!
            if (distanceToPlayer <= fuseTriggerDistance)
            {
                currentState = HazardState.Fusing;
                stateTimer = fuseDuration;
                rb.linearVelocity = Vector3.zero;
                Debug.Log("[GOBLIN SKULL BOMB] Caveira chegou perto do player! Armando bomba...");
                return;
            }

            // Movimento de perseguição
            Vector3 currentPos = transform.position;
            Vector3 targetXZ = new Vector3(playerTransform.position.x, currentPos.y, playerTransform.position.z);
            Vector3 direction = (targetXZ - currentPos).normalized;

            float bob = Mathf.Sin(Time.time * bobSpeed + bobOffset) * bobAmplitude;
            float targetY = Mathf.Clamp(playerTransform.position.y + 0.5f + bob, 0.5f, maxFlyHeight + 1f);
            float newY = Mathf.Lerp(currentPos.y, targetY, 6f * Time.fixedDeltaTime);

            Vector3 vel = direction * moveSpeed;
            vel.y = (newY - currentPos.y) / Time.fixedDeltaTime;
            rb.linearVelocity = vel;

            transform.LookAt(playerTransform);
        }
        else if (currentState == HazardState.Fusing)
        {
            rb.linearVelocity = Vector3.zero;
        }
    }

    public void Explode()
    {
        if (currentState == HazardState.Exploded) return;
        currentState = HazardState.Exploded;

        Vector3 explosionCenter = transform.position;
        Debug.Log("[GOBLIN SKULL BOMB] BOOM! Caveira explodiu!");

        // Aplica dano ao jogador se estiver no raio de explosão
        Collider[] hitColliders = Physics.OverlapSphere(explosionCenter, explosionRadius);
        foreach (var col in hitColliders)
        {
            if (col.CompareTag("Player"))
            {
                PlayerHealth ph = col.GetComponent<PlayerHealth>() ?? col.GetComponentInParent<PlayerHealth>();
                if (ph != null)
                {
                    ph.TakeDamage(explosionDamage, gameObject);
                    Debug.Log($"[GOBLIN SKULL BOMB] Dano de explosão ({explosionDamage}) causado ao Player!");
                }
            }
        }

        // Gera o efeito visual da explosão (onda de choque + flash de fogo)
        CreateExplosionVFX(explosionCenter);

        if (explosionVFXPrefab != null)
        {
            Instantiate(explosionVFXPrefab, explosionCenter, Quaternion.identity);
        }

        Destroy(gameObject);
    }

    private void CreateExplosionVFX(Vector3 position)
    {
        GameObject expObj = new GameObject("SkullExplosionVFX");
        expObj.transform.position = position;

        // Flash de Luz
        Light light = expObj.AddComponent<Light>();
        light.color = new Color(1f, 0.4f, 0.1f);
        light.range = explosionRadius * 2f;
        light.intensity = 6f;

        // Esfera de Impacto / Onda de choque
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.transform.position = position;
        sphere.transform.localScale = Vector3.one * 0.4f;

        Collider col = sphere.GetComponent<Collider>();
        if (col != null) Destroy(col);

        Renderer rend = sphere.GetComponent<Renderer>();
        if (rend != null)
        {
            Material mat = new Material(Shader.Find("Sprites/Default"));
            mat.color = new Color(1f, 0.35f, 0.0f, 0.85f);
            rend.material = mat;
        }

        StartCoroutine(AnimateExplosion(expObj, sphere, rend, light));
    }

    private IEnumerator AnimateExplosion(GameObject container, GameObject sphere, Renderer rend, Light light)
    {
        float duration = 0.35f;
        float elapsed = 0f;
        Vector3 startScale = Vector3.one * 0.4f;
        Vector3 endScale = Vector3.one * (explosionRadius * 1.8f);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            if (sphere != null)
            {
                sphere.transform.localScale = Vector3.Lerp(startScale, endScale, t);
                if (rend != null && rend.material != null)
                {
                    Color c = rend.material.color;
                    c.a = Mathf.Lerp(0.85f, 0f, t);
                    rend.material.color = c;
                }
            }

            if (light != null)
            {
                light.intensity = Mathf.Lerp(6f, 0f, t);
            }

            yield return null;
        }

        if (sphere != null) Destroy(sphere);
        if (container != null) Destroy(container);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, fuseTriggerDistance);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}