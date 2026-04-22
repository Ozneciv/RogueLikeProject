using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MerchantUIController : MonoBehaviour
{
    [Header("Referências da UI")]
    public GameObject interactionPrompt; 
    public Button[] pactButtons;
    public Button closeButton;

    [HideInInspector]
    public PlayerHealth playerHealth;

    private bool hasMadePact = false;
    
    void Awake()
    {
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(ClosePanel);
        }
        
        if (interactionPrompt != null) interactionPrompt.SetActive(false);
        gameObject.SetActive(false); 
    }

    // --- A FUNÇÃO QUE ESTAVA FALTANDO ---
    // O GameManager chama esta função para entregar o jogador à UI
    public void ConnectPlayer(PlayerHealth player)
    {
        playerHealth = player;
        if (playerHealth == null)
        {
            Debug.LogError("MerchantUIController: Recebi um jogador nulo do GameManager!");
        }
        else
        {
            Debug.Log("MerchantUIController: Jogador conectado com sucesso!");
        }
    }
    // ------------------------------------

    void Update()
    {
        if (gameObject.activeSelf && Input.GetKeyDown(KeyCode.Escape))
        {
            ClosePanel();
        }
    }

    public void ShowPrompt(bool show)
    {
        if (interactionPrompt != null && !hasMadePact)
        {
            interactionPrompt.SetActive(show);
        }
    }

    public bool IsUiOpen()
    {
        return gameObject.activeSelf;
    }

    public void OpenPanel()
    {
        if (hasMadePact) return;
        gameObject.SetActive(true);
        Time.timeScale = 0f;
        ShowPrompt(false);
    }

    public void ClosePanel()
    {
        gameObject.SetActive(false);
        Time.timeScale = 1f;
        ShowPrompt(true);
    }

    public void ApplyPactOfFury()
    {
        if (playerHealth == null) return;
        int healthCost = Mathf.RoundToInt(playerHealth.maxHealth * 0.3f);
        if (playerHealth.currentHealth > healthCost)
        {
            playerHealth.TakeCursedDamage(healthCost);
            playerHealth.damageMultiplier += 0.5f;
            playerHealth.damageTakenMultiplier += 0.5f;
            DeactivateAfterPact();
        }
    }

    public void ApplyPactOfGreed()
    {
        if (playerHealth == null) return;
        int healthCost = Mathf.RoundToInt(playerHealth.maxHealth * 0.25f);
        if (playerHealth.currentHealth > healthCost)
        {
            playerHealth.TakeCursedDamage(healthCost);
            // (Lógica da ganância aqui)
            DeactivateAfterPact();
        }
    }

    private void DeactivateAfterPact()
    {
        hasMadePact = true; 
        foreach (Button button in pactButtons)
        {
            button.interactable = false;
        }
        ClosePanel();
    }
}