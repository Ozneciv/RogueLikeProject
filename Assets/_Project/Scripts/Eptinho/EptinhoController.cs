using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EptinhoController : MonoBehaviour
{
    public GameObject MenuCanvas;
    public GameObject HUDCanvas;

    private Interactable objetoAtual;
    private static bool s_jaSpawnado = false;

    void Awake()
    {
        // Garante que EnemyDataAutoLoader existe
        if (EnemyDataAutoLoader.instancia == null)
        {
            GameObject loaderGO = new GameObject("EnemyDataAutoLoader");
            loaderGO.AddComponent<EnemyDataAutoLoader>();
            DontDestroyOnLoad(loaderGO);
        }
    }

    void Start()
    {
        if (MenuCanvas != null) MenuCanvas.SetActive(false);
        SpawnEptinhoPhysicalModel();
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Reseta flag ao carregar nova cena para permitir re-spawn na base
        s_jaSpawnado = false;
        SpawnEptinhoPhysicalModel();
    }

    private void SpawnEptinhoPhysicalModel()
    {
        // Evita spawn duplicado
        if (s_jaSpawnado) return;
        if (GameObject.Find("EptOracle") != null) { s_jaSpawnado = true; return; }

        // Carrega o prefab
        GameObject prefab = Resources.Load<GameObject>("Eptin");
        if (prefab == null)
        {
            Debug.LogWarning("[EPTINHO] Prefab 'Eptin' nao encontrado na pasta Resources.");
            return;
        }

        // ── Determina a posição de spawn ─────────────────────────────────────
        Vector3 spawnPos;
        Quaternion spawnRot = Quaternion.Euler(0f, 135f, 0f);

        // 1ª opção: perto da mesa de crafting
        GameObject table = GameObject.Find("crafting table");
        if (table != null)
        {
            spawnPos = table.transform.position + new Vector3(-1.8f, 1.3f, 1.2f);
        }
        // 2ª opção: perto do Sector_Laboratory
        else
        {
            GameObject lab = GameObject.Find("Sector_Laboratory");
            spawnPos = lab != null
                ? lab.transform.position + new Vector3(0f, 1.5f, 2f)
                : new Vector3(0f, 1.5f, 0f); // fallback absoluto
        }

        // ── Instancia e configura ────────────────────────────────────────────
        GameObject eptinho = Instantiate(prefab, spawnPos, spawnRot);
        eptinho.name = "EptOracle";
        s_jaSpawnado = true;

        // Garante que o root tem Rigidbody kinematic (necessário para OnTriggerEnter funcionar nos filhos)
        Rigidbody rb = eptinho.GetComponent<Rigidbody>();
        if (rb == null) rb = eptinho.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity  = false;

        // Adiciona EptinhoOracleInteract no filho 'Trigger Menu'
        bool foundTriggerMenu = false;
        foreach (Transform child in eptinho.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == "Trigger Menu")
            {
                BoxCollider bc = child.GetComponent<BoxCollider>();
                if (bc == null) bc = child.gameObject.AddComponent<BoxCollider>();
                bc.isTrigger = true;
                // Garante tamanho razoável para detecção
                if (bc.size.magnitude < 0.5f) bc.size = new Vector3(2f, 2f, 2f);

                if (child.GetComponent<EptinhoOracleInteract>() == null)
                    child.gameObject.AddComponent<EptinhoOracleInteract>();

                foundTriggerMenu = true;
                Debug.Log("[EPTINHO] EptinhoOracleInteract adicionado ao filho 'Trigger Menu'.");
                break;
            }
        }

        // Fallback: sem filho 'Trigger Menu', adiciona SphereCollider no root
        if (!foundTriggerMenu)
        {
            SphereCollider sc = eptinho.GetComponent<SphereCollider>();
            if (sc == null) sc = eptinho.AddComponent<SphereCollider>();
            sc.isTrigger = true;
            sc.radius = 2.5f;

            if (eptinho.GetComponent<EptinhoOracleInteract>() == null)
                eptinho.AddComponent<EptinhoOracleInteract>();

            Debug.Log("[EPTINHO] 'Trigger Menu' nao encontrado - EptinhoOracleInteract adicionado no root com SphereCollider.");
        }

        Debug.Log($"[EPTINHO] Oraculo spawnado em {spawnPos}.");
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            MenuCanvas.SetActive(!MenuCanvas.activeSelf);
            HUDCanvas.SetActive(!HUDCanvas.activeSelf);
        }
    }

    public void AbrirMenuDoObjeto(Interactable obj)
    {
        objetoAtual = obj;

        if (EptinhoMenuController.instancia != null)
        {
            EptinhoMenuController.instancia.AbrirMenu();
        }
        else
        {
            if (MenuCanvas != null) MenuCanvas.SetActive(true);
            if (HUDCanvas != null) HUDCanvas.SetActive(false);
        }

        Debug.Log("Menu aberto para: " + objetoAtual.objetoNome);

        AtualizarUI(objetoAtual);
    }

    public void FecharMenu()
    {
        if (EptinhoMenuController.instancia != null)
        {
            EptinhoMenuController.instancia.FecharMenu();
        }
        else
        {
            if (MenuCanvas != null) MenuCanvas.SetActive(false);
            if (HUDCanvas != null) HUDCanvas.SetActive(true);
        }
        objetoAtual = null;
    }

    void AtualizarUI(Interactable obj)
    {
        //Inserir elementos do objeto no menu

        Debug.Log("Atualizando UI com dados do objeto: " + obj.objetoNome);
    }
}
