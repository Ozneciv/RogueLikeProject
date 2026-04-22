using UnityEngine;

public class DetectorDoItem : MonoBehaviour
{
    public ItemCollectable collectable;
    public GameObject glowObject;
    public GameObject pressFUI;

    private bool playerPerto = false;

    void Awake()
    {
        if (collectable == null)
            collectable = GetComponentInParent<ItemCollectable>();

        if (glowObject != null)
            glowObject.SetActive(false);

        if (pressFUI != null)
            pressFUI.SetActive(false);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playerPerto = true;

        if (glowObject != null)
            glowObject.SetActive(true);

        if (collectable.PodeColetar())
        {
            if (pressFUI != null)
                pressFUI.SetActive(true);
        }
        else
            Debug.Log("Item trancado. Limpe a sala.");
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playerPerto = false;

        if (glowObject != null)
            glowObject.SetActive(false);

        if (pressFUI != null)
            pressFUI.SetActive(false);
    }

    void Update()
    {
        if (playerPerto && Input.GetKeyDown(KeyCode.F))
        {
            if (!collectable.PodeColetar())
            {
                Debug.Log("Ainda n�o pode coletar.");
                return;
            }

            CatalogoManager.instancia.Catalogar(collectable.interactable);

            if (pressFUI != null)
                pressFUI.SetActive(false);

            Destroy(gameObject);

            //CatalogoManager.instancia.Catalogar(item);
            Debug.Log("Item catalogado: " + collectable.interactable.objetoNome);
        }
    }
}
