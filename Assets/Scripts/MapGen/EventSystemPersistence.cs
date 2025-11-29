using UnityEngine;
using UnityEngine.EventSystems;

public class EventSystemPersistence : MonoBehaviour
{
    public static EventSystemPersistence instance;

    void Awake()
    {
        // Referência ao componente EventSystem neste objeto
        EventSystem myEventSystem = GetComponent<EventSystem>();

        if (instance == null)
        {
            // Se eu sou o primeiro, eu assumo o controle
            instance = this;
            DontDestroyOnLoad(this.gameObject);
            
            // LIGA o EventSystem (caso esteja desligado no Inspector)
            if (myEventSystem != null) myEventSystem.enabled = true;
        }
        else
        {
            // Se já existe um chefe (instance), eu sou uma cópia desnecessária.
            
            // GARANTE que eu fique desligado para não dar erro
            if (myEventSystem != null) myEventSystem.enabled = false;
            
            // Me destruo
            Destroy(this.gameObject);
        }
    }
}