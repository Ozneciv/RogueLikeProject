using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gerencia o bestiário de inimigos encontrados.
/// Quando um inimigo é visto pela primeira vez, registra e mostra popup.
/// Singleton — coloque em um GameObject persistente na cena.
/// </summary>
public class BestiarioManager : MonoBehaviour
{
    public static BestiarioManager instancia;

    public List<EnemyIdentity> inimigosEncontrados = new();

    void Awake()
    {
        instancia = this;
        Debug.Log("[BESTIÁRIO] Bestiário Manager iniciado.");

    }

    /// <summary>
    /// Registra um inimigo no bestiário. Se já foi encontrado, ignora.
    /// Chamado automaticamente quando o inimigo é ativado (player se aproxima).
    /// </summary>
    public void Registrar(EnemyIdentity inimigo)
    {
        if (inimigo == null || inimigo.foiEncontrado) return;

        // Verifica se já existe um inimigo com o mesmo nome (evita duplicatas de instâncias diferentes)
        foreach (EnemyIdentity registrado in inimigosEncontrados)
        {
            if (registrado.nomeInimigo == inimigo.nomeInimigo)
            {
                inimigo.foiEncontrado = true;
                return;
            }
        }

        inimigo.foiEncontrado = true;
        inimigosEncontrados.Add(inimigo);

        EptinhoPopupController.instancia.MostrarPopupInimigo(inimigo);

        Debug.Log("[BESTIÁRIO] Novo inimigo encontrado: " + inimigo.nomeInimigo);
    }
}
