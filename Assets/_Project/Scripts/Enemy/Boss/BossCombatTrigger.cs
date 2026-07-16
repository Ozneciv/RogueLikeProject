using UnityEngine;
using System.Collections;

/// <summary>
/// Trigger de início do combate com o Boss.
///
/// SETUP NO UNITY:
///   1. Coloque este componente no GameObject RAIZ do prefab BossRoom_Cromatico
///      (o mesmo que tem o Box Collider com Is Trigger = true)
///   2. Arraste o GameObject "ArenaSeal" no campo "arenaSeal"
///   3. Arraste o GameObject "Boss_Cromatico" (que tem o BossController) no campo "bossController"
///   4. Opcional: arraste GameObjects de portas no array "entranceDoors"
///
/// FLUXO:
///   1. Player entra no trigger → selo ativa → boss começa a lutar
///   2. Boss morre → selo é destruído pelo BossController
/// </summary>
[RequireComponent(typeof(Collider))]
public class BossCombatTrigger : MonoBehaviour
{
    [Header("Referências")]
    [Tooltip("O BossController do boss nesta arena.")]
    public BossController bossController;

    [Tooltip("O selo/barreira que tranca a arena durante o combate.\n" +
             "Começa DESATIVADO e é ativado quando o player entra.")]
    public GameObject arenaSeal;

    [Tooltip("GameObjects de portas extras que trancam durante o combate (opcional).\n" +
             "Funciona igual ao sistema do RoomController.")]
    public GameObject[] entranceDoors;

    [Header("Configuração")]
    [Tooltip("Delay em segundos após o player entrar antes de ativar o selo.\n" +
             "Dá tempo do player entrar completamente na arena.")]
    public float sealDelay = 1.0f;

    [Tooltip("Delay adicional após o selo antes de iniciar a luta.\n" +
             "Momento dramático antes do boss atacar.")]
    public float fightStartDelay = 1.5f;

    [Header("Debug")]
    public bool showDebugLog = true;

    private bool hasTriggered = false;

    void Start()
    {
        // Garante que o selo começa desativado
        if (arenaSeal != null)
            arenaSeal.SetActive(false);

        // Garante que o collider é trigger
        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
        {
            col.isTrigger = true;
            Debug.LogWarning("[BossCombatTrigger] Collider não era trigger — corrigido automaticamente.");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (hasTriggered) return;

        hasTriggered = true;

        if (showDebugLog) Debug.Log("[BossCombatTrigger] ⚔️ Player entrou na arena do boss!");

        StartCoroutine(StartBossFightSequence());
    }

    private IEnumerator StartBossFightSequence()
    {
        // Espera o player entrar completamente
        yield return new WaitForSeconds(sealDelay);

        // Ativa o selo (tranca a arena)
        if (arenaSeal != null)
        {
            arenaSeal.SetActive(true);
            if (showDebugLog) Debug.Log("[BossCombatTrigger] 🔒 Selo ativado — arena trancada!");
        }

        // Tranca portas extras
        foreach (GameObject door in entranceDoors)
        {
            if (door != null) door.SetActive(true);
        }

        // Momento dramático
        yield return new WaitForSeconds(fightStartDelay);

        // Inicia a luta
        if (bossController != null)
        {
            bossController.StartFight();
        }
        else
        {
            Debug.LogError("[BossCombatTrigger] ❌ BossController não atribuído! Arraste o boss no Inspector.");
        }

        // Desativa o trigger (não precisa mais)
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;
    }
}
