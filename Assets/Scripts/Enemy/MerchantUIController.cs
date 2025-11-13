using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MerchantUIController : MonoBehaviour
{
    [Header("Referências da UI")]
    public GameObject interactionPrompt;
    public Button[] pactButtons;
    public Button closeButton;

    // --- A CORREÇÃO ESTÁ AQUI ---
    // A variável precisa ser 'public' para que o GameManager a possa preencher.
    // [HideInInspector] esconde-a do Inspector para não nos confundir.
    [HideInInspector]
    public PlayerHealth playerHealth;

    private bool hasMadePact = false;
    
    void Awake()
    {
        // REMOVEMOS a lógica 'FindObjectOfType' daqui.
        // O GameManager vai preencher a variável 'playerHealth'.
        
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(ClosePanel);
        }
        
        if (interactionPrompt != null) interactionPrompt.SetActive(false);
        gameObject.SetActive(false); 
    }

    void Update()
    {
        if (gameObject.activeSelf && Input.GetKeyDown(KeyCode.Escape))
        {
            ClosePanel();
        }
    }
    
    // (O resto do seu script: ShowPrompt, IsUiOpen, OpenPanel, ClosePanel...
    // ...continuam exatamente iguais, pois já usam a variável 'playerHealth')

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
        // (A lógica do Merchant.cs vai re-mostrar o prompt se necessário)
    }

    public void ApplyPactOfFury()
    {
        if (playerHealth == null) 
        {
            Debug.LogError("ApplyPactOfFury falhou: 'playerHealth' é nulo!");
            return;
        }
        
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
        if (playerHealth == null) 
        {
            Debug.LogError("ApplyPactOfGreed falhou: 'playerHealth' é nulo!");
            return;
        }

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