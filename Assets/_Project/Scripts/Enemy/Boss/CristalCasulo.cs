using UnityEngine;

/// <summary>
/// Coloque este script em TODOS os Prefabs de Cristal que você usar na Fase 1.
/// Ele intercepta o ataque do player e desconta da vida do Boss.
/// </summary>
public class CristalCasulo : MonoBehaviour
{
    private DummyHealth bossHealth;

    // Recebe a referência da vida do boss na hora que nasce
    public void Setup(DummyHealth health)
    {
        bossHealth = health;
    }

    // =========================================================================
    // IMPORTANTE: Mude "TakeDamage" para o nome exato da função que sua 
    // espada/magia usa para causar dano nos inimigos!
    // =========================================================================
    public void TakeDamage(int amount, GameObject source = null)
    {
        if (bossHealth != null)
        {
            bossHealth.TakeDamage(amount, source);
            Debug.Log($"[Casulo] Absorveu {amount} de dano e repassou para o Boss!");
        }
    }
}