using System;
using UnityEngine;

namespace MortierFu
{
    [Serializable]
    public class AGM_PoisonPuddle : PuddleAugmentBase
    {
        [Serializable]
        public struct Params
        {
            public Ability Ability;
        }

        public AGM_PoisonPuddle(SO_Augment augmentData, PlayerCharacter owner, SO_AugmentDatabase db) : base(
            augmentData, owner, db)
        { }

        public override void Initialize()
        {
            base.Initialize();
            Owner.AddPuddleEffect(db.PoisonPuddleParams.Ability);
        }

        protected override void OnTriggerBombshellImpact(TriggerBombshellImpact evt)
        {
            if (evt.Bombshell.Owner != Owner) return;

            Vector3 pos = evt.Bombshell.transform.position + Vector3.up;

            SpawnPlayerPuddle(Owner, pos);
        }

        protected override void OnTriggerEndRound(TriggerEndRound evt)
        {
        }

        public override void Dispose()
        {
            Owner.AddPuddleEffect(db.PoisonPuddleParams.Ability);
            
            Owner.RemovePuddleEffect(db.PoisonPuddleParams.Ability);
        }
    }
}