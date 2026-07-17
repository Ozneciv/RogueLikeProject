using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Controla o popup de notificação do Eptinho (item coletado / inimigo encontrado).
/// Aparece automaticamente por 3 segundos quando algo novo é catalogado.
/// </summary>
public class EptinhoPopupController : MonoBehaviour
{
    private static EptinhoPopupController _instancia;
    public static EptinhoPopupController instancia
    {
        get
        {
            if (_instancia == null)
            {
                _instancia = FindFirstObjectByType<EptinhoPopupController>();
                if (_instancia == null)
                {
                    GameObject go = new GameObject("EptinhoPopupController_Auto");
                    _instancia = go.AddComponent<EptinhoPopupController>();
                    DontDestroyOnLoad(go);
                    Debug.Log("[EPTINHO POPUP] Criado automaticamente sob demanda.");
                }
            }
            return _instancia;
        }
        private set { _instancia = value; }
    }

    public GameObject popupUI;
    public Image imagemDoItem;
    public TextMeshProUGUI textoDoItem;

    [Header("Ajustes em Tempo Real")]
    [Range(200f, 1000f)] [SerializeField] private float panelWidth = 620f;
    [Range(50f, 400f)] [SerializeField] private float panelHeight = 250f;
    [SerializeField] private float marginX = -40f;
    [SerializeField] private float marginY = -40f;
    
    [Space(5)]
    [SerializeField] private Color panelColor = new Color(0.23f, 0.28f, 0.70f, 0.5f);
    [SerializeField] private Color borderColor = new Color(0.52f, 0.45f, 0.15f, 0.5f);
    [Range(0f, 40f)] [SerializeField] private float borderThickness = 20f;

    [Space(5)]
    [Range(30f, 400f)] [SerializeField] private float portraitSize = 192f;
    [SerializeField] private float portraitMarginLeft = 0f;
    [SerializeField] private float portraitMarginRight = 70f;
    [SerializeField] private Color portraitBgColor = new Color(0.08f, 0.05f, 0.18f, 0.95f);
    [SerializeField] private Color portraitBorderColor = new Color(0.62f, 0.38f, 0.92f, 1.0f);

    [Space(5)]
    [Range(10f, 80f)] [SerializeField] private float textFontSize = 40f;
    [SerializeField] private Color textColor = new Color(0.72f, 0.76f, 0.78f, 1.0f);
    [SerializeField] private bool previewPermanente = false;

    private Coroutine esconderCoroutine;

    // Unity chama Reset() quando o componente é adicionado pela primeira vez
    // e quando se clica em "Reset" no Inspector — garante os padrões corretos
    private void Reset()
    {
        panelWidth          = 620f;
        panelHeight         = 250f;
        marginX             = -40f;
        marginY             = -40f;
        panelColor          = new Color(0.23f, 0.28f, 0.70f, 0.5f);
        borderColor         = new Color(0.52f, 0.45f, 0.15f, 0.5f);
        borderThickness     = 20f;
        portraitSize        = 192f;
        portraitMarginLeft  = 0f;
        portraitMarginRight = 70f;
        portraitBgColor     = new Color(0.08f, 0.05f, 0.18f, 0.95f);
        portraitBorderColor = new Color(0.62f, 0.38f, 0.92f, 1.0f);
        textFontSize        = 40f;
        textColor           = new Color(0.72f, 0.76f, 0.78f, 1.0f);
        previewPermanente   = false;
    }

    void Awake()
    {
        if (_instancia == null)
        {
            _instancia = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instancia != this)
        {
            Destroy(gameObject);
            return;
        }
        EnsurePopupUIExists();
    }

    private void EnsurePopupUIExists()
    {
        if (popupUI == null)
        {
            // Busca apenas por PopupUI_Auto na cena para evitar colisão/sequestro de outros Canvas
            GameObject existing = GameObject.Find("PopupUI_Auto");
            if (existing != null)
            {
                popupUI = existing;
                Debug.Log("[EPTINHO POPUP] Conectou ao PopupUI_Auto existente.");
            }
            else
            {
                // Carrega do resources
                GameObject prefab = Resources.Load<GameObject>("PopupUI");
                if (prefab != null)
                {
                    popupUI = Instantiate(prefab);
                    popupUI.name = "PopupUI_Auto";
                    DontDestroyOnLoad(popupUI);
                    Debug.Log("[EPTINHO POPUP] Instanciou novo PopupUI a partir de Resources.");
                }
                else
                {
                    Debug.LogError("[EPTINHO POPUP] ERRO: Nao encontrou prefab 'PopupUI' na pasta Resources!");
                }
            }
        }

        // Auto-busca imagemDoItem e textoDoItem estritamente no nosso Canvas
        if (popupUI != null)
        {
            if (imagemDoItem == null)
            {
                // Busca direta pelo caminho do prefab
                Transform faceTransform = popupUI.transform.Find("PopupPanel/EptinhoFace");
                if (faceTransform == null) faceTransform = popupUI.transform.Find("EptinhoFace");
                
                if (faceTransform != null)
                {
                    imagemDoItem = faceTransform.GetComponent<Image>();
                }
                else
                {
                    // Busca segura baseada no nome exato
                    foreach (var img in popupUI.GetComponentsInChildren<Image>(true))
                    {
                        if (img.gameObject.name == "EptinhoFace")
                        {
                            imagemDoItem = img;
                            break;
                        }
                    }
                }
            }

            if (textoDoItem == null)
            {
                // Busca direta pelo caminho do prefab
                Transform textTransform = popupUI.transform.Find("PopupPanel/Text (TMP)");
                if (textTransform == null) textTransform = popupUI.transform.Find("Text (TMP)");

                if (textTransform != null)
                {
                    textoDoItem = textTransform.GetComponent<TextMeshProUGUI>();
                }
                else
                {
                    // Busca segura baseada no nome exato
                    foreach (var tmp in popupUI.GetComponentsInChildren<TextMeshProUGUI>(true))
                    {
                        if (tmp.gameObject.name == "Text (TMP)")
                        {
                            textoDoItem = tmp;
                            break;
                        }
                    }
                }
            }
            // --- Estilização Estilo JARVIS / Visor Holográfico ---
            // 1. Estiliza o painel principal (PopupPanel)
            Transform panel = popupUI.transform.Find("PopupPanel");
            if (panel != null)
            {
                // 1. Estiliza e Redimensiona o painel principal (PopupPanel)
                RectTransform panelRect = panel.GetComponent<RectTransform>();
                if (panelRect != null)
                {
                    // Posiciona o popup de forma fixa no Top Right (canto superior direito) da tela
                    panelRect.anchorMin = new Vector2(1f, 1f);
                    panelRect.anchorMax = new Vector2(1f, 1f);
                    panelRect.pivot = new Vector2(1f, 1f);
                    panelRect.sizeDelta = new Vector2(panelWidth, panelHeight); 
                    panelRect.anchoredPosition = new Vector2(marginX, marginY); 
                }

                Image panelImg = panel.GetComponent<Image>();
                if (panelImg != null)
                {
                    // Fundo escuro semi-transparente (glassmorphism)
                    panelImg.color = panelColor;
                    
                    // Adiciona borda neon ciano (visor do Iron Man / Jarvis)
                    Outline outline = panel.GetComponent<Outline>();
                    if (outline == null) outline = panel.gameObject.AddComponent<Outline>();
                    outline.effectColor = borderColor; 
                    outline.effectDistance = new Vector2(borderThickness, borderThickness);
                }
            }

            // 2. Estiliza e posiciona a imagem do Eptinho e cria uma moldura para o retrato
            if (imagemDoItem != null)
            {
                imagemDoItem.preserveAspect = true; // Impede achatamento/esticamento

                RectTransform imgRect = imagemDoItem.GetComponent<RectTransform>();
                if (imgRect != null)
                {
                    // Posiciona o retrato de forma fixa à esquerda do painel
                    imgRect.anchorMin = new Vector2(0f, 0.5f);
                    imgRect.anchorMax = new Vector2(0f, 0.5f);
                    imgRect.pivot = new Vector2(0f, 0.5f);
                    imgRect.sizeDelta = new Vector2(portraitSize, portraitSize); 
                    imgRect.anchoredPosition = new Vector2(portraitMarginLeft, 0f); 
                }

                Transform parent = imagemDoItem.transform.parent;
                Transform existingBg = parent.Find("PortraitBg");
                if (existingBg != null)
                {
                    DestroyImmediate(existingBg.gameObject);
                }
            }

            // 3. Estiliza e posiciona o texto para ocupar o espaço restante sem estourar
            if (textoDoItem != null)
            {
                textoDoItem.color = textColor;
                textoDoItem.fontSize = textFontSize;
                textoDoItem.alignment = TextAlignmentOptions.MidlineLeft; // Força alinhamento à esquerda para evitar sobrepor a imagem
                textoDoItem.enableWordWrapping = true; // Garante quebra de linha automática
                
                RectTransform textRect = textoDoItem.GetComponent<RectTransform>();
                if (textRect != null)
                {
                    // Estica o texto para ocupar o resto do painel à direita da imagem
                    textRect.anchorMin = new Vector2(0f, 0.5f);
                    textRect.anchorMax = new Vector2(1f, 0.5f);
                    textRect.pivot = new Vector2(0f, 0.5f);
                    textRect.offsetMin = new Vector2(portraitMarginLeft + portraitSize + portraitMarginRight, -panelHeight/2f + 10f); 
                    textRect.offsetMax = new Vector2(-20f, panelHeight/2f - 10f);  
                }
            }

            // 4. Oculta ou posiciona o botão/indicador AbrirEptinho se houver
            if (panel != null)
            {
                Transform abrirBtn = panel.Find("AbrirEptinho");
                if (abrirBtn != null)
                {
                    RectTransform btnRect = abrirBtn.GetComponent<RectTransform>();
                    if (btnRect != null)
                    {
                        // Posiciona discretamente no canto inferior direito do painel
                        btnRect.anchorMin = new Vector2(1f, 0f);
                        btnRect.anchorMax = new Vector2(1f, 0f);
                        btnRect.pivot = new Vector2(1f, 0f);
                        btnRect.anchoredPosition = new Vector2(-15f, 8f);
                        btnRect.sizeDelta = new Vector2(140f, 24f);
                    }
                }
            }
        }
    }

    /// <summary>Popup de aviso genérico ou restrição (usado em bloqueios de combate).</summary>
    public void MostrarPopupAviso(string mensagem)
    {
        Sprite eptinhoFace = Resources.Load<Sprite>("EPTONHO");
        MostrarPopupGenerico(eptinhoFace, mensagem);
    }

    /// <summary>Popup ao catalogar um novo ItemData.</summary>
    public void MostrarPopup(ItemData item)
    {
        if (item == null) return;
        MostrarPopupGenerico(item.icon, "Eptinho analisou: " + item.itemName);
    }

    /// <summary>Popup ao registrar um novo EnemyData no Bestiário.</summary>
    public void MostrarPopupInimigo(EnemyData inimigo)
    {
        if (inimigo == null) return;
        MostrarPopupGenerico(inimigo.icon, "Novo inimigo encontrado: " + inimigo.enemyName);
    }

    void MostrarPopupGenerico(Sprite icone, string mensagem)
    {
        EnsurePopupUIExists();
        if (popupUI == null)
        {
            Debug.LogWarning("[EPTINHO POPUP] Nao pode mostrar popup: popupUI e nulo!");
            return;
        }

        // Ativa o Canvas principal
        popupUI.SetActive(true);

        // Ativa o painel de fundo (PopupPanel) caso ele esteja desativado no prefab
        Transform panel = popupUI.transform.Find("PopupPanel");
        if (panel != null)
        {
            panel.gameObject.SetActive(true);
        }

        // Define o ícone final (EPTONHO como fallback)
        Sprite iconeFinal = icone;
        if (iconeFinal == null)
        {
            iconeFinal = Resources.Load<Sprite>("EPTONHO");
        }

        if (imagemDoItem != null)
        {
            imagemDoItem.gameObject.SetActive(true);
            imagemDoItem.sprite = iconeFinal;
        }
        
        if (textoDoItem != null)
        {
            textoDoItem.gameObject.SetActive(true);
            textoDoItem.text = mensagem;
            textoDoItem.fontSize = textFontSize;
            textoDoItem.color = textColor;

            // Aplica a mesma fonte bonita do inventário
            TMP_FontAsset customFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/Oswald Bold SDF");
            if (customFont != null)
            {
                textoDoItem.font = customFont;
            }
        }

        Debug.Log("[EPTINHO POPUP] MOSTRANDO POPUP: " + mensagem);

        if (esconderCoroutine != null)
            StopCoroutine(esconderCoroutine);

        if (!previewPermanente)
        {
            esconderCoroutine = StartCoroutine(EsconderApos(3.5f));
        }
    }

    IEnumerator EsconderApos(float segundos)
    {
        yield return new WaitForSecondsRealtime(segundos);
        if (popupUI != null) popupUI.SetActive(false);
    }

    // ─── MÉTODOS DE TESTE E PREVIEW ──────────────────────────────────────────

    [ContextMenu("Testar Popup Temporário (3.5s)")]
    public void TestarPopupTemporario()
    {
        MostrarPopupAviso("Eptinho: Teste de Popup temporário (3.5 segundos)!");
    }

    [ContextMenu("Ativar Preview Permanente")]
    public void AtivarPreviewPermanente()
    {
        previewPermanente = true;
        MostrarPopupAviso("Eptinho: Preview Permanente Ativado!");
    }

    [ContextMenu("Desativar Preview Permanente")]
    public void DesativarPreviewPermanente()
    {
        previewPermanente = false;
        if (popupUI != null) popupUI.SetActive(false);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying && popupUI != null)
        {
            EnsurePopupUIExists();
            
            if (previewPermanente)
            {
                // Garante que o painel e os elementos estejam ativos
                popupUI.SetActive(true);
                Transform panel = popupUI.transform.Find("PopupPanel");
                if (panel != null) panel.gameObject.SetActive(true);

                // Cancela o coroutine de esconder para manter na tela
                if (esconderCoroutine != null) StopCoroutine(esconderCoroutine);

                // Mostra mensagem de teste em tempo real
                Sprite eptFace = Resources.Load<Sprite>("EPTONHO");
                MostrarPopupGenerico(eptFace, "Eptinho: Preview permanente ativo!");
            }
            else
            {
                // Se desmarcou a flag em tempo real, oculta o popup
                popupUI.SetActive(false);
            }
        }
    }
#endif
}
