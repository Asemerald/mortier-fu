using UnityEngine;
using MortierFu;

public class DeathTrigger : MonoBehaviour
{
   private void OnTriggerEnter(Collider other)
   {
      if (other.GetComponentInParent<PlayerCharacter>().TryGetComponent(out PlayerCharacter character))
      {
         character.Health.TakeLethalDamage(gameObject);
         Debug.Log("Character health taken: " + character.Health);
      }
   }
}
