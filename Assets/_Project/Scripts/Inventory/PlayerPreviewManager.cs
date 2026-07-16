using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Cria um palco 3D escondido e renderiza a skin ativa do jogador
/// em tempo real para uma textura exibida na UI do Inventário.
/// </summary>
public class PlayerPreviewManager : MonoBehaviour
{
    public static PlayerPreviewManager Instance { get; private set; }

    [Header("Configurações do Palco")]
    [Tooltip("Posição onde o palco 3D do preview será criado. Longe do cenário.")]
    public Vector3 stagePosition = new Vector3(1000f, 1000f, 1000f);
    [Tooltip("Velocidade de rotação do personagem.")]
    public float rotationSpeed = 30f;
    [Tooltip("Resolução horizontal do render texture.")]
    public int textureWidth = 512;
    [Tooltip("Resolução vertical do render texture.")]
    public int textureHeight = 512;

    [Header("Ajustes da Câmera de Preview (Tempo Real)")]
    [Tooltip("Offset local (X, Y, Z) da câmera de preview em relação ao player.")]
    public Vector3 previewCamOffset = new Vector3(0f, 1.5f, 5.0f);
    [Tooltip("Campo de visão da câmera (FOV).")]
    public float previewCamFOV = 35f;
    [Tooltip("Inclinação da câmera no eixo X.")]
    public float previewCamRotationX = 3f;
    [Tooltip("Intensidade da iluminação frontal.")]
    public float previewLightIntensity = 4.0f;

    private Camera previewCamera;
    private RenderTexture renderTexture;
    private RawImage targetRawImage;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Configura o RawImage da UI para exibir a textura de renderização da câmera 3D
    /// </summary>
    public void SetupPreview(RawImage rawImage)
    {
        targetRawImage = rawImage;

        if (targetRawImage != null)
        {
            // Pega as dimensões do RectTransform para evitar qualquer distorção de aspecto (anti-esticamento)
            RectTransform rectTrans = targetRawImage.GetComponent<RectTransform>();
            int width = 512;
            int height = 1024;
            if (rectTrans != null)
            {
                width = Mathf.RoundToInt(rectTrans.rect.width * 2f); // Supersampling x2 para nitidez premium
                height = Mathf.RoundToInt(rectTrans.rect.height * 2f);
            }

            // Garante dimensões válidas
            if (width <= 0) width = 512;
            if (height <= 0) height = 1024;

            // Recria a textura se as dimensões mudarem
            if (renderTexture != null && (renderTexture.width != width || renderTexture.height != height))
            {
                renderTexture.Release();
                Destroy(renderTexture);
                renderTexture = null;
            }

            if (renderTexture == null)
            {
                renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
                renderTexture.antiAliasing = 4;
                renderTexture.Create();
            }

            targetRawImage.texture = renderTexture;
            targetRawImage.enabled = true;
        }
    }

    /// <summary>
    /// Ativa o preview 3D, acoplando a câmera ao jogador real da cena
    /// </summary>
    public void Activate()
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            // Tenta localizar ou criar a câmera de preview acoplada ao player
            Transform camT = player.transform.Find("PlayerPreviewCamera");
            if (camT == null)
            {
                GameObject camObj = new GameObject("PlayerPreviewCamera");
                camObj.transform.SetParent(player.transform, false);
                
                // Posiciona com base no previewCamOffset
                camObj.transform.localPosition = previewCamOffset;
                camObj.transform.localRotation = Quaternion.Euler(previewCamRotationX, 180f, 0f);

                previewCamera = camObj.AddComponent<Camera>();
                previewCamera.clearFlags = CameraClearFlags.SolidColor;
                // Fundo cinza escuro/preto para destacar o personagem
                previewCamera.backgroundColor = new Color(0.06f, 0.06f, 0.08f, 1f);
                previewCamera.fieldOfView = previewCamFOV;
                previewCamera.nearClipPlane = 0.1f;
                previewCamera.farClipPlane = previewCamOffset.z + 8f;

                // Adiciona uma luz frontal dedicada anexada ao player para iluminá-lo no preview
                GameObject lightObj = new GameObject("PreviewFrontLight");
                lightObj.transform.SetParent(camObj.transform, false);
                lightObj.transform.localPosition = new Vector3(-0.5f, 1f, -0.5f);
                lightObj.transform.localRotation = Quaternion.Euler(20f, 20f, 0f);

                Light lightComp = lightObj.AddComponent<Light>();
                lightComp.type = LightType.Spot;
                lightComp.range = previewCamOffset.z * 2f;
                lightComp.spotAngle = 60f;
                lightComp.intensity = previewLightIntensity;
                lightComp.color = new Color(0.9f, 0.92f, 1.0f); // Brilho frio
            }
            else
            {
                previewCamera = camT.GetComponent<Camera>();
            }

            if (previewCamera != null)
            {
                previewCamera.targetTexture = renderTexture;
                previewCamera.enabled = true;
            }
        }
    }

    /// <summary>
    /// Desliga a câmera e limpa a câmera temporária do player
    /// </summary>
    public void Deactivate()
    {
        if (previewCamera != null)
        {
            previewCamera.enabled = false;
        }

        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            Transform camT = player.transform.Find("PlayerPreviewCamera");
            if (camT != null)
            {
                Destroy(camT.gameObject);
            }
        }
        previewCamera = null;
    }

    private void Update()
    {
        if (previewCamera != null && previewCamera.enabled)
        {
            // Aplica as configurações do Inspector em tempo real durante o jogo!
            previewCamera.transform.localPosition = previewCamOffset;
            previewCamera.transform.localRotation = Quaternion.Euler(previewCamRotationX, 180f, 0f);
            previewCamera.fieldOfView = previewCamFOV;

            Light lightComp = previewCamera.GetComponentInChildren<Light>();
            if (lightComp != null)
            {
                lightComp.intensity = previewLightIntensity;
                lightComp.range = previewCamOffset.z * 2f;
            }
        }
    }

    private void OnDestroy()
    {
        if (renderTexture != null)
        {
            renderTexture.Release();
            Destroy(renderTexture);
        }
    }
}
