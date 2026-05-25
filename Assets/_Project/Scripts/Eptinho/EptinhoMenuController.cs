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
        // --- Itens catalogados (usa ItemData) ---
        foreach (Transform child in gridContentItens)
            Destroy(child.gameObject);

        if (CatalogoManager.instancia != null)
        {
            foreach (ItemData item in CatalogoManager.instancia.itensCatalogados)
            {
                GameObject novoItemUI = Instantiate(itemPrefab, gridContentItens);

                Image imgComp = novoItemUI.transform.Find("IconeItem")?.GetComponent<Image>();
                if (imgComp != null) imgComp.sprite = item.icon;

                TextMeshProUGUI txtComp = novoItemUI.transform.Find("NomeItem")?.GetComponent<TextMeshProUGUI>();
                if (txtComp != null) txtComp.text = item.itemName;

                // Opcional: exibe a cor do tier no nome
                if (txtComp != null) txtComp.color = item.GetTierColor();
            }
        }

        // --- Bestiário (usa EnemyData) ---
        if (gridContentInimigos != null)
        {
            foreach (Transform child in gridContentInimigos)
                Destroy(child.gameObject);

            if (BestiarioManager.instancia != null)
            {
                foreach (EnemyData inimigo in BestiarioManager.instancia.inimigosEncontrados)
                {
                    GameObject novoItemUI = Instantiate(itemPrefab, gridContentInimigos);

                    Image imgComp = novoItemUI.transform.Find("IconeItem")?.GetComponent<Image>();
                    if (imgComp != null) imgComp.sprite = inimigo.icon;

                    TextMeshProUGUI txtComp = novoItemUI.transform.Find("NomeItem")?.GetComponent<TextMeshProUGUI>();
                    if (txtComp != null) txtComp.text = inimigo.enemyName;
                }
            }
        }
    }
}
