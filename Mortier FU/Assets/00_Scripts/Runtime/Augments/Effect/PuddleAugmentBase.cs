using System;
using UnityEngine;

namespace MortierFu
{
    public abstract class PuddleAugmentBase : AugmentBase
    {
        protected PuddleSystem _puddleSystem;
        
        private EventBinding<TriggerBombshellImpact> _bombshellImpactBinding;
        private EventBinding<TriggerEndRound> _endRoundBinding;

        [Serializable]
        public struct Params
        {
            public float PuddleDuration;
            public Vector3 Scale;
        }
        
        protected PuddleAugmentBase(SO_Augment augmentData, PlayerCharacter owner, SO_AugmentDatabase db) : base(
            augmentData, owner, db)
        { }

        public override void Initialize()
        {
            _bombshellImpactBinding = new EventBinding<TriggerBombshellImpact>(OnTriggerBombshellImpact);
            EventBus<TriggerBombshellImpact>.Register(_bombshellImpactBinding);

            _endRoundBinding = new EventBinding<TriggerEndRound>(OnTriggerEndRound);
            EventBus<TriggerEndRound>.Register(_endRoundBinding);
            
            _puddleSystem = SystemManager.Instance.Get<PuddleSystem>();
        }

        protected abstract void OnTriggerBombshellImpact(TriggerBombshellImpact evt);

        protected abstract void OnTriggerEndRound(TriggerEndRound evt);
        
        protected void SpawnPlayerPuddle(PlayerCharacter owner, Vector3 pos)
        {
            // TODO: Refactor to use specific puddle params from derived class instead of generic
            // TODO: Make better
            _puddleSystem.PuddleFactory.CreatePuddle(
                owner,
                pos,
                db.GenericPuddleParams.Scale,
                db.GenericPuddleParams.PuddleDuration,
                Owner.GetPuddleAbilities
            );
        }
    
        public override void Dispose()
        {
            EventBus<TriggerBombshellImpact>.Deregister(_bombshellImpactBinding);
            EventBus<TriggerEndRound>.Deregister(_endRoundBinding);
        }
    }   
}