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
        if (popupUI == null)
        {
            Debug.LogError("[POPUP] popupUI não está configurado no Inspector!");
            return;
        }

        popupUI.SetActive(true);
        if (imagemDoItem != null && icone != null) imagemDoItem.sprite = icone;
        if (textoDoItem != null) textoDoItem.text = mensagem;

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
