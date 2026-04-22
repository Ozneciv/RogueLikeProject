using UnityEngine;

/// <summary>
/// Componente de identidade do inimigo.
/// Coloque em cada prefab de inimigo com nome e ícone.
/// Quando o inimigo for detectado pelo player, o Bestiário registra.
/// </summary>
public class EnemyIdentity : MonoBehaviour
{
    [Header("Configuração do Inimigo")]
    public string nomeInimigo;
    public Sprite icon;
    [TextArea] public string descricao;

    [HideInInspector] public bool foiEncontrado = false;
}
