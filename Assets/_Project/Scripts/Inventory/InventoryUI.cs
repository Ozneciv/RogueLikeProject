using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Controlador principal da UI do Inventário.
/// Singleton persistente (DontDestroyOnLoad).
///
/// ARQUITETURA:
///   Esta UI é um espelho passivo dos dados. Ela NÃO cria Canvas, painéis
///   ou backgrounds via código. Todas as referências visuais são expostas
///   via [SerializeField] para que o artista monte o layout no Unity Editor.
///
///   Os únicos GameObjects criados em runtime são os slots de inventário
///   (InventorySlotUI), pois a quantidade é dinâmica.
///
/// LAYOUT ESPERADO NO CANVAS (montado pela equipa de arte):
///   ┌─────────────────────────────────────────────────┐
///   │  INVENTÁRIO                                [X]  │
///   ├────────────────────────────────────────────────-─┤
///   │  INVENTÁRIO DE RUN          (runContainer)      │
///   │  [Slot] [Slot] [Slot] [Slot] [Slot]             │
///   │  [Slot] [Slot] [ . ] [ . ] [ . ]                │
///   ├─────────────────────────────────────────────────┤
///   │  BOLSA SINTÉTICA           (bolsaContainer)     │
///   │  [Slot] [Slot] [Slot]                           │
///   └─────────────────────────────────────────────────┘
///
/// SETUP NO EDITOR:
///   1. Monte o layout acima no Canvas da Unity.
///   2. Arraste cada elemento para o campo correspondente no Inspector.
///   3. Crie um Prefab de slot com o componente InventorySlotUI e arraste
///      para o campo inventorySlotPrefab.
///   4. O script cuida de toda a lógica automaticamente.
///
/// DEPENDÊNCIAS:
///   - PlayerInventory            (itens da run, via GameManager.instance.currentPlayer)
///   - SaveManager.instance       (Bolsa Sintética / baseResources)
///   - ItemDatabase.Instance      (ícones e nomes dos itens)
///   - InventoryTooltip           (tooltip no hover dos slots)
/// </summary>
public class InventoryUI : MonoBehaviour
{
    // Singleton
    public static InventoryUI Instance { get; private set; }

    // ─── REFERÊNCIAS DO EDITOR (arrastar no Inspector) ──────────────────────

    [Header("Painel Principal")]
    [Tooltip("O GameObject raiz do painel de inventário (será ativado/desativado)")]
    [SerializeField] private GameObject panelObject;

    [Header("Containers dos Slots")]
    [Tooltip("Transform pai onde os slots do inventário de RUN serão instanciados")]
    [SerializeField] private Transform runContainer;

    [Tooltip("Transform pai onde os slots da BOLSA SINTÉTICA serão instanciados")]
    [SerializeField] private Transform bolsaContainer;

    [Header("Prefab do Slot")]
    [Tooltip("Prefab contendo o componente InventorySlotUI (será instanciado para cada item)")]
    [SerializeField] private GameObject inventorySlotPrefab;

    [Header("Tooltip")]
    [Tooltip("Referência ao InventoryTooltip na cena (passado aos slots no Initialize)")]
    [SerializeField] private InventoryTooltip tooltipRef;

    [Header("Textos de Header")]
    [Tooltip("Texto do header da seção de Run (ex: 'INVENTÁRIO DE RUN  3/10')")]
    [SerializeField] private TextMeshProUGUI runHeaderText;

    [Tooltip("Texto do header da seção da Bolsa Sintética (ex: 'BOLSA SINTÉTICA  5 itens')")]
    [SerializeField] private TextMeshProUGUI bolsaHeaderText;

    [Header("Botão de Fechar")]
    [Tooltip("Botão X para fechar o inventário")]
    [SerializeField] private Button closeButton;

    [Header("Input")]
    [Tooltip("Tecla para abrir/fechar o inventário")]
    [SerializeField] private KeyCode toggleKey = KeyCode.Tab;

    [Header("Configurações dos Slots")]
    [Tooltip("Tamanho visual de cada slot (passado ao Initialize do InventorySlotUI)")]
    [SerializeField] private float slotSize = 64f;

    [Header("Personagem e Equipamento")]
    [Tooltip("Local onde o item equipado atualmente será exibido")]
    [SerializeField] private Transform equippedItemContainer;

    [Tooltip("Imagem que renderiza o personagem em tempo real")]
    [SerializeField] private UnityEngine.UI.RawImage playerRenderImage;

    // ─── ESTADO INTERNO ─────────────────────────────────────────────────────

    private bool isOpen = false;
    private PlayerInventory playerInventory;
    private List<InventorySlotUI> runSlots = new List<InventorySlotUI>();
    private List<InventorySlotUI> bolsaSlots = new List<InventorySlotUI>();

    // ─── CICLO DE VIDA ──────────────────────────────────────────────────────

    void Awake()
    {
        // Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        // Garante que o painel comece fechado
        if (panelObject != null)
            panelObject.SetActive(false);

        // Vincula o botão de fechar
        if (closeButton != null)
            closeButton.onClick.AddListener(CloseInventory);

        // Tenta conectar ao PlayerInventory do jogador atual
        ConnectToPlayerInventory();
    }

    void OnEnable()
    {
        // Inscreve nos eventos estáticos (sempre disponíveis)
        SaveManager.OnBaseResourcesChanged += OnDataChanged;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        // Remove inscrições estáticas
        SaveManager.OnBaseResourcesChanged -= OnDataChanged;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnDestroy()
    {
        // Remove listener da UnityEvent do PlayerInventory
        if (playerInventory != null)
            playerInventory.onInventoryChanged.RemoveListener(OnDataChanged);

        if (Instance == this)
            Instance = null;
    }

    void Update()
    {
        // Toggle com a tecla configurada (Tab)
        if (Input.GetKeyDown(toggleKey))
        {
            if (isOpen)
                CloseInventory();
            else
                OpenInventory();
        }

        // ESC para fechar se estiver aberto
        if (isOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseInventory();
        }
    }

    // ─── RECONEXÃO ENTRE CENAS ──────────────────────────────────────────────

    /// <summary>
    /// Chamado automaticamente quando uma nova cena carrega.
    /// Reconecta ao PlayerInventory (que é DontDestroyOnLoad).
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ConnectToPlayerInventory();

        // Fecha o inventário ao trocar de cena
        if (isOpen)
        {
            isOpen = false;
            if (panelObject != null) panelObject.SetActive(false);
        }

        Debug.Log("[INVENTORY UI] Reconectado após carregar cena: " + scene.name);
    }

    /// <summary>
    /// Encontra o PlayerInventory no jogador atual via GameManager.
    /// Remove listener antigo e adiciona novo para evitar duplicação.
    /// </summary>
    private void ConnectToPlayerInventory()
    {
        // Remove listener antigo se existir
        if (playerInventory != null)
            playerInventory.onInventoryChanged.RemoveListener(OnDataChanged);

        // Encontra o inventário do Player via GameManager
        if (GameManager.instance != null && GameManager.instance.currentPlayer != null)
        {
            playerInventory = GameManager.instance.currentPlayer.GetComponent<PlayerInventory>();
        }

        // Fallback: busca na cena
        if (playerInventory == null)
        {
            playerInventory = FindObjectOfType<PlayerInventory>();
        }

        if (playerInventory != null)
        {
            // Inscreve no evento da UnityEvent do jogador específico
            playerInventory.onInventoryChanged.AddListener(OnDataChanged);
            Debug.Log("[INVENTORY UI] Conectado ao PlayerInventory.");
        }
        else
        {
            Debug.LogWarning("[INVENTORY UI] PlayerInventory não encontrado!");
        }
    }

    // ─── CALLBACK DE EVENTOS ────────────────────────────────────────────────

    /// <summary>
    /// Callback unificado para qualquer mudança nos dados (run ou bolsa).
    /// Só atualiza a UI se ela estiver aberta.
    /// </summary>
    private void OnDataChanged()
    {
        if (isOpen)
            RefreshUI();
    }

    // ─── API PÚBLICA ────────────────────────────────────────────────────────

    /// <summary>Abre o painel do inventário.</summary>
    public void OpenInventory()
    {
        if (isOpen) return;

        // Reconexão de segurança
        if (playerInventory == null)
            ConnectToPlayerInventory();

        isOpen = true;

        if (panelObject != null)
            panelObject.SetActive(true);

        // Mostra cursor para interagir com a UI
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        RefreshUI();
        Debug.Log("[INVENTORY UI] Inventário aberto");
    }

    /// <summary>Fecha o painel do inventário.</summary>
    public void CloseInventory()
    {
        if (!isOpen) return;

        isOpen = false;

        if (panelObject != null)
            panelObject.SetActive(false);

        // Esconde cursor e trava para gameplay
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        if (tooltipRef != null)
            tooltipRef.Hide();

        // Fecha a tela de Infusão junto, se estiver aberta
        InfusionUI telaDeUpgrades = Object.FindFirstObjectByType<InfusionUI>(FindObjectsInactive.Include);
        if (telaDeUpgrades != null)
            telaDeUpgrades.ClosePanel();

        Debug.Log("[INVENTORY UI] Inventário fechado");
    }

    /// <summary>Retorna se o inventário está aberto.</summary>
    public bool IsOpen() => isOpen;

    // ─── REFRESH GERAL ──────────────────────────────────────────────────────

    /// <summary>
    /// Atualiza toda a UI: limpa ambos os containers e os repopula
    /// com base nos dados atuais do PlayerInventory e do SaveManager.
    /// </summary>
    public void RefreshUI()
    {
        RefreshRunInventory();
        RefreshBolsaSintetica();
    }

    // ─── INVENTÁRIO DE RUN ──────────────────────────────────────────────────

    /// <summary>
    /// Limpa e recria os slots do inventário de run.
    /// Lê os itens de PlayerInventory.GetAllItems() e instancia os slots.
    /// </summary>
    private void RefreshRunInventory()
    {
        if (runContainer == null) return;

        // Limpa slots antigos
        ClearContainer(runContainer, runSlots);

        if (playerInventory == null) return;

        // Lê os dados do inventário de run
        Dictionary<string, int> items = playerInventory.GetAllItems();
        int maxSlots = playerInventory.MaxSlots;

        // Atualiza header (se disponível)
        if (runHeaderText != null)
        {
            runHeaderText.text = $"INVENTÁRIO DE RUN  <color=#9B73E6>{playerInventory.OccupiedSlots}/{maxSlots}</color>";
        }

        // Cria um slot para cada item no inventário
        foreach (var kvp in items)
        {
            // Busca dados visuais no ItemDatabase
            ItemData itemData = null;
            if (ItemDatabase.Instance != null)
                itemData = ItemDatabase.Instance.GetItemData(kvp.Key);

            // Instancia e configura o slot
            InventorySlotUI slot = CreateSlot(runContainer, kvp.Key, kvp.Value, itemData);
            runSlots.Add(slot);
        }

        // Cria slots vazios para preencher até o maxSlots
        int emptySlotsNeeded = maxSlots - items.Count;
        for (int i = 0; i < emptySlotsNeeded; i++)
        {
            InventorySlotUI emptySlot = CreateEmptySlot(runContainer);
            runSlots.Add(emptySlot);
        }
    }

    // ─── BOLSA SINTÉTICA ────────────────────────────────────────────────────

    /// <summary>
    /// Limpa e recria os slots da Bolsa Sintética.
    /// Lê os recursos de SaveManager.instance.GetAllBaseResources().
    /// </summary>
    private void RefreshBolsaSintetica()
    {
        if (bolsaContainer == null) return;

        // Limpa slots antigos
        ClearContainer(bolsaContainer, bolsaSlots);

        if (SaveManager.instance == null) return;

        // Lê os recursos persistentes da Bolsa Sintética
        List<ItemSaveEntry> baseResources = SaveManager.instance.GetAllBaseResources();

        // Atualiza header (se disponível)
        if (bolsaHeaderText != null)
        {
            bolsaHeaderText.text = $"BOLSA SINTÉTICA  <color=#9B73E6>{baseResources.Count} {(baseResources.Count == 1 ? "item" : "itens")}</color>";
        }

        // Cria um slot para cada recurso na bolsa
        foreach (var entry in baseResources)
        {
            // Busca dados visuais no ItemDatabase
            ItemData itemData = null;
            if (ItemDatabase.Instance != null)
                itemData = ItemDatabase.Instance.GetItemData(entry.itemId);

            // Instancia e configura o slot
            InventorySlotUI slot = CreateSlot(bolsaContainer, entry.itemId, entry.quantity, itemData);
            bolsaSlots.Add(slot);
        }
    }

    // ─── HELPERS DE SLOTS ───────────────────────────────────────────────────

    /// <summary>
    /// Instancia um slot, inicializa-o e preenche com os dados do item.
    /// </summary>
    private InventorySlotUI CreateSlot(Transform parent, string itemId, int quantity, ItemData itemData)
    {
        GameObject slotObj;

        // Se existe um prefab, instancia-o; senão, cria um GameObject vazio
        if (inventorySlotPrefab != null)
        {
            slotObj = Instantiate(inventorySlotPrefab, parent);
        }
        else
        {
            slotObj = new GameObject("Slot_" + itemId);
            slotObj.transform.SetParent(parent, false);
        }

        slotObj.name = "Slot_" + itemId;

        // Obtém ou adiciona o componente InventorySlotUI
        InventorySlotUI slot = slotObj.GetComponent<InventorySlotUI>();
        if (slot == null)
            slot = slotObj.AddComponent<InventorySlotUI>();

        // Inicializa o slot (cria os elementos visuais internos)
        slot.Initialize(tooltipRef, slotSize);

        // Preenche com os dados do item
        slot.SetItem(itemId, quantity, itemData);

        return slot;
    }

    /// <summary>
    /// Cria um slot vazio (sem item) para preencher a grade.
    /// </summary>
    private InventorySlotUI CreateEmptySlot(Transform parent)
    {
        GameObject slotObj;

        if (inventorySlotPrefab != null)
        {
            slotObj = Instantiate(inventorySlotPrefab, parent);
        }
        else
        {
            slotObj = new GameObject("Slot_Empty");
            slotObj.transform.SetParent(parent, false);
        }

        slotObj.name = "Slot_Empty";

        InventorySlotUI slot = slotObj.GetComponent<InventorySlotUI>();
        if (slot == null)
            slot = slotObj.AddComponent<InventorySlotUI>();

        slot.Initialize(tooltipRef, slotSize);
        slot.SetEmpty();

        return slot;
    }

    /// <summary>
    /// Limpa todos os slots de um container, destruindo os GameObjects.
    /// </summary>
    private void ClearContainer(Transform container, List<InventorySlotUI> slotList)
    {
        foreach (var slot in slotList)
        {
            if (slot != null)
                Destroy(slot.gameObject);
        }
        slotList.Clear();
    }
}
