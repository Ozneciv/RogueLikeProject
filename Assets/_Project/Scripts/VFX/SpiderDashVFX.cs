using UnityEngine;

/// <summary>
/// VFX de rastro para o dash da Aranha (Leap e Retreat).
/// Cria um efeito de trail fantasmagorico durante o movimento.
///
/// MODOS DE GHOST MESH (ghostMeshMode):
///   SpiderMesh - Bake do SkinnedMeshRenderer no frame atual da animacao.
///                Resulta em uma silhueta exata da aranha na pose do dash.
///   Circle     - Disco plano no chao sob a aranha.
///                Mais performatico e legivel como indicador de posicao.
/// </summary>
public class SpiderDashVFX : MonoBehaviour
{
    [Header("Trail Settings")]
    [Tooltip("Cor principal do rastro")]
    public Color trailColor = new Color(0.6f, 0.2f, 0.8f, 0.7f);
    [Tooltip("Cor da ponta do rastro (fade)")]
    public Color trailEndColor = new Color(0.3f, 0.1f, 0.4f, 0f);
    [Tooltip("Largura do rastro")]
    public float trailWidth = 0.5f;
    [Tooltip("Duracao do rastro em segundos")]
    public float trailDuration = 0.3f;

    [Header("Ghost After-Image")]
    [Tooltip("Ativar efeito de after-image (copias fantasma)")]
    public bool useAfterImage = true;
    [Tooltip("Quantidade de after-images durante o dash")]
    public int afterImageCount = 3;
    [Tooltip("Cor do after-image")]
    public Color afterImageColor = new Color(0.5f, 0.2f, 0.6f, 0.4f);
    [Tooltip("Duracao de cada after-image em segundos")]
    public float afterImageDuration = 0.2f;
    [Tooltip("Multiplicador de escala das after-images. Ajuste se o ghost mesh ficar gigante/pequeno por escala de bones.")]
    public float ghostScaleMultiplier = 1.0f;

    public enum GhostMeshMode
    {
        SpiderMesh, // Bake do SkinnedMesh da aranha no frame do dash
        Circle      // Disco plano no chao sob a aranha
    }

    [Header("Ghost Mesh")]
    [Tooltip("SpiderMesh = copia do modelo animado da aranha (SkinnedMeshRenderer).\nCircle = disco plano no chao (mais performatico).")]
    public GhostMeshMode ghostMeshMode = GhostMeshMode.SpiderMesh;

    [Tooltip("Arraste aqui o SkinnedMeshRenderer do modelo 3D da aranha.\nSe ficar vazio, o script tenta detectar automaticamente (pode pegar a capsule).")]
    public SkinnedMeshRenderer targetRenderer;

    [Tooltip("Raio do circulo (modo Circle apenas).")]
    public float circleRadius = 0.6f;
    [Tooltip("Segmentos do circulo. Mais = mais suave. (modo Circle apenas)")]
    [Range(8, 64)]
    public int circleSegments = 24;

    private TrailRenderer trailRenderer;
    private Material trailMaterial;
    private bool isActive = false;
    private System.Collections.Generic.List<GameObject> activeGhosts
        = new System.Collections.Generic.List<GameObject>();

    void Start()
    {
        SetupTrailRenderer();
    }

    void SetupTrailRenderer()
    {
        GameObject trailObj = new GameObject("SpiderDashTrail");
        trailObj.transform.SetParent(transform);
        trailObj.transform.localPosition = new Vector3(0, 0.3f, 0);

        // Neutraliza escala do pai para evitar rastro gigante se a aranha for escalada na cena
        Vector3 parentScale = transform.lossyScale;
        trailObj.transform.localScale = new Vector3(
            parentScale.x > 0.0001f ? 1f / parentScale.x : 1f,
            parentScale.y > 0.0001f ? 1f / parentScale.y : 1f,
            parentScale.z > 0.0001f ? 1f / parentScale.z : 1f
        );

        trailRenderer = trailObj.AddComponent<TrailRenderer>();
        trailRenderer.time = trailDuration;
        trailRenderer.startWidth = trailWidth;
        trailRenderer.endWidth = trailWidth * 0.2f;
        trailRenderer.minVertexDistance = 0.1f;

        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(trailColor, 0f),
                new GradientColorKey(trailEndColor, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(trailColor.a, 0f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        trailRenderer.colorGradient = gradient;

        trailMaterial = new Material(Shader.Find("Sprites/Default"));
        trailMaterial.color = trailColor;
        trailRenderer.material = trailMaterial;
        trailRenderer.emitting = false;
    }

    /// <summary>Ativa o efeito de dash (chamar quando comecar o leap/retreat).</summary>
    public void StartDashEffect()
    {
        isActive = true;
        if (trailRenderer != null)
        {
            // Neutraliza a escala do pai em tempo de execucao para o rastro nao esticar
            Vector3 parentScale = transform.lossyScale;
            trailRenderer.transform.localScale = new Vector3(
                parentScale.x > 0.0001f ? 1f / parentScale.x : 1f,
                parentScale.y > 0.0001f ? 1f / parentScale.y : 1f,
                parentScale.z > 0.0001f ? 1f / parentScale.z : 1f
            );

            trailRenderer.Clear();
            trailRenderer.emitting = true;
        }
    }

    /// <summary>Desativa o efeito de dash (chamar quando terminar o leap/retreat).</summary>
    public void StopDashEffect()
    {
        isActive = false;
        if (trailRenderer != null)
        {
            trailRenderer.emitting = false;
            trailRenderer.Clear();
        }
    }

    /// <summary>
    /// Spawna um after-image fantasma na posicao atual.
    /// O formato depende de ghostMeshMode (SpiderMesh ou Circle).
    /// </summary>
    public void SpawnAfterImage()
    {
        if (!useAfterImage) return;

        if (ghostMeshMode == GhostMeshMode.Circle)
            SpawnCircleGhost();
        else
            SpawnSpiderMeshGhost();
    }

    // ------------------------------------------------------------------
    // MODO SpiderMesh: bake do SkinnedMeshRenderer no frame atual
    // ------------------------------------------------------------------
    private void SpawnSpiderMeshGhost()
    {
        // 1. Usa o renderer explicitamente configurado no Inspector (mais confiavel)
        SkinnedMeshRenderer source = targetRenderer;

        // 2. Auto-detect: busca SkinnedMeshRenderer nos filhos, ignora o root
        if (source == null)
        {
            SkinnedMeshRenderer[] all = GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (var smr in all)
            {
                // Ignora renderers cujo mesh se chama Capsule, Cylinder ou Cube (meshes de sistema)
                string meshName = smr.sharedMesh != null ? smr.sharedMesh.name.ToLower() : "";
                if (meshName.Contains("capsule") || meshName.Contains("cylinder") || meshName.Contains("cube"))
                    continue;
                // Ignora renderers que estao no root (geralmente sao o CharacterController ou placeholder)
                if (smr.transform == transform)
                    continue;
                source = smr;
                break;
            }
        }

        if (source != null)
        {
            Mesh bakedMesh = new Mesh();
            source.BakeMesh(bakedMesh);

            // Validacao: se a mesh baked vier vazia, o renderer era invalido
            if (bakedMesh.vertexCount == 0)
            {
                Debug.LogWarning("[SpiderDashVFX] BakeMesh retornou malha vazia. Verifique o targetRenderer no Inspector.");
                Destroy(bakedMesh);
                return;
            }

            GameObject ghost = new GameObject("SpiderGhost_Mesh");
            ghost.transform.position = source.transform.position;
            ghost.transform.rotation = source.transform.rotation;
            ghost.transform.localScale = source.transform.lossyScale * ghostScaleMultiplier;

            MeshFilter mf = ghost.AddComponent<MeshFilter>();
            mf.mesh = bakedMesh;

            MeshRenderer mr = ghost.AddComponent<MeshRenderer>();
            Material ghostMat = new Material(Shader.Find("Sprites/Default"));
            ghostMat.color = afterImageColor;
            mr.material = ghostMat;

            activeGhosts.Add(ghost);
            StartCoroutine(FadeOutGhost(ghost, ghostMat, bakedMesh, afterImageDuration));
            StartCoroutine(RemoveGhostWhenDone(ghost, afterImageDuration));
            return;
        }

        // 3. Fallback final: MeshFilter estatico que nao seja capsule/cylinder
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer rend in renderers)
        {
            if (rend == trailRenderer || rend is TrailRenderer) continue;
            if (rend.transform == transform) continue; // ignora root

            MeshFilter mf2 = rend.GetComponent<MeshFilter>();
            if (mf2 == null || mf2.sharedMesh == null) continue;

            string mName = mf2.sharedMesh.name.ToLower();
            if (mName.Contains("capsule") || mName.Contains("cylinder") || mName.Contains("cube")) continue;

            GameObject ghost = new GameObject("SpiderGhost_Static");
            ghost.transform.position = rend.transform.position;
            ghost.transform.rotation = rend.transform.rotation;
            ghost.transform.localScale = rend.transform.lossyScale * ghostScaleMultiplier;

            MeshFilter ghostMf = ghost.AddComponent<MeshFilter>();
            ghostMf.mesh = mf2.sharedMesh;

            MeshRenderer ghostMr = ghost.AddComponent<MeshRenderer>();
            Material ghostMat = new Material(Shader.Find("Sprites/Default"));
            ghostMat.color = afterImageColor;
            ghostMr.material = ghostMat;

            activeGhosts.Add(ghost);
            StartCoroutine(FadeOutGhost(ghost, ghostMat, null, afterImageDuration));
            StartCoroutine(RemoveGhostWhenDone(ghost, afterImageDuration));
            return;
        }

        Debug.LogWarning("[SpiderDashVFX] Nenhum renderer valido encontrado para o ghost. Atribua 'Target Renderer' no Inspector.");
    }

    // ------------------------------------------------------------------
    // MODO Circle: disco plano no chao sob a aranha
    // ------------------------------------------------------------------
    private void SpawnCircleGhost()
    {
        GameObject ghost = new GameObject("SpiderGhost_Circle");

        // Posiciona no chao sob a aranha via raycast
        Vector3 spawnPos = transform.position;
        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, out RaycastHit hit, 3f))
            spawnPos = hit.point + Vector3.up * 0.02f;

        ghost.transform.position = spawnPos;
        ghost.transform.rotation = Quaternion.Euler(90f, transform.eulerAngles.y, 0f);

        MeshFilter mf = ghost.AddComponent<MeshFilter>();
        mf.mesh = CreateCircleMesh(circleRadius, circleSegments);

        MeshRenderer mr = ghost.AddComponent<MeshRenderer>();
        Material ghostMat = new Material(Shader.Find("Sprites/Default"));
        ghostMat.color = afterImageColor;
        mr.material = ghostMat;

        activeGhosts.Add(ghost);
        StartCoroutine(FadeOutGhost(ghost, ghostMat, null, afterImageDuration));
        StartCoroutine(RemoveGhostWhenDone(ghost, afterImageDuration));
    }

    /// <summary>Gera uma malha de disco plano proceduralmente.</summary>
    private Mesh CreateCircleMesh(float radius, int segments)
    {
        Mesh mesh = new Mesh();
        Vector3[] vertices  = new Vector3[segments + 1];
        int[]     triangles = new int[segments * 3];

        vertices[0] = Vector3.zero; // Centro
        for (int i = 0; i < segments; i++)
        {
            float angle = 2f * Mathf.PI * i / segments;
            vertices[i + 1] = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);
        }
        for (int i = 0; i < segments; i++)
        {
            triangles[i * 3]     = 0;
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = (i + 2 > segments) ? 1 : i + 2;
        }

        mesh.vertices  = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        return mesh;
    }

    private System.Collections.IEnumerator FadeOutGhost(GameObject ghost, Material mat, Mesh bakedMeshToDispose, float duration)
    {
        float   elapsed    = 0f;
        Color   startColor = mat.color;
        Vector3 startScale = ghost.transform.localScale;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            mat.color = new Color(startColor.r, startColor.g, startColor.b, Mathf.Lerp(startColor.a, 0f, t));
            ghost.transform.localScale = Vector3.Lerp(startScale, startScale * 0.85f, t);
            yield return null;
        }

        Destroy(mat);
        if (bakedMeshToDispose != null) Destroy(bakedMeshToDispose);
        Destroy(ghost);
    }

    private System.Collections.IEnumerator RemoveGhostWhenDone(GameObject ghost, float delay)
    {
        yield return new WaitForSeconds(delay + 0.05f);
        activeGhosts.Remove(ghost);
    }

    /// <summary>Configura cores customizadas para o efeito.</summary>
    public void SetTrailColor(Color mainColor, Color fadeColor)
    {
        trailColor    = mainColor;
        trailEndColor = fadeColor;

        if (trailRenderer != null)
        {
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(trailColor, 0f),
                    new GradientColorKey(trailEndColor, 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(trailColor.a, 0f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            trailRenderer.colorGradient = gradient;
        }

        if (trailMaterial != null)
            trailMaterial.color = trailColor;
    }

    void OnDestroy()
    {
        foreach (GameObject ghost in activeGhosts)
            if (ghost != null) Destroy(ghost);
        activeGhosts.Clear();

        if (trailMaterial != null) Destroy(trailMaterial);
    }
}
