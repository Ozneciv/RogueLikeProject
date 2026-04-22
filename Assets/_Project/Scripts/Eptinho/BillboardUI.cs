using UnityEngine;
// Faz o UI sempre olhar para a câmera
public class BillboardUI : MonoBehaviour
{
 void LateUpdate()
    {
        transform.forward = Camera.main.transform.forward;
    }
}
