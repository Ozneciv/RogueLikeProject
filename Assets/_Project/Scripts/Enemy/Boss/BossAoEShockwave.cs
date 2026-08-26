using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Efeito de Impacto e Onda de Choque Exclusivo do Boss (AoE Knockback).
/// Substitui quads laranjas por uma linda onda de choque circular expansiva com LineRenderer e luz mística.
/// </summary>
public class BossAoEShockwave : MonoBehaviour
{
    [Header("💥 Parâmetros do Impacto Exclusivo do Boss")]
    public float shockwaveRadius = 6.0f;
    public int shockwaveDamage = 35;
    public float knockbackForce = 5.0f;
    public float upwardForce = 0.20f;

    [Header("✨ Estética Visual (Cristal Roxo/Ciano)")]
    public Color shockwaveColor = new Color(0.75f, 0.25f, 1.00f, 0.90f); // Roxo Místico
    public Color innerGlowColor = new Color(0.20f, 0.85f, 1.00f, 0.90f); // Ciano Brilhante
    public float duration = 0.65f;

    public static void TriggerBossExplosion(Vector3 centerPosition, float radius = 6.0f, int damage = 35, float pushForce = 5.0f)
    {
        GameObject shockwaveObj = new GameObject("Boss_Exclusive_Shockwave");
        shockwaveObj.transform.position = centerPosition;

        BossAoEShockwave script = shockwaveObj.AddComponent<BossAoEShockwave>();
        script.shockwaveRadius = radius;
        script.shockwaveDamage = damage;
        script.knockbackForce = pushForce;
        script.ExecuteBlast();
    }

    public void ExecuteBlast()
    {
        StartCoroutine(BlastRoutine());
    }

    private IEnumerator BlastRoutine()
    {
        // 1. Aplica o Dano e o Knockback pesado no Player
        Collider[] hits = Physics.OverlapSphere(transform.position, shockwaveRadius);
        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                PlayerHealth pHealth = hit.GetComponent<PlayerHealth>();
                if (pHealth != null)
                {
                    pHealth.TakeDamage(shockwaveDamage, gameObject);
                }

                Rigidbody pRb = hit.GetComponent<Rigidbody>();
                if (pRb != null && !pRb.isKinematic)
                {
                    Vector3 pushDir = (hit.transform.position - transform.position).normalized;
                    pushDir.y = upwardForce;
                    pRb.AddForce(pushDir * knockbackForce, ForceMode.Impulse);
                }
            }
        }

        // 2. Luz de Flash Mística de Cristal
        GameObject lightObj = new GameObject("BossBlastLight");
        lightObj.transform.position = transform.position + Vector3.up * 1.2f;
        Light lightComp = lightObj.AddComponent<Light>();
        lightComp.color = shockwaveColor;
        lightComp.intensity = 25f;
        lightComp.range = shockwaveRadius * 1.8f;

        // 3. Cria Anéis Circulares Expansivos elegantes com LineRenderer (Sem Quads Laranjas)
        int segments = 48;
        GameObject ringObj = new GameObject("CrystalShockwaveRing");
        ringObj.transform.position = transform.position + Vector3.up * 0.05f;

        LineRenderer lr = ringObj.AddComponent<LineRenderer>();
        lr.loop = true;
        lr.positionCount = segments + 1;
        lr.startWidth = 0.4f;
        lr.endWidth = 0.1f;
        lr.useWorldSpace = true;
        lr.numCapVertices = 4;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;

        Shader sh = Shader.Find("Sprites/Default") ?? Shader.Find("UI/Default");
        Material mat = new Material(sh);
        mat.color = shockwaveColor;
        lr.material = mat;

        float elapsed = 0f;
        float animDuration = duration;

        while (elapsed < animDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / animDuration);
            float currentRadius = Mathf.Lerp(0.2f, shockwaveRadius, Mathf.Sin(t * Mathf.PI * 0.5f));
            float alpha = Mathf.Lerp(1.0f, 0f, t * t);

            // Atualiza o círculo
            for (int i = 0; i <= segments; i++)
            {
                float angle = (float)i / segments * Mathf.PI * 2f;
                Vector3 pos = ringObj.transform.position + new Vector3(Mathf.Cos(angle) * currentRadius, 0.03f, Mathf.Sin(angle) * currentRadius);
                lr.SetPosition(i, pos);
            }

            Color currentColor = Color.Lerp(shockwaveColor, innerGlowColor, t);
            currentColor.a = alpha;
            lr.material.color = currentColor;
            lr.startWidth = Mathf.Lerp(0.5f, 0.05f, t);

            if (lightComp != null)
            {
                lightComp.intensity = Mathf.Lerp(25f, 0f, t);
            }

            yield return null;
        }

        Destroy(lightObj);
        Destroy(ringObj);
        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = shockwaveColor;
        Gizmos.DrawWireSphere(transform.position, shockwaveRadius);
    }
}
