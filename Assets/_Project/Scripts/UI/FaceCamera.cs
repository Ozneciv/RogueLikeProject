using UnityEngine;

/// <summary>
/// Billboard 100% estável para Canvases e Barras de Vida em 3D.
/// Força o Canvas a ficar 100% reto e alinhado de frente para a Câmera Principal,
/// independente do quanto o objeto pai (inimigo/core) gire ou mude de rotação.
/// </summary>
public class FaceCamera : MonoBehaviour
{
    private Camera mainCam;

    void Start()
    {
        FindCamera();
    }

    void FindCamera()
    {
        if (mainCam == null) mainCam = Camera.main;
    }

    void LateUpdate()
    {
        if (mainCam == null)
        {
            FindCamera();
            if (mainCam == null) return;
        }

        // Bloqueia a rotação para ficar sempre perfeitamente alinhado com a câmera do jogador
        transform.rotation = mainCam.transform.rotation;
    }
}
