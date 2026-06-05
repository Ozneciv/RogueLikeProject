using UnityEngine;
using System.Collections.Generic;

public class GhostTrail : MonoBehaviour
{
    private float ghostLifetime;
    private Material ghostMaterial;
    private float alpha;
    private float fadeSpeed;

  
    private List<Mesh> meshesToDraw = new List<Mesh>();
    private List<Matrix4x4> matricesToDraw = new List<Matrix4x4>();


    public void Init(Transform target, float lifetime, Material mat, float initialAlpha)
    {
        ghostLifetime = lifetime;
        
    
        transform.position = target.position;
        transform.rotation = target.rotation;
        transform.localScale = target.localScale;

    
        ghostMaterial = new Material(mat); 

    
        ghostMaterial.SetFloat("_Surface", 1); 
        ghostMaterial.SetFloat("_Mode", 2);    
        ghostMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        ghostMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        ghostMaterial.SetInt("_ZWrite", 0);
        ghostMaterial.DisableKeyword("_ALPHATEST_ON");
        ghostMaterial.EnableKeyword("_ALPHABLEND_ON");
        ghostMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        ghostMaterial.renderQueue = 3000;

  
        if (ghostMaterial.HasProperty("_Glossiness")) ghostMaterial.SetFloat("_Glossiness", 0f);
        if (ghostMaterial.HasProperty("_Smoothness")) ghostMaterial.SetFloat("_Smoothness", 0f);
        if (ghostMaterial.HasProperty("_Metallic")) ghostMaterial.SetFloat("_Metallic", 0f);
        if (ghostMaterial.HasProperty("_SpecularHighlights")) ghostMaterial.SetFloat("_SpecularHighlights", 0f);
        if (ghostMaterial.HasProperty("_EnvironmentReflections")) ghostMaterial.SetFloat("_EnvironmentReflections", 0f);

        alpha = initialAlpha;
        fadeSpeed = initialAlpha / lifetime;

    
        MeshFilter[] mfs = target.GetComponentsInChildren<MeshFilter>();
        foreach (var mf in mfs)
        {
            if (mf != null && mf.sharedMesh != null)
            {
                meshesToDraw.Add(mf.sharedMesh);
                matricesToDraw.Add(mf.transform.localToWorldMatrix);
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
                matricesToDraw.Add(smr.transform.localToWorldMatrix);
            }
        }


        Destroy(gameObject, ghostLifetime);
    }

    void Update()
    {

        alpha -= fadeSpeed * Time.deltaTime;
        if (alpha <= 0) return;

 
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


        for (int i = 0; i < meshesToDraw.Count; i++)
        {
            Graphics.DrawMesh(meshesToDraw[i], matricesToDraw[i], ghostMaterial, gameObject.layer);
        }
    }
}