using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gerencia o Bestiário do Eptinho — rastreia inimigos encontrados durante a run.
///
/// FLUXO:
///   1. EnemyIdentity detecta que o player avistou/atacou o inimigo.
///   2. Chama BestiarioManager.instancia.Registrar(enemyIdentity).
///   3. BestiarioManager adiciona o EnemyData à lista e dispara o popup.
///   4. EptinhoMenuController exibe a lista ao abrir o menu (tecla I).
/// </summary>
public class BestiarioManager : MonoBehaviour
{
    public static BestiarioManager instancia;

    // Lista de EnemyData já descobertos nesta run
    public List<EnemyData> inimigosEncontrados = new();
    private HashSet<string> nomesRegistrados = new();

    void Awake()
    {
        if (instancia == null)
        {
            instancia = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    /// <summary>
    /// Registra um inimigo no Bestiário pela primeira vez que é encontrado.
    /// Chamado pelo EnemyIdentity quando o player interage com o inimigo.
    /// </summary>
    public void Registrar(EnemyIdentity inimigo)
    {
        if (inimigo == null || inimigo.enemyData == null) return;
        if (inimigo.foiEncontrado) return;
        if (nomesRegistrados.Contains(inimigo.enemyData.enemyName)) return;

        inimigo.foiEncontrado = true;
        inimigosEncontrados.Add(inimigo.enemyData);
        nomesRegistrados.Add(inimigo.enemyData.enemyName);

        if (EptinhoPopupController.instancia != null)
            EptinhoPopupController.instancia.MostrarPopupInimigo(inimigo.enemyData);

        Debug.Log($"[BESTIÁRIO] Novo inimigo registrado: {inimigo.enemyData.enemyName}");
    }
}
