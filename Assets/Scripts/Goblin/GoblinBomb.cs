using UnityEngine;

public class BombaExplosiva : MonoBehaviour
{
    public float raioExplosao = 5f;
    public int danoExplosao = 40;
    public float tempoParaExplodir = 3f;
    public GameObject efeitoExplosao; // Arraste um sistema de partículas aqui

    void Start()
    {
        // Faz a bomba ignorar o colisor de quem a lançou (se o lançador for um Enemy)
        GameObject goblin = GameObject.FindGameObjectWithTag("Enemy"); 
        if(goblin != null) {
            Physics.IgnoreCollision(GetComponent<Collider>(), goblin.GetComponent<Collider>());
        }
        // Começa a contagem regressiva para explodir sozinha se não bater em nada
        Invoke("Explodir", tempoParaExplodir);
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Se bater no jogador ou no chão, explode imediatamente
        Explodir();
    }

    void Explodir()
    {
        // 1. Criar efeito visual
        if (efeitoExplosao != null)
            Instantiate(efeitoExplosao, transform.position, Quaternion.identity);

        // 2. Detectar objetos próximos
        Collider[] objetosAtingidos = Physics.OverlapSphere(transform.position, raioExplosao);

        foreach (Collider obj in objetosAtingidos)
        {
            // 3. Aplicar dano se o objeto tiver o script de Saude
            PlayerHealth s = obj.GetComponent<PlayerHealth>();
            if (s != null && obj.CompareTag("Player")) // Garante que atinge o Player
            {
                s.TakeDamage(danoExplosao);
            }
        }

        // 4. Destruir a bomba
        Destroy(gameObject);
    }

    // Para visualizar o raio da explosão no editor
    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, raioExplosao);
    }
}
