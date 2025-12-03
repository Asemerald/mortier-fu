using System;
using UnityEngine;

namespace MortierFu
{
    [Serializable]
    public class AGM_PoisonPuddle : ElementAugmentBase
    {
        [Serializable]
        public struct Params
        {
            public GameObject PuddlePrefab;
            
            public AugmentStatMod PoisonDamageMod;
            public AugmentStatMod Duration;
            public AugmentStatMod TickInterval;
        }

        public AGM_PoisonPuddle(SO_Augment augmentData, PlayerCharacter owner, SO_AugmentDatabase db) : base(
            augmentData, owner, db)
        { }

        protected override void OnTriggerBombshellImpact(TriggerBombshellImpact evt)
        {
            if (evt.Bombshell.Owner != Owner) return;

            var puddleObj = GameObject.Instantiate(
                db.PoisonPuddleParams.PuddlePrefab,
                evt.Bombshell.transform.position + Vector3.up,
                Quaternion.identity
            );
            
            var puddle = puddleObj.GetComponent<PuddleController>();
            
            puddle.Lifetime = db.PoisonPuddleParams.Duration.Value;
        }

        protected override void OnTriggerEndRound(TriggerEndRound evt)
        { }
    }
}