using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controlador Visual da Interface de Upgrades (Infusão e Reciclagem).
/// Você desenhará a UI no Editor do Unity e arrastará os botões para estas "garagens" (variáveis).
/// </summary>
public class InfusionUI : MonoBehaviour
{
    [Header("Conexão com o Motor")]
    [Tooltip("Arraste o Player (que tem o script InfusionManager) aqui!")]
    public InfusionManager infusionManager;
    
    [Header("Painel Principal")]
    [Tooltip("O GameObject inteiro da janela de Upgrades")]
    public GameObject painelUpgrades; 
    
    [Header("Área de Informação do Item Selecionado")]
    public Image itemIcon;
    public TextMeshProUGUI itemTitle;
    public TextMeshProUGUI itemRarity;
    public TextMeshProUGUI itemStatsDescription; // Onde os Buffs serão escritos (Ex: +20% Dano)
    public TextMeshProUGUI recycleValueText;     // Onde escreveremos (Ex: +50 Essências)
    
    [Header("Botões Interativos")]
    public Button btnInfundir;
    public Button btnReciclar;
    public Button btnFechar;

    // A memória mecânica de qual item o jogador "clicou" por último
    private string selectedItemId = "";

    void Start()
    {
        // Garante que o painel escute os botões
        if (btnFechar != null) btnFechar.onClick.AddListener(ClosePanel);
        if (btnInfundir != null) btnInfundir.onClick.AddListener(OnBtnInfundirClicked);
        if (btnReciclar != null) btnReciclar.onClick.AddListener(OnBtnReciclarClicked);
    }

    /// <summary>
    /// Abre o Painel e garante que ele flutue por cima da tela de inventário, além de destravar o mouse.
    /// </summary>
    public void OpenPanel()
    {
        if (painelUpgrades != null) 
        {
            painelUpgrades.SetActive(true);
            
            // O Inventário roda num canvas que gerou order 100 via código, ele engole tudo e ignora SetAsLastSibling. 
            // Solução Absoluta: Criar um Canvas na tela roxa e jogar Order pra 999.
            Canvas c = painelUpgrades.GetComponent<Canvas>();
            if (c == null) 
            {
                c = painelUpgrades.AddComponent<Canvas>();
                painelUpgrades.AddComponent<GraphicRaycaster>();
            }
            c.overrideSorting = true;
            c.sortingOrder = 999; // SEMPRE na frente de todas as UIs
        }

        // Destrava o mouse
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        ClearSelection();
    }

    public void ClosePanel()
    {
        if (painelUpgrades != null) painelUpgrades.SetActive(false);
    }

    /// <summary>
    /// Chama esta função passando a "ID" do item (ex: spider_fang) para exibir os dados no centro.
    /// (Mais pra frente podemos ligar isso nos quadrados esquerdos da interface)
    /// </summary>
    public void SelectItem(string itemId)
    {
        selectedItemId = itemId;
        
        // Puxa as infos do item no banco de dados!
        ItemData data = ItemDatabase.Instance.GetItemData(itemId);
        if (data == null) return;

        // Atualiza fotos e cores
        if (itemIcon != null) itemIcon.sprite = data.icon;
        if (itemTitle != null) itemTitle.text = data.itemName;
        if (itemRarity != null)
        {
            itemRarity.text = data.GetTierName();
            itemRarity.color = data.GetTierColor();
        }

        if (recycleValueText != null)
            recycleValueText.text = $"+{data.recycleEssenceValue} Essência";

        // Monta o texto dos Buffs matemáticos que nós criamos!
        if (itemStatsDescription != null)
        {
            if (data.itemAttributes.Count > 0)
            {
                string desc = "CONCEDE (Permanente):\n";
                foreach(var buff in data.itemAttributes)
                {
                    string signal = buff.value > 0 ? "+" : "";
                    string tipoMultiplier = buff.isMultiplier ? "x" : "";
                    
                    // Vai aparecer ex: "+1,2x BaseDamageMultiplier"
                    desc += $"\n<color=green>{signal}{buff.value}{tipoMultiplier}</color> {buff.attributeType}";
                }
                itemStatsDescription.text = desc;
            }
            else
            {
                itemStatsDescription.text = "Este item tem valor apenas comercial.\nNão possui atributos mágicos de infusão.";
            }
        }
        
        // Destrava os botões para o jogador poder clicar neles
        if (btnInfundir != null) 
        {
            btnInfundir.interactable = true;
            
            // Procura sozinho o texto que está dentro do Botão e injeta o custo nele!
            TextMeshProUGUI btnTxt = btnInfundir.GetComponentInChildren<TextMeshProUGUI>();
            if (btnTxt != null) 
                btnTxt.text = $"INFUNDIR \n<color=#E28CFF><size=75%>-{data.infusionEssenceCost} Essências</size></color>";
        }
            
        if (btnReciclar != null) 
        {
            btnReciclar.interactable = true;

            // Procura sozinho o texto do Reciclar e injeta o lucro!
            TextMeshProUGUI recTxt = btnReciclar.GetComponentInChildren<TextMeshProUGUI>();
            if (recTxt != null) 
                recTxt.text = $"RECICLAR \n<color=#E28CFF><size=75%>+{data.recycleEssenceValue} Essências</size></color>";
        }
    }

    /// <summary>
    /// Reseta a tela central quando recicla/infunde ou abre a tela.
    /// </summary>
    private void ClearSelection()
    {
        selectedItemId = "";
        
        if (itemIcon != null) itemIcon.sprite = null;
        if (itemTitle != null) itemTitle.text = "Selecione um Item";
        if (itemRarity != null) itemRarity.text = "";
        
        // MENSAGEM PADRÃO DO PAINEL VAZIO!
        // É exatamente aqui que você pode alterar o texto que aparece quando você 
        // recicla/infunde um item ou quando abre a tela pela primeira vez:
        if (itemStatsDescription != null) 
            itemStatsDescription.text = "<b><size=120%>INFUNDIR E RECICLAR</size></b>\n\nClique em um item do inventário para começar.";
        
        if (recycleValueText != null) recycleValueText.text = "";

        // Trava os botões por segurança
        if (btnInfundir != null) btnInfundir.interactable = false;
        if (btnReciclar != null) btnReciclar.interactable = false;
    }

    // ==========================================
    // REAÇÃO DOS BOTÕES
    // ==========================================

    private void OnBtnInfundirClicked()
    {
        // Impossivel infundir o vento
        if (string.IsNullOrEmpty(selectedItemId)) return;

        // Grita pro motor rodar a matemática lá atrás
        bool sucesso = infusionManager.InfuseItem(selectedItemId);
        if (sucesso)
        {
            ClearSelection(); 
            // DICA: Tocar Partícula de luz brilhante na tela aqui?
        }
    }

    private void OnBtnReciclarClicked()
    {
        if (string.IsNullOrEmpty(selectedItemId)) return;

        // Grita pro motor dar dinheiro e jogar o item no lixo
        bool sucesso = infusionManager.RecycleItem(selectedItemId);
        if (sucesso)
        {
            ClearSelection(); 
            // DICA: Tocar som de vidro quebrando aqui?
        }
    }
}
