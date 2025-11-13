using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.UI; // Para o Slider

public class GameManager : MonoBehaviour
{
    public static GameManager instance; // O Singleton

    [Header("Referências Globais")]
    // AQUI ESTÁ A VARIÁVEL QUE FALTAVA:
    public GameObject loadingScreenCanvas; // Arraste seu Canvas de Loading aqui
    public Slider loadingBar; // Arraste a barra de progresso do Canvas de Loading
    
    [HideInInspector]
    public GameObject currentPlayer; // Referência ao jogador "vivo" que vem da BaseLab


    void Awake()
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
}

    // AQUI ESTÁ A FUNÇÃO QUE FALTAVA:
    // Chamada pelo PlayerPersistence.cs na cena BaseLab
    public void RegisterPlayer(GameObject player)
    {
        if (currentPlayer == null)
        {
            currentPlayer = player;
            Debug.Log("GameManager: Jogador da BaseLab registrado!");
        }
        else if (currentPlayer != player)
        {
            // Impede a criação de jogadores duplicados se você recarregar a BaseLab
            Destroy(player);
        }
    }

    // Função chamada pela "Porta" (StartRunDoor.cs)
    public void LoadGameLevel()
    {
        StartCoroutine(LoadLevelAsync("GameScene")); // Certifique-se de que "GameScene" é o nome no Build Settings
    }

    private IEnumerator LoadLevelAsync(string sceneName)
    {
        if (loadingScreenCanvas != null)
            loadingScreenCanvas.SetActive(true);

        // Encontra o jogador (que já existe) e desativa seus controles
        if (currentPlayer != null)
        {
            PlayerM playerMovement = currentPlayer.GetComponent<PlayerM>();
            if (playerMovement != null)
            {
                playerMovement.enabled = false;
            }
        }

        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);

        while (!operation.isDone)
        {
            float progress = Mathf.Clamp01(operation.progress / 0.9f);
            if (loadingBar != null)
                loadingBar.value = progress;
            yield return null;
        }

        // A cena carregou, agora chama o LevelGenerator
        LevelGenerator levelGen = FindObjectOfType<LevelGenerator>();
        if (levelGen != null)
        {
            levelGen.GenerateLevel();
        }
    }

    // Chamada pelo LevelGenerator quando o mapa está PRONTO
        public void OnLevelReady(Transform spawnPoint)
        {
        // 1. Move o jogador (como antes)
        if (currentPlayer != null && spawnPoint != null)
        {
            currentPlayer.transform.position = spawnPoint.position;
            currentPlayer.transform.rotation = spawnPoint.rotation;

            PlayerM playerMovement = currentPlayer.GetComponent<PlayerM>();
            if (playerMovement != null)
            {
                playerMovement.enabled = true;
            }
        }

        // 2. Conecta a câmera (como antes)
        MoveCam mainCamera = FindObjectOfType<MoveCam>();
        if (mainCamera != null)
        {
            mainCamera.playerTransform = currentPlayer.transform;
        }

        // --- A NOVA LÓGICA DE "APRESENTAÇÃO" ---
        // 3. Encontra o "Cérebro" da UI do Mercador
        MerchantUIController merchantUI = FindObjectOfType<MerchantUIController>(true); // 'true' encontra-o mesmo desativado
        if (merchantUI != null && currentPlayer != null)
        {
            // 4. "Apresenta" o jogador ao Cérebro da UI
            merchantUI.playerHealth = currentPlayer.GetComponent<PlayerHealth>();
            Debug.Log("GameManager: Conectou o Jogador ao MerchantUIController.");
        }
        else
        {
            Debug.LogWarning("GameManager: Não foi possível conectar o Jogador ao MerchantUIController.");
        }
        // --- FIM DA NOVA LÓGICA ---

        if (loadingScreenCanvas != null)
            loadingScreenCanvas.SetActive(false);
    }
}