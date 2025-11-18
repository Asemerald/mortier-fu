using System;
using MortierFu;
using UnityEngine;

public class DeathZone : MonoBehaviour
{
   private void OnTriggerEnter(Collider other)
   {
      if (other.TryGetComponent(out PlayerCharacter character))
      {
         character.Health.TakeDamage(999999999, gameObject);
      }
   }
}
