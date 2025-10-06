using System.Collections;
using UnityEngine;
using TMPro;

public class DashM : MonoBehaviour
{
    private Rigidbody rb;
    
    [Header("Settings")]
    public float dashSpeed = 30f;
    public float dashDuration = 0.2f;

    [Header("Cooldown")]
    public float dashCd = 1.5f;
    public int maxDashes = 2;
    private int dashesLeft;
    private float dashCdTimer;

    [Header("UI")]
    public TextMeshProUGUI cooldownText;
    public TextMeshProUGUI dashCountText;

    [Header("Input")]
    public KeyCode dashKey = KeyCode.E;

    // A variável isDashing foi removida deste script
    // A lógica de controle de movimento agora está no PlayerM.cs

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("Rigidbody não encontrado. Adicione um Rigidbody ao jogador.");
        }
        
        dashesLeft = maxDashes;
        if (cooldownText != null) cooldownText.enabled = false;
        if (dashCountText != null) dashCountText.enabled = true;
    }

    private void Update()
    {
        if (Input.GetKeyDown(dashKey) && dashesLeft > 0 && dashCdTimer <= 0)
        {
            StartCoroutine(PerformDash());
        }

        // Lógica do cooldown do dash
        if (dashesLeft <= 0 && dashCdTimer > 0)
        {
            dashCdTimer -= Time.deltaTime;
        }
        else if (dashesLeft <= 0 && dashCdTimer <= 0)
        {
            dashesLeft = maxDashes;
        }

        HandleDashUI();
    }

    private IEnumerator PerformDash()
    {
        dashesLeft--;
        
        Vector3 dashDirection = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical")).normalized;
        if (dashDirection == Vector3.zero)
        {
            dashDirection = transform.forward;
        }

        rb.linearVelocity = Vector3.zero;
        rb.AddForce(dashDirection * dashSpeed, ForceMode.VelocityChange);

        yield return new WaitForSeconds(dashDuration);
    }

    private void HandleDashUI()
    {
        if (dashesLeft > 0)
        {
            cooldownText.enabled = false;
            dashCountText.enabled = true;
            dashCountText.text = "Dash (" + dashesLeft + ")";
        }
        else
        {
            dashCountText.enabled = false;
            if (dashCdTimer > 0)
            {
                cooldownText.enabled = true;
                cooldownText.text = dashCdTimer.ToString("F1");
            }
            else
            {
                cooldownText.enabled = false;
                dashCountText.enabled = true;
                dashCountText.text = "Dash (" + dashesLeft + ")";
            }
        }
    }
}