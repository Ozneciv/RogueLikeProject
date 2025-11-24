using System;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;

public class EptinhoController : MonoBehaviour
{
    public GameObject MenuCanvas;
    public GameObject HUDCanvas;

    private Interactable objetoAtual;

    void Start()
    {
        MenuCanvas.SetActive(false);
        //Debug.Log("EptinhoController initialized. Press Tab to toggle the menu.");
    }

    //ABRR MENU COM TAB
    //void Update()
    //{
    //    if(Input.GetKeyDown(KeyCode.Tab))
    //    {
    //        MenuCanvas.SetActive(!MenuCanvas.activeSelf);
    //        HUDCanvas.SetActive(!HUDCanvas.activeSelf);
    //    }
    //}

    public void AbrirMenuDoObjeto(Interactable obj)
    {
        objetoAtual = obj;

        // Abre menu e fecha HUD
        MenuCanvas.SetActive(true);
        HUDCanvas.SetActive(false);

        // Aqui você atualiza o conteúdo do menu com base no objeto
        Debug.Log("Menu aberto para: " + objetoAtual.objetoNome);

        AtualizarUI(objetoAtual);
    }

    public void FecharMenu()
    {
        MenuCanvas.SetActive(false);
        HUDCanvas.SetActive(true);
        objetoAtual = null;
    }

    void AtualizarUI(Interactable obj)
    {
        //Inserir elementos do objeto no menu

        Debug.Log("Atualizando UI com dados do objeto: " + obj.objetoNome);
    }
}
