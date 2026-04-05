using UnityEngine;

public class PlayerModelOffset : MonoBehaviour
{
    [Header("Correção de Afundamento (Animators)")]
    [Tooltip("Se ativado, impede que as animações empurrem o modelo (ou quadris) para dentro do chão.")]
    public bool lockYOffset = true;

    [Tooltip("Se precisar, pode desligar para permitir rotações do root da animação.")]
    public bool lockRootRotation = false;

    private float manualYOffset;
    private Quaternion manualRotation;

    void Start()
    {
        // Salva a altura exata que você configurou no "astronaut" no Editor do Unity
        manualYOffset = transform.localPosition.y;
        manualRotation = transform.localRotation;
    }

    void LateUpdate()
    {
        // Só rodamos no LateUpdate pois as animações atuam no Update normal.
        // Isso garante que vamos reescrever a posição *depois* da animação tentar afundar o boneco.
        
        if (lockYOffset)
        {
            Vector3 pos = transform.localPosition;
            pos.y = manualYOffset; // Volta para o Y que você deixou no Editor
            transform.localPosition = pos;
        }

        if (lockRootRotation)
        {
            transform.localRotation = manualRotation;
        }
    }
}
