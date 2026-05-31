using UnityEngine;

public class Interactable : MonoBehaviour
{
    [Header("Novo Sistema (preferencial)")]
    public ItemData itemData;

    [Header("Sistema Legado (fallback)")]
    public string objetoNome;
    public Sprite icon;
    [TextArea] public string descricao;

    [HideInInspector] public bool foiCatalogado = false;

    public string NomeDisplay => itemData != null ? itemData.itemName : objetoNome;
    public Sprite IconDisplay => itemData != null ? itemData.icon : icon;
}
