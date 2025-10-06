using System.Collections;
using UnityEngine;
using TMPro;

public class DashM : MonoBehaviour
{
    private Rigidbody rb;
    
    [Header("Settings")]
    public float dashSpeed = 50f;
    public float dashDuration = 0.2f;

    [Header("Cooldown")]
    public float dashCd = 1.5f;
    private float dashCdTimer;

    [Header("UI")]
    public TextMeshProUGUI cooldownText;
    public TextMeshProUGUI dashReadyText;

    [Header("Input")]
    public KeyCode dashKey = KeyCode.E;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("Rigidbody não encontrado. Adicione um Rigidbody ao jogador.");
        }
        
        if (cooldownText != null)
        {
            cooldownText.enabled = false;
        }
        if (dashReadyText != null)
        {
            dashReadyText.text = "Dash";
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(dashKey) && dashCdTimer <= 0)
        {
            StartCoroutine(PerformDash());
        }

        if (dashCdTimer > 0)
        {
            dashCdTimer -= Time.deltaTime;
            UpdateCooldownUI();
        }
        else
        {
            UpdateReadyUI();
        }
    }

    private void UpdateCooldownUI()
    {
        if (cooldownText != null)
        {
            cooldownText.enabled = true;
            cooldownText.text = dashCdTimer.ToString("F1");
        }
        if (dashReadyText != null)
        {
            dashReadyText.enabled = false;
        }
    }

    private void UpdateReadyUI()
    {
        if (cooldownText != null)
        {
            cooldownText.enabled = false;
        }
        if (dashReadyText != null)
        {
            dashReadyText.enabled = true;
            dashReadyText.text = "Dash";
        }
    }

    private IEnumerator PerformDash()
    {
        dashCdTimer = dashCd;

        Vector3 dashDirection = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical")).normalized;
        if (dashDirection == Vector3.zero)
        {
            dashDirection = transform.forward;
        }

        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        rb.AddForce(dashDirection * dashSpeed, ForceMode.VelocityChange);

        yield return new WaitForSeconds(dashDuration);
    }
}