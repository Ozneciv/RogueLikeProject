using UnityEngine;
using TMPro;
using UnityEngine.UI; // Necessário para os botões

public class Merchant : MonoBehaviour
{
    [Header("Referências da UI")]
    public GameObject merchantPanel;
    public GameObject interactionPrompt;
    public Button[] pactButtons;
    private PlayerHealth playerHealth;

    [Header("Configurações")]
    public float maxInteractionDistance = 5f;

    private bool canInteract = false;
    private bool isUiOpen = false;
    private bool hasMadePact = false;

    void Start()
    {
        if (merchantPanel != null) merchantPanel.SetActive(false);
        if (interactionPrompt != null) interactionPrompt.SetActive(false);
    }

    void Update()
    {
        if (playerHealth == null) return;
        if (hasMadePact) return;

        if (isUiOpen)
        {
            if (Vector3.Distance(transform.position, playerHealth.transform.position) > maxInteractionDistance)
            {
                ClosePanel(); 
            }
        }
        
        if (canInteract && !isUiOpen && Input.GetKeyDown(KeyCode.F))
        {
            OpenPanel();
        }
        
        if (isUiOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            ClosePanel();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !hasMadePact)
        {
            playerHealth = other.GetComponent<PlayerHealth>();
            if (playerHealth != null && !isUiOpen)
            {
                interactionPrompt.SetActive(true);
                canInteract = true;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (interactionPrompt != null) interactionPrompt.SetActive(false);
            canInteract = false;
            if (isUiOpen) ClosePanel();
        }
    }

    private void OpenPanel()
    {
        isUiOpen = true;
        merchantPanel.SetActive(true);
        Time.timeScale = 0f;
        if (interactionPrompt != null) interactionPrompt.SetActive(false);
    }

    public void ClosePanel() // Função pública para o botão de fechar
    {
        isUiOpen = false;
        merchantPanel.SetActive(false);
        Time.timeScale = 1f;

        if (!hasMadePact && playerHealth != null)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, playerHealth.transform.position);
            if (distanceToPlayer <= maxInteractionDistance)
            {
                if (interactionPrompt != null) interactionPrompt.SetActive(true);
                canInteract = true;
            }
        }
    }

    private void DeactivateAfterPact() // Função interna para desativar tudo
    {
        isUiOpen = false;
        merchantPanel.SetActive(false);
        Time.timeScale = 1f;
        hasMadePact = true;

        if (interactionPrompt != null) interactionPrompt.SetActive(false);
        canInteract = false;

        // Desativa todos os botões de pacto
        foreach (Button button in pactButtons)
        {
            button.interactable = false;
        }
    }

    // --- PACTO 1 ---
    public void ApplyPactOfFury()
    {
        if (playerHealth == null) return;

        int healthCost = Mathf.RoundToInt(playerHealth.maxHealth * 0.5f);
        if (playerHealth.currentHealth > healthCost)
        {
            playerHealth.TakeCursedDamage(healthCost);
            playerHealth.damageMultiplier += 0.5f;
            playerHealth.damageTakenMultiplier += 0.5f;
            Debug.Log("PACTO DA FÚRIA ACEITO!");
            DeactivateAfterPact();
        }
        else
        {
            Debug.Log("Você não tem vida suficiente para este pacto.");
        }
    }

    // --- PACTO 2 (NOVO) ---
    public void ApplyPactOfGreed()
    {
        if (playerHealth == null) return;

        // Custo: 25% da vida MÁXIMA (como no seu GDD)
        int healthCost = Mathf.RoundToInt(playerHealth.maxHealth * 0.25f);
        
        // Checa se o jogador pode pagar
        if (playerHealth.currentHealth > healthCost)
        {
            playerHealth.TakeCursedDamage(healthCost);
            
            // (Aqui você adicionaria a lógica para +Recursos e Inimigos mais fortes)
            Debug.Log("PACTO DA GANÂNCIA ACEITO!");
            
            DeactivateAfterPact();
        }
        else
        {
            Debug.Log("Você não tem vida suficiente para este pacto.");
        }
    }

    // Você pode adicionar ApplyPactOfImmortality(), etc., aqui...
}