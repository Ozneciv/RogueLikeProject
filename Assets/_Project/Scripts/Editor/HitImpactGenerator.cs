using UnityEngine;
using UnityEditor;

public class HitImpactGenerator
{
    [MenuItem("Tools/VFX/Gerar Hit Impact (Corte)")]
    public static void GenerateSlashImpact()
    {
        string originalPath = "Assets/_Project/Art/VFX/HIt/hitImpactPrefab.prefab";
        string newPath = "Assets/_Project/Art/VFX/HIt/hitImpact_Slash.prefab";

        // 1. Carrega o original
        GameObject original = AssetDatabase.LoadAssetAtPath<GameObject>(originalPath);
        if (original == null)
        {
            Debug.LogError("❌ Prefab original não encontrado no caminho: " + originalPath);
            return;
        }

        // 2. Cria o clone na cena temporariamente
        GameObject clone = (GameObject)PrefabUtility.InstantiatePrefab(original);
        clone.name = "hitImpact_Slash";

        // 3. Modifica o núcleo do impacto (Impact) para parecer um rasgo rápido
        Transform impactTr = clone.transform.Find("Impact");
        if (impactTr != null)
        {
            ParticleSystem ps = impactTr.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                var main = ps.main;
                main.startSize3D = true;
                main.startSizeX = new ParticleSystem.MinMaxCurve(0.5f, 1f);
                main.startSizeY = new ParticleSystem.MinMaxCurve(4f, 8f);
                main.startSizeZ = 1f;
                main.startRotation = new ParticleSystem.MinMaxCurve(-60f * Mathf.Deg2Rad, 60f * Mathf.Deg2Rad);
                // Tempo de vida bem menor (snappy)
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.05f, 0.15f); 
            }
        }

        // 4. Modifica as faíscas (Spark) com mais spread e vida menor
        Transform sparkTr = clone.transform.Find("Spark");
        if (sparkTr != null)
        {
            ParticleSystem ps = sparkTr.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                var main = ps.main;
                main.startSpeed = new ParticleSystem.MinMaxCurve(15f, 30f);
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.1f, 0.25f); // Morrem mais rápido
                
                var shape = ps.shape;
                shape.shapeType = ParticleSystemShapeType.Cone;
                shape.angle = 45f; // Spread muito maior (antes era 15)
                shape.radius = 0.1f;
            }
        }
        
        // 5. Modifica a onda de choque (ShockWave) para sumir rápido
        Transform shockTr = clone.transform.Find("ShockWave");
        if (shockTr != null)
        {
            ParticleSystem ps = shockTr.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                var main = ps.main;
                main.startSize3D = true;
                main.startSizeX = new ParticleSystem.MinMaxCurve(1f, 2f);
                main.startSizeY = new ParticleSystem.MinMaxCurve(4f, 6f);
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.1f, 0.2f); // Desaparece rápido
            }
        }

        // 6. Salva o clone como um novo Prefab e apaga da cena
        PrefabUtility.SaveAsPrefabAsset(clone, newPath);
        Object.DestroyImmediate(clone);
        
        Debug.Log("✅ Sucesso! Novo efeito criado: " + newPath);
    }
}
