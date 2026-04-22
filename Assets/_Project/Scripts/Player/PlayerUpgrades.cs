using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class PlayerUpgrades : MonoBehaviour
{
    [Header("Referências Globais")]
    public PlayerM playerMovement; // CONFIRA SE O PLAYER ESTÁ AQUI
    public GameObject skillTreePanel;

    [Header("Lista de Upgrades")]
    public List<UpgradeDefinition> upgrades; 

    private void Start()
    {
        // Diagnóstico Inicial
        if (playerMovement == null) 
            Debug.LogError("ERRO CRÍTICO NO START: O campo 'Player Movement' está vazio no Inspector!");
        
        if (upgrades == null || upgrades.Count == 0)
            Debug.LogWarning("ALERTA: A lista de Upgrades está vazia ou não foi criada.");

        if (skillTreePanel != null) skillTreePanel.SetActive(false);
    }

    public void ToggleSkillTree()
    {
        if (skillTreePanel != null) skillTreePanel.SetActive(!skillTreePanel.activeSelf);
    }

    public void CloseSkillTree()
    {
        if (skillTreePanel != null) skillTreePanel.SetActive(false);
    }

    public void BuyUpgrade(int index)
    {
        Debug.Log($"--- INICIANDO COMPRA (Índice: {index}) ---");

        // 1. Verifica se o Player existe
        if (playerMovement == null)
        {
            Debug.LogError("FALHA: O script não encontrou o 'playerMovement'. Arraste o Player novamente para o Inspector do UpgradeManager.");
            return;
        }

        // 2. Verifica a Lista
        if (index < 0 || index >= upgrades.Count)
        {
            Debug.LogError($"FALHA: Índice {index} inválido. A lista tem tamanho {upgrades.Count}. Verifique o botão.");
            return;
        }

        UpgradeDefinition upgrade = upgrades[index];
        Debug.Log($"Dados do Upgrade: Nome='{upgrade.name}', Tipo={upgrade.type}, Valor Novo={upgrade.value}");

        // 3. Verifica o valor ANTES de mudar
        float valorAntigo = 0f;
        
        switch (upgrade.type)
        {
            case UpgradeType.HitboxAnimSpeed:
                valorAntigo = playerMovement.hitboxAnimSpeed;
                playerMovement.hitboxAnimSpeed = upgrade.value; // APLICAÇÃO DO VALOR
                
                // 4. Confirmação se mudou mesmo
                if (playerMovement.hitboxAnimSpeed == upgrade.value)
                    Debug.Log($"SUCESSO: AnimSpeed alterado de {valorAntigo} para {playerMovement.hitboxAnimSpeed}");
                else
                    Debug.LogError("ERRO ESTRANHO: O valor não foi atualizado no PlayerM!");
                break;

            case UpgradeType.HitboxMoveSpeed:
                valorAntigo = playerMovement.hitboxMoveSpeed;
                playerMovement.hitboxMoveSpeed = upgrade.value;
                Debug.Log($"SUCESSO: MoveSpeed alterado de {valorAntigo} para {playerMovement.hitboxMoveSpeed}");
                break;

            case UpgradeType.HitboxRotationSpeed:
                valorAntigo = playerMovement.hitboxRotationSpeed;
                playerMovement.hitboxRotationSpeed = upgrade.value;
                Debug.Log($"SUCESSO: RotationSpeed alterado de {valorAntigo} para {playerMovement.hitboxRotationSpeed}");
                break;
        }

        if (upgrade.button != null) upgrade.button.interactable = false;
        CloseSkillTree();
    }
}

// --- MANTENHA ESSAS DEFINIÇÕES IGUAIS ---
public enum UpgradeType { HitboxAnimSpeed, HitboxMoveSpeed, HitboxRotationSpeed }

[System.Serializable]
public class UpgradeDefinition
{
    public string name;      
    public UpgradeType type; 
    public float value;      
    public Button button;    
}