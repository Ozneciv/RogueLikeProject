using GLTFast.Addons;
using UnityEngine;
using UnityEngine.UI;

public class PageController : MonoBehaviour
{

    public Image[] tabImages;
    public GameObject[] pages;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ActivateTab(0); //Inicia na aba 0 - Adicionar lógica de abrir com determinada aba depois
    }

    public void ActivateTab (int tabNo)
    {

        for (int i = 0; i< pages.Length; i++)
        {
            pages[i].SetActive(false);
            tabImages[i].color = Color.grey;

        }
        pages[tabNo].SetActive(true);
        tabImages[tabNo].color = Color.white;
    }
}
