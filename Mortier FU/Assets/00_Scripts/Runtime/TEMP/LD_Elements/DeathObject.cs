using UnityEngine;
using MortierFu;

public class DeathObject : MonoBehaviour
{
    private void OnCollisionEnter(Collision other)
    {
        if (other.rigidbody != null && other.rigidbody.TryGetComponent(out PlayerCharacter character))
        {
            character.Health.TakeLethalDamage(gameObject);
        }
    }
}