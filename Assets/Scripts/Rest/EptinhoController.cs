using System;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;

public class EptinhoController : MonoBehaviour
{
    public GameObject MenuCanvas;
    public GameObject HUDCanvas;

    void Start()
    {
        MenuCanvas.SetActive(false);
        Debug.Log("EptinhoController initialized. Press Tab to toggle the menu.");
    }

    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Tab))
        {
            MenuCanvas.SetActive(!MenuCanvas.activeSelf);
            HUDCanvas.SetActive(!HUDCanvas.activeSelf);
        }


    }
}
