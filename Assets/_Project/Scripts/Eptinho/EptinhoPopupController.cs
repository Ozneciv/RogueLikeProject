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
    [Range(50f, 400f)] [SerializeField] private float panelHeight = 110f;
    [SerializeField] private float marginX = -40f;
    [SerializeField] private float marginY = -40f;
    
    [Space(5)]
    [SerializeField] private Color panelColor = new Color(0.04f, 0.03f, 0.08f, 0.85f);
    [SerializeField] private Color borderColor = new Color(0.0f, 0.75f, 1.0f, 0.65f);
    [Range(0f, 10f)] [SerializeField] private float borderThickness = 2f;

    [Space(5)]
    [Range(30f, 200f)] [SerializeField] private float portraitSize = 70f;
    [SerializeField] private float portraitMarginLeft = 20f;
    [SerializeField] private Color portraitBgColor = new Color(0.12f, 0.08f, 0.22f, 0.90f);
    [SerializeField] private Color portraitBorderColor = new Color(0.6f, 0.35f, 1.0f, 0.6f);

    [Space(5)]
    [Range(10f, 60f)] [SerializeField] private float textFontSize = 18f;
    [SerializeField] private Color textColor = new Color(0.85f, 0.95f, 1.0f, 1.0f);

    private Coroutine esconderCoroutine;

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
                GameObject bgGO;
                Image bgImg;
                Outline bgOutline;
                if (existingBg == null)
                {
                    bgGO = new GameObject("PortraitBg");
                    bgGO.transform.SetParent(parent, false);
                    bgGO.transform.SetSiblingIndex(imagemDoItem.transform.GetSiblingIndex());

                    bgImg = bgGO.AddComponent<Image>();
                    bgOutline = bgGO.AddComponent<Outline>();
                }
                else
                {
                    bgGO = existingBg.gameObject;
                    bgImg = bgGO.GetComponent<Image>();
                    if (bgImg == null) bgImg = bgGO.AddComponent<Image>();
                    bgOutline = bgGO.GetComponent<Outline>();
                    if (bgOutline == null) bgOutline = bgGO.AddComponent<Outline>();
                }

                bgImg.color = portraitBgColor;
                bgOutline.effectColor = portraitBorderColor;
                bgOutline.effectDistance = new Vector2(borderThickness * 0.75f, borderThickness * 0.75f);

                RectTransform bgRect = bgGO.GetComponent<RectTransform>();
                if (bgRect != null && imgRect != null)
                {
                    bgRect.anchorMin = imgRect.anchorMin;
                    bgRect.anchorMax = imgRect.anchorMax;
                    bgRect.pivot = imgRect.pivot;
                    bgRect.anchoredPosition = imgRect.anchoredPosition;
                    bgRect.sizeDelta = imgRect.sizeDelta + new Vector2(borderThickness * 4f, borderThickness * 4f); 
                }
            }

            // 3. Estiliza e posiciona o texto para ocupar o espaço restante sem estourar
            if (textoDoItem != null)
            {
                textoDoItem.color = textColor;
                textoDoItem.fontSize = textFontSize;
                
                RectTransform textRect = textoDoItem.GetComponent<RectTransform>();
                if (textRect != null)
                {
                    // Estica o texto para ocupar o resto do painel à direita da imagem
                    textRect.anchorMin = new Vector2(0f, 0.5f);
                    textRect.anchorMax = new Vector2(1f, 0.5f);
                    textRect.pivot = new Vector2(0f, 0.5f);
                    textRect.offsetMin = new Vector2(portraitMarginLeft + portraitSize + portraitMarginLeft, -panelHeight/2f + 15f); 
                    textRect.offsetMax = new Vector2(-20f, panelHeight/2f - 15f);  
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

        esconderCoroutine = StartCoroutine(EsconderApos(3.5f));
    }

    IEnumerator EsconderApos(float segundos)
    {
        yield return new WaitForSecondsRealtime(segundos);
        if (popupUI != null) popupUI.SetActive(false);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying && popupUI != null)
        {
            EnsurePopupUIExists();
            
            // Força a ativação do painel para visualização em tempo real no Editor
            popupUI.SetActive(true);
            Transform panel = popupUI.transform.Find("PopupPanel");
            if (panel != null) panel.gameObject.SetActive(true);
        }
    }
#endif
}
