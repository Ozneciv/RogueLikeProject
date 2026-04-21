using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.EventSystems;

/// <summary>
/// Controlador Visual da Interface de Upgrades (Infusão e Reciclagem).
/// Adicionada automação visual Premium sem quebrar o Setup da Inspector.
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
    public TextMeshProUGUI itemStatsDescription;
    public TextMeshProUGUI recycleValueText;
    
    [Header("Botões Interativos")]
    public Button btnInfundir;
    public Button btnReciclar;
    public Button btnFechar;

    private string selectedItemId = "";
    private Coroutine openAnimCoroutine;

    void Start()
    {
        if (btnFechar != null) btnFechar.onClick.AddListener(ClosePanel);
        if (btnInfundir != null) btnInfundir.onClick.AddListener(OnBtnInfundirClicked);
        if (btnReciclar != null) btnReciclar.onClick.AddListener(OnBtnReciclarClicked);

        // Prepara elementos para animações dinâmicas
        SetupPremiumButton(btnInfundir);
        SetupPremiumButton(btnReciclar);
        SetupPremiumButton(btnFechar, 1.2f);
    }

    public void OpenPanel()
    {
        // Reconexão de Segurança: Se a travessia do portal apagou o fio do Player, nós ligamos de novo!
        if (infusionManager == null)
        {
            infusionManager = FindFirstObjectByType<InfusionManager>();
        }

        if (painelUpgrades != null) 
        {
            painelUpgrades.SetActive(true);
            
            Canvas c = painelUpgrades.GetComponent<Canvas>();
            if (c == null) 
            {
                c = painelUpgrades.AddComponent<Canvas>();
                painelUpgrades.AddComponent<GraphicRaycaster>();
            }
            c.overrideSorting = true;
            c.sortingOrder = 999; 

            // Animação Foda de Abertura
            if(openAnimCoroutine != null) StopCoroutine(openAnimCoroutine);
            openAnimCoroutine = StartCoroutine(AnimatePanelOpen());
        }

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        ClearSelection();
    }

    private IEnumerator AnimatePanelOpen()
    {
        CanvasGroup cg = painelUpgrades.GetComponent<CanvasGroup>();
        if (cg == null) cg = painelUpgrades.AddComponent<CanvasGroup>();

        float duration = 0.35f;
        float time = 0f;
        
        RectTransform rt = painelUpgrades.GetComponent<RectTransform>();
        
        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            float t = time / duration;
            // Efeito Elástico de Curva (Out Back)
            float easeT = 1f - Mathf.Pow(1f - t, 3f); 
            
            cg.alpha = Mathf.Lerp(0f, 1f, easeT);
            rt.localScale = Vector3.Lerp(new Vector3(0.8f, 0.8f, 0.8f), Vector3.one, easeT);
            
            yield return null;
        }
    }

    public void ClosePanel()
    {
        if (painelUpgrades != null) painelUpgrades.SetActive(false);
    }

    public void SelectItem(string itemId)
    {
        selectedItemId = itemId;
        
        ItemData data = ItemDatabase.Instance.GetItemData(itemId);
        if (data == null) return;

        if (itemIcon != null) 
        {
            itemIcon.sprite = data.icon;
            // Efeito sutil ao clicar num card
            StartCoroutine(PulseEffect(itemIcon.transform, 1.3f, 0.2f));
        }

        if (itemTitle != null) itemTitle.text = $"<spacing=2>{data.itemName.ToUpper()}</spacing>";
        
        if (itemRarity != null)
        {
            itemRarity.text = $"— {data.GetTierName().ToUpper()} —";
            itemRarity.color = data.GetTierColor();
        }

        if (recycleValueText != null)
            recycleValueText.text = $"<color=#FFD700>+{data.recycleEssenceValue}</color> <size=60%>ESSÊNCIAS</size>";

        if (itemStatsDescription != null)
        {
            if (data.itemAttributes.Count > 0)
            {
                string desc = "<color=#9999BB>CONCEDE PERMANENTEMENTE</color>\n<size=50%>\n</size>";
                foreach(var buff in data.itemAttributes)
                {
                    string signal = buff.value > 0 ? "+" : "";
                    string tipoMultiplier = buff.isMultiplier ? "%" : "";
                    float displayVal = buff.isMultiplier ? (buff.value * 100f) : buff.value;
                    
                    // Formatação rica
                    desc += $"<color=#00FFAA>• {signal}{displayVal}{tipoMultiplier}</color>  <color=#DDDDDD>{FormatterName(buff.attributeType.ToString())}</color>\n";
                }
                itemStatsDescription.text = desc;
            }
            else
            {
                itemStatsDescription.text = "\n<color=#777777><i>Este item é puramente material.\nNão possui energia rúnica extraível.</i></color>";
            }
        }
        
        if (btnInfundir != null) 
        {
            btnInfundir.interactable = true;
            int realCost = infusionManager != null ? infusionManager.GetInflatedCost(data) : data.infusionEssenceCost;

            TextMeshProUGUI btnTxt = btnInfundir.GetComponentInChildren<TextMeshProUGUI>();
            if (btnTxt != null)
            {
                bool isInflated = realCost > data.infusionEssenceCost;
                string inflaTag = isInflated 
                    ? $" <size=50%><color=#FF6666>({data.infusionEssenceCost} base)</color></size>" 
                    : "";
                btnTxt.text = $"<b>INFUNDIR</b>\n<color=#E28CFF><size=75%>-{realCost} Essências</size></color>{inflaTag}";
            }

            UpdateButtonVisuals(btnInfundir);
        }
            
        if (btnReciclar != null) 
        {
            btnReciclar.interactable = true;
            TextMeshProUGUI recTxt = btnReciclar.GetComponentInChildren<TextMeshProUGUI>();
            if (recTxt != null) 
                recTxt.text = $"<b>RECICLAR</b>\n<color=#FFD700><size=75%>+{data.recycleEssenceValue} Essências</size></color>";
            
            UpdateButtonVisuals(btnReciclar);
        }
    }

    private string FormatterName(string attribute)
    {
         // Exemplo de tradutório rápido
         if(attribute.ToLower().Contains("health")) return "Vida Máxima";
         if(attribute.ToLower().Contains("damage")) return "Poder de Dano";
         if(attribute.ToLower().Contains("speed")) return "Velocidade Mágica";
         return attribute;
    }

    private void ClearSelection()
    {
        selectedItemId = "";
        
        if (itemIcon != null) itemIcon.sprite = null;
        if (itemTitle != null) itemTitle.text = "<color=#666688>ANALISADOR</color>";
        if (itemRarity != null) itemRarity.text = "";
        
        if (itemStatsDescription != null) 
            itemStatsDescription.text = "\n\n<color=#8888AA>ESCOLHA UMA RELÍQUIA PARA CANALIZAR SEUS PODERES OU DESTRUÍ-LA.</color>";
        
        if (recycleValueText != null) recycleValueText.text = "";

        if (btnInfundir != null) { btnInfundir.interactable = false; UpdateButtonVisuals(btnInfundir); }
        if (btnReciclar != null) { btnReciclar.interactable = false; UpdateButtonVisuals(btnReciclar); }
    }

    private void OnBtnInfundirClicked()
    {
        if (string.IsNullOrEmpty(selectedItemId)) return;
        bool sucesso = infusionManager.InfuseItem(selectedItemId);
        if (sucesso)
        {
            StartCoroutine(ScreenFlash(new Color(0.6f, 0.2f, 1f, 0.4f))); // Flash Roxo
            ClearSelection(); 
        }
    }

    private void OnBtnReciclarClicked()
    {
        if (string.IsNullOrEmpty(selectedItemId)) return;
        bool sucesso = infusionManager.RecycleItem(selectedItemId);
        if (sucesso)
        {
            StartCoroutine(ScreenFlash(new Color(1f, 0.8f, 0.2f, 0.4f))); // Flash Dourado
            ClearSelection(); 
        }
    }

    // ==========================================
    // SISTEMA DE BOTÕES PREMIUM E ANIMAÇÕES
    // ==========================================

    private void SetupPremiumButton(Button btn, float hoverScale = 1.05f)
    {
        if(btn == null) return;
        
        EventTrigger trigger = btn.gameObject.AddComponent<EventTrigger>();
        
        EventTrigger.Entry entryEnter = new EventTrigger.Entry();
        entryEnter.eventID = EventTriggerType.PointerEnter;
        entryEnter.callback.AddListener((data) => { if(btn.interactable) StartCoroutine(LerpScale(btn.transform, hoverScale)); });
        trigger.triggers.Add(entryEnter);

        EventTrigger.Entry entryExit = new EventTrigger.Entry();
        entryExit.eventID = EventTriggerType.PointerExit;
        entryExit.callback.AddListener((data) => { StartCoroutine(LerpScale(btn.transform, 1.0f)); });
        trigger.triggers.Add(entryExit);
    }

    private void UpdateButtonVisuals(Button btn)
    {
        CanvasGroup cg = btn.GetComponent<CanvasGroup>();
        if(cg == null) cg = btn.gameObject.AddComponent<CanvasGroup>();
        cg.alpha = btn.interactable ? 1f : 0.4f;
    }

    private IEnumerator LerpScale(Transform t, float targetScale)
    {
        Vector3 target = new Vector3(targetScale, targetScale, 1f);
        float speed = 10f;
        while(Vector3.Distance(t.localScale, target) > 0.01f)
        {
            t.localScale = Vector3.Lerp(t.localScale, target, Time.unscaledDeltaTime * speed);
            yield return null;
        }
        t.localScale = target;
    }
    
    private IEnumerator PulseEffect(Transform t, float peakScale, float duration)
    {
        Vector3 orig = t.localScale;
        yield return LerpScale(t, peakScale);
        yield return LerpScale(t, orig.x); // volta pro base
    }

    private IEnumerator ScreenFlash(Color flashColor)
    {
        // Cria um overlay rápido
        GameObject flashObj = new GameObject("FlashOverlay");
        flashObj.transform.SetParent(painelUpgrades.transform, false);
        flashObj.transform.SetAsLastSibling();
        
        Image img = flashObj.AddComponent<Image>();
        img.color = flashColor;
        
        RectTransform rt = img.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMax = Vector2.zero;
        rt.offsetMin = Vector2.zero;

        float elapsed = 0f;
        float dur = 0.4f;
        while(elapsed < dur)
        {
            elapsed += Time.unscaledDeltaTime;
            float alpha = Mathf.Lerp(flashColor.a, 0f, elapsed / dur);
            img.color = new Color(flashColor.r, flashColor.g, flashColor.b, alpha);
            yield return null;
        }
        Destroy(flashObj);
    }
}
