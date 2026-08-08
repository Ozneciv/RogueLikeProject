using UnityEngine;
using System.Collections;

/// <summary>
/// Controlador do Casulo de Cristal da Fase 1 do Boss.
/// 
/// FUNCIONALIDADES:
///   1. Cria/Ativa um escudo de cristal envolvente em volta do Boss na Fase 1.
///   2. O Boss fica passivo no centro enquanto o Casulo protege ele.
///   3. A cada HIT que o jogador dá no Casulo, dispara um pulso de KNOCKBACK que empurra o player para trás.
///   4. Quando o Casulo é destruído (HP zerado):
///      - Purga (mata) todos os mobs restantes na sala instantaneamente.
///      - Destrói o escudo de cristal com efeito visual de rachadura/fragmentos.
///      - Libera o Boss para a próxima etapa do combate.
/// </summary>
[RequireComponent(typeof(BossController))]
public class BossPhase1Cocoon : MonoBehaviour
{
    [Header("🛡️ Configurações do Casulo")]
    [Tooltip("Objeto 3D do Casulo (opcional). Se nulo, o script cria uma cúpula cristalina automaticamente.")]
    public GameObject customCocoonMesh;

    [Tooltip("Vida total do Casulo de Cristal na Fase 1.")]
    public int cocoonMaxHealth = 300;

    [Tooltip("Força de Knockback aplicada no jogador a cada golpe no Casulo.")]
    public float knockbackForceOnHit = 14.0f;

    [Tooltip("Elevação da força de repulsão.")]
    public float knockbackUpwardForce = 0.3f;

    [Header("✨ Efeitos Visuais & Áudio")]
    public Color cocoonColor = new Color(0.2f, 0.8f, 1.0f, 0.45f); // Azul Cristalino Transparente
    public GameObject cocoonShatterVFX;

    // Estado Interno
    private int currentCocoonHealth;
    private bool isCocoonActive = false;
    private GameObject activeCocoonObject;
    private BossController bossController;
    private DummyHealth dummyHealth;

    void Awake()
    {
        bossController = GetComponent<BossController>();
        dummyHealth = GetComponent<DummyHealth>();
        currentCocoonHealth = cocoonMaxHealth;
    }

    void OnEnable()
    {
        BossEvents.OnPhaseChanged += HandlePhaseChange;
        if (dummyHealth != null)
        {
            dummyHealth.OnDamageTaken += HandleDamageTaken;
        }
    }

    void OnDisable()
    {
        BossEvents.OnPhaseChanged -= HandlePhaseChange;
        if (dummyHealth != null)
        {
            dummyHealth.OnDamageTaken -= HandleDamageTaken;
        }
    }

    private void HandlePhaseChange(int newPhase)
    {
        if (newPhase == 1)
        {
            ActivateCocoon();
        }
        else
        {
            DeactivateCocoon(false);
        }
    }

    /// <summary>
    /// Ativa o Casulo da Fase 1.
    /// </summary>
    public void ActivateCocoon()
    {
        if (isCocoonActive) return;

        isCocoonActive = true;
        currentCocoonHealth = cocoonMaxHealth;

        Debug.Log("🛡️ [CASULO] Ativando o Casulo de Cristal da Fase 1!");

        // Congela o Boss no lugar durante a fase do Casulo
        if (bossController != null)
        {
            bossController.OverrideMovement = true;
        }

        // Se já tiver uma mesh providenciada
        if (customCocoonMesh != null)
        {
            activeCocoonObject = customCocoonMesh;
            activeCocoonObject.SetActive(true);
        }
        else
        {
            // Cria uma Cúpula Cristalina Transparente automática em volta do Boss
            CreateProceduralCocoonSphere();
        }
    }

    private void CreateProceduralCocoonSphere()
    {
        if (activeCocoonObject != null) Destroy(activeCocoonObject);

        activeCocoonObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        activeCocoonObject.name = "CrystalCocoonShield";
        activeCocoonObject.transform.SetParent(transform, false);
        activeCocoonObject.transform.localPosition = Vector3.up * 1.2f;
        activeCocoonObject.transform.localScale = new Vector3(3.6f, 3.6f, 3.6f);

        // Remove collider para não interferir na física do player (o dano vai pro DummyHealth)
        Destroy(activeCocoonObject.GetComponent<Collider>());

        MeshRenderer mr = activeCocoonObject.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default"));
            mat.color = cocoonColor;
            if (mat.HasProperty("_Cull")) mat.SetInt("_Cull", 0);
            mr.material = mat;
        }
    }

    /// <summary>
    /// Evento disparado a cada HIT que o Boss toma enquanto o Casulo estiver ativo.
    /// </summary>
    private void HandleDamageTaken(int damage)
    {
        if (!isCocoonActive) return;

        currentCocoonHealth -= damage;
        Debug.Log($"🛡️ [CASULO] Dano recebido no Casulo: -{damage}. HP Restante do Casulo: {currentCocoonHealth}/{cocoonMaxHealth}");

        // 1. KNOCKBACK PULSE NO PLAYER
        ApplyKnockbackToPlayer();

        // 2. VERIFICA SE O CASULO QUEBROU
        if (currentCocoonHealth <= 0)
        {
            BreakCocoon();
        }
    }

    private void ApplyKnockbackToPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        Rigidbody playerRb = player.GetComponent<Rigidbody>() ?? player.GetComponentInParent<Rigidbody>();
        if (playerRb != null)
        {
            Vector3 pushDir = (player.transform.position - transform.position).normalized;
            pushDir.y = knockbackUpwardForce;
            playerRb.AddForce(pushDir * knockbackForceOnHit, ForceMode.Impulse);

            Debug.Log("💥 [CASULO] Knockback disparado contra o jogador!");
        }
    }

    /// <summary>
    /// Quebra o Casulo: mata todos os mobs da sala e libera o Boss!
    /// </summary>
    private void BreakCocoon()
    {
        Debug.Log("💥 [CASULO] O CASULO DE CRISTAL FOI QUEBRADO! Purificando a sala e liberando o Boss...");

        // 1. Purga de Mobs (Mata todos os inimigos da sala)
        PurgeRoomEnemies();

        // 2. Efeito Visual de Morte/Fragmentos do Casulo
        if (cocoonShatterVFX != null)
        {
            Instantiate(cocoonShatterVFX, transform.position + Vector3.up * 1.2f, Quaternion.identity);
        }

        // 3. Desativa o Casulo e libera movimento
        DeactivateCocoon(true);
    }

    private void PurgeRoomEnemies()
    {
        int count = 0;

        // Mata todos os DummyHealth da sala (exceto o próprio Boss)
        DummyHealth[] enemies = Object.FindObjectsByType<DummyHealth>(FindObjectsSortMode.None);
        foreach (var enemy in enemies)
        {
            if (enemy != null && enemy.gameObject != gameObject && !enemy.transform.IsChildOf(transform))
            {
                enemy.isInvulnerable = false;
                enemy.TakeDamage(99999);
                count++;
            }
        }

        // Mata todos os ShardSwarms
        ShardSwarmHealth[] swarms = Object.FindObjectsByType<ShardSwarmHealth>(FindObjectsSortMode.None);
        foreach (var swarm in swarms)
        {
            if (swarm != null)
            {
                swarm.isInvulnerable = false;
                swarm.SetHealth(0);
                count++;
            }
        }

        Debug.Log($"✨ [PURGA DO CASULO] {count} inimigos foram purgados da arena ao quebrar o Casulo!");
    }

    private void DeactivateCocoon(bool broken)
    {
        isCocoonActive = false;

        if (activeCocoonObject != null)
        {
            if (customCocoonMesh != null && activeCocoonObject == customCocoonMesh)
            {
                activeCocoonObject.SetActive(false);
            }
            else
            {
                Destroy(activeCocoonObject);
            }
        }

        if (bossController != null)
        {
            bossController.OverrideMovement = false;
        }
    }
}
