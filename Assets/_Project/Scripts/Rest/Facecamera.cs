using UnityEngine;

public class FaceCamera : MonoBehaviour
{
    private Camera mainCamera;

    void Start()
    {
        // Encontra e guarda a referência da câmera principal
        mainCamera = Camera.main;
    }

    void LateUpdate()
    {
        // Gira o objeto para "encarar" a câmera a cada frame
        // (Usamos LateUpdate para garantir que a câmera já terminou de se mover)
        if (mainCamera != null)
        {
            transform.LookAt(transform.position + mainCamera.transform.forward);
        }
    }
}