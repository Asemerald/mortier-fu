using UnityEngine;
using MortierFu;

public class DeathObject : MonoBehaviour
{
    private ShakeService _shakeService;
    private void Start()
    {
        _shakeService = ServiceManager.Instance.Get<ShakeService>();
    }

    private void OnCollisionEnter(Collision other)
    {
        if (other.rigidbody != null && other.rigidbody.TryGetComponent(out PlayerCharacter character))
        {
            character.Health.TakeLethalDamage(gameObject);
            AudioService.PlayOneShot(AudioService.FMODEvents.SFX_Player_Fall, other.transform.position);
            _shakeService.ShakeController(character.Owner, ShakeService.ShakeType.MID);
        }
    }
}