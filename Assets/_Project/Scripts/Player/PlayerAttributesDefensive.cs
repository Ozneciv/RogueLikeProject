using UnityEngine;

/// <summary>
/// Atributos DEFENSIVOS e de MOBILIDADE do jogador (ficam no GameObject pai "Player").
/// Usado por PlayerHealth, PlayerM, DashM.
/// </summary>
public class PlayerAttributesDefensive : MonoBehaviour
{
    // ========== ATRIBUTOS DEFENSIVOS (3.2) ==========
    [Header("Defensive Attributes")]
    [Tooltip("Multiplicador de velocidade de regeneração de armadura. 1.0 = normal")]
    public float armorRegen = 1.0f;
    
    [Tooltip("Chance percentual de anular completamente o dano recebido (0-100)")]
    [Range(0f, 100f)]
    public float dodgeChance = 0f;
    
    [Tooltip("Redução percentual de todo dano recebido (0-100). Ex: 20 = -20% dano")]
    [Range(0f, 100f)]
    public float damageNegation = 0f;
    
    [Tooltip("Dano devolvido ao atacante quando jogador é atingido por contato físico")]
    public int thorns = 0;
    
    // ========== ATRIBUTOS DE MOBILIDADE (3.3) ==========
    [Header("Mobility Attributes")]
    [Tooltip("Multiplicador de tempo de recarga do Dash. 1.0 = normal, 0.5 = metade do tempo")]
    public float dashCooldownMultiplier = 1.0f;
    
    [Tooltip("Número máximo de cargas de Dash que podem ser armazenadas")]
    public int dashCounts = 1;
    
    [Tooltip("Duração da janela de invencibilidade durante o Dash (em segundos)")]
    public float dashInvulnerability = 0.2f;
    
    [Tooltip("Multiplicador de velocidade de movimento. 1.0 = normal, 1.5 = +50% velocidade")]
    public float speedMultiplier = 1.0f;

    /// <summary>
    /// Modifica um atributo pelo nome.
    /// </summary>
    public void ModifyAttribute(string attributeName, float value, bool isMultiplier = false)
    {
        switch (attributeName.ToLower())
        {
            // Defensivos
            case "armorregen":
                armorRegen = isMultiplier ? armorRegen * value : armorRegen + value;
                break;
            case "dodgechance":
            case "dodge":
                dodgeChance = Mathf.Clamp(isMultiplier ? dodgeChance * value : dodgeChance + value, 0f, 100f);
                break;
            case "damagenegation":
            case "negation":
                damageNegation = Mathf.Clamp(isMultiplier ? damageNegation * value : damageNegation + value, 0f, 100f);
                break;
            case "thorns":
                thorns = Mathf.Max(0, isMultiplier ? Mathf.RoundToInt(thorns * value) : thorns + Mathf.RoundToInt(value));
                break;
                
            // Mobilidade
            case "dashcooldownmultiplier":
                dashCooldownMultiplier = isMultiplier ? dashCooldownMultiplier * value : dashCooldownMultiplier + value;
                break;
            case "dashcounts":
                dashCounts = Mathf.Max(1, isMultiplier ? Mathf.RoundToInt(dashCounts * value) : dashCounts + Mathf.RoundToInt(value));
                break;
            case "dashinvulnerability":
                dashInvulnerability = isMultiplier ? dashInvulnerability * value : dashInvulnerability + value;
                break;
            case "speedmultiplier":
            case "speed":
                speedMultiplier = isMultiplier ? speedMultiplier * value : speedMultiplier + value;
                break;
                
            default:
                Debug.LogWarning($"PlayerAttributesDefensive: Atributo '{attributeName}' não encontrado!");
                break;
        }
    }
    
    /// <summary>
    /// Retorna o valor de um atributo pelo nome.
    /// </summary>
    public float GetAttribute(string attributeName)
    {
        switch (attributeName.ToLower())
        {
            // Defensivos
            case "armorregen": return armorRegen;
            case "dodgechance":
            case "dodge": return dodgeChance;
            case "damagenegation":
            case "negation": return damageNegation;
            case "thorns": return thorns;
            
            // Mobilidade
            case "dashcooldownmultiplier": return dashCooldownMultiplier;
            case "dashcounts": return dashCounts;
            case "dashinvulnerability": return dashInvulnerability;
            case "speedmultiplier":
            case "speed": return speedMultiplier;
            
            default:
                Debug.LogWarning($"PlayerAttributesDefensive: Atributo '{attributeName}' não encontrado!");
                return 0f;
        }
    }
    
    /// <summary>
    /// Reseta todos os atributos para valores padrão.
    /// </summary>
    public void ResetToDefaults()
    {
        // Defensivos
        armorRegen = 1.0f;
        dodgeChance = 0f;
        damageNegation = 0f;
        thorns = 0;
        
        // Mobilidade
        dashCooldownMultiplier = 1.0f;
        dashCounts = 1;
        dashInvulnerability = 0.2f;
        speedMultiplier = 1.0f;
        
        Debug.Log("PlayerAttributesDefensive: Atributos defensivos e de mobilidade resetados.");
    }
}
