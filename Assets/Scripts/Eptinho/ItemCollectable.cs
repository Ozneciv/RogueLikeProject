using UnityEngine;

public class ItemCollectable : MonoBehaviour
{
    private bool canCollect = false;
    public Interactable interactable;

    void Awake()
    {
        if (interactable == null)
            interactable = GetComponent<Interactable>();
    }

    public void EnableCollection()
    {
        canCollect = true;
    }

    public void DisableCollection()
    {
        canCollect = false;
    }

    public bool PodeColetar()
    {
        return canCollect && !interactable.foiCatalogado;
    }
}
