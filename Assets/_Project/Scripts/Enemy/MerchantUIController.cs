using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MerchantUIController : MonoBehaviour
{
    [Header("Referências da UI Geral")]
    public GameObject interactionPrompt; 
    public GameObject rootPanel; // Fundo escuro
    
    [Header("Menu Principal de Seleção")]
    public GameObject mainMenuPanel; // Contém as 4 opções principais
    public Button btnPactoDeSangue; // Abre as cartas de Tarô
    public Button btnCambioSangue;
    public Button btnRemocao; // Abre a lista de remoção
    public Button btnComprarArtefato;

    [Header("Painel: O Tarô Proibido")]
    public GameObject tarotCardsPanel; 
    public Button[] tarotButtons; 
    public TextMeshProUGUI[] tarotNames;
    public TextMeshProUGUI[] tarotDescriptions;
    public TextMeshProUGUI[] tarotCosts;

    public TextMeshProUGUI txtCambioCost;
    public TextMeshProUGUI txtRemocaoCost;
    public TextMeshProUGUI txtArtefatoCost;

    [Header("Painel: Cirurgia de Remoção")]
    public GameObject removalListPanel;
    public Transform removalListContent; 
    public GameObject removalItemButtonPrefab; 
    
    [Header("Geral")]
    public Button closeButton;

    [HideInInspector]
    public PlayerHealth playerHealth;
    private InfusionManager infusionManager;
    private PlayerEssence playerEssence;
    private PlayerInventory playerInventory;

    private bool hasMadePact = false;
    private Camera pactCamera; 
    private Transform merchantTransform;

    // Definição das 5 Maldições do Tarô Proibido
    private string[] cardNames = { "A Ganância", "O Frenesi", "O Parasita", "O Espectro", "O Sacrifício" };
    private string[] cardDescriptions = {
        "Dropa dobro de loot/essência.\nVocê sofre 50% mais dano.",
        "Velocidade e Ataque +40%.\nArmadura Máxima cai a ZERO.",
        "Vampirismo ao matar inimigos.\nNecrose contínua após 5s sem matar.",
        "Esquiva (Dodge) +50%.\nSeu dano é reduzido em 30%.",
        "Dano Base +150%.\nVocê nunca mais poderá se curar."
    };
    private float[] cardHealthCostPercent = { 0.20f, 0.25f, 0.30f, 0.15f, 0.50f };

    void Awake()
    {
        if (closeButton != null) closeButton.onClick.AddListener(ClosePanel);
        
        if (btnPactoDeSangue != null) btnPactoDeSangue.onClick.AddListener(ShowTarotCards);
        if (btnCambioSangue != null) btnCambioSangue.onClick.AddListener(OnCambioSangueClicked);
        if (btnRemocao != null) btnRemocao.onClick.AddListener(ShowRemovalList);
        if (btnComprarArtefato != null) btnComprarArtefato.onClick.AddListener(OnComprarArtefatoClicked);

        if (interactionPrompt != null) interactionPrompt.SetActive(false);
        if (rootPanel != null) rootPanel.SetActive(false);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (removalListPanel != null) removalListPanel.SetActive(false);
        if (tarotCardsPanel != null) tarotCardsPanel.SetActive(false);
        
        SetupTarotButtons();
        SetupRightSideText();
    }

    public void ConnectPlayer(PlayerHealth player)
    {
        playerHealth = player;
        infusionManager = player.GetComponent<InfusionManager>();
        playerEssence = player.GetComponent<PlayerEssence>();
        playerInventory = player.GetComponent<PlayerInventory>();
    }

    void SetupTarotButtons()
    {
        for (int i = 0; i < tarotButtons.Length; i++)
        {
            if (i >= cardNames.Length) break;
            
            if (tarotNames != null && i < tarotNames.Length && tarotNames[i] != null) 
                tarotNames[i].text = cardNames[i];
            
            if (tarotDescriptions != null && i < tarotDescriptions.Length && tarotDescriptions[i] != null) 
                tarotDescriptions[i].text = cardDescriptions[i];
                
            if (tarotCosts != null && i < tarotCosts.Length && tarotCosts[i] != null) 
                tarotCosts[i].text = $"-{cardHealthCostPercent[i] * 100}% Vida";

            int cardIndex = i;
            if (tarotButtons[i] != null)
            {
                tarotButtons[i].onClick.RemoveAllListeners();
                tarotButtons[i].onClick.AddListener(() => OnTarotCardClicked(cardIndex));
            }
        }
    }

    void SetupRightSideText()
    {
        if (txtCambioCost != null) txtCambioCost.text = "-15% Vida\n+300 Essências";
        if (txtRemocaoCost != null) txtRemocaoCost.text = "Remove Infusão\n-150 Essências";
        if (txtArtefatoCost != null) txtArtefatoCost.text = "Artefato Tier Alto\n-600 Essências";
    }

    void Update()
    {
        if (IsUiOpen() && Input.GetKeyDown(KeyCode.Escape))
        {
            ClosePanel();
        }
    }

    public void ShowPrompt(bool show)
    {
        if (interactionPrompt != null && !hasMadePact)
            interactionPrompt.SetActive(show);
    }

    public bool IsUiOpen()
    {
        return rootPanel != null && rootPanel.activeSelf;
    }

    public void OpenPanel(Transform merchantPos = null)
    {
        if (hasMadePact) return;
        
        merchantTransform = merchantPos;
        SetupPactCamera();

        if (rootPanel != null) rootPanel.SetActive(true);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (tarotCardsPanel != null) tarotCardsPanel.SetActive(false);
        if (removalListPanel != null) removalListPanel.SetActive(false);

        Time.timeScale = 0f;
        ShowPrompt(false);
    }

    public void ShowTarotCards()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (tarotCardsPanel != null) tarotCardsPanel.SetActive(true);
    }

    private void SetupPactCamera()
    {
        if (merchantTransform == null || playerHealth == null) return;
        if (Camera.main == null) return;

        GameObject camObj = new GameObject("PactCamera_Temporary");
        pactCamera = camObj.AddComponent<Camera>();
        pactCamera.CopyFrom(Camera.main); 

        Vector3 eyeOffset = Vector3.up * 1.6f;
        camObj.transform.position = playerHealth.transform.position + eyeOffset;
        camObj.transform.LookAt(merchantTransform.position + eyeOffset);

        pactCamera.depth = Camera.main.depth + 1;
    }

    // ============================================
    // SERVIÇO 1: O Tarô Proibido
    // ============================================
    public void OnTarotCardClicked(int index)
    {
        if (playerHealth == null || hasMadePact) return;

        float percentCost = cardHealthCostPercent[index];
        int healthCost = Mathf.RoundToInt(playerHealth.maxHealth * percentCost);

        if (playerHealth.maxHealth - healthCost > 0)
        {
            ApplyTarotEffect(index, healthCost);
            hasMadePact = true;
            ClosePanel();
        }
        else
        {
            Debug.LogWarning("[Mercador] Sangue insuficiente para este sacrifício!");
        }
    }

    private void ApplyTarotEffect(int index, int maxHealthCost)
    {
        playerHealth.ModifyAttribute("maxhealth", -maxHealthCost, false);
        switch(index)
        {
            case 0:
                playerHealth.hasDoubleLoot = true;
                playerHealth.damageTakenMultiplier += 0.5f;
                break;
            case 1:
                playerHealth.damageMultiplier += 0.4f;
                if (playerHealth.playerMovement != null) playerHealth.playerMovement.hitboxMoveSpeed *= 1.4f;
                if (playerHealth.playerAttack != null) playerHealth.playerAttack.attackAnimationSpeed *= 1.4f;
                playerHealth.ModifyAttribute("maxarmor", -playerHealth.maxArmor, false);
                break;
            case 2:
                playerHealth.hasVampirism = true;
                playerHealth.hasNecrosis = true;
                break;
            case 3:
                PlayerAttributesDefensive def = playerHealth.GetComponent<PlayerAttributesDefensive>();
                if (def != null) def.dodgeChance += 50f;
                playerHealth.damageMultiplier -= 0.3f;
                break;
            case 4:
                playerHealth.damageMultiplier += 1.5f;
                playerHealth.canHeal = false;
                break;
        }
    }

    // ============================================
    // SERVIÇO 2: Câmbio de Sangue
    // ============================================
    public void OnCambioSangueClicked()
    {
        if (playerHealth == null || playerEssence == null || hasMadePact) return;

        int cost = Mathf.RoundToInt(playerHealth.maxHealth * 0.15f);
        if (playerHealth.maxHealth - cost > 0)
        {
            playerHealth.ModifyAttribute("maxhealth", -cost, false);
            playerEssence.AddEssence(300);
            Debug.Log("[Mercador] Câmbio de Sangue efetuado. +300 Essências.");
            hasMadePact = true;
            ClosePanel();
        }
        else
        {
            Debug.LogWarning("Sangue insuficiente!");
        }
    }

    // ============================================
    // SERVIÇO 3: Cirurgia de Remoção
    // ============================================
    public void ShowRemovalList()
    {
        if (infusionManager == null || hasMadePact) return;
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (removalListPanel != null) removalListPanel.SetActive(true);

        // Limpa a lista visual
        foreach (Transform child in removalListContent)
        {
            Destroy(child.gameObject);
        }

        // Popula a lista baseada nos infusedItems
        if (infusionManager.infusedItems.Count == 0)
        {
            // Criar um texto "Nenhum item infundido" pode ser feito, mas pra simplificar o guia, ignoramos
            Debug.Log("Nenhum item infundido para remover.");
            return;
        }

        foreach (ItemData item in infusionManager.infusedItems)
        {
            if (removalItemButtonPrefab != null)
            {
                GameObject btnObj = Instantiate(removalItemButtonPrefab, removalListContent);
                TextMeshProUGUI txt = btnObj.GetComponentInChildren<TextMeshProUGUI>();
                if (txt != null) txt.text = $"{item.itemName} (Tier {item.tier})";

                Button btn = btnObj.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.AddListener(() => OnConfirmRemovalClicked(item));
                }
            }
        }
    }

    public void OnConfirmRemovalClicked(ItemData item)
    {
        if (playerEssence == null || infusionManager == null) return;

        int removeCost = 150;
        if (playerEssence.GetEssence() >= removeCost)
        {
            playerEssence.SpendEssence(removeCost);
            infusionManager.RemoveInfusion(item);
            Debug.Log($"[Mercador] Cirurgia concluída! {item.itemName} removido. Custo: 150.");
            hasMadePact = true;
            ClosePanel();
        }
        else
        {
            Debug.LogWarning("Essência insuficiente para a Cirurgia de Remoção!");
        }
    }

    // ============================================
    // SERVIÇO 4: Artefatos Refinados
    // ============================================
    public void OnComprarArtefatoClicked()
    {
        if (playerEssence == null || playerInventory == null || hasMadePact) return;

        int cost = 600;
        if (playerEssence.GetEssence() >= cost)
        {
            // Pega um item tier alto (ex: T3 ou T4) do banco de dados (supondo que o DB permite pegar random)
            // Como não temos a função pronta de pegar random por tier, vamos pegar qualquer item por enquanto
            if (ItemDatabase.Instance != null && ItemDatabase.Instance.allItems.Count > 0)
            {
                ItemData randomItem = ItemDatabase.Instance.allItems[Random.Range(0, ItemDatabase.Instance.allItems.Count)];
                
                playerEssence.SpendEssence(cost);
                playerInventory.AddItem(randomItem.itemId, 1);
                
                Debug.Log($"[Mercador] Artefato comprado! {randomItem.itemName} adquirido por 600 Essências.");
                hasMadePact = true;
                ClosePanel();
            }
        }
        else
        {
            Debug.LogWarning("Essência insuficiente para comprar o Artefato!");
        }
    }

    public void ClosePanel()
    {
        if (rootPanel != null) rootPanel.SetActive(false);
        if (removalListPanel != null) removalListPanel.SetActive(false);
        
        if (pactCamera != null)
        {
            Destroy(pactCamera.gameObject);
            pactCamera = null;
        }

        Time.timeScale = 1f;
        
        if (!hasMadePact) ShowPrompt(true);
    }
}