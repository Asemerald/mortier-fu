using UnityEngine;

namespace MortierFu
{
    public class SnapPlayer : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent(out PlayerCharacter character))
            {
                character.gameObject.transform.SetParent(gameObject.transform.parent.gameObject.transform);
            }
        }
    
        private void OnTriggerExit(Collider other)
        {
            if (other.TryGetComponent(out PlayerCharacter character))
            {
                character.gameObject.transform.SetParent(null);
            }
        }
    }   
}
