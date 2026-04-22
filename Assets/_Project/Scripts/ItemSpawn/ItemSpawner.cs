using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class ItemSpawnData
{
    public GameObject prefab;
    [Range(0f, 1f)]
    public float chance;
}

public class ItemSpawner : MonoBehaviour
{
    [Header("Configuração Geral")]
    [Range(0f, 1f)]
    public float spawnChancePerPoint = 0.5f;

    [Header("Itens Coletáveis")]
    public List<ItemSpawnData> collectableItems;

    [Header("Decoração do Cenário")]
    public List<ItemSpawnData> decorationItems;

    [Header("Armadilhas")]
    public List<ItemSpawnData> trapItems;

    GameObject GetRandomItem(List<ItemSpawnData> list)
    {
        if (list == null || list.Count == 0)
            return null;

        float total = list.Sum(i => i.chance);
        float random = Random.value * total;

        float current = 0f;
        foreach (var item in list)
        {
            current += item.chance;
            if (random <= current)
                return item.prefab;
        }

        return null;
    }

    public void SpawnItems()
    {
        var spawnPoints = GameObject.FindGameObjectsWithTag("SpawnPoint");

        foreach (var point in spawnPoints)
        {
            if (Random.value > spawnChancePerPoint)
                continue;

            // Junta tudo numa lista temporária
            List<ItemSpawnData> allItems = new();
            allItems.AddRange(collectableItems);
            allItems.AddRange(decorationItems);
            allItems.AddRange(trapItems);

            GameObject item = GetRandomItem(allItems);

            if (item != null)
                Instantiate(item, point.transform.position, Quaternion.identity);
        }
    }
}






//using System.Collections.Generic;
//using System.Linq;
//using UnityEngine;

//[System.Serializable]
//public class ItemSpawnData
//{
//    public GameObject itemPrefab;
//    [Range(0f, 1f)]
//    public float chance;
//}


//public class ItemSpawner : MonoBehaviour
//{
//    public float spawnChancePerPoint = 0.5f;
//    public List<ItemSpawnData> possibleItems;


//    GameObject GetRandomItem()
//    {
//        float total = possibleItems.Sum(i => i.chance);
//        float random = Random.value * total;

//        float current = 0f;
//        foreach (var item in possibleItems)
//        {
//            current += item.chance;
//            if (random <= current)
//                return item.itemPrefab;
//        }

//        return null;
//    }

//    public void SpawnItems()
//    {
//        var spawnPoints = GameObject.FindGameObjectsWithTag("ItemSpawnPoint");

//        foreach (var point in spawnPoints)
//        {
//            if (Random.value > spawnChancePerPoint)
//                continue;

//            GameObject item = GetRandomItem();
//            if (item != null)
//                Instantiate(item, point.transform.position, Quaternion.identity);
//        }
//    }
//}
