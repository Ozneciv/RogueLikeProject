using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Sistema de Ira do Mercador das Sombras (4 Estágios de Punição):
///   • 4º Hit: Olho de Peixe Dinâmico (Lens Distortion + Panini + Micro-Zoom de 0.12m).
///   • 7º Hit: Escuridão do Ambiente (-1.2 EV + Luzes a 35% + Névoa Sombria).
///   • 9º Hit: Inversão Espectral de Cores (Hue Inversion 180° + Contraste + Filtro Negativo Nítido).
///   • 10º Hit: Morte Imediata / Execução das Sombras.
///   • Persistência: Os efeitos NÃO se esvaem enquanto você ataca. Permanece ativo por 25s de inatividade.
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
    public float fadeOutSpeed = 0.35f;

    [Header("🐟 4º Hit: Olho de Peixe Dinâmico")]
    [Range(-1f, 0f)] public float maxFishEyeDistortion = -0.80f;
    [Range(-1f, 0f)] public float minFishEyeDistortion = -0.15f;
    public float lensOscillationSpeed = 1.6f;
    [Range(0f, 1f)] public float maxPaniniCurvature = 0.60f;
    [Range(0f, 1f)] public float minPaniniCurvature = 0.05f;
    public float maxFovBonus = 1.0f;
    public float minFovBonus = 0.0f;
    public float maxZoomDistance = 0.12f;
    public float minZoomDistance = 0.0f;

    [Header("🌑 7º Hit: Escuridão do Ambiente")]
    public float maxDarkExposure = -1.2f; // Exatamente -1.2 EV
    public Color darkAmbientColor = new Color(0.22f, 0.10f, 0.15f, 1.0f);
    public Color darkFogColor = new Color(0.12f, 0.06f, 0.10f, 1.0f);
    public float darkFogDensity = 0.016f;
    public Color darkColorFilter = new Color(0.72f, 0.48f, 0.55f);

    [Header("🌈 9º Hit: Inversão de Cores Nítida (Espectro Negativo)")]
    public Color invertedColorFilter = new Color(0.35f, 0.70f, 0.95f); // Azul/Ciano espectral (sem tela branca)

    // Câmera & FOV & Zoom
    private Camera targetCamera;
    private float baseFOV = 60f;
    private float currentZoomDistance = 0f;

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

    // Backup da Iluminação Original do Ambiente
    private bool hasSavedEnvironment = false;
    private Color originalAmbientLight;
    private Color originalAmbientSky;
    private Color originalAmbientEquator;
    private Color originalAmbientGround;
    private bool originalFogEnabled;
    private Color originalFogColor;
    private float originalFogDensity;
    private Dictionary<Light, float> originalLightIntensities = new Dictionary<Light, float>();
    private Dictionary<Light, Color> originalLightColors = new Dictionary<Light, Color>();

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
        SaveOriginalEnvironmentLighting();
        SetupPostProcessingVolume();
    }

    void Start()
    {
        InitCamera();
    }

    private void InitCamera()
    {
        if (targetCamera == null)
        {
            targetCamera = GetComponent<Camera>() ?? Camera.main;
            if (targetCamera != null)
            {
                baseFOV = targetCamera.fieldOfView;

                var urpCam = targetCamera.GetComponent<UniversalAdditionalCameraData>();
                if (urpCam != null)
                {
                    urpCam.renderPostProcessing = true;
                }
            }
        }
    }

    private void SaveOriginalEnvironmentLighting()
    {
        if (hasSavedEnvironment) return;
        hasSavedEnvironment = true;

        originalAmbientLight = RenderSettings.ambientLight;
        originalAmbientSky = RenderSettings.ambientSkyColor;
        originalAmbientEquator = RenderSettings.ambientEquatorColor;
        originalAmbientGround = RenderSettings.ambientGroundColor;

        originalFogEnabled = RenderSettings.fog;
        originalFogColor = RenderSettings.fogColor;
        originalFogDensity = RenderSettings.fogDensity > 0 ? RenderSettings.fogDensity : 0.01f;

        Light[] allLights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
        foreach (Light l in allLights)
        {
            if (l != null && !originalLightIntensities.ContainsKey(l))
            {
                originalLightIntensities[l] = l.intensity;
                originalLightColors[l] = l.color;
            }
        }
    }

    private void SetupPostProcessingVolume()
    {
        if (dynamicVolume != null) return;

        GameObject volObj = new GameObject("Merchant_FishEye_Volume");
        volObj.transform.SetParent(transform);
        volObj.layer = 0; // Default Layer

        dynamicVolume = volObj.AddComponent<Volume>();
        dynamicVolume.isGlobal = true;
        dynamicVolume.priority = 999f;

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

        // 3. Color Adjustments (Escuridão, Matiz e Inversão Nítida)
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

    private void StartFishEyeInternal()
    {
        isEffectActive = true;
        highestActiveTier = Mathf.Max(highestActiveTier, 1);
        currentLingerTimer = lingerDuration;
        RefreshTargetIntensities();
        Debug.Log("🐟 [MERCADOR] (4º Hit) ✨ Efeito Olho de Peixe ativo!");
    }

    private void StartDarkEnvironmentInternal()
    {
        isEffectActive = true;
        highestActiveTier = Mathf.Max(highestActiveTier, 2);
        currentLingerTimer = lingerDuration;
        RefreshTargetIntensities();
        Debug.Log("🌑 [MERCADOR] (7º Hit) 💀 Escuridão do Mapa ativa (-1.2 EV)!");
    }

    private void StartColorInversionInternal()
    {
        isEffectActive = true;
        highestActiveTier = Mathf.Max(highestActiveTier, 3);
        currentLingerTimer = lingerDuration;
        RefreshTargetIntensities();
        Debug.Log("👁️‍🗨️ [MERCADOR] (9º Hit) 🌈 INVERSÃO DE CORES ATIVA (Nítida e sem tela branca)!");
    }

    private void RefreshTargetIntensities()
    {
        targetFishEyeIntensity = (highestActiveTier >= 1) ? 1.0f : 0.0f;
        targetDarkIntensity = (highestActiveTier >= 2) ? 1.0f : 0.0f;
        targetInvertIntensity = (highestActiveTier >= 3) ? 1.0f : 0.0f;
    }

    private void ExecutePlayerInternal(GameObject merchantSource)
    {
        Debug.Log("☠️ [MERCADOR] (10º Hit) ⚡ PACTO VIOLADO: Execução instantânea do jogador!");

        if (colorAdjustments != null)
        {
            colorAdjustments.postExposure.value = -6.0f;
        }

        BossController.TriggerCameraShake(1.2f, 0.55f);

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
        if (targetCamera == null) InitCamera();

        if (dynamicVolume == null)
        {
            SetupPostProcessingVolume();
            if (dynamicVolume == null) return;
        }

        // 1. Controle do Timer de Linger (Persistência)
        if (isEffectActive)
        {
            if (currentLingerTimer > 0f)
            {
                currentLingerTimer -= Time.deltaTime;
                // Mantém os alvos dos estágios desbloqueados travados em 1.0f enquanto houver tempo
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

        // Se todos estiverem em zero, reseta iluminação e economiza
        if (fishEyeIntensity <= 0.001f && darkIntensity <= 0.001f && invertIntensity <= 0.001f)
        {
            ResetAllEffects();
            return;
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
            if (targetCamera != null)
            {
                targetCamera.fieldOfView = baseFOV + (currentFovBonus * fishEyeIntensity);
            }

            currentZoomDistance = Mathf.Lerp(minZoomDistance, maxZoomDistance, waveFactor);
        }
        else
        {
            currentZoomDistance = 0f;
        }

        // 4. Aplicação da Escuridão e Inversão Espectral (Hit 7 e Hit 9)
        if (colorAdjustments != null)
        {
            // Exposição: mantida em -1.2 EV (sem lavar a tela)
            colorAdjustments.postExposure.value = maxDarkExposure * darkIntensity;

            // Filtro de Cor: transita para tom sombrio no Hit 7 e para o espectro invertido no Hit 9
            Color currentFilter = Color.Lerp(Color.white, darkColorFilter, darkIntensity);
            if (invertIntensity > 0.001f)
            {
                currentFilter = Color.Lerp(currentFilter, invertedColorFilter, invertIntensity);
            }
            colorAdjustments.colorFilter.value = currentFilter;

            // Inversão de Matiz de 180°: inverte todas as cores primárias/secundárias mantendo nitidez absoluta
            colorAdjustments.hueShift.value = Mathf.Lerp(0f, 180f, invertIntensity);

            // Contraste e Saturação para deixar os detalhes bem definidos e visíveis
            colorAdjustments.contrast.value = Mathf.Lerp(0f, 40f, invertIntensity);
            colorAdjustments.saturation.value = Mathf.Lerp(0f, 25f, invertIntensity);
        }

        ApplyEnvironmentLighting(darkIntensity);
    }

    void LateUpdate()
    {
        if (targetCamera == null) return;

        if (fishEyeIntensity > 0.001f && currentZoomDistance > 0.001f)
        {
            Vector3 zoomOffset = targetCamera.transform.forward * (currentZoomDistance * fishEyeIntensity);
            targetCamera.transform.position += zoomOffset;
        }
    }

    private void ApplyEnvironmentLighting(float darkness)
    {
        if (!hasSavedEnvironment) return;

        RenderSettings.ambientLight = Color.Lerp(originalAmbientLight, darkAmbientColor, darkness);
        RenderSettings.ambientSkyColor = Color.Lerp(originalAmbientSky, darkAmbientColor, darkness);
        RenderSettings.ambientEquatorColor = Color.Lerp(originalAmbientEquator, darkAmbientColor * 0.6f, darkness);
        RenderSettings.ambientGroundColor = Color.Lerp(originalAmbientGround, Color.black, darkness);

        if (darkness > 0.05f)
        {
            RenderSettings.fog = true;
            RenderSettings.fogColor = Color.Lerp(originalFogColor, darkFogColor, darkness);
            RenderSettings.fogDensity = Mathf.Lerp(originalFogDensity, darkFogDensity, darkness);
        }
        else
        {
            RenderSettings.fog = originalFogEnabled;
            RenderSettings.fogColor = originalFogColor;
            RenderSettings.fogDensity = originalFogDensity;
        }

        foreach (var kvp in originalLightIntensities)
        {
            Light l = kvp.Key;
            if (l != null)
            {
                float origInt = kvp.Value;
                Color origCol = originalLightColors.ContainsKey(l) ? originalLightColors[l] : Color.white;

                l.intensity = Mathf.Lerp(origInt, origInt * 0.35f, darkness);
                l.color = Color.Lerp(origCol, new Color(0.55f, 0.25f, 0.35f), darkness * 0.65f);
            }
        }
    }

    private void ResetAllEffects()
    {
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

        if (targetCamera != null)
        {
            targetCamera.fieldOfView = baseFOV;
        }

        ApplyEnvironmentLighting(0f);
    }

    void OnDisable()
    {
        ResetAllEffects();
        if (dynamicVolume != null)
        {
            dynamicVolume.weight = 0f;
        }
    }

    void OnDestroy()
    {
        ResetAllEffects();
        if (dynamicProfile != null)
        {
            Destroy(dynamicProfile);
        }
    }
}
