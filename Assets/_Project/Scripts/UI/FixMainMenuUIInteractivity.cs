using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Script de Desbloqueio Geral de Cliques e Hovers do Main Menu.
/// Resolve automaticamente todos os motivos de a UI não responder:
/// 1. Desativa 'Raycast Target' na imagem de Vídeo (VideoDisplay) e fundos para não bloquear o mouse.
/// 2. Cria e ativa o EventSystem e StandaloneInputModule se faltarem na cena.
/// 3. Adiciona GraphicRaycaster no Canvas se estiver faltando.
/// 4. Garante que o MainMenuController esteja ativo e os ouvintes de clique conectados.
/// </summary>

[DefaultExecutionOrder(-100)]
public class FixMainMenuUIInteractivity : MonoBehaviour
{
    private void Awake()
    {
        UnlockAllInteractions();
    }

    private void Start()
    {
        UnlockAllInteractions();
    }

    private void OnEnable()
    {
        UnlockAllInteractions();
    }

    public void UnlockAllInteractions()
    {
        // 1. Destrava o cursor do mouse para navegação
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        Time.timeScale = 1f;

        // 2. Garante que o EventSystem existe na cena
        EventSystem es = Object.FindFirstObjectByType<EventSystem>(FindObjectsInactive.Include);
        if (es == null)
        {
            GameObject esGo = new GameObject("EventSystem_AutoCreated");
            es = esGo.AddComponent<EventSystem>();
            esGo.AddComponent<StandaloneInputModule>();
            Debug.Log("[UI FIX] EventSystem criado na cena para habilitar cliques e hover!");
        }
        else if (!es.gameObject.activeInHierarchy)
        {
            es.gameObject.SetActive(true);
        }

        // 3. Garante GraphicRaycaster no Canvas e insere MainMenuController
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Canvas c in canvases)
        {
            if (c != null)
            {
                if (!c.gameObject.activeSelf) c.gameObject.SetActive(true);
                c.enabled = true;

                GraphicRaycaster gr = c.GetComponent<GraphicRaycaster>();
                if (gr == null)
                {
                    gr = c.gameObject.AddComponent<GraphicRaycaster>();
                    Debug.Log($"[UI FIX] GraphicRaycaster adicionado ao Canvas '{c.name}'.");
                }
                gr.enabled = true;

                MainMenuController mmc = c.GetComponent<MainMenuController>();
                if (mmc == null)
                {
                    mmc = c.gameObject.AddComponent<MainMenuController>();
                }
                mmc.AutoConnectButtons();
            }
        }

        // 4. DESATIVA 'Raycast Target' na RawImage do Vídeo e telas de fundo (CRUCIAL!)
        RawImage[] rawImages = FindObjectsByType<RawImage>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (RawImage raw in rawImages)
        {
            if (raw != null)
            {
                raw.raycastTarget = false;
            }
        }

        // Desativa Raycast Target em Imagens de fundo puras (não-botões)
        Image[] allImages = FindObjectsByType<Image>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Image img in allImages)
        {
            if (img != null)
            {
                Button parentBtn = img.GetComponentInParent<Button>();
                if (parentBtn == null)
                {
                    img.raycastTarget = false;
                }
                else
                {
                    img.raycastTarget = true;
                }
            }
        }

        // 5. Garante Raycast Target = false em todos os textos TMP (evita bloqueios)
        TextMeshProUGUI[] allTexts = FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (TextMeshProUGUI txt in allTexts)
        {
            if (txt != null)
            {
                txt.raycastTarget = false;
            }
        }

        // 6. Garante que os Botões estão interativos e com targetGraphic válido
        Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Button b in buttons)
        {
            if (b != null)
            {
                b.interactable = true;
                Image bImg = b.GetComponent<Image>();
                if (bImg != null)
                {
                    b.targetGraphic = bImg;
                    bImg.raycastTarget = true;
                }
            }
        }

        Debug.Log($"[UI FIX] Desbloqueio e conexões concluídas! {buttons.Length} botão(ões) prontos para cliques.");
    }
}
