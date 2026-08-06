using UnityEngine;

/// <summary>
/// Componente do prefab de sangue ácido que o Boss pinga durante a invisibilidade.
/// Autodestrutível após o tempo de vida configurado.
///
/// SETUP NO UNITY:
///   1. Crie um GameObject com o visual desejado (partícula, sprite, mesh de poça, etc.)
///   2. Adicione este script ao GameObject
///   3. Ajuste o "lifetime" no Inspector
///   4. Salve como Prefab e arraste no campo "toxicBloodPrefab" do BossController
/// </summary>
public class ToxicBlood : MonoBehaviour
{
    [Tooltip("Tempo em segundos antes do sangue ácido desaparecer.")]
    public float lifetime = 3.5f;

    void Start()
    {
        Destroy(gameObject, lifetime);
    }
}
