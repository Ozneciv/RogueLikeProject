using UnityEngine;

public class DashRecharge : MonoBehaviour
{
    public int dashesToRecharge = 1; // Quantos dashes o cristal recarrega

    private void OnTriggerEnter(Collider other)
    {
        // Verifica se o objeto que entrou na área é o jogador
        if (other.gameObject.CompareTag("Player"))
        {
            // Pega o script de Dash do jogador
            DashM playerDash = other.gameObject.GetComponent<DashM>();

            // Se o script de Dash for encontrado, recarrega os dashes
            if (playerDash != null)
            {
                playerDash.RechargeDashes(dashesToRecharge);
                Debug.Log("Dashes recarregados! " + dashesToRecharge + " dashes adicionados.");
            }

            // Destroi o cristal após a colisão
            Destroy(this.gameObject);
        }
    }
}