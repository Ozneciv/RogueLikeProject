using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// PulseVisualizer melhorado: emite múltiplos anéis concêntricos que expandem
/// a partir do centro da caveira, indicando a área de dano do pulso.
/// Chame TriggerPulse() para disparar o efeito.
/// </summary>
public class PulseVisualizer : MonoBehaviour
{
    [Header("Configurações do Pulso")]
    [Tooltip("Duração total da animação do pulso.")]
    public float pulseDuration = 0.5f;

    [Tooltip("Raio máximo que os anéis atingem (equivale ao maxScale anterior).")]
    public float maxRadius = 3f;

    [Tooltip("Quantos anéis são emitidos por pulso.")]
    public int ringCount = 3;

    [Tooltip("Atraso entre cada anel (segundos).")]
    public float ringDelay = 0.08f;

    [Tooltip("Cor principal dos anéis.")]
    public Color ringColor = new Color(0.6f, 0f, 1f, 1f); // Roxo vibrante

    [Tooltip("Cor interna / núcleo do anel.")]
    public Color coreColor = new Color(1f, 0.5f, 1f, 1f); // Rosa claro

    [Tooltip("Espessura do anel.")]
    public float ringWidth = 0.07f;

    [Tooltip("Resolução do círculo (mais = mais suave).")]
    public int segments = 52;

    // --- MANTIDO para compatibilidade com código que usava maxScale ---
    [HideInInspector]
    public Vector3 maxScale = new Vector3(6f, 0.01f, 6f);
    [HideInInspector]
    public float maxAlpha = 0.5f;

    // Internos
    private List<LineRenderer> pool = new List<LineRenderer>();
    private Material sharedMat;

    void Awake()
    {
        sharedMat = new Material(Shader.Find("Sprites/Default"));
        sharedMat.color = Color.white;
        gameObject.SetActive(false);
    }

    // ─── API pública (mantém assinatura original) ───────────────────
    public void TriggerPulse()
    {
        gameObject.SetActive(true);
        StartCoroutine(EmitRings());
    }

    // ─── Coroutines ─────────────────────────────────────────────────
    private IEnumerator EmitRings()
    {
        for (int i = 0; i < ringCount; i++)
        {
            StartCoroutine(AnimateRing(i));
            if (i < ringCount - 1)
                yield return new WaitForSeconds(ringDelay);
        }

        // Espera o último anel terminar antes de desativar
        yield return new WaitForSeconds(pulseDuration + ringDelay * ringCount);
        gameObject.SetActive(false);
    }

    private IEnumerator AnimateRing(int index)
    {
        LineRenderer lr = GetOrCreateRing();
        lr.gameObject.SetActive(true);

        float timer = 0f;
        // Pequena escala inicial para dar sensação de expander a partir do ponto
        float startRadius = maxRadius * 0.05f;

        while (timer < pulseDuration)
        {
            timer += Time.deltaTime;
            float t = timer / pulseDuration;

            // Expansão com ease-out
            float eased = 1f - Mathf.Pow(1f - t, 2.5f);
            float r = Mathf.Lerp(startRadius, maxRadius, eased);

            // Alpha: começa cheio, desaparece no final
            float alpha = Mathf.Lerp(1f, 0f, t);

            // Largura: espessa no início, afina ao expandir
            float w = Mathf.Lerp(ringWidth * 1.8f, ringWidth * 0.2f, t);
            lr.startWidth = w;
            lr.endWidth   = w;

            // Cor interpolada entre coreColor (início) e ringColor (final)
            Color c = Color.Lerp(coreColor, ringColor, t);
            c.a = alpha;
            lr.material.color = c;

            SetCirclePositions(lr, r);
            yield return null;
        }

        lr.gameObject.SetActive(false);
        pool.Add(lr); // devolve ao pool
    }

    // ─── Helpers ────────────────────────────────────────────────────
    LineRenderer GetOrCreateRing()
    {
        // Tenta reutilizar um ring do pool
        for (int i = pool.Count - 1; i >= 0; i--)
        {
            var lr = pool[i];
            if (lr != null && !lr.gameObject.activeSelf)
            {
                pool.RemoveAt(i);
                return lr;
            }
        }

        // Cria um novo
        GameObject obj = new GameObject("PulseRing");
        obj.transform.SetParent(transform);
        obj.transform.localPosition = new Vector3(0, 0.02f, 0);
        obj.transform.localRotation = Quaternion.identity;

        LineRenderer lr2 = obj.AddComponent<LineRenderer>();
        lr2.loop             = true;
        lr2.positionCount    = segments + 1;
        lr2.useWorldSpace    = false;
        lr2.numCapVertices   = 3;
        lr2.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr2.receiveShadows   = false;
        lr2.material         = new Material(sharedMat);
        return lr2;
    }

    void SetCirclePositions(LineRenderer lr, float r)
    {
        for (int i = 0; i <= segments; i++)
        {
            float angle = (float)i / segments * Mathf.PI * 2f;
            lr.SetPosition(i, new Vector3(Mathf.Cos(angle) * r, 0f, Mathf.Sin(angle) * r));
        }
    }
}