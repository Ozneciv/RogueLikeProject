using UnityEngine;
using TMPro;

/// <summary>
/// Gerencia a interação do jogador com o Eptinho Oráculo na Base.
/// Deve ser colocado no GameObject raiz do prefab Eptin.
/// Detecta quando o player entra no BoxCollider trigger filho chamado 'Trigger Menu'
/// e permite abrir o menu do Bestiário/Oráculo ao pressionar F.
/// </summary>
public class EptinhoOracleInteract : MonoBehaviour
{
    [Header("Referências")]
    [Tooltip("Label que aparece acima do Eptinho para indicar interação. Criado automaticamente se nulo.")]
    public GameObject presseFLabel;

    private bool playerNoPerto = false;
    private GameObject labelCriado;

    void Start()
    {
        // Cria um label 3D flutuando acima do Eptinho se não configurado
        if (presseFLabel == null)
        {
            labelCriado = new GameObject("EptinhoLabel");
            labelCriado.transform.SetParent(transform);
            labelCriado.transform.localPosition = new Vector3(0f, 2.5f, 0f);

            // Billboard para sempre olhar para a câmera
            labelCriado.AddComponent<EptinhoBillboard>();

            var canvas = labelCriado.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;

            RectTransform rt = labelCriado.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(2f, 0.5f);
            rt.localScale = new Vector3(0.01f, 0.01f, 0.01f);

            var go = new GameObject("Texto");
            go.transform.SetParent(labelCriado.transform, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = "[ F ] Falar com Eptinho";
            tmp.fontSize = 28;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.85f, 0.80f, 0.95f, 1f);
            var rt2 = go.GetComponent<RectTransform>();
            rt2.anchorMin = Vector2.zero;
            rt2.anchorMax = Vector2.one;
            rt2.offsetMin = Vector2.zero;
            rt2.offsetMax = Vector2.zero;

            presseFLabel = labelCriado;
        }

        presseFLabel.SetActive(false);
    }

    void Update()
    {
        if (playerNoPerto && Input.GetKeyDown(KeyCode.F))
        {
            AbrirMenuOraculo();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerNoPerto = true;
        if (presseFLabel != null) presseFLabel.SetActive(true);
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerNoPerto = false;
        if (presseFLabel != null) presseFLabel.SetActive(false);
    }

    private void AbrirMenuOraculo()
    {
        if (EptinhoMenuController.instancia != null)
        {
            EptinhoMenuController.instancia.AbrirMenu();
            Debug.Log("[EPTINHO ORACLE] Menu do Oráculo aberto!");
        }
        else
        {
            Debug.LogWarning("[EPTINHO ORACLE] EptinhoMenuController.instancia é nulo!");
        }
    }
}

/// <summary>Billboard simples para o label do Eptinho olhar para a câmera.</summary>
public class EptinhoBillboard : MonoBehaviour
{
    void LateUpdate()
    {
        if (Camera.main != null)
            transform.forward = Camera.main.transform.forward;
    }
}
