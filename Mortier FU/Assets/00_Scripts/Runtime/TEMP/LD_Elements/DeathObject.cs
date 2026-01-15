using UnityEngine;
using MortierFu;

public class DeathObject : MonoBehaviour
{
    private void OnCollisionEnter(Collision other)
    {
        if (other.rigidbody != null && other.rigidbody.TryGetComponent(out PlayerCharacter character))
        {
            character.Health.TakeLethalDamage(gameObject);
            AudioService.PlayOneShot(AudioService.FMODEvents.SFX_Player_Fall, other.transform.position);
        }
    }
}