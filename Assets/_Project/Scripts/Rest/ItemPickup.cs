using UnityEngine;
using System.Collections.Generic;

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

    [Header("Forçar Categoria (Definido por Room Spawner)")]
    [HideInInspector]
    public string forceCategory = "";

    private bool isInitialized = false;

    public void InitializeItem(string category)
    {
        forceCategory = category;
        RandomizeItemData();
    }

    void Start()
    {
        spawnTime = Time.time;
        if (lifetime > 0) Destroy(gameObject, lifetime);
        
        if (!isInitialized)
        {
            RandomizeItemData();
        }
    }

    private void RandomizeItemData()
    {
        if (isInitialized) return;
        isInitialized = true;

        if (interactable == null || interactable.itemData == null) return;

        bool isGenericPlaceholder = !string.IsNullOrEmpty(forceCategory) ||
                                     interactable.itemData.itemId == "crystal" || 
                                     interactable.itemData.itemId == "little_frog" || 
                                     interactable.itemData.itemId == "planta" || 
                                     interactable.itemData.itemId == "tinker" || 
                                     string.IsNullOrEmpty(interactable.itemData.enemySource);

        if (!isGenericPlaceholder) return;

        string source = forceCategory;
        if (string.IsNullOrEmpty(source))
        {
            source = interactable.itemData.enemySource;
        }

        if (string.IsNullOrEmpty(source))
        {
            string nameLower = gameObject.name.ToLower();
            if (nameLower.Contains("planta")) source = "Flora";
            else if (nameLower.Contains("frog") || nameLower.Contains("tinker") || nameLower.Contains("carpet")) source = "Fauna";
            else if (nameLower.Contains("crystal") || nameLower.Contains("stone")) source = "Minerals";
        }

        if (string.IsNullOrEmpty(source)) return;

        if (ItemDatabase.Instance != null)
        {
            List<ItemData> matchingItems = new List<ItemData>();
            foreach (var item in ItemDatabase.Instance.allItems)
            {
                if (item != null && item.enemySource == source)
                {
                    matchingItems.Add(item);
                }
            }

            if (matchingItems.Count > 0)
            {
                ItemData chosen = matchingItems[UnityEngine.Random.Range(0, matchingItems.Count)];
                interactable.itemData = chosen;
                interactable.objetoNome = chosen.itemName;
                interactable.descricao = chosen.description;
                interactable.icon = chosen.icon;
                gameObject.name = $"{chosen.itemId}_Pickup";
                
                CustomizeItemVisuals(source);
            }
        }
    }

    private void CustomizeItemVisuals(string source)
    {
        if (source == "Minerals")
        {
            // Minerals: solid items of medium size (represented by a cube)
            // Disable existing MeshRenderers except UI components
            foreach (var r in GetComponentsInChildren<MeshRenderer>())
            {
                if (r.gameObject.name.Contains("Text") || r.gameObject.name.Contains("Canvas") || r.gameObject.name.Contains("glow"))
                    continue;
                r.enabled = false;
            }

            // Create primitive cube child
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "MineralCube";
            cube.transform.SetParent(transform);
            cube.transform.localPosition = Vector3.zero;
            cube.transform.localRotation = Quaternion.identity;
            cube.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f); // Medium size

            // Remove box collider from the created cube to avoid blocking the trigger
            Collider col = cube.GetComponent<Collider>();
            if (col != null) Destroy(col);

            // Change color to gold/orange mineral color
            MeshRenderer cubeRenderer = cube.GetComponent<MeshRenderer>();
            if (cubeRenderer != null)
            {
                cubeRenderer.material.color = new Color(0.9f, 0.6f, 0.2f);
            }
        }
        else if (source == "Fauna")
        {
            // Fauna: glowing items positioned higher. Add a nice cyan glow light.
            Light light = GetComponentInChildren<Light>();
            if (light == null)
            {
                GameObject lightGo = new GameObject("FaunaGlowLight");
                lightGo.transform.SetParent(transform);
                lightGo.transform.localPosition = Vector3.zero;
                light = lightGo.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = new Color(0f, 0.8f, 1f); // Cyan
                light.range = 6f;
                light.intensity = 2f;
            }

            // Extend collider downwards so it can be picked up from the ground
            BoxCollider boxCol = GetComponent<BoxCollider>();
            if (boxCol != null)
            {
                boxCol.center = new Vector3(0f, -1.0f, 0f);
                boxCol.size = new Vector3(boxCol.size.x, boxCol.size.y + 2.0f, boxCol.size.z);
            }
        }
        else if (source == "Flora")
        {
            // Flora: glowing items flat on ground or near walls. Add green glow.
            Light light = GetComponentInChildren<Light>();
            if (light == null)
            {
                GameObject lightGo = new GameObject("FloraGlowLight");
                lightGo.transform.SetParent(transform);
                lightGo.transform.localPosition = Vector3.zero;
                light = lightGo.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = new Color(0.1f, 1f, 0.2f); // Vibrant Green
                light.range = 5f;
                light.intensity = 1.8f;
            }
        }
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
