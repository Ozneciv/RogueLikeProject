using UnityEngine;

public class CraftingTableInteraction : MonoBehaviour
{
    [Header("UI References")]
    public GameObject pressTUI;        // UI_T_Key prompt (world space, filho da crafting table)
    public GameObject craftingTableUI;  // Painel da CraftingTableUI (Screen Space)

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
        if (craftingTableUI != null)
            craftingTableUI.SetActive(false);
    }

    void Update()
    {
        if (!playerPerto) return;

        if (Input.GetKeyDown(KeyCode.T))
        {
            if (craftingTableUI != null)
                craftingTableUI.SetActive(!craftingTableUI.activeSelf);
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (craftingTableUI != null && craftingTableUI.activeSelf)
                craftingTableUI.SetActive(false);
        }

        // Billboard: pressTUI sempre virado para a câmera
        if (pressTUI != null && pressTUI.activeSelf)
        {
            pressTUI.transform.forward = Camera.main.transform.forward;
        }
    }
}
