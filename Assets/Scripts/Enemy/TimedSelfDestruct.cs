using UnityEngine;

public class TimedSelfDestruct : MonoBehaviour
{
    [Tooltip("Quanto tempo o raio fica na tela antes de sumir?")]
    public float lifetime = 2.0f; 

    void Start()
    {
        // Destrói este objeto (e o VFX filho) após X segundos
        Destroy(gameObject, lifetime);
    }
}