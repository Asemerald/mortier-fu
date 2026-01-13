using UnityEngine;
namespace MortierFu
{
    public class AGM_PerfectPush : AugmentBase
    {
        [System.Serializable]
        public struct Params
        {
            public AugmentStatMod OnSuccessfulPushMaxHealthMod;
        }
        
        private EventBinding<TriggerSuccessfulPush> _successfulPushBinding;
        private EventBinding<TriggerEndRound> _endRoundBinding;
        
        public AGM_PerfectPush(SO_Augment augmentData, PlayerCharacter owner, SO_AugmentDatabase db) : base(augmentData, owner, db)
        { }

        public override void Initialize()
        {
            _successfulPushBinding = new EventBinding<TriggerSuccessfulPush>(OnSuccessfulPush);
            EventBus<TriggerSuccessfulPush>.Register(_successfulPushBinding);

            _endRoundBinding = new EventBinding<TriggerEndRound>(OnTriggerEndRound);
            EventBus<TriggerEndRound>.Register(_endRoundBinding);
        }
        
        private void OnSuccessfulPush(TriggerSuccessfulPush evt)
        {
            // Owner is being pushed
            if (evt.Character == owner) return;
            // Source of the push is no player character
            if (evt.Source is not PlayerCharacter sourceCharacter) return;
            // Source of the push is not the owner of this augment
            if (sourceCharacter != owner) return;
         
            Debug.Log("AGM_PerfectPush: OnSuccessfulPush triggered: " + evt.Character.Owner.PlayerIndex + " was pushed by + " + sourceCharacter.Owner.PlayerIndex);
            
            stats.MaxHealth.AddModifier(db.PerfectPushParams.OnSuccessfulPushMaxHealthMod.ToMod(this));
        }

        private void OnTriggerEndRound(TriggerEndRound evt)
        {
            stats.MaxHealth.RemoveAllModifiersFromSource(this);
        }
        
        public override void Dispose()
        {
            EventBus<TriggerSuccessfulPush>.Deregister(_successfulPushBinding);
            EventBus<TriggerEndRound>.Deregister(_endRoundBinding);
            
            stats.MaxHealth.RemoveAllModifiersFromSource(this);
        }
    }
}