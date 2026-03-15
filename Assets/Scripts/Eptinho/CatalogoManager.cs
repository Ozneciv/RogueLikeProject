using System.Collections.Generic;
using UnityEngine;

public class CatalogoManager : MonoBehaviour
{
    public static CatalogoManager instancia;

    public List<Interactable> itensCatalogados = new();

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

    public void Catalogar(Interactable item)
    {
        if (item.foiCatalogado) return;

        item.foiCatalogado = true;
        itensCatalogados.Add(item);

        EptinhoPopupController.instancia.MostrarPopup(item);
    }
}
