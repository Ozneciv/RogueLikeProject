using UnityEngine;
using System.Collections.Generic;

public class DashHologram : MonoBehaviour
{
    private List<Mesh> meshes = new List<Mesh>();
    private List<Vector3> positions = new List<Vector3>();
    private List<Quaternion> rotations = new List<Quaternion>();
    private Material hologramMaterial;
    
    private float alpha;
    private float fadeSpeed;

    public void Init(Transform target, Vector3 futurePos, Quaternion futureRot, float lifetime, Material mat, float initialAlpha)
    {
        // IMPORTANTE: Cria uma cópia única do material para este holograma.
        // Assim, o esmaecimento afeta apenas este "fantasma" e não o arquivo original.
        hologramMaterial = new Material(mat);

        alpha = initialAlpha;
        // Calcula a velocidade do fade para que o Alpha chegue a 0 exatamente quando o 'lifetime' acabar
        fadeSpeed = initialAlpha / lifetime;

        // Captura as malhas do monstro (mantendo a lógica que resolveu o problema do tamanho)
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>();

        foreach (var r in renderers)
        {
            Mesh mesh = null;
            if (r is MeshRenderer mr)
            {
                MeshFilter mf = mr.GetComponent<MeshFilter>();
                if (mf != null) mesh = mf.sharedMesh;
            }
            else if (r is SkinnedMeshRenderer smr)
            {
                mesh = new Mesh();
                smr.BakeMesh(mesh);
            }

            if (mesh != null)
            {
                meshes.Add(mesh);
                positions.Add(r.transform.position - target.position);
                rotations.Add(Quaternion.Inverse(target.rotation) * r.transform.rotation);
            }
        }

        transform.position = futurePos;
        transform.rotation = futureRot;

        // Removeu o 'Destroy(gameObject, lifetime)' daqui, 
        // pois agora o objeto se destrói sozinho quando o Alpha chega a zero.
    }

    void Update()
    {
        if (hologramMaterial == null) return;

        // 1. Reduz o Alpha progressivamente com base no tempo
        alpha -= fadeSpeed * Time.deltaTime;

        // 2. Se ficou totalmente invisível, destrói o holograma e para a execução
        if (alpha <= 0)
        {
            Destroy(gameObject);
            return;
        }

        // 3. Aplica o novo valor de Alpha no material (funciona em Shaders Standard e URP)
        if (hologramMaterial.HasProperty("_Color"))
        {
            Color col = hologramMaterial.color;
            col.a = alpha;
            hologramMaterial.color = col;
        }
        else if (hologramMaterial.HasProperty("_BaseColor"))
        {
            Color col = hologramMaterial.GetColor("_BaseColor");
            col.a = alpha;
            hologramMaterial.SetColor("_BaseColor", col);
        }

        // 4. Desenha as malhas na tela
        for (int i = 0; i < meshes.Count; i++)
        {
            Vector3 worldPos = transform.position + (transform.rotation * positions[i]);
            Quaternion worldRot = transform.rotation * rotations[i];
            
            Graphics.DrawMesh(meshes[i], worldPos, worldRot, hologramMaterial, gameObject.layer, null, 0);
        }
    }
}