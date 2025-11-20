using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RoomController : MonoBehaviour
{
    [Header("Configuração do Encontro")]
    [Tooltip("Marque esta caixa se esta for a sala inicial (segura)")]
    public bool isSafeRoom = false; // Marque isso no seu 'startRoomPrefab'

    [Tooltip("Todos os inimigos que devem ser instanciados nesta sala.")]
    public GameObject[] enemyPrefabs; // Arraste os prefabs dos inimigos (ex: MagicStone) aqui

    [Tooltip("Pontos vazios que marcam onde os inimigos podem nascer.")]
    public Transform[] spawnPoints;

    [Tooltip("Portas ou barreiras a serem ativadas para trancar a sala.")]
    public GameObject[] doors; // Arraste os objetos das portas/barreiras aqui

    private bool hasBeenTriggered = false; // Garante que a sala só ative uma vez

    // Esta é a função que "começa o processo"
    private void OnTriggerEnter(Collider other)
    {
        // Se o jogador entrar, se a sala não for segura, e se ainda não foi ativada
        if (other.CompareTag("Player") && !isSafeRoom && !hasBeenTriggered)
        {
            hasBeenTriggered = true;
            Debug.Log("Jogador entrou na sala! INICIANDO PROCESSO...");

            // 1. Tranca as portas
            LockDoors(true);

            // 2. Instancia os inimigos
            SpawnEnemies();
        }
    }

    void LockDoors(bool shouldLock)
    {
        foreach (GameObject door in doors)
        {
            door.SetActive(shouldLock);
        }
    }

    void SpawnEnemies()
    {
        if (enemyPrefabs.Length == 0 || spawnPoints.Length == 0)
        {
            Debug.LogWarning("Sala " + gameObject.name + " não tem inimigos ou spawn points definidos.");
            return;
        }

        foreach (GameObject enemyPrefab in enemyPrefabs)
        {
            // Pega um ponto de spawn aleatório
            Transform randomSpawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
            
            // Instancia o inimigo
            Instantiate(enemyPrefab, randomSpawnPoint.position, randomSpawnPoint.rotation);
        }
    }
}