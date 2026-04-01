using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct ItemCatalogado
{
    public string nome;
    public Sprite icon;
    public string descricao;
}

public class CatalogoManager : MonoBehaviour
{
    public static CatalogoManager instancia;

    public List<ItemCatalogado> itensCatalogados = new();
    private HashSet<string> nomesCatalogados = new();

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
        if (nomesCatalogados.Contains(item.objetoNome)) return;

        item.foiCatalogado = true;

        ItemCatalogado dados = new ItemCatalogado
        {
            nome = item.objetoNome,
            icon = item.icon,
            descricao = item.descricao
        };
        itensCatalogados.Add(dados);
        nomesCatalogados.Add(item.objetoNome);

        EptinhoPopupController.instancia.MostrarPopup(dados);
    }
}
