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
    
    // O jogador que está vivo e viajando entre cenas
    [HideInInspector] public GameObject currentPlayer;

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

    // Chamado pelo PlayerPersistence
    public void RegisterPlayer(GameObject player)
    {
        // Só aceita o registro se não tivermos um jogador, ou se for o mesmo
        if (currentPlayer == null)
        {
            currentPlayer = player;
            Debug.Log("GameManager: Jogador registrado/reencontrado.");
        }
    }

    public void LoadGameLevel()
    {
        StartCoroutine(LoadLevelAsync("GameScene"));
    }

    // Chamada pelo PlayerHealth ao morrer para voltar
// ... (dentro do GameManager.cs)

    public void ReturnToBase()
    {
        StartCoroutine(LoadBaseRoutine());
    }

    private IEnumerator LoadBaseRoutine()
    {
        // 1. Carrega a cena da Base
        AsyncOperation operation = SceneManager.LoadSceneAsync("BaseLab");
        while (!operation.isDone) yield return null;

        // 2. Encontra o ponto de spawn da base (se você criou um)
        // Se não tiver um "Base_SpawnPoint", ele vai ficar onde a cena carregar, o que pode ser ok.
        GameObject baseSpawn = GameObject.Find("Base_SpawnPoint"); 
        Vector3 spawnPos = Vector3.zero; // Posição padrão se não achar o ponto
        Quaternion spawnRot = Quaternion.identity;

        if (baseSpawn != null)
        {
            spawnPos = baseSpawn.transform.position;
            spawnRot = baseSpawn.transform.rotation;
        }

        // 3. Reseta o Jogador
        if (currentPlayer != null)
        {
            // Move para a posição inicial da base
            currentPlayer.transform.position = spawnPos;
            currentPlayer.transform.rotation = spawnRot;

            // Chama a função que criamos para reativar os controles e vida
            PlayerHealth pHealth = currentPlayer.GetComponent<PlayerHealth>();
            if (pHealth != null)
            {
                pHealth.ResetPlayerState();
            }
        }
    }

    private IEnumerator LoadLevelAsync(string sceneName)
    {
        if (loadingScreenCanvas != null) loadingScreenCanvas.SetActive(true);

        // Pausa o jogador durante o loading
        if (currentPlayer != null)
        {
            PlayerM pm = currentPlayer.GetComponent<PlayerM>();
            if (pm != null) pm.enabled = false;
        }

        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);

        while (!operation.isDone)
        {
            float progress = Mathf.Clamp01(operation.progress / 0.9f);
            if (loadingBar != null) loadingBar.value = progress;
            yield return null;
        }

        LevelGenerator levelGen = FindObjectOfType<LevelGenerator>();
        if (levelGen != null)
        {
            levelGen.GenerateLevel();
        }
        else
        {
            // Se não houver gerador (ex: voltando pra base), libera o jogador direto
            OnLevelReady(null); 
        }
    }

        public void OnLevelReady(Transform spawnPoint)
    {
        // 1. Move o jogador
        if (currentPlayer != null && spawnPoint != null)
        {
            currentPlayer.transform.position = spawnPoint.position;
            currentPlayer.transform.rotation = spawnPoint.rotation;
            
            PlayerM playerMovement = currentPlayer.GetComponent<PlayerM>();
            if (playerMovement != null) playerMovement.enabled = true;

            // --- A MÁGICA ACONTECE AQUI ---
            // Manda o jogador procurar a nova UI da GameScene
            PlayerHealth pHealth = currentPlayer.GetComponent<PlayerHealth>();
            if (pHealth != null) pHealth.FindUIReferences();

            DashM pDash = currentPlayer.GetComponent<DashM>();
            if (pDash != null) pDash.FindUIReferences();
            // -----------------------------
        }
        
        // 2. Conecta a câmera (se estiver usando MoveCam separado)
        MoveCam mainCamera = FindObjectOfType<MoveCam>();
        if (mainCamera != null && currentPlayer != null)
        {
            mainCamera.playerTransform = currentPlayer.transform;
        }

        // 3. Conecta o Mercador
        MerchantUIController merchantUI = FindObjectOfType<MerchantUIController>(true);
        if (merchantUI != null && currentPlayer != null)
        {
            merchantUI.ConnectPlayer(currentPlayer.GetComponent<PlayerHealth>());
        }
        
        // 4. Tira o loading
        if (loadingScreenCanvas != null)
            loadingScreenCanvas.SetActive(false);
    }
}