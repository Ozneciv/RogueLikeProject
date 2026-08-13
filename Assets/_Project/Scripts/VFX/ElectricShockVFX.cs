using System.Collections;
using UnityEngine;

/// <summary>
/// Efeito Visual e Físico de Choque Elétrico no Player.
/// Faz o modelo do player vibrar/tremer rapidamente, gera arcos elétricos (LineRenderer)
/// e iluminação pulsante amarela/ciano para dar a sensação imediata de CHOQUE!
/// </summary>
public class ElectricShockVFX : MonoBehaviour
{
    [Header("Configurações do Choque")]
    public float duration = 1.5f;
    public Color shockColor = new Color(0.2f, 0.9f, 1f); // Ciano Elétrico
    public Color sparkColor = new Color(1f, 0.85f, 0.3f); // Amarelo Faísca

    private Transform playerTarget;
    private Vector3 originalPosition;
    private LineRenderer[] arcRenderers;
    private Light shockLight;
    private float timer = 0f;

    public static void AttachToPlayer(GameObject playerGo, float shockDuration)
    {
        if (playerGo == null) return;

        // Se já tiver um choque ativo, apenas renova a duração
        ElectricShockVFX existing = playerGo.GetComponentInChildren<ElectricShockVFX>();
        if (existing != null)
        {
            existing.duration = Mathf.Max(existing.duration, shockDuration);
            return;
        }

        GameObject shockObj = new GameObject("ElectricShockVFX");
        shockObj.transform.SetParent(playerGo.transform, false);

        ElectricShockVFX shock = shockObj.AddComponent<ElectricShockVFX>();
        shock.playerTarget = playerGo.transform;
        shock.duration = shockDuration;
    }

    void Start()
    {
        if (playerTarget == null) playerTarget = transform.parent;

        // Criar 3 arcos elétricos em volta do corpo do player
        arcRenderers = new LineRenderer[3];
        for (int i = 0; i < 3; i++)
        {
            GameObject arcGo = new GameObject("Arc_" + i);
            arcGo.transform.SetParent(transform, false);

            LineRenderer line = arcGo.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.startWidth = 0.06f;
            line.endWidth = 0.02f;
            line.positionCount = 4;

            // Material emissivo simples
            Material mat = new Material(Shader.Find("Sprites/Default"));
            mat.color = (i % 2 == 0) ? shockColor : sparkColor;
            line.material = mat;

            arcRenderers[i] = line;
        }

        // Luz pulsante do choque
        GameObject lightObj = new GameObject("ShockLight");
        lightObj.transform.SetParent(transform, false);
        shockLight = lightObj.AddComponent<Light>();
        shockLight.type = LightType.Point;
        shockLight.color = shockColor;
        shockLight.range = 4f;
        shockLight.intensity = 3f;

        Destroy(gameObject, duration);
    }

    void Update()
    {
        if (playerTarget == null) return;

        timer += Time.deltaTime;

        // 1. Tremor/Vibração rápida no player para indicar choque físico
        Vector3 jitter = Random.insideUnitSphere * 0.08f;
        jitter.y = 0f; // Mantém no plano horizontal
        transform.position = playerTarget.position + Vector3.up * 1.0f + jitter;

        // 2. Atualizar arcos elétricos ao redor do corpo
        for (int i = 0; i < arcRenderers.Length; i++)
        {
            if (arcRenderers[i] == null) continue;

            Vector3 startPos = playerTarget.position + Vector3.up * (0.3f + i * 0.4f) + Random.insideUnitSphere * 0.3f;
            Vector3 endPos = playerTarget.position + Vector3.up * (0.8f + i * 0.3f) + Random.insideUnitSphere * 0.4f;
            Vector3 midPos1 = Vector3.Lerp(startPos, endPos, 0.33f) + Random.insideUnitSphere * 0.25f;
            Vector3 midPos2 = Vector3.Lerp(startPos, endPos, 0.66f) + Random.insideUnitSphere * 0.25f;

            arcRenderers[i].SetPosition(0, startPos);
            arcRenderers[i].SetPosition(1, midPos1);
            arcRenderers[i].SetPosition(2, midPos2);
            arcRenderers[i].SetPosition(3, endPos);
        }

        // 3. Piscar intensidade da luz
        if (shockLight != null)
        {
            shockLight.intensity = Random.Range(1.5f, 4.5f);
        }
    }
}
