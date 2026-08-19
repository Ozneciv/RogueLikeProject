using UnityEngine;

public class SonicCrystal : MonoBehaviour
{
    [Header("Configurações de Status")]

    public float slowAmount = 1f; 
    public float slowDuration = 2f;
    public float knockbackForce = 70f;
    public float lifeTime = 3f; 


    [Header("Efeitos Visuais")]
    [Tooltip("Prefab de Particle System")]
    public GameObject breakEffectPrefab;

    private void Start()
    {
 
        Destroy(gameObject, lifeTime);
    }

    private void OnTriggerEnter(Collider other)
    {
     
        bool isPlayer = other.CompareTag("Player");
        bool isAttack = other.name.Contains("Attack") || other.name.Contains("Weapon") || other.gameObject.layer == LayerMask.NameToLayer("PlayerAttack");

        if (isPlayer || isAttack)
        {
            if (isPlayer)
            {
                ApplyEffects(other.gameObject);
            }

            SelfDestruct();
        }
    }

void ApplyEffects(GameObject player)
{
    Rigidbody rb = player.GetComponent<Rigidbody>();
    if (rb != null)
    {

        Vector3 direction = player.transform.position - transform.position;
        direction.y = 0; 
   
        rb.AddForce(direction.normalized * knockbackForce, ForceMode.Impulse);
    }


}





    public void SelfDestruct()
    {
  
        if (breakEffectPrefab != null)
        {
            GameObject fx = Instantiate(breakEffectPrefab, transform.position, Quaternion.identity);
            Destroy(fx, 2f); 
        }

        Destroy(gameObject);
    }
}