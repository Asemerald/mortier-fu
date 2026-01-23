using UnityEngine;
using FMOD;
using FMOD.Studio;
using FMODUnity;

namespace MortierFu
{
    [CreateAssetMenu(fileName = "FMODEvents", menuName = "Mortier Fu/FMOD")]
    public class FMODEventsSO : ScriptableObject
    {
        #region EVENTS REFERENCES
        
        [field: SerializeField] public EventReference SFX_Player_Stun { get; private set; }
        [field: SerializeField] public EventReference SFX_Player_Fall { get; private set; }
        [field: SerializeField] public EventReference SFX_Player_Death { get; private set; }
        [field: SerializeField] public EventReference SFX_Player_CarCrash { get; private set; }
        [field: SerializeField] public EventReference SFX_Player_Footsteps { get; private set; }
        [field: SerializeField] public EventReference SFX_Player_Summon { get; private set; }
        
        [field: SerializeField] public EventReference SFX_Mortar_Shot { get; private set; }
        [field: SerializeField] public EventReference SFX_Mortar_ImpactNone { get; private set; }
        [field: SerializeField] public EventReference SFX_Mortar_ImpactProps { get; private set; }
        [field: SerializeField] public EventReference SFX_Mortar_ImpactPlayer { get; private set; }
        [field: SerializeField] public EventReference SFX_Mortar_Cant { get; private set; }
        [field: SerializeField] public EventReference SFX_Mortar_ReloadComplete { get; private set; }
        [field: SerializeField] public EventReference SFX_Mortar_Water { get; private set; }
        
        [field: SerializeField] public EventReference SFX_Strike_Dash { get; private set; }
        [field: SerializeField] public EventReference SFX_Strike_Cant { get; private set; }
        [field: SerializeField] public EventReference SFX_Strike_Knockback { get; private set; }
        [field: SerializeField] public EventReference SFX_Misc_Break { get; private set; }
        
        [field: SerializeField] public EventReference SFX_Augment_Grab { get; private set; }
        [field: SerializeField] public EventReference SFX_Augment_Bounce { get; private set; }
        [field: SerializeField] public EventReference SFX_Augment_Buff { get; private set; }
        [field: SerializeField] public EventReference SFX_Augment_Showcase { get; private set; }
        [field: SerializeField] public EventReference SFX_Augment_ToWorld { get; private set; }
        [field: SerializeField] public EventReference SFX_Augment_Flip { get; private set; }
        [field: SerializeField] public EventReference SFX_Augment_Pop { get; private set; }
        [field: SerializeField] public EventReference SFX_Augment_NoPick { get; private set; }
        
        [field: SerializeField] public EventReference SFX_GameplayUI_EndRace { get; private set; }
        [field: SerializeField] public EventReference SFX_GameplayUI_CountdownNumber { get; private set; }
        [field: SerializeField] public EventReference SFX_GameplayUI_CountdownGo { get; private set; }
        [field: SerializeField] public EventReference SFX_GameplayUI_MatchPoint { get; private set; }
        [field: SerializeField] public EventReference SFX_GameplayUI_NewLeader { get; private set; }
        [field: SerializeField] public EventReference SFX_GameplayUI_ScoreIncrease { get; private set; }
        [field: SerializeField] public EventReference SFX_GameplayUI_Victory { get; private set; }
        
        
        [field: SerializeField] public EventReference SFX_UI_Navigate { get; private set; }
        [field: SerializeField] public EventReference SFX_UI_Select { get; private set; }
        [field: SerializeField] public EventReference SFX_UI_Return { get; private set; }
        [field: SerializeField] public EventReference SFX_UI_Tick { get; private set; }
        [field: SerializeField] public EventReference SFX_UI_Slider { get; private set; }
        [field: SerializeField] public EventReference SFX_UI_Join { get; private set; }
        [field: SerializeField] public EventReference SFX_UI_ChangeSkin { get; private set; }
        [field: SerializeField] public EventReference SFX_UI_Ready { get; private set; }
        [field: SerializeField] public EventReference SFX_UI_Pause { get; private set; }
        
        [field: SerializeField] public EventReference SFX_TransitionIn { get; private set; }
        [field: SerializeField] public EventReference SFX_TransitionOut { get; private set; }
        
        
        [field: SerializeField] public EventReference MUS_MainMenu { get; private set; }
        [field: SerializeField] public EventReference MUS_Gameplay { get; private set; }
        [field: SerializeField] public EventReference MUS_Victory { get; private set; }
        
        [field: SerializeField] public EventReference AMBI_Day { get; private set; }
        [field: SerializeField] public EventReference AMBI_Night { get; private set; }
        [field: SerializeField] public EventReference AMBI_Tuktuk_Drift { get; private set; }

        #endregion
        
        //ANIMATION CURVES
        [Header("Bombshell")]
        public AnimationCurve rangeCurve;
        public AnimationCurve damageCurve;
    }
}
