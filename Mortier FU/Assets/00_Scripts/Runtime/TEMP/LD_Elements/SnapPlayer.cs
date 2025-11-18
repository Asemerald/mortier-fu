using UnityEngine;

namespace MortierFu
{
    public class SnapPlayer : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {   Debug.Log("1");
            if (other.TryGetComponent(out PlayerCharacter character))
            {
                Debug.Log("snaped");
                character.gameObject.transform.SetParent(gameObject.transform.parent.gameObject.transform);
            }
        }
    
        private void OnTriggerExit(Collider other)
        {
            Debug.Log("2");
            if (other.TryGetComponent(out PlayerCharacter character))
            {
                Debug.Log("Unsnaped");
                character.gameObject.transform.SetParent(null);
            }
        }
    }   
}
