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
            public GameObject PuddlePrefab;
            public Ability Ability;
            public float PuddleDuration;
            public Vector3 Scale;
        }

        public AGM_FreezePuddle(SO_Augment augmentData, PlayerCharacter owner, SO_AugmentDatabase db) : base(
            augmentData, owner, db)
        {
        }

        protected override void OnTriggerBombshellImpact(TriggerBombshellImpact evt)
        {
            if (evt.Bombshell.Owner != Owner) return;

            //TODO: Better (pooling)
            var puddleData = new Puddle.Data
            {
                Owner = evt.Bombshell.Owner,
                InstantiatePos = evt.Bombshell.transform.position + Vector3.up,
                Scale = db.FreezePuddleParams.Scale,
                Lifetime = db.FreezePuddleParams.PuddleDuration
            };

            var puddle = _puddleSystem.RequestPuddle(puddleData);
            puddle.AddAbility(db.FreezePuddleParams.Ability);
        }

        protected override void OnTriggerEndRound(TriggerEndRound evt)
        {
        }
    }
}