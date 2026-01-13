using UnityEngine;
using FMOD;
using FMOD.Studio;
using FMODUnity;

namespace MortierFu
{
    [CreateAssetMenu(fileName = "FMODEvents", menuName = "Mortier Fu/FMOD")]
    public class FMODEventsSO : ScriptableObject
    {
        [field: SerializeField] public EventReference SFX_Player_Stun { get; private set; }
        [field: SerializeField] public EventReference SFX_Player_Fall { get; private set; }
        [field: SerializeField] public EventReference SFX_Player_Death { get; private set; }
        
        [field: SerializeField] public EventReference SFX_Mortar_Shot { get; private set; }
        [field: SerializeField] public EventReference SFX_Mortar_ImpactNone { get; private set; }
        [field: SerializeField] public EventReference SFX_Mortar_ImpactProps { get; private set; }
        [field: SerializeField] public EventReference SFX_Mortar_ImpactPlayer { get; private set; }
        [field: SerializeField] public EventReference SFX_Mortar_ImpactKill { get; private set; }
        [field: SerializeField] public EventReference SFX_Mortar_Cant { get; private set; }
        [field: SerializeField] public EventReference SFX_Mortar_ReloadComplete { get; private set; }
        
        [field: SerializeField] public EventReference SFX_Strike_Dash { get; private set; }
        [field: SerializeField] public EventReference SFX_Strike_Cant { get; private set; }
        [field: SerializeField] public EventReference SFX_Strike_Knockback { get; private set; }
    }
}
