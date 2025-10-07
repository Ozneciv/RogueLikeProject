using System.Collections;
using UnityEngine;
using TMPro;

public class DashM : MonoBehaviour
{
    private Rigidbody rb;
    
    [Header("Settings")]
    public float dashSpeed = 30f;
    public float dashDuration = 0.2f;

    [Header("Dash")]
    public int maxDashes = 2;
    private int dashesLeft;

    [Header("UI")]
    public TextMeshProUGUI dashCountText;

    [Header("Input")]
    public KeyCode dashKey = KeyCode.E;

    private bool isDashing = false;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("Rigidbody não encontrado. Adicione um Rigidbody ao jogador.");
        }
        
        dashesLeft = maxDashes;
        if (dashCountText != null) dashCountText.enabled = true;
    }

    private void Update()
    {
        if (Input.GetKeyDown(dashKey) && dashesLeft > 0 && !isDashing)
        {
            StartCoroutine(PerformDash());
        }

        HandleDashUI();
    }

    private IEnumerator PerformDash()
    {
        isDashing = true;
        dashesLeft--;
        
        Vector3 dashDirection = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical")).normalized;
        if (dashDirection == Vector3.zero)
        {
            dashDirection = transform.forward;
        }

        rb.linearVelocity = Vector3.zero;
        rb.AddForce(dashDirection * dashSpeed, ForceMode.VelocityChange);

        yield return new WaitForSeconds(dashDuration);

        isDashing = false;
    }

    private void HandleDashUI()
    {
        if (dashCountText != null)
        {
            if (dashesLeft > 0)
            {
                dashCountText.text = "Dash: " + dashesLeft;
            }
            else
            {
                dashCountText.text = "Dash: 0";
            }
        }
    }
    public void RechargeDashes(int amount)
    {
        dashesLeft += amount;
        if (dashesLeft > maxDashes)
        {
            dashesLeft = maxDashes;
        }
    }
}