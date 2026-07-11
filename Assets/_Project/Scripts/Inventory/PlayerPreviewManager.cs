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

    private GameObject stageObject;
    private Camera previewCamera;
    private Light previewLight;
    private GameObject spawnedPreviewModel;
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

        // Cria a textura de renderização em tempo real se não existir
        if (renderTexture == null)
        {
            renderTexture = new RenderTexture(textureWidth, textureHeight, 24, RenderTextureFormat.ARGB32);
            renderTexture.antiAliasing = 4;
            renderTexture.Create();
        }

        if (targetRawImage != null)
        {
            targetRawImage.texture = renderTexture;
            targetRawImage.enabled = true;
        }

        // Constrói o Palco 3D fisicamente isolado
        if (stageObject == null)
        {
            stageObject = new GameObject("PlayerPreview_Stage");
            stageObject.transform.position = stagePosition;
            DontDestroyOnLoad(stageObject);

            // Criar um pequeno piso para o personagem
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "PreviewFloor";
            floor.transform.SetParent(stageObject.transform);
            floor.transform.localPosition = Vector3.zero;
            floor.transform.localScale = new Vector3(0.3f, 1f, 0.3f);
            
            // Remove o colisor do piso para evitar colisões com o mundo
            Collider col = floor.GetComponent<Collider>();
            if (col != null) Destroy(col);

            // Criar a Câmera de Preview focando o modelo
            GameObject camObj = new GameObject("PreviewCamera");
            camObj.transform.SetParent(stageObject.transform);
            camObj.transform.localPosition = new Vector3(0f, 1.1f, -1.8f);
            camObj.transform.localRotation = Quaternion.Euler(5f, 0f, 0f);

            previewCamera = camObj.AddComponent<Camera>();
            previewCamera.clearFlags = CameraClearFlags.SolidColor;
            previewCamera.backgroundColor = new Color(0.06f, 0.06f, 0.09f, 1f); // Fundo escuro premium
            previewCamera.fieldOfView = 38f;
            previewCamera.targetTexture = renderTexture;
            previewCamera.nearClipPlane = 0.1f;
            previewCamera.farClipPlane = 10f;
            previewCamera.enabled = false;

            // Criar luz Direcional/Spot para o personagem
            GameObject lightObj = new GameObject("PreviewLight");
            lightObj.transform.SetParent(stageObject.transform);
            lightObj.transform.localPosition = new Vector3(-1f, 3f, -2f);
            lightObj.transform.localRotation = Quaternion.Euler(40f, 25f, 0f);

            previewLight = lightObj.AddComponent<Light>();
            previewLight.type = LightType.Directional;
            previewLight.intensity = 1.8f;
            previewLight.color = new Color(0.85f, 0.9f, 1f); // Brilho levemente azulado futurista
            previewLight.enabled = false;
        }
    }

    /// <summary>
    /// Ativa o preview 3D, instanciando e limpando o clone do player
    /// </summary>
    public void Activate()
    {
        if (stageObject == null) return;

        // Liga componentes de render
        if (previewCamera != null) previewCamera.enabled = true;
        if (previewLight != null) previewLight.enabled = true;

        // Limpa o anterior
        if (spawnedPreviewModel != null) Destroy(spawnedPreviewModel);

        // Clona o visual atual do jogador baseado no PlayerSkinManager
        PlayerSkinManager skinManager = FindFirstObjectByType<PlayerSkinManager>();
        if (skinManager != null)
        {
            string activeSkin = skinManager.ActiveSkinID;
            PlayerSkinConfig activeConfig = new PlayerSkinConfig();
            bool found = false;

            foreach (var s in skinManager.skins)
            {
                if (s.skinID == activeSkin)
                {
                    activeConfig = s;
                    found = true;
                    break;
                }
            }

            if (found)
            {
                if (activeConfig.isPrefab && activeConfig.skinPrefab != null)
                {
                    spawnedPreviewModel = Instantiate(activeConfig.skinPrefab, stageObject.transform);
                }
                else if (activeConfig.existingChildObjects != null && activeConfig.existingChildObjects.Count > 0)
                {
                    // Clona o primeiro objeto da lista de filhos visuais
                    GameObject visualSource = activeConfig.existingChildObjects[0];
                    if (visualSource != null)
                    {
                        spawnedPreviewModel = Instantiate(visualSource, stageObject.transform);
                    }
                }
            }
        }

        // Fallback genérico caso não ache o SkinManager
        if (spawnedPreviewModel == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                Animator anim = player.GetComponentInChildren<Animator>();
                if (anim != null && anim.gameObject != player)
                {
                    spawnedPreviewModel = Instantiate(anim.gameObject, stageObject.transform);
                }
            }
        }

        // Limpa o clone para que ele não se comporte como o jogador real (sem scripts, colisores e física)
        if (spawnedPreviewModel != null)
        {
            spawnedPreviewModel.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            spawnedPreviewModel.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

            // Desativa colliders e rigidbodies
            foreach (var col in spawnedPreviewModel.GetComponentsInChildren<Collider>())
                col.enabled = false;
            foreach (var rb in spawnedPreviewModel.GetComponentsInChildren<Rigidbody>())
                Destroy(rb);
            
            // Remove scripts de movimentação, lógica ou IA
            foreach (var comp in spawnedPreviewModel.GetComponentsInChildren<MonoBehaviour>())
            {
                string typeName = comp.GetType().Name.ToLower();
                if (typeName.Contains("player") || typeName.Contains("movement") || typeName.Contains("controller") || typeName.Contains("health"))
                {
                    Destroy(comp);
                }
            }

            // Garante animação de repouso no clone se houver Animator
            Animator previewAnim = spawnedPreviewModel.GetComponentInChildren<Animator>();
            if (previewAnim != null)
            {
                // Força o controller principal para garantir suporte a animações
                if (skinManager != null && skinManager.mainAnimatorController != null)
                {
                    previewAnim.runtimeAnimatorController = skinManager.mainAnimatorController;
                }
                previewAnim.Play("Idle", 0, 0f);
            }
        }
    }

    /// <summary>
    /// Desliga a câmera e limpa o clone
    /// </summary>
    public void Deactivate()
    {
        if (previewCamera != null) previewCamera.enabled = false;
        if (previewLight != null) previewLight.enabled = false;

        if (spawnedPreviewModel != null)
        {
            Destroy(spawnedPreviewModel);
            spawnedPreviewModel = null;
        }
    }

    private void Update()
    {
        // Gira o clone do jogador levemente para exibição em 360°
        if (spawnedPreviewModel != null)
        {
            spawnedPreviewModel.transform.Rotate(Vector3.up, rotationSpeed * Time.unscaledDeltaTime);
        }
    }

    private void OnDestroy()
    {
        if (renderTexture != null)
        {
            renderTexture.Release();
            Destroy(renderTexture);
        }
        if (stageObject != null)
        {
            Destroy(stageObject);
        }
    }
}
