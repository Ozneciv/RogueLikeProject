using System.Collections;
using UnityEngine;
using TMPro;

public class DashM : MonoBehaviour
{
    private Rigidbody rb;
    public bool isDashing = false;

    [Header("Dash Settings")]
    public float dashSpeed = 30f;
    public float dashDuration = 0.2f;

    [Header("Dash Charges")]
    public int maxDashes = 2;
    private int dashesLeft;

    [Header("Cooldown")]
    public float dashCooldown = 2f; // Tempo em segundos para recarregar todos os dashes
    private float cooldownTimer;
    private bool isRecharging = false;

    [Header("UI")]
    public TextMeshProUGUI dashCountText;

    [Header("Input")]
    public KeyCode dashKey = KeyCode.E;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("Rigidbody não encontrado. Adicione um Rigidbody ao jogador.");
        }
        
        // Começa o jogo com o máximo de dashes
        dashesLeft = maxDashes;
    }

    private void Update()
    {
        // Lógica para iniciar o dash
        if (Input.GetKeyDown(dashKey) && dashesLeft > 0 && !isDashing && !isRecharging)
        {
            StartCoroutine(PerformDash());
        }

        // Lógica do Cooldown
        if (isRecharging)
        {
            // Se a recarga está ativa, diminui o timer
            cooldownTimer -= Time.deltaTime;
            if (cooldownTimer <= 0)
            {
                // Quando o timer acaba, reseta os dashes e para a recarga
                isRecharging = false;
                dashesLeft = maxDashes;
            }
        }

        // Atualiza a UI a cada frame
        HandleDashUI();
    }

    private IEnumerator PerformDash()
    {
        isDashing = true;
        dashesLeft--; // Gasta um dash
        
        Vector3 dashDirection = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical")).normalized;
        if (dashDirection == Vector3.zero)
        {
            dashDirection = transform.forward;
        }

        rb.linearVelocity = Vector3.zero; // Usar 'velocity' em vez de 'linearVelocity' e AddForce
        rb.linearVelocity = dashDirection * dashSpeed;

        yield return new WaitForSeconds(dashDuration);

        rb.linearVelocity = Vector3.zero; // Para o movimento bruscamente no final do dash
        isDashing = false;

        // Se acabaram os dashes, inicia o cooldown
        if (dashesLeft <= 0)
        {
            isRecharging = true;
            cooldownTimer = dashCooldown;
        }
    }

    private void HandleDashUI()
    {
        if (dashCountText != null)
        {
            if (isRecharging)
            {
                // Se estiver recarregando, mostra o timer
                dashCountText.text = "Dash: " + cooldownTimer.ToString("F1"); // "F1" mostra 1 casa decimal
            }
            else
            {
                // Senão, mostra a quantidade de dashes
                dashCountText.text = "Dash: " + dashesLeft;
            }
        }
    }

    // Função chamada pelo item de recarga (DashRecharge.cs)
    public void RechargeDashes(int amount)
    {
        // Se estiver em cooldown, a recarga o cancela e enche os dashes.
        if (isRecharging)
        {
            isRecharging = false;
        }
        
        dashesLeft += amount;
        
        // Garante que a quantidade de dashes não ultrapasse o máximo
        if (dashesLeft > maxDashes)
        {
            dashesLeft = maxDashes;
        }
        Debug.Log("Dashes recarregados! Total agora: " + dashesLeft);
    }
}