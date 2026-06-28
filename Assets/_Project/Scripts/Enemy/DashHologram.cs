using UnityEngine;
using System.Collections.Generic;

public class DashHologram : MonoBehaviour
{
    private float ghostLifetime;
    private Material ghostMaterial;
    private float alpha;
    private float fadeSpeed;

    private List<Mesh> meshesToDraw = new List<Mesh>();
    private List<Matrix4x4> matricesToDraw = new List<Matrix4x4>();

    public void Init(Transform target, Vector3 futurePos, Quaternion futureRot, float lifetime, Material mat, float initialAlpha)
    {
        ghostLifetime = lifetime;
        
        transform.position = futurePos;
        transform.rotation = futureRot;
        transform.localScale = target.localScale;

        // Cria uma cópia do material que você configurou na Unity para podermos sumir com ele aos poucos
        ghostMaterial = new Material(mat); 

        alpha = initialAlpha;
        fadeSpeed = initialAlpha / lifetime;

        Matrix4x4 rootInverse = target.worldToLocalMatrix;
        Matrix4x4 holoRootMatrix = Matrix4x4.TRS(futurePos, futureRot, target.localScale);

        MeshFilter[] mfs = target.GetComponentsInChildren<MeshFilter>();
        foreach (var mf in mfs)
        {
            if (mf != null && mf.sharedMesh != null)
            {
                meshesToDraw.Add(mf.sharedMesh);
                Matrix4x4 originalMatrix = mf.transform.localToWorldMatrix;
                Matrix4x4 localMatrix = rootInverse * originalMatrix;
                matricesToDraw.Add(holoRootMatrix * localMatrix);
            }
        }

        SkinnedMeshRenderer[] smrs = target.GetComponentsInChildren<SkinnedMeshRenderer>();
        foreach (var smr in smrs)
        {
            if (smr != null)
            {
                Mesh bakedMesh = new Mesh();
                smr.BakeMesh(bakedMesh); 
                meshesToDraw.Add(bakedMesh);
                
                Matrix4x4 originalMatrix = smr.transform.localToWorldMatrix;
                Matrix4x4 localMatrix = rootInverse * originalMatrix;
                matricesToDraw.Add(holoRootMatrix * localMatrix);
            }
        }

        Destroy(gameObject, ghostLifetime);
    }

    void Update()
    {
        alpha -= fadeSpeed * Time.deltaTime;
        if (alpha <= 0) return;

        // Faz o holograma desvanecer (fade out)
        if (ghostMaterial.HasProperty("_Color"))
        {
            Color col = ghostMaterial.color;
            col.a = alpha;
            ghostMaterial.color = col;
        }
        else if (ghostMaterial.HasProperty("_BaseColor"))
        {
            Color col = ghostMaterial.GetColor("_BaseColor");
            col.a = alpha;
            ghostMaterial.SetColor("_BaseColor", col);
        }

        // Desenha as malhas na tela usando o material transparente
        for (int i = 0; i < meshesToDraw.Count; i++)
        {
            Graphics.DrawMesh(meshesToDraw[i], matricesToDraw[i], ghostMaterial, gameObject.layer);
        }
    }
}