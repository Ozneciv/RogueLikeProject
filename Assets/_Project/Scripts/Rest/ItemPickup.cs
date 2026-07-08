using UnityEngine;

/// <summary>
/// Script unificado de coleta de itens.
/// Substitui: CharacteristicItemPickup + DetectorDoItem + ItemCollectable.
///
/// RESPONSABILIDADES:
///   • Animação de flutuação e rotação do item no chão
///   • Ativar glowObject e pressFUI quando o player se aproxima
///   • Ao pressionar F: adiciona ao PlayerInventory + registra no CatalogoManager
///   • Respeita pickupDelay e lifetime configuráveis
///
/// SETUP NO PREFAB:
///   Adicione este script + Interactable (com ItemData preenchido) + Collider (Is Trigger).
///   Atribua glowObject e pressFUI no Inspector (filhos do prefab em World Space).
/// </summary>
[RequireComponent(typeof(Interactable))]
public class ItemPickup : MonoBehaviour
{
    [Header("Destaque por Proximidade")]
    [Tooltip("Filho do prefab com efeito de brilho (ativado quando player está perto)")]
    public GameObject glowObject;
    [Tooltip("UI 'Pressione F' em World Space, filho do prefab")]
    public GameObject pressFUI;

    [Header("Coleta")]
    [Tooltip("Delay após spawn antes de poder ser coletado")]
    public float pickupDelay = 0.5f;
    [Tooltip("Tempo em segundos até o item desaparecer. 0 = nunca")]
    public float lifetime = 60f;

    private Interactable interactable;
    private float spawnTime;
    private bool canBePickedUp = false;
    private GameObject playerNearby = null;

    void Awake()
    {
        interactable = GetComponent<Interactable>();
        if (glowObject != null) glowObject.SetActive(false);
        if (pressFUI != null) pressFUI.SetActive(false);
    }

    void Start()
    {
        spawnTime = Time.time;
        if (lifetime > 0) Destroy(gameObject, lifetime);
    }

    void Update()
    {
        // Libera coleta após o delay de spawn
        if (!canBePickedUp && Time.time - spawnTime >= pickupDelay)
        {
            canBePickedUp = true;
            // Se o player já estava dentro da zona quando o delay terminou, ativa o UI agora
            if (playerNearby != null)
            {
                if (glowObject != null) glowObject.SetActive(true);
                if (pressFUI != null) pressFUI.SetActive(true);
            }
        }

        // Coleta por tecla F (somente quando player está na zona)
        if (playerNearby != null && canBePickedUp && Input.GetKeyDown(KeyCode.F))
            TryCollect(playerNearby);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        // Sempre registra o player; UI só aparece se o delay já passou
        playerNearby = other.gameObject;
        if (!canBePickedUp) return;

        if (glowObject != null) glowObject.SetActive(true);
        if (pressFUI != null) pressFUI.SetActive(true);
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playerNearby = null;
        if (glowObject != null) glowObject.SetActive(false);
        if (pressFUI != null) pressFUI.SetActive(false);
    }

    /// <summary>
    /// Executa a coleta:
    ///   • returnsToBase == true  → Bolsa Sintética via SaveManager (persistente)
    ///   • returnsToBase == false → Inventário de run via PlayerInventory (temporário)
    /// Aborta sem destruir se o inventário de run estiver cheio.
    /// </summary>
    private void TryCollect(GameObject player)
    {
        if (interactable == null || interactable.foiCatalogado) return;

        if (interactable.itemData == null)
        {
            Debug.LogWarning($"[ITEM] '{gameObject.name}' não tem ItemData no Interactable! " +
                             "Configure o campo 'Item Data' no Inspector do prefab.");
            return;  // não destrói o item enquanto estiver mal configurado
        }
        else if (interactable.itemData.returnsToBase)
        {
            // Recurso permanente — vai direto para a Bolsa Sintética
            if (SaveManager.instance != null)
                SaveManager.instance.AddResourceToBase(interactable.itemData.itemId, 1);
            else
            {
                Debug.LogWarning("[ITEM] SaveManager não encontrado! Recurso perdido: " + interactable.NomeDisplay);
                return;  // não destrói o item se não puder registrar
            }
        }
        else
        {
            // Item de run — vai para o inventário temporário
            PlayerInventory inventory = player.GetComponentInParent<PlayerInventory>();
            if (inventory == null) inventory = player.GetComponent<PlayerInventory>();

            if (inventory != null)
            {
                bool added = inventory.AddItem(interactable.itemData.itemId, 1);
                if (!added)
                {
                    Debug.Log("[ITEM] Inventário cheio! Não coletou: " + interactable.NomeDisplay);
                    return;
                }
            }
            else
            {
                Debug.LogWarning("[ITEM] PlayerInventory não encontrado no player!");
            }
        }

        // Registra no Catálogo do Eptinho (dispara o popup)
        if (CatalogoManager.instancia != null)
            CatalogoManager.instancia.Catalogar(interactable);

        Debug.Log("[ITEM] Coletado: " + interactable.NomeDisplay);
        Destroy(gameObject);
    }
}
