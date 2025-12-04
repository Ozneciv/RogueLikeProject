using UnityEngine;

public class DetectorDoItem : MonoBehaviour
{
    public Interactable item;
    private bool playerPerto = false;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerPerto = true;
            Debug.Log("Pressione 'F' para catalogar o item.");
            // Ativa brilho, highlight, etc
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerPerto = false;
        }
    }

    void Update()
    {
        if (playerPerto && Input.GetKeyDown(KeyCode.F))
        {
            CatalogoManager.instancia.Catalogar(item);
            Debug.Log("Item catalogado: " + item.objetoNome);
        }
    }
}
