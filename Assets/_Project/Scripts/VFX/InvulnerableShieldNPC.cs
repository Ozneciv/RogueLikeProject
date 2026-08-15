using System.Collections;
using UnityEngine;

/// <summary>
/// Componente de Escudo de Invulnerabilidade para NPCs e Inimigos (Eptinho, Mercador, Star, etc.).
/// Aciona um feedback visual de escudo (ex: Hovl Studio Magic Shield) com fade in/out suave,
/// suporte a escala customizada (esférico 360° ou elipsoide alongado) e pulsação de impacto.
/// </summary>
public class InvulnerableShieldNPC : MonoBehaviour
{
    [Header("Configuração Visual do Escudo")]
    [Tooltip("GameObject do escudo (instância ou filho do modelo 3D). Ex: Magic shield blue.")]
    public GameObject shieldVisualObject;

    [Tooltip("Escala customizada do escudo no Transform. Usar (1.5, 1.5, 1.5) para esfera completa ou (1.2, 2.3, 1.2) para elipsoide/mercador.")]
    public Vector3 customScale = Vector3.one;

    [Header("Efeito de Faísca de Impacto (Hit Spark)")]
    [Tooltip("Lista de objetos de faísca filhos pré-posicionados no prefab (Ex: Faísca Esquerda, Direita, Frente). O script alterna ou sorteia entre eles a cada hit.")]
    public GameObject[] hitSparkChildObjects;

    [Tooltip("Objeto de faísca filho único (caso prefira usar apenas um).")]
    public GameObject hitSparkChildObject;

    [Tooltip("Prefab de faísca dinâmico (opcional caso prefira instanciar via código).")]
    public GameObject hitSparkPrefab;

    private int nextSparkIndex = 0;

    [Tooltip("Duração total do flash do escudo em segundos (aumentado por padrão).")]
    public float shieldDuration = 1.2f;

    [Tooltip("Velocidade de crescimento/expansão inicial do escudo ao receber o golpe (ex: 4.0 = pop instantâneo).")]
    public float expansionSpeed = 4.0f;

    [Tooltip("Multiplicador de pulsação no impacto (ex: 1.08 = expande 8% no ápice do golpe).")]
    public float impactPulseMultiplier = 1.08f;

    [Header("Camera Shake Feedback")]
    [Tooltip("Se verdadeiro, dispara um tremor de câmera ao acertar o escudo.")]
    public bool enableCameraShake = false;

    [Tooltip("Duração do tremor de câmera em segundos.")]
    public float cameraShakeDuration = 0.25f;

    [Tooltip("Intensidade/Força do tremor de câmera (ex: 0.45 para tremor forte).")]
    public float cameraShakeIntensity = 0.35f;

    [Header("Áudio & Repulsão Mística (Opcional)")]
    public AudioSource audioSource;
    public AudioClip deflectSound;

    [Tooltip("Se verdadeiro, aplica um leve empurrão de repulsão no jogador ao atingir o escudo.")]
    public bool enableRepulsionKnockback = true;

    [Tooltip("Força do empurrão suave de repulsão (Padrão: 4.0).")]
    public float repulsionForce = 4.0f;

    [Tooltip("Duração da força de empurrão em segundos.")]
    public float knockbackDuration = 0.12f;

    private Coroutine shieldCoroutine;
    private Material shieldMaterial;
    private string colorProperty;
    private Color originalColor = Color.cyan;

    private void Awake()
    {
        InitializeShield();
    }

    private void InitializeShield()
    {
        if (shieldVisualObject == null)
        {
            // Tenta encontrar um filho chamado "Shield" ou com "shield" no nome
            Transform shieldChild = transform.Find("Shield");
            if (shieldChild == null)
            {
                foreach (Transform child in transform)
                {
                    if (child.name.ToLower().Contains("shield"))
                    {
                        shieldChild = child;
                        break;
                    }
                }
            }
            if (shieldChild != null) shieldVisualObject = shieldChild.gameObject;
        }

        if (hitSparkChildObject != null)
        {
            hitSparkChildObject.SetActive(false);
        }

        if (shieldVisualObject != null)
        {
            shieldVisualObject.transform.localScale = customScale;

            Renderer r = shieldVisualObject.GetComponent<Renderer>();
            if (r != null)
            {
                shieldMaterial = r.material;
                if (shieldMaterial.HasProperty("_Color")) colorProperty = "_Color";
                else if (shieldMaterial.HasProperty("_BaseColor")) colorProperty = "_BaseColor";
                else if (shieldMaterial.HasProperty("_HologramColor")) colorProperty = "_HologramColor";

                if (!string.IsNullOrEmpty(colorProperty))
                {
                    originalColor = shieldMaterial.GetColor(colorProperty);
                }
            }

            shieldVisualObject.SetActive(false);
        }
    }

    /// <summary>
    /// Método público para acionar o flash do escudo manualmente.
    /// </summary>
    public void TriggerShield()
    {
        TriggerShield(Vector3.zero);
    }

    /// <summary>
    /// Sobrecarga que instancia a faísca de impacto (Hit Spark) exatamente no ponto de contato.
    /// </summary>
    public void TriggerShield(Vector3 hitPosition)
    {
        // Se houver uma lista de objetos de faíscas filhos pré-posicionados, sorteia/alterna entre eles!
        if (hitSparkChildObjects != null && hitSparkChildObjects.Length > 0)
        {
            int index = Random.Range(0, hitSparkChildObjects.Length);
            GameObject sparkObj = hitSparkChildObjects[index];

            if (sparkObj != null)
            {
                sparkObj.SetActive(false);
                sparkObj.SetActive(true);
                ParticleSystem[] particles = sparkObj.GetComponentsInChildren<ParticleSystem>(true);
                foreach (var ps in particles)
                {
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    ps.Play(true);
                }
            }
        }
        else if (hitSparkChildObject != null)
        {
            hitSparkChildObject.SetActive(false);
            hitSparkChildObject.SetActive(true);
            ParticleSystem[] particles = hitSparkChildObject.GetComponentsInChildren<ParticleSystem>(true);
            foreach (var ps in particles)
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                ps.Play(true);
            }
        }
        else if (hitSparkPrefab != null)
        {
            Vector3 sparkPos = (hitPosition != Vector3.zero) ? hitPosition : transform.position + Vector3.up * 0.8f;
            Quaternion sparkRot = (hitPosition != Vector3.zero && hitPosition != transform.position) 
                ? Quaternion.LookRotation(hitPosition - transform.position) 
                : Quaternion.identity;

            GameObject spark = Instantiate(hitSparkPrefab, sparkPos, sparkRot);
            Destroy(spark, 1.5f);
        }

        if (shieldVisualObject == null) InitializeShield();
        if (shieldVisualObject == null) return;

        if (enableCameraShake)
        {
            if (CameraShakeFeedback.Instance != null)
            {
                CameraShakeFeedback.TriggerShake(cameraShakeDuration, cameraShakeIntensity, 35f);
            }
            else
            {
                BossController.TriggerCameraShake(cameraShakeDuration, cameraShakeIntensity);
            }
        }

        if (audioSource != null && deflectSound != null)
        {
            audioSource.PlayOneShot(deflectSound);
        }

        if (enableRepulsionKnockback)
        {
            ApplyPlayerRepulsion();
        }

        if (shieldCoroutine != null) StopCoroutine(shieldCoroutine);
        shieldCoroutine = StartCoroutine(ShieldFlashRoutine());
    }

    private void ApplyPlayerRepulsion()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null)
        {
            PlayerM p = Object.FindFirstObjectByType<PlayerM>();
            if (p != null) playerObj = p.gameObject;
        }

        if (playerObj != null)
        {
            Rigidbody playerRb = playerObj.GetComponent<Rigidbody>();
            if (playerRb != null)
            {
                Vector3 pushDir = (playerObj.transform.position - transform.position);
                pushDir.y = 0f;
                if (pushDir.sqrMagnitude < 0.001f) pushDir = -transform.forward;
                else pushDir.Normalize();

                StartCoroutine(RepulsionRoutine(playerRb, pushDir));
            }
        }
    }

    private IEnumerator RepulsionRoutine(Rigidbody playerRb, Vector3 pushDir)
    {
        float elapsed = 0f;
        while (elapsed < knockbackDuration)
        {
            elapsed += Time.deltaTime;
            float currentForce = Mathf.Lerp(repulsionForce, 0f, elapsed / knockbackDuration);
            playerRb.linearVelocity = new Vector3(pushDir.x * currentForce, playerRb.linearVelocity.y, pushDir.z * currentForce);
            yield return null;
        }
    }

    /// <summary>
    /// Suporte automático a chamadas de TakeDamage (para integridade com Player Attacks).
    /// </summary>
    public void TakeDamage(int damage, bool isCritical = false)
    {
        TriggerShield();
    }

    private IEnumerator ShieldFlashRoutine()
    {
        shieldVisualObject.SetActive(true);
        Transform shieldTr = shieldVisualObject.transform;
        shieldTr.localScale = customScale;

        ParticleSystem[] shieldParticles = shieldVisualObject.GetComponentsInChildren<ParticleSystem>(true);
        if (shieldParticles != null && shieldParticles.Length > 0)
        {
            foreach (var ps in shieldParticles)
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                ps.Play(true);
            }
        }

        float elapsed = 0f;
        float duration = Mathf.Max(0.2f, shieldDuration);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            float expansionT = Mathf.Clamp01(t * expansionSpeed);
            float currentPulse = Mathf.Lerp(0.85f, impactPulseMultiplier, expansionT);
            shieldTr.localScale = customScale * currentPulse;

            yield return null;
        }

        // Para a emissão suavemente para que as partículas sumam respeitando o tempo de fade out do prefab
        if (shieldParticles != null && shieldParticles.Length > 0)
        {
            foreach (var ps in shieldParticles)
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
            yield return new WaitForSeconds(0.6f);
        }

        shieldTr.localScale = customScale;
        shieldVisualObject.SetActive(false);
        shieldCoroutine = null;
    }
}
