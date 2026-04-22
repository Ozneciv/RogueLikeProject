using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct InimigoCatalogado
{
    public string nome;
    public Sprite icon;
    public string descricao;
}

public class BestiarioManager : MonoBehaviour
{
    public static BestiarioManager instancia;

    public List<InimigoCatalogado> inimigosEncontrados = new();
    private HashSet<string> nomesCatalogados = new();

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

    public void Registrar(EnemyIdentity inimigo)
    {
        if (inimigo == null || inimigo.foiEncontrado) return;
        if (nomesCatalogados.Contains(inimigo.nomeInimigo)) return;

        inimigo.foiEncontrado = true;

        InimigoCatalogado dados = new InimigoCatalogado
        {
            nome = inimigo.nomeInimigo,
            icon = inimigo.icon,
            descricao = inimigo.descricao
        };
        inimigosEncontrados.Add(dados);
        nomesCatalogados.Add(inimigo.nomeInimigo);

        EptinhoPopupController.instancia.MostrarPopupInimigo(dados);

        Debug.Log("[BESTIÁRIO] Novo inimigo encontrado: " + inimigo.nomeInimigo);
    }
}
