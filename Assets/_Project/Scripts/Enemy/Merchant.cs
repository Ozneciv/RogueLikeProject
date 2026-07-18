using UnityEngine;

public class Merchant : MonoBehaviour
{
    private MerchantUIController uiController;
    private bool canInteract = false;

    void Start()
    {
        // Encontra o controlador da UI. 'true' permite encontrá-lo mesmo que esteja desativado.
        uiController = Object.FindFirstObjectByType<MerchantUIController>(FindObjectsInactive.Include); 
        
        if (uiController == null)
        {
            Debug.LogError("Merchant: Não foi possível encontrar o 'MerchantUIController' na cena!");
        }
    }

    void Update()
    {
        // Se o jogador está na área, a UI não está aberta, e ele aperta F
        if (canInteract && uiController != null && !uiController.IsUiOpen() && Input.GetKeyDown(KeyCode.F))
        {
            uiController.OpenPanel(this.transform);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && uiController != null)
        {
            uiController.ShowPrompt(true); // Mostra "Pressione F"
            canInteract = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && uiController != null)
        {
            uiController.ShowPrompt(false); // Esconde "Pressione F"
            if (uiController.IsUiOpen())
            {
                uiController.ClosePanel(); // Fecha o painel se o jogador sair
            }
            canInteract = false;
        }
    }
}