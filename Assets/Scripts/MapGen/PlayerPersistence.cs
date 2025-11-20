using UnityEngine;

public class PlayerPersistence : MonoBehaviour
{
    // A trava de Singleton para o JOGADOR
    public static PlayerPersistence instance; 

    void Awake()
    {
        if (instance == null)
        {
            // Este é o primeiro Jogador. Ele se torna o "instance" e sobrevive.
            instance = this;
            DontDestroyOnLoad(this.gameObject);

            // Tenta se registrar no GameManager
            if (GameManager.instance != null)
            {
                GameManager.instance.RegisterPlayer(this.gameObject);
            }
            else
            {
                Debug.LogError("PlayerPersistence: Não foi possível encontrar o GameManager! (Verifique a Ordem de Execução de Script)");
            }
        }
        else
        {
            // Já existe um Jogador "imortal". Este (que acabou de ser carregado) é um duplicado.
            Destroy(this.gameObject);
        }
    }
}