using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [Header("Referências Globais")]
    public GameObject loadingScreenCanvas;
    public Slider loadingBar;
    
    [HideInInspector]
    public GameObject currentPlayer;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (loadingScreenCanvas != null) loadingScreenCanvas.SetActive(false);
    }

    // --- NOVA LÓGICA DE CENA ---
    // Inscreve-se no evento para saber quando uma cena carregou
    void OnEnable() { SceneManager.sceneLoaded += OnSceneLoaded; }
    void OnDisable() { SceneManager.sceneLoaded -= OnSceneLoaded; }

    // Chamado automaticamente toda vez que uma cena termina de carregar
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Se chegamos na Base...
        if (scene.name == "BaseLab")
        {
            if (currentPlayer != null)
            {
                // 1. Encontra o Ponto de Spawn da Base
                GameObject baseSpawn = GameObject.Find("Base_SpawnPoint");
                if (baseSpawn != null)
                {
                    currentPlayer.transform.position = baseSpawn.transform.position;
                    currentPlayer.transform.rotation = baseSpawn.transform.rotation;
                }
                else
                {
                    Debug.LogWarning("GameManager: 'Base_SpawnPoint' não encontrado na BaseLab!");
                }

                // 2. Manda o jogador tocar a animação de acordar
                PlayerHealth pHealth = currentPlayer.GetComponent<PlayerHealth>();
                if (pHealth != null)
                {
                    pHealth.TriggerBaseRespawn();
                }
            }

            // Esconde a tela de loading (caso esteja ativa)
            if (loadingScreenCanvas != null) loadingScreenCanvas.SetActive(false);
            
            // Fade In da tela (se o jogador tiver ScreenFader)
            if (currentPlayer != null)
            {
                ScreenFader fader = currentPlayer.GetComponentInChildren<ScreenFader>();
                if (fader != null) StartCoroutine(fader.FadeIn());
            }
        }
    }
    // ---------------------------

    public void RegisterPlayer(GameObject player)
    {
        if (currentPlayer == null) currentPlayer = player;
        else if (currentPlayer != player) Destroy(player);
    }

    public void LoadGameLevel()
    {
        StartCoroutine(LoadLevelAsync("GameScene"));
    }

    public void ReturnToBase()
    {
        if (loadingScreenCanvas != null) loadingScreenCanvas.SetActive(true);
        SceneManager.LoadScene("BaseLab");
    }

    private IEnumerator LoadLevelAsync(string sceneName)
    {
        if (loadingScreenCanvas != null) loadingScreenCanvas.SetActive(true);

        if (currentPlayer != null)
        {
            PlayerM playerMovement = currentPlayer.GetComponent<PlayerM>();
            if (playerMovement != null) playerMovement.enabled = false;
        }

        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        while (!operation.isDone)
        {
            float progress = Mathf.Clamp01(operation.progress / 0.9f);
            if (loadingBar != null) loadingBar.value = progress;
            yield return null;
        }

        LevelGenerator levelGen = FindObjectOfType<LevelGenerator>();
        if (levelGen != null) levelGen.GenerateLevel();
    }

    public void OnLevelReady(Transform spawnPoint)
    {
        if (currentPlayer != null && spawnPoint != null)
        {
            currentPlayer.transform.position = spawnPoint.position;
            currentPlayer.transform.rotation = spawnPoint.rotation;
            
            PlayerM playerMovement = currentPlayer.GetComponent<PlayerM>();
            if (playerMovement != null) playerMovement.enabled = true;
            
            // Conecta Mercador e UI da fase
            PlayerHealth pHealth = currentPlayer.GetComponent<PlayerHealth>();
            if (pHealth != null) pHealth.FindUIReferences();
            
            DashM pDash = currentPlayer.GetComponent<DashM>();
            if (pDash != null) pDash.FindUIReferences();

            MerchantUIController merchantUI = FindObjectOfType<MerchantUIController>(true);
            if (merchantUI != null) merchantUI.ConnectPlayer(pHealth);
        }
        
        if (loadingScreenCanvas != null) loadingScreenCanvas.SetActive(false);
    }
}