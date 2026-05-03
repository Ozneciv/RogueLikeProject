using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class HomingHazard : MonoBehaviour
{
    [Tooltip("A velocidade com que a caveira persegue o jogador.")]
    public float moveSpeed = 2.5f;

    [Header("Altura")]
    [Tooltip("Altura máxima que a caveira pode atingir (em unidades do mundo, a partir do Y=0).")]
    public float maxFlyHeight = 1.5f;

    [Tooltip("Amplitude da flutuação vertical (sobe e desce suavemente).")]
    public float bobAmplitude = 0.15f;

    [Tooltip("Velocidade da flutuação vertical.")]
    public float bobSpeed = 2.2f;

    private Transform playerTransform;
    private Rigidbody rb;
    private float bobOffset; // fase aleatória para cada caveira não flutuar em sincronia

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false; // garante que gravidade não interfira
        bobOffset = Random.Range(0f, Mathf.PI * 2f);

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            Transform targetPoint = player.transform.Find("TorsoTarget");
            playerTransform = (targetPoint != null) ? targetPoint : player.transform;

            if (targetPoint == null)
                Debug.LogWarning("Não foi encontrado um 'TorsoTarget' no jogador. A caveira mirará nos pés.");
        }
        else
        {
            Debug.LogError("HomingHazard: Não foi possível encontrar o jogador! Verifique a tag 'Player'.");
            enabled = false;
        }
    }

    void FixedUpdate()
    {
        if (playerTransform == null) return;

        // ── Movimento horizontal ────────────────────────────────────
        Vector3 currentPos = transform.position;
        Vector3 targetXZ   = new Vector3(playerTransform.position.x, currentPos.y, playerTransform.position.z);
        Vector3 direction  = (targetXZ - currentPos).normalized;

        // ── Altura alvo: flutuação suave + clamped ao maxFlyHeight ──
        float bob       = Mathf.Sin(Time.time * bobSpeed + bobOffset) * bobAmplitude;
        float targetY   = Mathf.Clamp(maxFlyHeight + bob, 0.3f, maxFlyHeight + bobAmplitude);

        // Suaviza a correção de altura gradualmente
        float newY = Mathf.Lerp(currentPos.y, targetY, 6f * Time.fixedDeltaTime);

        // Aplica velocidade final
        Vector3 vel = direction * moveSpeed;
        vel.y = (newY - currentPos.y) / Time.fixedDeltaTime; // converte deslocamento em velocidade Y
        rb.linearVelocity = vel;

        // Rotação: olha para o player sem inclinar demais
        transform.LookAt(playerTransform);
    }
}