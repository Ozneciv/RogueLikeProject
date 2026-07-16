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
            // Tenta encontrar por diversos nomes possíveis na cena
            GameObject existing = GameObject.Find("PopupUI");
            if (existing == null) existing = GameObject.Find("PopupUI_Auto");
            if (existing == null) existing = GameObject.Find("PopUpCanvas");
            if (existing == null) existing = GameObject.Find("PopupPanel");
            
            if (existing != null)
            {
                popupUI = existing;
                Debug.Log("[EPTINHO POPUP] Conectou ao Canvas existente na cena: " + popupUI.name);
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

        // Auto-busca imagemDoItem e textoDoItem
        if (popupUI != null)
        {
            if (imagemDoItem == null)
            {
                Transform faceTransform = popupUI.transform.Find("PopupPanel/EptinhoFace");
                if (faceTransform == null) faceTransform = popupUI.transform.Find("EptinhoFace");
                
                if (faceTransform != null)
                {
                    imagemDoItem = faceTransform.GetComponent<Image>();
                }
                
                if (imagemDoItem == null)
                {
                    foreach (var img in popupUI.GetComponentsInChildren<Image>(true))
                    {
                        if (img.gameObject.name == "EptinhoFace" || img.gameObject.name.Contains("Face"))
                        {
                            imagemDoItem = img;
                            break;
                        }
                    }
                }
                
                if (imagemDoItem == null) imagemDoItem = popupUI.GetComponentInChildren<Image>(true);
            }

            if (textoDoItem == null)
            {
                foreach (var tmp in popupUI.GetComponentsInChildren<TextMeshProUGUI>(true))
                {
                    string nameLower = tmp.gameObject.name.ToLower();
                    if (nameLower.Contains("text") && !nameLower.Contains("abrir") && tmp.gameObject.name != "Text")
                    {
                        textoDoItem = tmp;
                        break;
                    }
                }
                
                if (textoDoItem == null)
                {
                    foreach (var tmp in popupUI.GetComponentsInChildren<TextMeshProUGUI>(true))
                    {
                        if (tmp.gameObject.name != "Text")
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
                Image panelImg = panel.GetComponent<Image>();
                if (panelImg != null)
                {
                    // Fundo escuro semi-transparente (glassmorphism)
                    panelImg.color = new Color(0.04f, 0.03f, 0.08f, 0.85f);
                    
                    // Adiciona borda neon ciano (visor do Iron Man / Jarvis)
                    Outline outline = panel.GetComponent<Outline>();
                    if (outline == null) outline = panel.gameObject.AddComponent<Outline>();
                    outline.effectColor = new Color(0.0f, 0.75f, 1.0f, 0.65f); // Ciano neon brilhante
                    outline.effectDistance = new Vector2(2f, 2f);
                }
            }

            // 2. Estiliza a imagem do Eptinho e cria uma moldura para o retrato
            if (imagemDoItem != null)
            {
                imagemDoItem.preserveAspect = true; // Impede achatamento/esticamento

                Transform parent = imagemDoItem.transform.parent;
                Transform existingBg = parent.Find("PortraitBg");
                if (existingBg == null)
                {
                    GameObject bgGO = new GameObject("PortraitBg");
                    bgGO.transform.SetParent(parent, false);
                    bgGO.transform.SetSiblingIndex(imagemDoItem.transform.GetSiblingIndex());

                    Image bgImg = bgGO.AddComponent<Image>();
                    bgImg.color = new Color(0.12f, 0.08f, 0.22f, 0.90f); // Fundo escuro para a foto

                    Outline bgOutline = bgGO.AddComponent<Outline>();
                    bgOutline.effectColor = new Color(0.6f, 0.35f, 1.0f, 0.6f); // Borda roxa neon holográfica
                    bgOutline.effectDistance = new Vector2(1.5f, 1.5f);

                    RectTransform bgRect = bgGO.GetComponent<RectTransform>();
                    RectTransform imgRect = imagemDoItem.GetComponent<RectTransform>();

                    bgRect.anchorMin = imgRect.anchorMin;
                    bgRect.anchorMax = imgRect.anchorMax;
                    bgRect.pivot = imgRect.pivot;
                    bgRect.anchoredPosition = imgRect.anchoredPosition;
                    bgRect.sizeDelta = imgRect.sizeDelta + new Vector2(10f, 10f); // Moldura ligeiramente maior
                }
            }

            // 3. Estiliza a cor do texto para ciano claro holográfico
            if (textoDoItem != null)
            {
                textoDoItem.color = new Color(0.85f, 0.95f, 1.0f, 1.0f);
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
}
