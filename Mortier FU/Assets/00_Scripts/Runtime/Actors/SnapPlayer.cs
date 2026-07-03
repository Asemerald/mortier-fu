using UnityEngine;

namespace MortierFu
{
    public class SnapPlayer : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            if (other.transform.parent.parent.GetComponent<PlayerCharacter>() != null)
            {
                other.transform.parent.parent.SetParent(transform);
            }
        }
    
        private void OnTriggerExit(Collider other)
        {
            if (other.transform.parent.parent.GetComponent<PlayerCharacter>() != null)
            {
                other.transform.parent.parent.SetParent(null);
            }
            
        }
    }   
}
