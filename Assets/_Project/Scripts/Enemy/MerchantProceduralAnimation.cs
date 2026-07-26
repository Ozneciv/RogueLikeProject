using UnityEngine;

/// <summary>
/// Animação Procedural sem Rig para o Mercador das Sombras.
/// Suporta animações ativas continuamente mesmo quando o jogo é pausado (Time.timeScale = 0).
/// </summary>
public class MerchantProceduralAnimation : MonoBehaviour
{
    [Header("1. Flutuação Mística (Levitação)")]
    public bool enableHovering = true;
    public float hoverAmplitude = 0.08f;  // Altura da flutuação em metros
    public float hoverSpeed = 1.8f;      // Velocidade do ciclo de flutuação

    [Header("2. Respiração Orgânica (Escala)")]
    public bool enableBreathing = true;
    public float breathScaleAmplitude = 0.025f; // Variação de escala no peito/manto
    public float breathSpeed = 1.5f;

    [Header("3. Inclinação Sinistra (Balanço do Chapéu/Corpo fora da UI)")]
    public bool enableSway = true;
    public float swayAngleMax = 2.0f;   // Ângulo máximo de inclinação em graus
    public float swaySpeed = 1.2f;

    // Estado Interno
    private Vector3 initialLocalPos;
    private Quaternion initialLocalRot;
    private Vector3 initialScale;
    
    private MerchantUIController uiController;

    void Awake()
    {
        initialLocalPos = transform.localPosition;
        initialLocalRot = transform.localRotation;
        initialScale = transform.localScale;
    }

    void Start()
    {
        uiController = MerchantUIController.Instance;
    }

    void Update()
    {
        bool isPactOpen = (uiController != null && uiController.IsUiOpen());

        // Usa Time.unscaledTime para que a animação NUNCA congele quando a UI pausar o jogo (Time.timeScale = 0)
        float timeToUse = Time.unscaledTime;

        // 1. Flutuação Vertical (Levitação Mística) - Continua ativa durante a UI!
        float targetYOffset = 0f;
        if (enableHovering)
        {
            float currentHoverSpeed = isPactOpen ? hoverSpeed * 0.7f : hoverSpeed;
            targetYOffset = Mathf.Sin(timeToUse * currentHoverSpeed) * hoverAmplitude;
        }

        // 2. Respiração (Escala) - Continua ativa durante a UI!
        if (enableBreathing)
        {
            float breath = Mathf.Sin(timeToUse * breathSpeed);
            float scaleY = initialScale.y + (breath * breathScaleAmplitude);
            float scaleXZ = initialScale.x - (breath * (breathScaleAmplitude * 0.5f));
            transform.localScale = new Vector3(scaleXZ, scaleY, scaleXZ);
        }

        // Aplica Posição com Flutuação contínua
        transform.localPosition = initialLocalPos + new Vector3(0f, targetYOffset, 0f);

        // 3. Rotação: Na UI fica 100% Ereto e Fixo no initialLocalRot. Fora da UI aplica o balanço suave.
        if (isPactOpen)
        {
            // Transição suave para a postura padrão 100% ereta ao abrir a UI
            transform.localRotation = Quaternion.Slerp(transform.localRotation, initialLocalRot, Time.unscaledDeltaTime * 6.0f);
        }
        else
        {
            float swayZ = enableSway ? Mathf.Sin(timeToUse * swaySpeed) * swayAngleMax : 0f;
            Quaternion offsetRot = Quaternion.Euler(0f, 0f, swayZ);
            transform.localRotation = initialLocalRot * offsetRot;
        }
    }
}
