using UnityEngine;

/// <summary>
/// Coloque este script NO MESMO GameObject que tem o BoxCollider (IsTrigger = true).
/// Ele delega o evento OnTriggerEnter para o RoomController pai,
/// resolvendo o problema de propagação de trigger no Unity.
///
/// SETUP:
///   SpawnArea (filho da sala)
///    ├── BoxCollider  (IsTrigger = true)
///    └── RoomTriggerProxy (este script) ← arrasta o RoomController do root aqui
/// </summary>
public class RoomTriggerProxy : MonoBehaviour
{
    [Tooltip("Arraste aqui o RoomController da sala (normalmente o root do prefab).")]
    public RoomController roomController;

    private void Awake()
    {
        // Auto-busca no pai se não foi atribuído no Inspector
        if (roomController == null)
            roomController = GetComponentInParent<RoomController>();

        if (roomController == null)
            Debug.LogError($"[RoomTriggerProxy] '{gameObject.name}': RoomController não encontrado! " +
                           "Arraste o RoomController no campo do Inspector.");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (roomController != null)
            roomController.OnPlayerEnteredRoom(other);
    }
}
