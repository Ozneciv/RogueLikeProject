using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Script de Editor para criar uma cena de teste de inimigos
/// Acesse via: Menu > Tools > Create Enemy Test Scene
/// </summary>
public class EnemyTestSceneCreator : EditorWindow
{
    [MenuItem("Tools/Create Enemy Test Scene")]
    public static void CreateTestScene()
    {
        // Cria nova cena
        var newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // === CHÃO ===
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.position = Vector3.zero;
        ground.transform.localScale = new Vector3(10, 1, 10); // 100x100 unidades

        // Material do chão
        Renderer groundRenderer = ground.GetComponent<Renderer>();
        Material groundMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        groundMat.color = new Color(0.3f, 0.3f, 0.35f); // Cinza escuro
        groundRenderer.material = groundMat;

        // === LUZ DIRECIONAL ===
        // Já existe uma luz padrão na cena, vamos configurá-la
        Light mainLight = Object.FindObjectOfType<Light>();
        if (mainLight != null)
        {
            mainLight.transform.rotation = Quaternion.Euler(50, -30, 0);
            mainLight.intensity = 1.2f;
        }

        // === CONFIGURA CÂMERA PARA SEGUIR O PLAYER ===
        Camera mainCamera = Object.FindFirstObjectByType<Camera>();
        if (mainCamera != null)
        {
            // Posição inicial da câmera
            mainCamera.transform.position = new Vector3(0, 10, -8);
            mainCamera.transform.rotation = Quaternion.Euler(50, 0, 0);
            
            Debug.Log("Câmera configurada para o Player!");
        }

        // === SPAWN POINT DO PLAYER ===
        GameObject playerSpawn = new GameObject("PlayerSpawnPoint");
        playerSpawn.transform.position = new Vector3(0, 0.5f, 0);
        
        // Visual helper (cubo verde)
        GameObject spawnVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        spawnVisual.name = "SpawnVisual";
        spawnVisual.transform.SetParent(playerSpawn.transform);
        spawnVisual.transform.localPosition = Vector3.zero;
        spawnVisual.transform.localScale = new Vector3(0.5f, 1f, 0.5f);
        Object.DestroyImmediate(spawnVisual.GetComponent<Collider>());
        Renderer spawnRenderer = spawnVisual.GetComponent<Renderer>();
        Material spawnMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        spawnMat.color = Color.green;
        spawnRenderer.material = spawnMat;

        // === LOCAL DA ADAGA ===
        GameObject daggerPickup = new GameObject("DaggerPickupPoint");
        daggerPickup.transform.position = new Vector3(5, 0.5f, 0);

        // Visual helper (cubo amarelo)
        GameObject daggerVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        daggerVisual.name = "DaggerVisual";
        daggerVisual.transform.SetParent(daggerPickup.transform);
        daggerVisual.transform.localPosition = Vector3.zero;
        daggerVisual.transform.localScale = new Vector3(0.8f, 0.3f, 0.3f);
        Object.DestroyImmediate(daggerVisual.GetComponent<Collider>());
        Renderer daggerRenderer = daggerVisual.GetComponent<Renderer>();
        Material daggerMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        daggerMat.color = Color.yellow;
        daggerRenderer.material = daggerMat;

        // Adiciona texto informativo
        AddLabel(daggerPickup, "DAGGER HERE", Color.yellow);

        // === ÁREAS DE SPAWN DE INIMIGOS ===
        CreateEnemySpawnArea("SpiderSpawnArea", new Vector3(-10, 0, 10), Color.red, "SPIDER");
        CreateEnemySpawnArea("GolemSpawnArea", new Vector3(10, 0, 10), Color.cyan, "GOLEM");
        CreateEnemySpawnArea("EnemySpawnArea_1", new Vector3(-10, 0, -10), Color.magenta, "ENEMY 1");
        CreateEnemySpawnArea("EnemySpawnArea_2", new Vector3(10, 0, -10), Color.magenta, "ENEMY 2");

        // === PAREDES (Limites) ===
        CreateWall("Wall_North", new Vector3(0, 2.5f, 50), new Vector3(100, 5, 1));
        CreateWall("Wall_South", new Vector3(0, 2.5f, -50), new Vector3(100, 5, 1));
        CreateWall("Wall_East", new Vector3(50, 2.5f, 0), new Vector3(1, 5, 100));
        CreateWall("Wall_West", new Vector3(-50, 2.5f, 0), new Vector3(1, 5, 100));

        // === INSTRUÇÕES ===
        GameObject instructions = new GameObject("-- INSTRUÇÕES --");
        instructions.transform.position = Vector3.zero;

        // Salva a cena
        string scenePath = "Assets/Scenes/EnemyTestScene.unity";
        EditorSceneManager.SaveScene(newScene, scenePath);

        Debug.Log("===========================================");
        Debug.Log("CENA DE TESTE CRIADA: " + scenePath);
        Debug.Log("===========================================");
        Debug.Log("1. Arraste seu PLAYER PREFAB para 'PlayerSpawnPoint'");
        Debug.Log("2. Arraste seu DAGGER PREFAB para 'DaggerPickupPoint'");
        Debug.Log("3. Crie inimigos nas áreas marcadas (Spider, Golem, etc)");
        Debug.Log("===========================================");

        EditorUtility.DisplayDialog(
            "Cena Criada!",
            "A cena 'EnemyTestScene' foi criada em Assets/Scenes/\n\n" +
            "Próximos passos:\n" +
            "1. Arraste seu Player Prefab para 'PlayerSpawnPoint'\n" +
            "2. Arraste um Dagger Prefab para 'DaggerPickupPoint'\n" +
            "3. Crie inimigos (Spider, Golem) nas áreas marcadas",
            "OK"
        );
    }

    static void CreateEnemySpawnArea(string name, Vector3 position, Color color, string label)
    {
        GameObject area = new GameObject(name);
        area.transform.position = position;

        // Visual: círculo no chão (cilindro achatado)
        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        visual.name = "AreaVisual";
        visual.transform.SetParent(area.transform);
        visual.transform.localPosition = new Vector3(0, 0.05f, 0);
        visual.transform.localScale = new Vector3(5, 0.05f, 5);
        Object.DestroyImmediate(visual.GetComponent<Collider>());
        
        Renderer rend = visual.GetComponent<Renderer>();
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = new Color(color.r, color.g, color.b, 0.5f);
        rend.material = mat;

        // Adiciona label
        AddLabel(area, label, color);
    }

    static void CreateWall(string name, Vector3 position, Vector3 scale)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = name;
        wall.transform.position = position;
        wall.transform.localScale = scale;
        wall.tag = "Untagged";

        Renderer rend = wall.GetComponent<Renderer>();
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = new Color(0.2f, 0.2f, 0.25f);
        rend.material = mat;
    }

    static void AddLabel(GameObject parent, string text, Color color)
    {
        // Cria um objeto de texto 3D (TextMesh)
        GameObject labelObj = new GameObject("Label_" + text);
        labelObj.transform.SetParent(parent.transform);
        labelObj.transform.localPosition = new Vector3(0, 2f, 0);

        TextMesh textMesh = labelObj.AddComponent<TextMesh>();
        textMesh.text = text;
        textMesh.fontSize = 50;
        textMesh.characterSize = 0.2f;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.color = color;
    }
}
