using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MortierFu
{
    [Serializable]
    public class AGM_PoisonPuddle : ElementAugmentBase
    {
        [Serializable]
        public struct Params
        {
            public GameObject PuddlePrefab;
            public Ability Ability;
            public float PuddleDuration;
            public Vector3 Scale;
        }

        public AGM_PoisonPuddle(SO_Augment augmentData, PlayerCharacter owner, SO_AugmentDatabase db) : base(
            augmentData, owner, db)
        { }

        protected override void OnTriggerBombshellImpact(TriggerBombshellImpact evt)
        {
            if (evt.Bombshell.Owner != Owner) return;

            //TODO: Better (pooling)
            var puddleData = new Puddle.Data
            {
                Owner = evt.Bombshell.Owner,
                InstantiatePos = evt.Bombshell.transform.position + Vector3.up,
                Scale = db.PoisonPuddleParams.Scale,
                Lifetime = db.PoisonPuddleParams.PuddleDuration
            };

            var puddle = _puddleSystem.RequestPuddle(puddleData); 
            puddle.AddAbility(db.PoisonPuddleParams.Ability);
        }

        protected override void OnTriggerEndRound(TriggerEndRound evt)
        {
            
        }
    }
}