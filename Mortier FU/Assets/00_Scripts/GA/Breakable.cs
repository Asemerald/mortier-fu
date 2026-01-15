using Unity.VisualScripting;
using UnityEngine;

namespace MortierFu
{
    public class Breakable : MonoBehaviour
    {
        [SerializeField] GameObject SM_Box:
        [SerializeField] GameObject SM_BoxDestroy;

        BoxCollider bc;

        private void Awake()
        {
            SM_Box.SetActive(true);
            SM_BoxDestroy.SetActive(false);

            bc = GetComponent<BoxCollider>();

        }

        private void OnMouseDown()
        {
            Break();
        }

       
        {
            )
        }




    }
}
