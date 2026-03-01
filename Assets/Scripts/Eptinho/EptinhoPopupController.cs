using UnityEngine;
using UnityEngine.UI;

public class EptinhoPopupController : MonoBehaviour
{
    public static EptinhoPopupController instancia;

    public GameObject popupUI;
    public Image imagemDoItem;
    public Text textoDoItem;

    void Awake()
    {
        instancia = this;
    }

    public void MostrarPopup(Interactable item)
    {
        popupUI.SetActive(true);
        imagemDoItem.sprite = item.icon;
        textoDoItem.text = "Eptinho analisou: " + item.objetoNome;

        CancelInvoke();
        Invoke(nameof(EsconderPopup), 3f);
    }

    void EsconderPopup()
    {
        popupUI.SetActive(false);
    }
}
