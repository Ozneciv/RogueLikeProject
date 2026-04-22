using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EptinhoMenuController : MonoBehaviour
{
    public static EptinhoMenuController instancia;

    public GameObject menuUI;
    public GameObject HUDCanvas;


    [Header("Configuração da Lista")]
    public Transform gridContentItens;      // Grid dos itens catalogados
    public Transform gridContentInimigos;   // Grid do bestiário
    public GameObject itemPrefab;

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

    void Start()
    {
        menuUI.SetActive(false);
    }

    void Update()
    {
        // Tecla I abre/fecha o catálogo
        if (Input.GetKeyDown(KeyCode.I))
        {
            if (menuUI.activeSelf)
                FecharMenu();
            else
                AbrirMenu();
        }

        // ESC também fecha
        if (Input.GetKeyDown(KeyCode.Escape) && menuUI.activeSelf)
        {
            FecharMenu();
        }
    }

    public void AbrirMenu()
    {
        menuUI.SetActive(true);
        if (HUDCanvas != null) HUDCanvas.SetActive(false);
        AtualizarListaVisual();
    }

    public void FecharMenu()
    {
        menuUI.SetActive(false);
        if (HUDCanvas != null) HUDCanvas.SetActive(true);
    }

    void AtualizarListaVisual()
    {
        // Limpa grid de itens
        foreach (Transform child in gridContentItens)
            Destroy(child.gameObject);

        // Limpa grid de inimigos
        if (gridContentInimigos != null)
        {
            foreach (Transform child in gridContentInimigos)
                Destroy(child.gameObject);
        }

        // Itens catalogados
        foreach (ItemCatalogado item in CatalogoManager.instancia.itensCatalogados)
        {
            GameObject novoItemUI = Instantiate(itemPrefab, gridContentItens);

            Image imgComp = novoItemUI.transform.Find("IconeItem").GetComponent<Image>();
            if (imgComp != null) imgComp.sprite = item.icon;

            TextMeshProUGUI txtComp = novoItemUI.transform.Find("NomeItem").GetComponent<TextMeshProUGUI>();
            if (txtComp != null) txtComp.text = item.nome;
        }

        // Inimigos catalogados
        if (BestiarioManager.instancia != null && gridContentInimigos != null)
        {
            foreach (InimigoCatalogado inimigo in BestiarioManager.instancia.inimigosEncontrados)
            {
                GameObject novoItemUI = Instantiate(itemPrefab, gridContentInimigos);

                Image imgComp = novoItemUI.transform.Find("IconeItem").GetComponent<Image>();
                if (imgComp != null) imgComp.sprite = inimigo.icon;

                TextMeshProUGUI txtComp = novoItemUI.transform.Find("NomeItem").GetComponent<TextMeshProUGUI>();
                if (txtComp != null) txtComp.text = inimigo.nome;
            }
        }
    }
}
