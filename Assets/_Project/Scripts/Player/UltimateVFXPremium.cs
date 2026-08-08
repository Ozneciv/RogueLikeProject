using UnityEngine;

/// <summary>
/// Desativado: Antigo efeito visual legado do Ultimate.
/// </summary>
public class UltimateVFXPremium : MonoBehaviour
{
    void Awake()
    {
        // Destrói este objeto legado imediatamente para evitar que efeitos antigos apareçam
        Destroy(gameObject);
    }

    public void PlayEffect() { }
    public void StopEffect() { }
}
