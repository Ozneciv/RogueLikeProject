using UnityEngine;

public class Dagger_Pickup: MonoBehaviour
{
    private bool playerIsNear = false;
    private GameObject playerObject;

    // Detecta quando o jogador entra na área da adaga
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerIsNear = true;
            playerObject = other.gameObject;
            Debug.Log("Pressione F para pegar a adaga.");
        }
    }

    // Detecta quando o jogador sai da área da adaga
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerIsNear = false;
            playerObject = null;
            Debug.Log("Você se afastou da adaga.");
        }
    }

    // Verifica se o jogador pressionou a tecla de pegar
// Verifica se o jogador pressionou a tecla de pegar
// Verifica se o jogador pressionou a tecla de pegar
    private void Update()
    {
        if (playerIsNear && Input.GetKeyDown(KeyCode.F))
        {
            // Pega o script do jogador e chama a função para equipar a adaga
            if (playerObject != null)
            {
                playerObject.GetComponent<Player_WeaponManager>().EquipDagger(this.gameObject);
            }
            else
            {
                Debug.LogWarning("playerObject is null in Update. OnTriggerEnter might not have fired.");
            }
        }
    }
}