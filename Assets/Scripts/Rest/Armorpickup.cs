using UnityEngine;

public class ArmorPickup : MonoBehaviour
{
    [Tooltip("A quantidade de armadura que este item restaura.")]
    public int armorToRestore = 50;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.RestoreArmor(armorToRestore);
                // Opcional: Adicionar um som de coleta aqui
                Destroy(gameObject); // Destrói o item após ser coletado
            }
        }
    }
}