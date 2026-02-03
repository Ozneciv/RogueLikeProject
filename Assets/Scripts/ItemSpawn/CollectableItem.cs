// SCRIPT ERRADO E COM BUGS

//using UnityEngine;

//public class CollectableItem : MonoBehaviour
//{
//    private bool canCollect = false;

//    public void EnableCollection()
//    {
//        canCollect = true;
//    }

//    void OnTriggerEnter(Collider other)
//    {
//        if (!canCollect)
//        {
//            Debug.Log("Item coletado!");
//            return;
//        }
//        if (!other.CompareTag("Player")) return;

//        Collect();
//    }

//    void Collect()
//    {
//        Debug.Log("Item coletado!");
//        Destroy(gameObject);
//    }
//}

