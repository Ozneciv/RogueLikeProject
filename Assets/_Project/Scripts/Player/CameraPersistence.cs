using UnityEngine;

public class CameraPersistence : MonoBehaviour
{
    void Awake()
    {
        // Diz à Unity para não destruir este objeto (a Câmera)
        // quando uma new cena for carregada.
        DontDestroyOnLoad(this.gameObject);

        // Garante que só haja uma câmera do jogador
        // (Encontra outras câmeras com este script e destrói as duplicatas)
        CameraPersistence[] cameras = FindObjectsOfType<CameraPersistence>();
        if (cameras.Length > 1)
        {
            Destroy(this.gameObject);
        }
    }
}