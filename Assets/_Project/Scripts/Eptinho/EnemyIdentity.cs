using UnityEngine;

/// <summary>
/// Componente de identidade do inimigo — coloque em cada prefab de inimigo.
/// Referencia o EnemyData ScriptableObject com todos os dados do inimigo.
/// Quando o player avista o inimigo, o BestiarioManager registra automaticamente.
/// </summary>
public class EnemyIdentity : MonoBehaviour
{
    [Header("Dados do Inimigo")]
    [Tooltip("ScriptableObject com todos os dados deste inimigo. " +
             "Crie via: Assets > Create > Eptinho > Enemy Data")]
    public EnemyData enemyData;

    [HideInInspector] public bool foiEncontrado = false;

    // Propriedades de compatibilidade para não quebrar scripts de IA que usavam os campos antigos
    public string nomeInimigo => enemyData != null ? enemyData.enemyName : gameObject.name;
    public Sprite icon => enemyData != null ? enemyData.icon : null;
    public string descricao => enemyData != null ? enemyData.descricao : "";
}
