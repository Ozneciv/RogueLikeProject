using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Carrega os sprites dos inimigos em runtime e cria EnemyData dinamicamente na memória.
/// Não requer configuração manual no editor. Os sprites são carregados da pasta Resources.
/// </summary>
public class EnemyDataAutoLoader : MonoBehaviour
{
    public static EnemyDataAutoLoader instancia;

    // Lista dos dados de inimigos criados em runtime
    private List<EnemyData> _enemyDataCache = new List<EnemyData>();

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

        InitializeEnemyData();
    }

    private void InitializeEnemyData()
    {
        // Primeiro tenta carregar EnemyData assets do Resources (se existirem)
        EnemyData[] loaded = Resources.LoadAll<EnemyData>("EnemyData");
        foreach (var data in loaded)
        {
            _enemyDataCache.Add(data);
            Debug.Log($"[ENEMY DATA LOADER] Asset carregado: {data.enemyName}");
        }

        // Se não há assets configurados, cria em memória com os sprites disponíveis
        if (_enemyDataCache.Count == 0)
        {
            _enemyDataCache.AddRange(CriarDadosEmMemoria());
            Debug.Log($"[ENEMY DATA LOADER] {_enemyDataCache.Count} EnemyData criados em memória.");
        }
    }

    private List<EnemyData> CriarDadosEmMemoria()
    {
        var lista = new List<EnemyData>();

        lista.Add(CriarEnemyData("Goblin",    "EnemySprites/Inimigo_Goblin", "Mob Menor", "Uma criatura ágil com picareta. Preferem atacar em grupo e recuar rapidamente."));
        lista.Add(CriarEnemyData("Golem",     "EnemySprites/Golem",          "Tanque",    "Construção mineral animada. Extremamente resistente, mas lento e previsível."));
        lista.Add(CriarEnemyData("Aranha",    "EnemySprites/spider",         "Atirador",  "Criatura ágil que dispara projéteis de teia. Difícil de alcançar em espaços abertos."));
        lista.Add(CriarEnemyData("Sentinela", "EnemySprites/ALLSEEING",      "Elite",     "Olho cristalino que patrulha áreas. Detecta movimento e alerta outros inimigos."));
        lista.Add(CriarEnemyData("Totem",     "EnemySprites/Inimigo_Totem",  "Suporte",   "Estrutura estática que emite campos de energia. Destruí-lo enfraquece os inimigos ao redor."));

        return lista;
    }

    private EnemyData CriarEnemyData(string nome, string spritePath, string classe, string lore)
    {
        EnemyData data = ScriptableObject.CreateInstance<EnemyData>();
        data.enemyName = nome;
        data.enemyClass = classe;
        data.descricao = lore;
        data.icon = Resources.Load<Sprite>(spritePath);
        return data;
    }

    /// <summary>
    /// Retorna todos os EnemyData disponíveis (assets + memória).
    /// </summary>
    public List<EnemyData> GetAllEnemyData() => _enemyDataCache;

    /// <summary>
    /// Retorna EnemyData pelo nome do inimigo (case insensitive).
    /// </summary>
    public EnemyData GetByName(string nome)
    {
        return _enemyDataCache.Find(d => string.Equals(d.enemyName, nome, System.StringComparison.OrdinalIgnoreCase));
    }
}
