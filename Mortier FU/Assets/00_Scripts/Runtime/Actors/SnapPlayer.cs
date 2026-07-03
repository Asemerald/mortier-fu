using System;
using UnityEngine;

namespace MortierFu
{
    public class SnapPlayer : MonoBehaviour
    {
        private void OnTriggerStay(Collider other)
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
