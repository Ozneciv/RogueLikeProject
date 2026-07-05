using UnityEngine;

public class LoadingSpinner : MonoBehaviour
{
    [Header("Rotation Settings")]
    [Tooltip("Rotation speed in degrees per second.")]
    public float rotationSpeed = -150f; // Negative for clockwise rotation

    private void Update()
    {
        // Rotate the UI element around the Z axis
        transform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);
    }
}
