using UnityEngine;
using UnityEngine.EventSystems;

public class EventSystemPersistence : MonoBehaviour
{
    public static EventSystemPersistence instance;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
        else
        {
            // --- CORREÇÃO DO ERRO ---
            // Desativa o componente EventSystem IMEDIATAMENTE para a Unity não reclamar
            // que existem dois ativos ao mesmo tempo.
            var system = GetComponent<EventSystem>();
            if (system != null) system.enabled = false;
            
            Destroy(this.gameObject);
        }
    }
}