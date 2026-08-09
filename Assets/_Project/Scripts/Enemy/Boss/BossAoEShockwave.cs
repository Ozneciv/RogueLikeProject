using UnityEngine;
using System.Collections;

/// <summary>
/// Efeito de Impacto e Onda de Choque Exclusivo do Boss (AoE Knockback).
/// Substitui o uso da bomba do goblin por uma explosão mística cristalina de área expandida (7.5m).
/// </summary>
public class BossAoEShockwave : MonoBehaviour
{
    [Header("💥 Parâmetros do Impacto Exclusivo do Boss")]
    public float shockwaveRadius = 7.5f;
    public int shockwaveDamage = 35;
    public float knockbackForce = 16.0f;
    public float upwardForce = 0.35f;

    [Header("✨ Estética Visual")]
    public Color shockwaveColor = new Color(1.00f, 0.55f, 0.10f, 0.90f);
    public float duration = 0.8s;

    public static void TriggerBossExplosion(Vector3 centerPosition, float radius = 7.5f, int damage = 35, float pushForce = 16.0f)
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
        // 1. Cria a onda de choque expansiva no chão (Ring Decal)
        GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Quad);
        ring.name = "BossShockwaveRing";
        Destroy(ring.GetComponent<Collider>());
        
        ring.transform.position = transform.position + Vector3.up * 0.05f;
        ring.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        ring.transform.localScale = Vector3.zero;

        Renderer r = ring.GetComponent<Renderer>();
        if (r != null)
        {
            Shader uShader = Shader.Find("Universal Render Pipeline/Unlit")
                          ?? Shader.Find("Universal Render Pipeline/Lit")
                          ?? Shader.Find("Unlit/Color");
            Material m = new Material(uShader);
            m.color = shockwaveColor;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", shockwaveColor);
            m.EnableKeyword("_EMISSION");
            if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", shockwaveColor * 3.0f);
            r.material = m;
        }

        // 2. Luz de Flash de Explosão
        GameObject lightObj = new GameObject("BossBlastLight");
        lightObj.transform.position = transform.position + Vector3.up * 1.0f;
        Light lightComp = lightObj.AddComponent<Light>();
        lightComp.color = shockwaveColor;
        lightComp.intensity = 15f;
        lightComp.range = shockwaveRadius * 1.5f;

        // 3. Aplica o Dano e o Knockback pesado no Player
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
                if (pRb != null)
                {
                    Vector3 pushDir = (hit.transform.position - transform.position).normalized;
                    pushDir.y = upwardForce;
                    pRb.AddForce(pushDir * knockbackForce, ForceMode.Impulse);
                    Debug.Log($"[BossAoEShockwave] 💥 Player arremessado com força de knockback {knockbackForce}!");
                }
            }
        }

        // 4. Animação de Expansão e Desvanecimento do Anel
        float elapsed = 0f;
        float animDuration = 0.55f;
        Vector3 maxScale = new Vector3(shockwaveRadius * 2f, shockwaveRadius * 2f, 1f);

        while (elapsed < animDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / animDuration);
            float expandCurve = Mathf.Sin(t * Mathf.PI * 0.5f);

            if (ring != null)
            {
                ring.transform.localScale = Vector3.Lerp(Vector3.zero, maxScale, expandCurve);
                if (r != null && r.material != null)
                {
                    Color c = shockwaveColor;
                    c.a = Mathf.Lerp(0.9f, 0f, t);
                    r.material.color = c;
                }
            }

            if (lightComp != null)
            {
                lightComp.intensity = Mathf.Lerp(15f, 0f, t);
            }

            yield return null;
        }

        Destroy(lightObj);
        Destroy(ring);
        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = shockwaveColor;
        Gizmos.DrawWireSphere(transform.position, shockwaveRadius);
    }
}
