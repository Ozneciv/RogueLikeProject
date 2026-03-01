    using UnityEngine;

    public class DetectorDoItem : MonoBehaviour
    {
        public ItemCollectable collectable;
        public GameObject glowObject;


    //public Interactable item;
    private bool playerPerto = false;

        void Awake()
        {
            if (collectable == null)
                collectable = GetComponentInParent<ItemCollectable>();

            if (glowObject != null)
                glowObject.SetActive(false);
    }

        void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;

            playerPerto = true;

            if (glowObject != null)
                glowObject.SetActive(true);


        if (collectable.PodeColetar())
                Debug.Log("Pressione F para coletar");
                //ativar press F
            else
                Debug.Log("Item trancado. Limpe a sala.");
                //stivar ui de item trancado
    }

    void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                playerPerto = false;
            }

            if (glowObject != null)
                glowObject.SetActive(false);
        }

        void Update()
        {
            if (playerPerto && Input.GetKeyDown(KeyCode.F))
            {
                if (!collectable.PodeColetar())
                {
                    Debug.Log("Ainda não pode coletar.");
                    return;
                }

                CatalogoManager.instancia.Catalogar(collectable.interactable);
                Destroy(gameObject);

                //CatalogoManager.instancia.Catalogar(item);
                Debug.Log("Item catalogado: " + collectable.interactable.objetoNome);
            }
        }
    }
