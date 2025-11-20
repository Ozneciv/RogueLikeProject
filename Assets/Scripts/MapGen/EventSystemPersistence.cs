using UnityEngine;

public class EventSystemPersistence : MonoBehaviour
{
    // A trava de Singleton para o EventSystem
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
            Destroy(this.gameObject);
        }
    }
}