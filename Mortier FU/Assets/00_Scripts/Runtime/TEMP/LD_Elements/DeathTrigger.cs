using UnityEngine;
using MortierFu;

public class DeathTrigger : MonoBehaviour
{
   private void OnTriggerEnter(Collider other)
   {
      var rb = other.attachedRigidbody;
      if (rb != null && rb.TryGetComponent(out PlayerCharacter character))
      {
         character.Health.TakeLethalDamage(gameObject);
      }
   }
}
