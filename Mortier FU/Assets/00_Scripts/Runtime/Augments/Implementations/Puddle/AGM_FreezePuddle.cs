using System;
using UnityEngine;

namespace MortierFu
{
    [Serializable]
    public class AGM_FreezePuddle : PuddleAugmentBase
    {
        [Serializable]
        public struct Params
        {
            public Ability Ability;
            public Vector3 Scale;
            public float LifeTime;
        }

        public AGM_FreezePuddle(SO_Augment augmentData, PlayerCharacter owner, SO_AugmentDatabase db) : base(
            augmentData, owner, db)
        {
        }

        public override void Initialize()
        {
            base.Initialize();
            Owner.AddPuddleEffect(db.FreezePuddleParams.Ability);
        }

        protected override void OnTriggerBombshellImpact(TriggerBombshellImpact evt)
        {
            if (evt.Bombshell.Owner != Owner) return;
            if (!evt.HitGround) return;
            
            Vector3 pos = evt.HitPoint;

            SpawnPlayerPuddle(Owner, pos, db.FreezePuddleParams.Scale, db.FreezePuddleParams.LifeTime);
        }

        protected override void OnTriggerEndRound(TriggerEndRound evt)
        {
        }

        public override void Dispose()
        {
            Owner.RemovePuddleEffect(db.FreezePuddleParams.Ability);
        }
    }
}