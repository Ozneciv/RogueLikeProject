using UnityEngine;
using UnityEngine.Events;

public class GoblinHealth : MonoBehaviour
{
    public int vidaMaxima = 100;
    private int vidaAtual;
    public bool isDead = false;

    // Eventos para disparar sons ou animações de dano/morte
    public UnityEvent aoReceberDano;
    public UnityEvent aoMorrer;

    void Start()
    {
        vidaAtual = vidaMaxima;
    }

    public void TakeDamage(int quantidade)
    {
        if (isDead) return;

        vidaAtual -= quantidade;
        Debug.Log(gameObject.name + " recebeu dano! Vida atual: " + vidaAtual);
        
        aoReceberDano.Invoke();

        if (vidaAtual <= 0)
        {
            Morrer();
        }
    }

    void Morrer()
    {
        isDead = true;
        aoMorrer.Invoke();
        Debug.Log(gameObject.name + " morreu!");

        // Destruir o objeto após alguns segundos ou tocar animação
        Destroy(gameObject, 3f); 
    }
}
