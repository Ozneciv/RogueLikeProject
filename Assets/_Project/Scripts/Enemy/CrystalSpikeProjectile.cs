using UnityEngine;

[RequireComponent(typeof(Collider))]
public class CrystalSpikeProjectile : MonoBehaviour
{
    [HideInInspector] public GameObject owner;
    [HideInInspector] public int damage = 8;

    private Vector3 direction = Vector3.forward;
    private float speed = 20f;
    private float lifetime = 4f;
    private Rigidbody rb;

    public void Launch(Vector3 direction, float speed, float lifetime)
    {
        this.direction = direction.normalized;
        this.speed = speed;
        this.lifetime = lifetime;

        if (rb != null)
        {
            rb.linearVelocity = this.direction * this.speed;
        }

        if (lifetime > 0f)
        {
            Destroy(gameObject, lifetime);
        }
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity = false;
        }
    }

    private void Update()
    {
        if (rb == null)
        {
            transform.position += direction * speed * Time.deltaTime;
        }
    }

    private void HandleHit(GameObject hitObject)
    {
        // Ignore if this component is not a real projectile (owner should be set on spawned projectiles).
        if (owner == null) return;
        if (hitObject == owner) return;
        if (!hitObject.CompareTag("Player")) return;

        PlayerHealth playerHealth = hitObject.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(damage, owner);
        }

        Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        HandleHit(other.gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        HandleHit(collision.gameObject);
    }
}
