using UnityEngine;

public class PlayerPersistence : MonoBehaviour
{
    public static PlayerPersistence instance;

    void Awake()
    {
        // Se não existe um jogador persistente, eu sou ele.
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
        // Se JÁ existe um jogador (que veio da run anterior), eu sou uma cópia da BaseLab.
        // Eu devo me destruir para deixar o jogador "veterano" assumir.
        else
        {
            Destroy(this.gameObject);
            return;
        }
    }

    void Start()
    {
        // Se apresenta ao GameManager sempre que a cena inicia
        if (GameManager.instance != null)
        {
            GameManager.instance.RegisterPlayer(this.gameObject);
        }
    }
}