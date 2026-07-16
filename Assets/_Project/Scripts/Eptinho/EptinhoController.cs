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
        SpawnEptinhoPhysicalModel();
    }

    private void SpawnEptinhoPhysicalModel()
    {
        // Só spawna se estivermos na Base onde a mesa de crafting existe
        GameObject table = GameObject.Find("crafting table");
        if (table != null)
        {
            GameObject prefab = Resources.Load<GameObject>("EptinhoModel");
            if (prefab != null)
            {
                // Posiciona o Eptinho flutuando ligeiramente perto da mesa de crafting
                Vector3 spawnPos = table.transform.position + new Vector3(-1.8f, 1.3f, 1.2f);
                GameObject eptinho = Instantiate(prefab, spawnPos, Quaternion.Euler(0f, 135f, 0f));
                eptinho.name = "EptinhoOracle";
                
                // Configura o componente Interactable
                Interactable interactable = eptinho.GetComponent<Interactable>();
                if (interactable == null)
                {
                    interactable = eptinho.AddComponent<Interactable>();
                }
                interactable.objetoNome = "Eptinho";
                interactable.descricao = "Eptinho, o Oraculo. Pressione F para interagir.";
                
                // Configura o Trigger de interação
                SphereCollider trigger = eptinho.GetComponent<SphereCollider>();
                if (trigger == null)
                {
                    trigger = eptinho.AddComponent<SphereCollider>();
                }
                trigger.isTrigger = true;
                trigger.radius = 1.5f;
                
                Debug.Log("[EPTINHO] Modelo fisico do Oraculo spawnado perto da mesa de crafting na Base.");
            }
            else
            {
                Debug.LogWarning("[EPTINHO] Prefab EptinhoModel nao encontrado na pasta Resources.");
            }
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            MenuCanvas.SetActive(!MenuCanvas.activeSelf);
            HUDCanvas.SetActive(!HUDCanvas.activeSelf);
        }
    }

    public void AbrirMenuDoObjeto(Interactable obj)
    {
        objetoAtual = obj;

        // Abre menu e fecha HUD
        MenuCanvas.SetActive(true);
        HUDCanvas.SetActive(false);

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
