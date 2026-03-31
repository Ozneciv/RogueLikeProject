using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class EptinhoPopupController : MonoBehaviour
{
    public static EptinhoPopupController instancia;

    public GameObject popupUI;
    public Image imagemDoItem;
    public TextMeshProUGUI textoDoItem;

    private Coroutine esconderCoroutine;

    void Awake()
    {
        if (instancia == null)
        {
            instancia = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    public void MostrarPopup(ItemCatalogado item)
    {
        MostrarPopupGenerico(item.icon, "Eptinho analisou: " + item.nome);
    }

    public void MostrarPopupInimigo(InimigoCatalogado inimigo)
    {
        MostrarPopupGenerico(inimigo.icon, "Novo inimigo encontrado: " + inimigo.nome);
    }

    void MostrarPopupGenerico(Sprite icone, string mensagem)
    {
        if (popupUI == null)
        {
            Debug.LogError("[POPUP] popupUI não está configurado no Inspector!");
            return;
        }

        popupUI.SetActive(true);
        if (imagemDoItem != null && icone != null) imagemDoItem.sprite = icone;
        if (textoDoItem != null) textoDoItem.text = mensagem;

        Debug.Log("[POPUP] Mostrando: " + mensagem);

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
