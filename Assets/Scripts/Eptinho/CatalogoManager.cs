using System.Collections.Generic;
using UnityEngine;

public class CatalogoManager : MonoBehaviour
{
    public static CatalogoManager instancia;

    public List<Interactable> itensCatalogados = new List<Interactable>();

    void Awake()
    {
        instancia = this;
    }

    public void Catalogar(Interactable item)
    {
        if (item.foiCatalogado) return;

        item.foiCatalogado = true;
        itensCatalogados.Add(item);

        EptinhoPopupController.instancia.MostrarPopup(item);
    }
}
