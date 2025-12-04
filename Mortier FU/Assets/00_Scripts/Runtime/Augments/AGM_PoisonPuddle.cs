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
        }

        public AGM_PoisonPuddle(SO_Augment augmentData, PlayerCharacter owner, SO_AugmentDatabase db) : base(
            augmentData, owner, db)
        { }

        protected override void OnTriggerBombshellImpact(TriggerBombshellImpact evt)
        {
            if (evt.Bombshell.Owner != Owner) return;

            //TODO: Better (pooling)
            var puddleObj = Object.Instantiate(
                db.PoisonPuddleParams.PuddlePrefab,
                evt.Bombshell.transform.position + Vector3.up,
                Quaternion.identity
            );
            
            var puddle = puddleObj.GetComponent<PuddleController>();
            puddle.AddAbility(db.PoisonPuddleParams.Ability);
            puddle.Lifetime = db.PoisonPuddleParams.PuddleDuration;
        }

        protected override void OnTriggerEndRound(TriggerEndRound evt)
        { }
    }
}