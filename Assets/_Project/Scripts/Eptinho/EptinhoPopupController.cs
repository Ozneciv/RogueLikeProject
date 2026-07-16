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
            // Tenta achar na cena
            GameObject existing = GameObject.Find("PopupUI");
            if (existing != null)
            {
                popupUI = existing;
            }
            else
            {
                // Instancia da pasta Resources
                GameObject prefab = Resources.Load<GameObject>("PopupUI");
                if (prefab != null)
                {
                    popupUI = Instantiate(prefab);
                    popupUI.name = "PopupUI_Auto";
                    DontDestroyOnLoad(popupUI);
                }
                else
                {
                    Debug.LogError("[POPUP] Não foi possível encontrar ou carregar o prefab 'PopupUI' na pasta Resources!");
                }
            }
        }

        // Auto-detecta imagemDoItem e textoDoItem se forem nulos
        if (popupUI != null)
        {
            if (imagemDoItem == null)
            {
                imagemDoItem = popupUI.transform.Find("PopupPanel/EPTONHO")?.GetComponent<Image>();
                if (imagemDoItem == null) imagemDoItem = popupUI.GetComponentInChildren<Image>();
            }
            if (textoDoItem == null)
            {
                // Tenta achar o primeiro TextMeshProUGUI que não seja o de abrir/fechar do botão
                foreach (var tmp in popupUI.GetComponentsInChildren<TextMeshProUGUI>(true))
                {
                    if (tmp.gameObject.name != "AbrirEptinho" && tmp.gameObject.name != "Text")
                    {
                        textoDoItem = tmp;
                        break;
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
        MostrarPopupGenerico(item.icon, $"Eptinho analisou: {item.itemName}");
    }

    /// <summary>Popup ao registrar um novo EnemyData no Bestiário.</summary>
    public void MostrarPopupInimigo(EnemyData inimigo)
    {
        MostrarPopupGenerico(inimigo.icon, $"Novo inimigo encontrado: {inimigo.enemyName}");
    }

    void MostrarPopupGenerico(Sprite icone, string mensagem)
    {
        EnsurePopupUIExists();
        if (popupUI == null) return;

        popupUI.SetActive(true);

        // Se icone for nulo, tenta carregar a face do Eptinho como padrão
        Sprite iconeFinal = icone;
        if (iconeFinal == null)
        {
            iconeFinal = Resources.Load<Sprite>("EPTONHO");
        }

        if (imagemDoItem != null && iconeFinal != null)
        {
            imagemDoItem.sprite = iconeFinal;
        }
        if (textoDoItem != null)
        {
            textoDoItem.text = mensagem;
        }

        Debug.Log($"[POPUP] Mostrando: {mensagem}");

        if (esconderCoroutine != null)
            StopCoroutine(esconderCoroutine);

        esconderCoroutine = StartCoroutine(EsconderApos(3f));
    }

    IEnumerator EsconderApos(float segundos)
    {
        yield return new WaitForSecondsRealtime(segundos);
        if (popupUI != null) popupUI.SetActive(false);
    }
}
