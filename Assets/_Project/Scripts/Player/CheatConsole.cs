using UnityEngine;
using TMPro; // Usado para acessar campos de texto moderno da interface

public class CheatConsole : MonoBehaviour
{
    [Header("UI do Console")]
    [Tooltip("Arraste o objeto CheatConsole_Input aqui do seu Canvas")]
    public TMP_InputField consoleInput; 
    
    [Header("Referências do Player")]
    [Tooltip("O Animator do personagem que onde vai tocar o breakdance")]
    public Animator playerAnimator; 
    
    // Variável para sabermos se o console tá aberto ou fechado
    private bool isConsoleActive = false;

    // Scripts para congelar o jogador
    private PlayerM movementScript;
    private PrimaryAttackKnife attackScript;

    void Start()
    {
        movementScript = GetComponent<PlayerM>();
        attackScript = GetComponent<PrimaryAttackKnife>();

        if (playerAnimator == null)
        {
            playerAnimator = GetComponentInChildren<Animator>();
        }

        if (consoleInput == null)
        {
            FindConsoleInput();
        }

        // Garante que comece invisível
        if (consoleInput != null)
        {
            consoleInput.gameObject.SetActive(false);
        }
    }

    private void FindConsoleInput()
    {
        TMP_InputField[] inputs = Resources.FindObjectsOfTypeAll<TMP_InputField>();
        foreach (var input in inputs)
        {
            if (input.name == "CheatConsole_Input" && input.gameObject.scene.isLoaded)
            {
                consoleInput = input;
                break;
            }
        }

        if (consoleInput == null)
        {
            Debug.LogWarning("🔍 CHEAT: Não foi possível encontrar 'CheatConsole_Input' na cena ativa atual.");
        }
    }

    void Update()
    {
        // Ligar o console
        if (Input.GetKeyDown(KeyCode.Slash) || Input.GetKeyDown(KeyCode.KeypadDivide))
        {
            Debug.Log("🔍 CHEAT: Tecla Barra '/' pressionada!");
            if (consoleInput == null)
            {
                FindConsoleInput();
            }
            if (!isConsoleActive) ToggleConsole();
            return;
        }

        // Se o console está aberto e ele dá um Enter
        if (isConsoleActive && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)))
        {
            Debug.Log("🔍 CHEAT: Tecla 'Enter' pressionada com o painel aberto!");
            // O próprio código aciona a função sem você precisar clicar no menu chato do Unity
            if (consoleInput != null)
            {
                Debug.Log($"🔍 CHEAT: A caixa de texto no momento do clique estava com o valor: '{consoleInput.text}'");
                CheckConsoleCommand(consoleInput.text);
            }
            else
            {
                Debug.LogError("🚨 CHEAT ERROR: O componente 'consoleInput' está NULL! Você não arrastou ele no Inspector.");
            }
        }
    }

    void ToggleConsole()
    {
        if (consoleInput == null) return;

        isConsoleActive = !isConsoleActive; // Inverte o estado
        
        // Ativa ou desativa a telinha do console
        consoleInput.gameObject.SetActive(isConsoleActive);

        // Congela/Descongela o movimento e ataque
        if (movementScript != null) movementScript.enabled = !isConsoleActive;
        if (attackScript != null) attackScript.enabled = !isConsoleActive;
        // Congela fisicamente (zera velocidade) para ele não deslizar se abriu o console correndo
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null && isConsoleActive) rb.linearVelocity = Vector3.zero;
        
        if (isConsoleActive)
        {
            // Limpa o texto da última tentativa
            consoleInput.text = ""; 
            // Já foca o cursor pra você não precisar clicar com o mouse lá
            consoleInput.ActivateInputField();
            Debug.Log("🔍 CHEAT: Console ABERTO eocado na tela.");
        }
        else
        {
            Debug.Log("🔍 CHEAT: Console FECHADO.");
        }
    }

    // Essa função DEVE ser conectada no "On End Edit" do seu componente TMP_InputField no Editor do Unity
    public void CheckConsoleCommand(string typedText)
    {
        // ---------------------------------------------------------
        // MAGIA NEGRA PARA IGNORAR O BUG DO INSPECTOR DO UNITY:
        // Se você sem querer linkou o botão no "Static Parameters", o Unity passa texto vazio.
        // Então nós ignoramos o que o Unity passou e arrancamos o texto à força de dentro da caixa!
        string textoReal = "";
        if (consoleInput != null) textoReal = consoleInput.text;
        // ---------------------------------------------------------

        // O jogador pode bater o Enter vazio ou tentar fechar
        if (string.IsNullOrWhiteSpace(textoReal)) 
        {
            Debug.LogWarning("🔍 CHEAT: A caixa de texto foi lida como completamente vazia.");
            ToggleConsole();
            return;
        }

        // Limpa possíveis carácteres sujos como a própria barra '/' que pode vazar pro texto
        string command = textoReal.Replace("/", "").Trim();
        Debug.Log($"🔍 CHEAT: O script limpou o texto recebido e tentará bater esta palavra com a senha: '{command}'");

        // Faz a checagem ignorando completamente se é maiúsculo ou minúsculo
        if (string.Equals(command, "EPTA", System.StringComparison.OrdinalIgnoreCase))
        {
            Debug.Log("💻 CHEAT APROVADO: Comando EPTA Recebido! Iniciando Breakdance...");
            
            if (playerAnimator != null)
            {
                // Dispara a trigger lá pro seu Animator
                playerAnimator.SetTrigger("Breakdance");
                Debug.Log("💻 CHEAT: Gatilho 'Breakdance' enviado com sucesso para o Animator!");
            }
            else
            {
                Debug.LogError("🚨 CHEAT ERROR: O comando rodou, mas você não arrastou o Animator para o script CheatConsole!");
            }
        }
        else if (string.Equals(command, "killall", System.StringComparison.OrdinalIgnoreCase))
        {
            Debug.Log("💻 CHEAT APROVADO: Comando KILLALL Recebido! Eliminando todos os inimigos...");

            // Busca e mata todos os DummyHealth ativos
            DummyHealth[] dummies = Object.FindObjectsByType<DummyHealth>(FindObjectsSortMode.None);
            int count = 0;
            foreach (DummyHealth dummy in dummies)
            {
                if (dummy != null)
                {
                    // Evita matar o jogador por engano se o script estiver no jogador (não está, mas por segurança)
                    if (dummy.CompareTag("Player")) continue;
                    
                    dummy.isInvulnerable = false;
                    dummy.TakeDamage(999999);
                    count++;
                }
            }

            // Busca e mata todos os ShardSwarmHealth ativos
            ShardSwarmHealth[] swarms = Object.FindObjectsByType<ShardSwarmHealth>(FindObjectsSortMode.None);
            foreach (ShardSwarmHealth swarm in swarms)
            {
                if (swarm != null)
                {
                    swarm.isInvulnerable = false;
                    swarm.SetHealth(0);
                    count++;
                }
            }

            Debug.Log($"💻 CHEAT: {count} inimigos eliminados com sucesso.");
        }
        else
        {
            Debug.LogWarning($"💻 CHEAT RECUSADO: A palavra '{command}' não é igual a EPTA ou killall.");
        }

        // Depois de tentar o cheat, fecha o console pro jogo voltar ao normal
        ToggleConsole(); 
    }
}
