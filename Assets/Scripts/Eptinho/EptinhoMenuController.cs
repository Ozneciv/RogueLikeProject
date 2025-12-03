using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EptinhoMenuController : MonoBehaviour
{
    public GameObject menuUI;

    [Header("Configuração da Lista")]
    public Transform gridContent;   // O objeto onde colocamos o Grid Layout Group
    public GameObject itemPrefab;

    void Start()
    {
        menuUI.SetActive(false);
    }


    private void OnMouseDown()
    {
        AbrirMenu();
    }


    public void AbrirMenu()
    {
        menuUI.SetActive(true);
        AtualizarListaVisual();

        // aqui você deve atualizar a UI listando:
        // CatalogoManager.instancia.itensCatalogados
    }

    void Update()
    {
        // Se apertar ESC e o menu estiver aberto (activeSelf), fecha ele
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (menuUI.activeSelf)
            {
                FecharMenu();
            }
        }
    }

    public void FecharMenu()
    {
        menuUI.SetActive(false);
    }

    void AtualizarListaVisual()
    {
        //Destrói os itens antigos para não duplicar a lista
        foreach (Transform child in gridContent)
        {
            Destroy(child.gameObject);
        }

        //Passa por cada item salvo no CatalogoManager
        foreach (Interactable item in CatalogoManager.instancia.itensCatalogados)
        {
            // Cria uma cópia do prefab dentro do gridContent
            GameObject novoItemUI = Instantiate(itemPrefab, gridContent);

            // 3. Preenchimento: Busca os componentes dentro do prefab e troca os dados
        
            Image imgComp = novoItemUI.transform.Find("IconeItem").GetComponent<Image>();
            if (imgComp != null) imgComp.sprite = item.icon;

            TextMeshProUGUI txtComp = novoItemUI.transform.Find("NomeItem").GetComponent<TextMeshProUGUI>();
            if (txtComp != null) txtComp.text = item.objetoNome;
        }
    }
}
