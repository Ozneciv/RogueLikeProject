using UnityEngine;

/// <summary>
/// Script de interação física com a Mesa de Trabalho na cena da Base.
/// Detecta proximidade do jogador e abre a UI de Crafting ao pressionar T.
///
/// SETUP NA CENA:
///   1. Adicione este script ao GameObject da mesa de trabalho
///   2. Adicione um BoxCollider com isTrigger = true ao objeto
///   3. Arraste o GameObject do prompt visual "T" para pressTUI
///   4. O CraftingUI é encontrado automaticamente (singleton)
///
/// DEPENDÊNCIAS:
///   - CraftingUI.Instance (tela de crafting — criada automaticamente)
///   - Player deve ter a tag "Player"
/// </summary>
public class CraftingTableInteraction : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Prompt visual 'Pressione T' (world space, filho da crafting table)")]
    public GameObject pressTUI;

    [Header("Fallback")]
    [Tooltip("Painel legado da CraftingTableUI. Se CraftingUI.Instance existir, este é ignorado.")]
    public GameObject craftingTableUI;

    private bool playerPerto = false;

    void Awake()
    {
        if (pressTUI != null)
            pressTUI.SetActive(false);
        if (craftingTableUI != null)
            craftingTableUI.SetActive(false);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerPerto = true;
        if (pressTUI != null)
            pressTUI.SetActive(true);
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerPerto = false;
        if (pressTUI != null)
            pressTUI.SetActive(false);

        // Fecha o crafting ao sair da área
        if (CraftingUI.Instance != null && CraftingUI.Instance.IsOpen())
            CraftingUI.Instance.CloseCrafting();
        else if (craftingTableUI != null)
            craftingTableUI.SetActive(false);
    }

    void Update()
    {
        if (!playerPerto) return;

        if (Input.GetKeyDown(KeyCode.T))
        {
            // Usa o novo CraftingUI se disponível
            if (CraftingUI.Instance != null)
            {
                if (CraftingUI.Instance.IsOpen())
                    CraftingUI.Instance.CloseCrafting();
                else
                    CraftingUI.Instance.OpenCrafting();
            }
            // Fallback para o painel legado
            else if (craftingTableUI != null)
            {
                craftingTableUI.SetActive(!craftingTableUI.activeSelf);
            }
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (CraftingUI.Instance != null && CraftingUI.Instance.IsOpen())
                CraftingUI.Instance.CloseCrafting();
            else if (craftingTableUI != null && craftingTableUI.activeSelf)
                craftingTableUI.SetActive(false);
        }

        // Billboard: pressTUI sempre virado para a câmera
        if (pressTUI != null && pressTUI.activeSelf && Camera.main != null)
        {
            pressTUI.transform.forward = Camera.main.transform.forward;
        }
    }
}
