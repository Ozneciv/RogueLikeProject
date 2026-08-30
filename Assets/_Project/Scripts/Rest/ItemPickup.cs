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
        
        // Converte qualquer Mesh de Cubo para Esfera (Círculo 3D) automaticamente
        ConvertCubesToSpheres();

        if (!isInitialized)
        {
            RandomizeItemData();
        }
    }

    private void ConvertCubesToSpheres()
    {
        MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>(true);
        Mesh sphereMesh = null;

        foreach (var mf in meshFilters)
        {
            if (mf != null && mf.sharedMesh != null && mf.sharedMesh.name.ToLower().Contains("cube"))
            {
                if (sphereMesh == null)
                {
                    GameObject tempSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    sphereMesh = tempSphere.GetComponent<MeshFilter>().sharedMesh;
                    Destroy(tempSphere);
                }
                mf.sharedMesh = sphereMesh;
            }
        }

        // Garante que nenhum material rosa/magenta apareça nos orbes
        ApplyCleanMaterialToRenderers();
    }

    private void ApplyCleanMaterialToRenderers()
    {
        ApplyTierColorsToItem();
    }

    [Header("Luz e Iluminação do Drop")]
    [Tooltip("Raio de alcance da luz do item no cenário (em metros)")]
    public float lightRange = 2.0f;
    [Tooltip("Intensidade mínima da luz na oscilação")]
    public float minLightIntensity = 1.2f;
    [Tooltip("Intensidade máxima da luz na oscilação")]
    public float maxLightIntensity = 1.6f;
    [Tooltip("Velocidade da oscilação do brilho da luz")]
    public float lightPulseSpeed = 2.4f;

    private Light cachedPointLight;

    private void ApplyTierColorsToItem()
    {
        Shader defaultShader = Shader.Find("Universal Render Pipeline/Lit");
        if (defaultShader == null) defaultShader = Shader.Find("Standard");

        // Pega a cor exata da raridade (Tier 1 = Branco, Tier 2 = Azul, Tier 3 = Roxo, Tier 4 = Dourado)
        Color tierColor = Color.white;
        if (interactable != null && interactable.itemData != null)
        {
            tierColor = interactable.itemData.GetTierColor();
        }

        // 1. Aplica a cor do Tier no material e na emissão dos MeshRenderers da esfera
        foreach (var r in GetComponentsInChildren<Renderer>(true))
        {
            if (r == null || r.gameObject.name.Contains("Text") || r.gameObject.name.Contains("Canvas") || r.gameObject.name.Contains("glow"))
                continue;

            Material mat = r.material;
            if (mat == null || mat.name.Contains("Default") || mat.name.Contains("Internal"))
            {
                mat = new Material(defaultShader);
                r.material = mat;
            }

            mat.color = tierColor;
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", tierColor * 0.7f);
        }

        // 2. Aplica a cor do Tier na Luz PointLight do item dropado (Ex: CFXR3 Point Light)
        cachedPointLight = GetComponentInChildren<Light>(true);
        if (cachedPointLight == null)
        {
            GameObject lightGo = new GameObject("TierGlowLight");
            lightGo.transform.SetParent(transform, false);
            lightGo.transform.localPosition = Vector3.up * 0.2f;
            cachedPointLight = lightGo.AddComponent<Light>();
            cachedPointLight.type = LightType.Point;
        }

        cachedPointLight.color = tierColor;
        cachedPointLight.intensity = minLightIntensity;
        cachedPointLight.range = lightRange;
        cachedPointLight.enabled = true;

        // 3. Aplica a cor do Tier em Sistemas de Partículas (Rays, Small Stars, CFXR3)
        ParticleSystem[] particleSystems = GetComponentsInChildren<ParticleSystem>(true);
        foreach (var ps in particleSystems)
        {
            if (ps == null) continue;
            var main = ps.main;
            main.startColor = tierColor;
        }

        // 4. Aplica a cor do Tier na imagem de brilho/glow caso exista
        if (glowObject != null)
        {
            SpriteRenderer glowSr = glowObject.GetComponent<SpriteRenderer>();
            if (glowSr != null) glowSr.color = new Color(tierColor.r, tierColor.g, tierColor.b, 0.8f);
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
            // Disable existing MeshRenderers except UI components
            foreach (var r in GetComponentsInChildren<MeshRenderer>())
            {
                if (r.gameObject.name.Contains("Text") || r.gameObject.name.Contains("Canvas") || r.gameObject.name.Contains("glow"))
                    continue;
                r.enabled = false;
            }

            // Create primitive sphere child (esfera/orbe circular em vez de cubo)
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "MineralSphere";
            sphere.transform.SetParent(transform);
            sphere.transform.localPosition = Vector3.zero;
            sphere.transform.localRotation = Quaternion.identity;
            sphere.transform.localScale = new Vector3(0.55f, 0.55f, 0.55f); // Tamanho circular proporcional

            // Remove sphere collider from the created sphere to avoid blocking the trigger
            Collider col = sphere.GetComponent<Collider>();
            if (col != null) Destroy(col);

            // Change color to gold/orange mineral color
            MeshRenderer sphereRenderer = sphere.GetComponent<MeshRenderer>();
            if (sphereRenderer != null)
            {
                Color tierColor = interactable != null && interactable.itemData != null ? interactable.itemData.GetTierColor() : new Color(0.9f, 0.6f, 0.2f);
                sphereRenderer.material.color = tierColor;
                sphereRenderer.material.EnableKeyword("_EMISSION");
                sphereRenderer.material.SetColor("_EmissionColor", tierColor * 0.7f);
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

        // Aplica a cor do Tier em todas as luzes e materiais
        ApplyTierColorsToItem();
    }

    private Vector3 initialScale;
    private bool baseScaleSaved = false;

    private void UpdatePulseEffect()
    {
        if (!baseScaleSaved)
        {
            initialScale = transform.localScale;
            baseScaleSaved = true;
        }

        // Animação sutil de respiração: oscila entre 85% e 100% da escala inicial
        float pulseT = (Mathf.Sin(Time.time * 2.6f) + 1.0f) * 0.5f;
        float scaleRatio = Mathf.Lerp(0.85f, 1.0f, pulseT);
        transform.localScale = initialScale * scaleRatio;
    }

    private void UpdateLightPulse()
    {
        if (cachedPointLight == null)
        {
            cachedPointLight = GetComponentInChildren<Light>(true);
        }

        if (cachedPointLight != null)
        {
            cachedPointLight.range = lightRange;
            float lightT = (Mathf.Sin(Time.time * lightPulseSpeed) + 1.0f) * 0.5f;
            cachedPointLight.intensity = Mathf.Lerp(minLightIntensity, maxLightIntensity, lightT);
        }
    }

    void Update()
    {
        // Animação sutil de respiração e oscilação de luz (0.8 a 1.2)
        UpdatePulseEffect();
        UpdateLightPulse();

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
        {
            // Bloqueia coleta se há inimigos ativos
            bool hasActiveEnemies = false;
            GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
            foreach (var enemy in enemies)
            {
                if (enemy == null || !enemy.activeInHierarchy) continue;
                DummyHealth health = enemy.GetComponentInChildren<DummyHealth>();
                if (health != null)
                {
                    if (health.CurrentHealth > 0) { hasActiveEnemies = true; break; }
                }
                else
                {
                    hasActiveEnemies = true; break;
                }
            }

            if (hasActiveEnemies)
            {
                if (EptinhoPopupController.instancia != null)
                    EptinhoPopupController.instancia.MostrarPopupAviso("Não é hora de distrações — há inimigos por aqui!");
                return;
            }

            TryCollect(playerNearby);
        }
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
