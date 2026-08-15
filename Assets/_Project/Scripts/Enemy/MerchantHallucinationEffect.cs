using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Sistema de Ira do Mercador das Sombras (Altamente Otimizado / Zero Lag):
///   • 4º Hit: Olho de Peixe Dinâmico (Lens Distortion + Panini + FOV Micro-Breathing).
///   • 7º Hit: Escuridão Atmosférica (-1.2 EV via URP GPU Post-Processing - Zero CPU overhead).
///   • 9º Hit: Inversão Espectral de Cores (Hue Inversion 180° + Contraste Nítido - Sem tela branca).
///   • 10º Hit: Morte Imediata / Execução das Sombras.
///   • Reset Automático no Respawn/Morte: Ao morrer ou respawnar, todos os efeitos são limpos instantaneamente.
/// </summary>
public class MerchantHallucinationEffect : MonoBehaviour
{
    private static MerchantHallucinationEffect _instance;
    public static MerchantHallucinationEffect Instance => _instance;

    [Header("⏱️ Duração & Transição")]
    [Tooltip("Duração (em segundos) que a ira permanece travada em potência máxima após o último golpe (Padrão: 25s).")]
    public float lingerDuration = 25.0f;

    [Tooltip("Velocidade de entrada dos efeitos (Fade In).")]
    public float fadeInSpeed = 3.0f;

    [Tooltip("Velocidade de retorno suave ao normal após o tempo expirar (Fade Out).")]
    public float fadeOutSpeed = 0.40f;

    [Header("🐟 4º Hit: Olho de Peixe Dinâmico")]
    [Range(-1f, 0f)] public float maxFishEyeDistortion = -0.80f;
    [Range(-1f, 0f)] public float minFishEyeDistortion = -0.15f;
    public float lensOscillationSpeed = 1.6f;
    [Range(0f, 1f)] public float maxPaniniCurvature = 0.60f;
    [Range(0f, 1f)] public float minPaniniCurvature = 0.05f;
    public float maxFovBonus = 1.0f;
    public float minFovBonus = 0.0f;

    [Header("🌑 7º Hit: Escuridão do Ambiente (GPU Post-Processing)")]
    public float maxDarkExposure = -1.2f; // Exatamente -1.2 EV
    public Color darkColorFilter = new Color(0.72f, 0.48f, 0.55f);

    [Header("🌈 9º Hit: Inversão de Cores Nítida (Espectro Negativo)")]
    public Color invertedColorFilter = new Color(0.35f, 0.70f, 0.95f);

    // Câmera & FOV
    private Camera targetCamera;
    private float baseFOV = 60f;
    private bool hasBaseFOV = false;

    // Volume URP e Componentes de Post-Processing
    private Volume dynamicVolume;
    private VolumeProfile dynamicProfile;
    private LensDistortion lensDistortion;
    private PaniniProjection paniniProjection;
    private ColorAdjustments colorAdjustments;

    // Estado dos Estágios de Ira
    private int highestActiveTier = 0; // 0 = Normal, 1 = FishEye, 2 = Dark, 3 = Invert
    private float fishEyeIntensity = 0f;
    private float targetFishEyeIntensity = 0f;
    private float darkIntensity = 0f;
    private float targetDarkIntensity = 0f;
    private float invertIntensity = 0f;
    private float targetInvertIntensity = 0f;
    private float currentLingerTimer = 0f;
    private bool isEffectActive = false;

    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else if (_instance != this)
        {
            Destroy(this);
            return;
        }

        InitCamera();
        SetupPostProcessingVolume();

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        ResetAllEffectsImmediate();
        if (dynamicProfile != null)
        {
            Destroy(dynamicProfile);
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ResetAllEffectsImmediate();
        InitCamera();
    }

    private void InitCamera()
    {
        targetCamera = GetComponent<Camera>() ?? Camera.main;
        if (targetCamera != null)
        {
            if (!hasBaseFOV)
            {
                baseFOV = targetCamera.fieldOfView > 10f ? targetCamera.fieldOfView : 60f;
                hasBaseFOV = true;
            }

            var urpCam = targetCamera.GetComponent<UniversalAdditionalCameraData>();
            if (urpCam != null)
            {
                urpCam.renderPostProcessing = true;
            }
        }
    }

    private void SetupPostProcessingVolume()
    {
        if (dynamicVolume != null) return;

        GameObject volObj = new GameObject("Merchant_FishEye_Volume");
        volObj.transform.SetParent(transform);
        volObj.layer = 0;

        dynamicVolume = volObj.AddComponent<Volume>();
        dynamicVolume.isGlobal = true;
        dynamicVolume.priority = 999f;
        dynamicVolume.weight = 0f;

        dynamicProfile = ScriptableObject.CreateInstance<VolumeProfile>();
        dynamicProfile.name = "Merchant_FishEyeProfile";
        dynamicVolume.profile = dynamicProfile;

        // 1. Lens Distortion (Olho de Peixe)
        lensDistortion = dynamicProfile.Add<LensDistortion>(true);
        lensDistortion.intensity.overrideState = true;
        lensDistortion.intensity.value = 0f;
        lensDistortion.scale.overrideState = true;
        lensDistortion.scale.value = 1f;

        // 2. Panini Projection (Curvatura esférica)
        paniniProjection = dynamicProfile.Add<PaniniProjection>(true);
        paniniProjection.distance.overrideState = true;
        paniniProjection.distance.value = 0f;

        // 3. Color Adjustments (Escuridão e Inversão Espectral via GPU)
        colorAdjustments = dynamicProfile.Add<ColorAdjustments>(true);
        colorAdjustments.postExposure.overrideState = true;
        colorAdjustments.postExposure.value = 0f;
        colorAdjustments.colorFilter.overrideState = true;
        colorAdjustments.colorFilter.value = Color.white;
        colorAdjustments.hueShift.overrideState = true;
        colorAdjustments.hueShift.value = 0f;
        colorAdjustments.contrast.overrideState = true;
        colorAdjustments.contrast.value = 0f;
        colorAdjustments.saturation.overrideState = true;
        colorAdjustments.saturation.value = 0f;
    }

    private static void EnsureInstanceExists()
    {
        if (_instance == null)
        {
            Camera cam = Camera.main;
            if (cam != null)
            {
                _instance = cam.gameObject.AddComponent<MerchantHallucinationEffect>();
            }
            else
            {
                GameObject obj = new GameObject("MerchantHallucinationManager");
                _instance = obj.AddComponent<MerchantHallucinationEffect>();
            }
        }
    }

    /// <summary>
    /// 4º Golpe: Inicia ou renova o efeito Olho de Peixe.
    /// </summary>
    public static void TriggerHallucination()
    {
        EnsureInstanceExists();
        if (_instance != null)
        {
            _instance.StartFishEyeInternal();
        }
    }

    /// <summary>
    /// 7º Golpe: Inicia ou renova a escuridão do ambiente.
    /// </summary>
    public static void TriggerDarkEnvironment()
    {
        EnsureInstanceExists();
        if (_instance != null)
        {
            _instance.StartDarkEnvironmentInternal();
        }
    }

    /// <summary>
    /// 9º Golpe: Inicia ou renova a inversão total de cores (Visão Negativa Nítida).
    /// </summary>
    public static void TriggerColorInversion()
    {
        EnsureInstanceExists();
        if (_instance != null)
        {
            _instance.StartColorInversionInternal();
        }
    }

    /// <summary>
    /// 10º Golpe: O Mercador ceifa a alma do jogador imediatamente.
    /// </summary>
    public static void TriggerMerchantExecution(GameObject merchantSource)
    {
        EnsureInstanceExists();
        if (_instance != null)
        {
            _instance.ExecutePlayerInternal(merchantSource);
        }
    }

    /// <summary>
    /// Reseta imediatamente todos os efeitos de ira (chamado no Respawn, Morte ou Troca de Cena).
    /// </summary>
    public static void ResetImmediate()
    {
        if (_instance != null)
        {
            _instance.ResetAllEffectsImmediate();
        }
    }

    private void StartFishEyeInternal()
    {
        isEffectActive = true;
        highestActiveTier = Mathf.Max(highestActiveTier, 1);
        currentLingerTimer = lingerDuration;
        RefreshTargetIntensities();
    }

    private void StartDarkEnvironmentInternal()
    {
        isEffectActive = true;
        highestActiveTier = Mathf.Max(highestActiveTier, 2);
        currentLingerTimer = lingerDuration;
        RefreshTargetIntensities();
    }

    private void StartColorInversionInternal()
    {
        isEffectActive = true;
        highestActiveTier = Mathf.Max(highestActiveTier, 3);
        currentLingerTimer = lingerDuration;
        RefreshTargetIntensities();
    }

    private void RefreshTargetIntensities()
    {
        targetFishEyeIntensity = (highestActiveTier >= 1) ? 1.0f : 0.0f;
        targetDarkIntensity = (highestActiveTier >= 2) ? 1.0f : 0.0f;
        targetInvertIntensity = (highestActiveTier >= 3) ? 1.0f : 0.0f;
        if (dynamicVolume != null) dynamicVolume.weight = 1.0f;
    }

    private void ExecutePlayerInternal(GameObject merchantSource)
    {
        if (colorAdjustments != null)
        {
            colorAdjustments.postExposure.value = -6.0f;
        }

        BossController.TriggerCameraShake(0.8f, 0.45f);

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null)
        {
            PlayerM p = Object.FindFirstObjectByType<PlayerM>();
            if (p != null) playerObj = p.gameObject;
        }

        if (playerObj != null)
        {
            PlayerHealth ph = playerObj.GetComponent<PlayerHealth>() ?? playerObj.GetComponentInParent<PlayerHealth>();
            if (ph != null)
            {
                ph.isInvulnerable = false;
                ph.TakeDamage(999999, merchantSource);
            }
        }
    }

    void Update()
    {
        // Se nenhum efeito estiver ativo e as intensidades já estiverem zeradas, não faz nada (0% CPU)
        if (!isEffectActive && fishEyeIntensity <= 0.001f && darkIntensity <= 0.001f && invertIntensity <= 0.001f)
        {
            if (dynamicVolume != null && dynamicVolume.weight > 0f)
            {
                dynamicVolume.weight = 0f;
            }
            return;
        }

        if (targetCamera == null) InitCamera();

        // 1. Controle do Timer de Linger (Persistência)
        if (isEffectActive)
        {
            if (currentLingerTimer > 0f)
            {
                currentLingerTimer -= Time.deltaTime;
                RefreshTargetIntensities();
            }
            else
            {
                // Apenas após 25s de inatividade sem nenhum golpe, começa o Fade Out suave
                targetFishEyeIntensity = 0f;
                targetDarkIntensity = 0f;
                targetInvertIntensity = 0f;

                if (fishEyeIntensity <= 0.005f && darkIntensity <= 0.005f && invertIntensity <= 0.005f)
                {
                    isEffectActive = false;
                    highestActiveTier = 0;
                    ResetAllEffectsImmediate();
                    return;
                }
            }
        }

        // 2. Interpolação Suave das Intensidades
        float speedFish = targetFishEyeIntensity > fishEyeIntensity ? fadeInSpeed : fadeOutSpeed;
        fishEyeIntensity = Mathf.MoveTowards(fishEyeIntensity, targetFishEyeIntensity, Time.deltaTime * speedFish);

        float speedDark = targetDarkIntensity > darkIntensity ? fadeInSpeed : fadeOutSpeed;
        darkIntensity = Mathf.MoveTowards(darkIntensity, targetDarkIntensity, Time.deltaTime * speedDark);

        float speedInvert = targetInvertIntensity > invertIntensity ? fadeInSpeed : fadeOutSpeed;
        invertIntensity = Mathf.MoveTowards(invertIntensity, targetInvertIntensity, Time.deltaTime * speedInvert);

        if (dynamicVolume != null)
        {
            dynamicVolume.weight = Mathf.Max(fishEyeIntensity, Mathf.Max(darkIntensity, invertIntensity));
        }

        // 3. Aplicação do Efeito Olho de Peixe Dinâmico (Hit 4)
        if (fishEyeIntensity > 0.001f)
        {
            float w1 = Mathf.Sin(Time.time * lensOscillationSpeed);
            float w2 = Mathf.Cos(Time.time * (lensOscillationSpeed * 0.65f));
            float w3 = Mathf.Sin(Time.time * (lensOscillationSpeed * 1.35f) + 1.2f);
            float waveFactor = Mathf.Clamp01(((w1 + (w2 * 0.6f) + (w3 * 0.4f)) / 2.0f + 1.0f) * 0.5f);

            float currentDistortion = Mathf.Lerp(minFishEyeDistortion, maxFishEyeDistortion, waveFactor);
            if (lensDistortion != null)
            {
                lensDistortion.intensity.value = currentDistortion * fishEyeIntensity;
            }

            float currentPanini = Mathf.Lerp(minPaniniCurvature, maxPaniniCurvature, waveFactor);
            if (paniniProjection != null)
            {
                paniniProjection.distance.value = currentPanini * fishEyeIntensity;
            }

            float currentFovBonus = Mathf.Lerp(minFovBonus, maxFovBonus, waveFactor);
            if (targetCamera != null && hasBaseFOV)
            {
                targetCamera.fieldOfView = baseFOV + (currentFovBonus * fishEyeIntensity);
            }
        }

        // 4. Aplicação da Escuridão e Inversão Espectral (Hit 7 e Hit 9 via GPU)
        if (colorAdjustments != null)
        {
            colorAdjustments.postExposure.value = maxDarkExposure * darkIntensity;

            Color currentFilter = Color.Lerp(Color.white, darkColorFilter, darkIntensity);
            if (invertIntensity > 0.001f)
            {
                currentFilter = Color.Lerp(currentFilter, invertedColorFilter, invertIntensity);
            }
            colorAdjustments.colorFilter.value = currentFilter;

            colorAdjustments.hueShift.value = Mathf.Lerp(0f, 180f, invertIntensity);
            colorAdjustments.contrast.value = Mathf.Lerp(0f, 40f, invertIntensity);
            colorAdjustments.saturation.value = Mathf.Lerp(0f, 25f, invertIntensity);
        }
    }

    public void ResetAllEffectsImmediate()
    {
        isEffectActive = false;
        highestActiveTier = 0;
        currentLingerTimer = 0f;

        fishEyeIntensity = 0f;
        targetFishEyeIntensity = 0f;
        darkIntensity = 0f;
        targetDarkIntensity = 0f;
        invertIntensity = 0f;
        targetInvertIntensity = 0f;

        if (dynamicVolume != null)
        {
            dynamicVolume.weight = 0f;
        }

        if (lensDistortion != null) lensDistortion.intensity.value = 0f;
        if (paniniProjection != null) paniniProjection.distance.value = 0f;
        if (colorAdjustments != null)
        {
            colorAdjustments.postExposure.value = 0f;
            colorAdjustments.colorFilter.value = Color.white;
            colorAdjustments.hueShift.value = 0f;
            colorAdjustments.contrast.value = 0f;
            colorAdjustments.saturation.value = 0f;
        }

        if (targetCamera != null && hasBaseFOV)
        {
            targetCamera.fieldOfView = baseFOV;
        }
    }

    void OnDisable()
    {
        ResetAllEffectsImmediate();
    }
}
