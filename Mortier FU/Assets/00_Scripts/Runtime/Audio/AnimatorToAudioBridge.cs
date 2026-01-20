using MortierFu;
using UnityEngine;

public class AnimatorToAudioBridge : MonoBehaviour
{
    public void PlayFootsteps()
    {
        AudioService.PlayOneShot(AudioService.FMODEvents.SFX_Player_Footsteps, transform.position);
    }
}
