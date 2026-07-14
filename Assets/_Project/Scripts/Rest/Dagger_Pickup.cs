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
            Debug.Log($"[Dagger_Pickup] Tecla F pressionada. playerObject: {(playerObject != null ? playerObject.name : "null")}");
            if (playerObject != null)
            {
                Player_WeaponManager wm = playerObject.GetComponent<Player_WeaponManager>();
                if (wm != null)
                {
                    Debug.Log("[Dagger_Pickup] Solicitando EquipDagger ao Player_WeaponManager.");
                    wm.EquipDagger(this.gameObject);
                }
                else
                {
                    Debug.LogError("[Dagger_Pickup] Falha: Player_WeaponManager não encontrado no playerObject!");
                }
            }
            else
            {
                Debug.LogWarning("[Dagger_Pickup] playerObject está nulo no Update, não é possível equipar.");
            }
        }
    }
}