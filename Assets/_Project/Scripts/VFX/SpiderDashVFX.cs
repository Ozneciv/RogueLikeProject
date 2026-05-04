using UnityEngine;

/// <summary>
/// VFX de rastro para o dash da Aranha (Leap e Retreat)
/// Cria um efeito de trail fantasmagórico durante o movimento
/// </summary>
public class SpiderDashVFX : MonoBehaviour
{
    [Header("Trail Settings")]
    [Tooltip("Cor principal do rastro")]
    public Color trailColor = new Color(0.6f, 0.2f, 0.8f, 0.7f); // Roxo fantasmagórico
    [Tooltip("Cor da ponta do rastro (fade)")]
    public Color trailEndColor = new Color(0.3f, 0.1f, 0.4f, 0f);
    [Tooltip("Largura do rastro")]
    public float trailWidth = 0.5f;
    [Tooltip("Duração do rastro (segundos)")]
    public float trailDuration = 0.3f;

    [Header("Ghost After-Image")]
    [Tooltip("Ativar efeito de after-image (cópias fantasma)")]
    public bool useAfterImage = true;
    [Tooltip("Quantidade de after-images durante o dash")]
    public int afterImageCount = 3;
    [Tooltip("Cor do after-image")]
    public Color afterImageColor = new Color(0.5f, 0.2f, 0.6f, 0.4f);
    [Tooltip("Duração de cada after-image")]
    public float afterImageDuration = 0.2f;

    private TrailRenderer trailRenderer;
    private Material trailMaterial;
    private bool isActive = false;
    // Rastreia todos os ghost objects ativos para limpeza ao morrer
    private System.Collections.Generic.List<GameObject> activeGhosts
        = new System.Collections.Generic.List<GameObject>();

    void Start()
    {
        SetupTrailRenderer();
    }

    void SetupTrailRenderer()
    {
        // Cria o Trail Renderer
        GameObject trailObj = new GameObject("SpiderDashTrail");
        trailObj.transform.SetParent(transform);
        trailObj.transform.localPosition = new Vector3(0, 0.3f, 0); // Offset para centro do corpo

        trailRenderer = trailObj.AddComponent<TrailRenderer>();
        
        // Configurações do trail
        trailRenderer.time = trailDuration;
        trailRenderer.startWidth = trailWidth;
        trailRenderer.endWidth = trailWidth * 0.2f;
        trailRenderer.minVertexDistance = 0.1f;
        
        // Gradiente de cor
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
        
        // Material emissivo
        trailMaterial = new Material(Shader.Find("Sprites/Default"));
        trailMaterial.color = trailColor;
        trailRenderer.material = trailMaterial;
        
        // Começa desativado
        trailRenderer.emitting = false;
    }

    /// <summary>
    /// Ativa o efeito de dash (chamar quando começar o leap/retreat)
    /// </summary>
    public void StartDashEffect()
    {
        isActive = true;
        
        if (trailRenderer != null)
        {
            trailRenderer.Clear();
            trailRenderer.emitting = true;
        }
    }

    /// <summary>
    /// Desativa o efeito de dash (chamar quando terminar o leap/retreat)
    /// </summary>
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
    /// Spawna um after-image fantasma na posição atual
    /// </summary>
    public void SpawnAfterImage()
    {
        if (!useAfterImage) return;

        // Pega o MeshRenderer ou SkinnedMeshRenderer da aranha
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        
        foreach (Renderer rend in renderers)
        {
            if (rend == trailRenderer) continue;
            if (rend is TrailRenderer) continue;
            
            // Cria cópia visual
            GameObject ghost = new GameObject("SpiderGhost");
            ghost.transform.position = rend.transform.position;
            ghost.transform.rotation = rend.transform.rotation;
            ghost.transform.localScale = rend.transform.lossyScale;

            // Copia mesh
            MeshFilter originalMesh = rend.GetComponent<MeshFilter>();
            if (originalMesh != null && originalMesh.mesh != null)
            {
                MeshFilter ghostMesh = ghost.AddComponent<MeshFilter>();
                ghostMesh.mesh = originalMesh.mesh;

                MeshRenderer ghostRend = ghost.AddComponent<MeshRenderer>();
                
                // Material transparente fantasmagórico
                Material ghostMat = new Material(Shader.Find("Sprites/Default"));
                ghostMat.color = afterImageColor;
                ghostRend.material = ghostMat;
                
                // Fade out e destruir
                var coroutine = StartCoroutine(FadeOutGhost(ghost, ghostMat, afterImageDuration));
                activeGhosts.Add(ghost);
                // Garante remoção da lista quando terminar
                StartCoroutine(RemoveGhostWhenDone(ghost, afterImageDuration));
            }
            else
            {
                Destroy(ghost);
            }
            
            break; // Só uma cópia por spawn
        }
    }

    private System.Collections.IEnumerator FadeOutGhost(GameObject ghost, Material mat, float duration)
    {
        float elapsed = 0f;
        Color startColor = mat.color;
        Vector3 startScale = ghost.transform.localScale;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            // Fade alpha
            mat.color = new Color(startColor.r, startColor.g, startColor.b, Mathf.Lerp(startColor.a, 0f, t));
            
            // Leve encolhimento
            ghost.transform.localScale = Vector3.Lerp(startScale, startScale * 0.85f, t);
            
            yield return null;
        }

        Destroy(mat);
        Destroy(ghost);
    }

    private System.Collections.IEnumerator RemoveGhostWhenDone(GameObject ghost, float delay)
    {
        yield return new WaitForSeconds(delay + 0.05f);
        activeGhosts.Remove(ghost);
    }

    /// <summary>
    /// Configura cores customizadas para o efeito
    /// </summary>
    public void SetTrailColor(Color mainColor, Color fadeColor)
    {
        trailColor = mainColor;
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
        {
            trailMaterial.color = trailColor;
        }
    }

    void OnDestroy()
    {
        // Destrói todos os ghost objects restantes quando a Spider morre
        foreach (GameObject ghost in activeGhosts)
        {
            if (ghost != null) Destroy(ghost);
        }
        activeGhosts.Clear();

        if (trailMaterial != null) Destroy(trailMaterial);
    }
}
