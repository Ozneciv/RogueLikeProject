using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Efeito de alerta: anéis vermelhos que expandem do centro como ondas de chão pulsantes.
/// Adicione ao prefab AttackMarker. Funciona sozinho, sem filhos necessários.
/// </summary>
public class AttackMarkerEffect : MonoBehaviour
{
    [Header("Área")]
    public float radius   = 2f;
    public float duration = 1.5f;

    [Header("Pulso")]
    [Tooltip("Quantos anéis expandem por segundo")]
    public float spawnRate   = 3f;
    [Tooltip("Tempo que cada anel fica visível antes de sumir")]
    public float ringLifetime = 0.35f;
    [Tooltip("Cor dos anéis")]
    public Color ringColor = new Color(1f, 0.08f, 0.08f, 1f); // Vermelho vivo
    [Tooltip("Espessura do anel")]
    public float ringWidth = 0.06f;
    [Tooltip("Resolução do círculo")]
    public int segments = 48;

    // Internos
    private float elapsed     = 0f;
    private float spawnTimer  = 0f;
    private List<RingData> rings = new List<RingData>();
    private Material ringMat;

    struct RingData
    {
        public LineRenderer lr;
        public float born;
    }

    void Awake()
    {
        ringMat = new Material(Shader.Find("Sprites/Default"));
        ringMat.color = Color.white;

        SpawnRing();
        Destroy(gameObject, duration);
    }

    void Update()
    {
        elapsed    += Time.deltaTime;
        spawnTimer += Time.deltaTime;

        if (spawnTimer >= 1f / spawnRate)
        {
            spawnTimer = 0f;
            SpawnRing();
        }

        for (int i = rings.Count - 1; i >= 0; i--)
        {
            RingData rd = rings[i];
            float age = elapsed - rd.born;
            float t   = age / ringLifetime;

            if (t >= 1f)
            {
                Destroy(rd.lr.gameObject);
                rings.RemoveAt(i);
                continue;
            }

            float r     = Mathf.Lerp(0f, radius, t);
            float alpha = Mathf.Lerp(1f, 0f, t);

            // Fade-out global nos últimos 20%
            float globalT = elapsed / duration;
            if (globalT > 0.8f)
                alpha *= Mathf.Lerp(1f, 0f, (globalT - 0.8f) / 0.2f);

            float w = Mathf.Lerp(ringWidth, ringWidth * 0.3f, t);
            rd.lr.startWidth = w;
            rd.lr.endWidth   = w;

            Color c = ringColor;
            c.a = alpha;
            rd.lr.material.color = c;

            SetCircle(rd.lr, r);
        }
    }

    void SpawnRing()
    {
        GameObject obj = new GameObject("Ring");
        obj.transform.SetParent(transform);
        obj.transform.localPosition = Vector3.zero;

        LineRenderer lr = obj.AddComponent<LineRenderer>();
        lr.loop             = true;
        lr.positionCount    = segments + 1;
        lr.startWidth       = ringWidth;
        lr.endWidth         = ringWidth;
        lr.useWorldSpace    = false;
        lr.numCapVertices   = 3;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows   = false;
        lr.material         = new Material(ringMat);

        SetCircle(lr, 0f);
        rings.Add(new RingData { lr = lr, born = elapsed });
    }

    void SetCircle(LineRenderer lr, float r)
    {
        for (int i = 0; i <= segments; i++)
        {
            float angle = (float)i / segments * Mathf.PI * 2f;
            lr.SetPosition(i, new Vector3(Mathf.Cos(angle) * r, 0.02f, Mathf.Sin(angle) * r));
        }
    }
}
