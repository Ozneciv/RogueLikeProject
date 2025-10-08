using System.Collections.Generic;
using UnityEngine;

public class RoomSpawner : MonoBehaviour
{
    [Header("Prefabs de Sala")]
    public GameObject startRoomPrefab;
    public GameObject[] roomPrefabs;

    [Header("Configuração")]
    public int numberOfRooms = 5;

    private List<GameObject> spawnedRooms = new List<GameObject>();
    private bool roomsGenerated = false;

    void Start()
    {
        GameObject startRoom = Instantiate(startRoomPrefab, Vector3.zero, Quaternion.identity);
        spawnedRooms.Add(startRoom);
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log("Colisão detectada com: " + other.name);
        if (other.CompareTag("Player"))
        {
            Debug.Log("Player entrou no trigger!");
        }
        // Verifica se o jogador entrou na área
        //if (!roomsGenerated && other.CompareTag("Player"))
        //{
        //    roomsGenerated = true;
        //    Debug.Log("Trigger ativada!");
        //    GenerateRooms();
        //}
        //teste
    }
    void GenerateRooms()
    {
        GameObject previousRoom = spawnedRooms[0];

        for (int i = 0; i < numberOfRooms; i++)
        {
            // Escolhe uma sala aleatória
            GameObject roomPrefab = roomPrefabs[Random.Range(0, roomPrefabs.Length)];

            // Pega o ExitPoint da sala anterior
            Transform previousExit = previousRoom.transform.Find("ExitPoint");
            if (previousExit == null)
            {
                Debug.LogError("Sala anterior não tem ExitPoint!");
                return;
            }

            // Pega o EntryPoint da nova sala
            Transform newRoomEntry = roomPrefab.transform.Find("EntryPoint");
            if (newRoomEntry == null)
            {
                Debug.LogError("Sala nova não tem EntryPoint!");
                return;
            }

            // Calcula a posição da nova sala para alinhar os pontos
            Vector3 spawnPosition = previousExit.position - (newRoomEntry.position - roomPrefab.transform.position);

            // Instancia a sala
            GameObject newRoom = Instantiate(roomPrefab, spawnPosition, Quaternion.identity);
            spawnedRooms.Add(newRoom);

            // Atualiza previousRoom para a próxima iteração
            previousRoom = newRoom;
        }
    }
}
