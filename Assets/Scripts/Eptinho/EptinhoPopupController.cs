using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class EptinhoPopupController : MonoBehaviour
{
    public static EptinhoPopupController instancia;

    public GameObject popupUI;
    public Image imagemDoItem;
    public Text textoDoItem;

    private Coroutine esconderCoroutine;

    void Awake()
    {
        instancia = this;
    }

    public void MostrarPopup(Interactable item)
    {
        MostrarPopupGenerico(item.icon, "Eptinho analisou: " + item.objetoNome);
    }

    public void MostrarPopupInimigo(EnemyIdentity inimigo)
    {
        MostrarPopupGenerico(inimigo.icon, "Novo inimigo encontrado: " + inimigo.nomeInimigo);
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
