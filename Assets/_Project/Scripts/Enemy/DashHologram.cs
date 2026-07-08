using UnityEngine;
using System.Collections.Generic;

public class DashHologram : MonoBehaviour
{
    private List<Mesh> meshes = new List<Mesh>();
    private List<Vector3> positions = new List<Vector3>();
    private List<Quaternion> rotations = new List<Quaternion>();
    private Material hologramMaterial;
    
    private float alpha;

    public void Init(Transform target, Vector3 futurePos, Quaternion futureRot, float lifetime, Material mat, float initialAlpha)
    {
        // Cria uma cópia única do material para este holograma
        hologramMaterial = new Material(mat);
        alpha = initialAlpha;

        // 1. Aplica o valor FIXO de Alpha no material logo na criação (acontece só uma vez!)
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

        // Captura as malhas do monstro
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

        // 2. Como o alpha não muda mais, o próprio Unity destrói o objeto após o tempo acabar
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        if (hologramMaterial == null) return;

        // O código de redução de alpha e o Destroy foram removidos daqui.
        // O holograma mantém a mesma transparência do início ao fim.

        // 3. Desenha as malhas na tela
        for (int i = 0; i < meshes.Count; i++)
        {
            Vector3 worldPos = transform.position + (transform.rotation * positions[i]);
            Quaternion worldRot = transform.rotation * rotations[i];
            
            Graphics.DrawMesh(meshes[i], worldPos, worldRot, hologramMaterial, gameObject.layer, null, 0);
        }
    }
}