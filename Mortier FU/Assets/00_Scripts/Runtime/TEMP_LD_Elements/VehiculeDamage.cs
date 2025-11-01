using System;
using MortierFu;
using UnityEngine;

public class VehiculeDamage : MonoBehaviour
{
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.TryGetComponent(out PlayerCharacter character))
        {
            character.Health.TakeDamage(999);
        }
    }
}
