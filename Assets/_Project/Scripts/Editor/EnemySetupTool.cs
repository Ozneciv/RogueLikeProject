using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Ferramenta de configuração automática dos prefabs de inimigos.
/// Acesse via: Menu > Eptinho > Setup Inimigos
///
/// Para cada inimigo configurado:
///   1. Cria um asset EnemyData na pasta do inimigo (se ainda não existir)
///   2. Adiciona EnemyIdentity ao prefab raiz (se ainda não existir)
///   3. Adiciona DetectorDeInimigo ao prefab raiz (se ainda não existir)
///   4. Vincula o EnemyData ao EnemyIdentity
/// </summary>
public class EnemySetupTool : EditorWindow
{
    // ── Dados de cada inimigo a configurar ───────────────────────────────────
    private struct EnemyConfig
    {
        public string displayName;   // Nome de exibição no Bestiário
        public string enemyClass;    // Classe/tipo do inimigo
        public string prefabPath;    // Caminho do prefab principal (relativo a Assets/)
        public string dataFolder;    // Pasta onde o EnemyData será criado
        public string dataAssetName; // Nome do arquivo .asset
        public int    vidaBase;
        public float  danoBase;
        public int    custoBudget;
    }

    private static readonly EnemyConfig[] Enemies = new EnemyConfig[]
    {
        new EnemyConfig
        {
            displayName   = "Crystal Tuner",
            enemyClass    = "Crystal",
            prefabPath    = "Assets/_Project/Enemies/CrystalTunner/CrystalTuner_Root.prefab",
            dataFolder    = "Assets/_Project/Enemies/CrystalTunner",
            dataAssetName = "Crystal Tuner Data",
            vidaBase      = 300,
            danoBase      = 0f,   // Tuner não ataca diretamente
            custoBudget   = 5,
        },
        new EnemyConfig
        {
            displayName   = "Crystal Watcher",
            enemyClass    = "Crystal",
            prefabPath    = "Assets/_Project/Enemies/CrystalWatcher/CrystalWatcher.prefab",
            dataFolder    = "Assets/_Project/Enemies/CrystalWatcher",
            dataAssetName = "Crystal Watcher Data",
            vidaBase      = 500,
            danoBase      = 30f,
            custoBudget   = 6,
        },
        new EnemyConfig
        {
            displayName   = "Dummy",
            enemyClass    = "Treino",
            prefabPath    = "Assets/_Project/Enemies/Dummy/Dummy.prefab",
            dataFolder    = "Assets/_Project/Enemies/Dummy",
            dataAssetName = "Dummy Data",
            vidaBase      = 9999,
            danoBase      = 0f,
            custoBudget   = 1,
        },
        new EnemyConfig
        {
            displayName   = "Goblin",
            enemyClass    = "Goblin",
            prefabPath    = "Assets/_Project/Enemies/Goblin/Goblin.prefab",
            dataFolder    = "Assets/_Project/Enemies/Goblin",
            dataAssetName = "Goblin Data",
            vidaBase      = 200,
            danoBase      = 20f,
            custoBudget   = 3,
        },
        new EnemyConfig
        {
            displayName   = "Golem",
            enemyClass    = "Golem",
            prefabPath    = "Assets/_Project/Enemies/Golem/Golem.prefab",
            dataFolder    = "Assets/_Project/Enemies/Golem",
            dataAssetName = "Golem Data",
            vidaBase      = 800,
            danoBase      = 60f,
            custoBudget   = 10,
        },
        new EnemyConfig
        {
            displayName   = "Magic Stone",
            enemyClass    = "Arcano",
            prefabPath    = "Assets/_Project/Enemies/MagicStone/MagicStoneEnemy.prefab",
            dataFolder    = "Assets/_Project/Enemies/MagicStone",
            dataAssetName = "Magic Stone Data",
            vidaBase      = 450,
            danoBase      = 40f,
            custoBudget   = 7,
        },
        new EnemyConfig
        {
            displayName   = "Shard Swarm",
            enemyClass    = "Enxame",
            prefabPath    = "Assets/_Project/Enemies/ShardSwarm/Shard Swarm.prefab",
            dataFolder    = "Assets/_Project/Enemies/ShardSwarm",
            dataAssetName = "Shard Swarm Data",
            vidaBase      = 600,
            danoBase      = 25f,
            custoBudget   = 8,
        },
        new EnemyConfig
        {
            displayName   = "Spider",
            enemyClass    = "Aracnídeo",
            prefabPath    = "Assets/_Project/Enemies/Spider/Spider.prefab",
            dataFolder    = "Assets/_Project/Enemies/Spider",
            dataAssetName = "Spider Data",
            vidaBase      = 250,
            danoBase      = 35f,
            custoBudget   = 4,
        },
        new EnemyConfig
        {
            displayName   = "Totem",
            enemyClass    = "Totem",
            prefabPath    = "Assets/_Project/Enemies/Totem/Totem_3_FBX_low.prefab",
            dataFolder    = "Assets/_Project/Enemies/Totem",
            dataAssetName = "Totem Data",
            vidaBase      = 700,
            danoBase      = 45f,
            custoBudget   = 9,
        },
    };

    // ── Menu entry ────────────────────────────────────────────────────────────
    [MenuItem("Eptinho/Setup Inimigos (Todos)")]
    public static void RunSetup()
    {
        int created = 0, updated = 0, skipped = 0;

        foreach (var cfg in Enemies)
        {
            // 1. Garantir pasta de destino
            if (!AssetDatabase.IsValidFolder(cfg.dataFolder))
                Directory.CreateDirectory(cfg.dataFolder);

            // 2. Criar ou carregar EnemyData asset
            string dataPath = cfg.dataFolder + "/" + cfg.dataAssetName + ".asset";
            EnemyData data = AssetDatabase.LoadAssetAtPath<EnemyData>(dataPath);
            bool dataIsNew = (data == null);

            if (dataIsNew)
            {
                data = ScriptableObject.CreateInstance<EnemyData>();
                data.enemyName   = cfg.displayName;
                data.enemyClass  = cfg.enemyClass;
                data.vidaBase    = cfg.vidaBase;
                data.danoBase    = cfg.danoBase;
                data.custoBudget = cfg.custoBudget;
                AssetDatabase.CreateAsset(data, dataPath);
                created++;
                Debug.Log($"[EnemySetup] EnemyData criado: {dataPath}");
            }

            // 3. Carregar prefab
            GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(cfg.prefabPath);
            if (prefabAsset == null)
            {
                Debug.LogWarning($"[EnemySetup] Prefab não encontrado: {cfg.prefabPath} — pulando.");
                skipped++;
                continue;
            }

            // 4. Abrir prefab para edição
            string prefabFullPath = cfg.prefabPath;
            using (var scope = new PrefabUtility.EditPrefabContentsScope(prefabFullPath))
            {
                GameObject root = scope.prefabContentsRoot;
                bool changed = false;

                // 4a. Adicionar EnemyIdentity se ausente
                EnemyIdentity identity = root.GetComponent<EnemyIdentity>();
                if (identity == null)
                {
                    identity = root.AddComponent<EnemyIdentity>();
                    changed = true;
                }

                // 4b. Vincular EnemyData (sempre vincula se estiver nulo)
                if (identity.enemyData == null)
                {
                    identity.enemyData = data;
                    changed = true;
                }

                // 4c. Adicionar DetectorDeInimigo se ausente
                DetectorDeInimigo detector = root.GetComponent<DetectorDeInimigo>();
                if (detector == null)
                {
                    detector = root.AddComponent<DetectorDeInimigo>();
                    detector.usarTrigger   = true;
                    detector.usarDistancia = false;
                    changed = true;
                }

                if (changed) updated++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Setup Inimigos Concluído",
            $"Assets EnemyData criados: {created}\n" +
            $"Prefabs atualizados: {updated}\n" +
            $"Prefabs não encontrados: {skipped}",
            "OK");
    }
}
