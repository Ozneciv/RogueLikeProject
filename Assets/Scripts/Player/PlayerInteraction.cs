using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    public GameObject popupUI; // Pop-up do Eptinho
    public EptinhoController eptinhoController;

    private Interactable objetoAtual;

    void Start()
    {
        popupUI.SetActive(false);
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log("ENTROU NO TRIGGER COM: " + other.name);
        Interactable interactable = other.GetComponent<Interactable>();

        if (interactable != null)
        {
            objetoAtual = interactable;
            popupUI.SetActive(true);
            Debug.Log("Tentou ativar popup. popupUI ativo? " + popupUI.activeSelf);

        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<Interactable>())
        {
            popupUI.SetActive(false);
            objetoAtual = null;
        }
    }

    // Chamada pelo botão do pop up (no UI Button)
    public void AbrirMenu()
    {
        if (objetoAtual != null)
        {
            eptinhoController.AbrirMenuDoObjeto(objetoAtual);
            popupUI.SetActive(false);
        }
    }
}
