using System;
using UnityEngine;

namespace MortierFu
{
    [Serializable]
    public struct PinhataDropRule
    {
        [Min(1)] public int PlayerCount;
        [Min(1)] public int PickupsPerHit;
        public bool DropAllRemaining;
    }
    
    [CreateAssetMenu(fileName = "DA_RaceMode_Pinhata", menuName = "Mortier Fu/Race Modes/Pinhata")]
    public sealed class SO_PinhataRaceModeDefinition : SO_RaceModeDefinition
    {
        [Header("Drop")]
        [Min(0.1f)] public float DropRadius = 7f;
        [Min(0f)] public float DropHeight = 1.8f;
        [Min(0.1f)] public float DropDuration = 0.5f;
        [Min(0f)] public float HitCooldown = 0.25f;
        [Min(0f)] public float InhalePickupDuration = 0.45f;
        
        [Header("Pinhata Pickup Positions")]
        public bool OverrideDropWorldY = true;
        public float DropWorldY = 1.93f;
        
        [Header("Drop Safety")]
        public LayerMask DropBlockingMask;
        [Min(0.05f)] public float DropClearanceRadius = 0.6f;
        [Min(0f)] public float DropProbeHeight = 0.6f;
        
        [Header("Drop Scaling")]
        [Min(1)] public int DefaultPickupsPerHit = 1;
        [Min(0f)] public float MultiDropDelay = 0.03f;
        [Min(0f)] public float MultiDropAngleStep = 25f;

        public PinhataDropRule[] DropRules =
        {
            new PinhataDropRule { PlayerCount = 2, PickupsPerHit = 99, DropAllRemaining = true },
            new PinhataDropRule { PlayerCount = 3, PickupsPerHit = 2, DropAllRemaining = false },
            new PinhataDropRule { PlayerCount = 4, PickupsPerHit = 1, DropAllRemaining = false }
        };
        
        private void Reset()
        {
            UsePreviousRoundWinnerAsBully = true;
            BullyContext = PlayerControlContext.AugmentRaceBullyClassic;
            RacerContext = PlayerControlContext.AugmentRace;
        }

        public override RaceModeRuntimeBase CreateRuntime() => new PinhataRaceModeRuntime();
        
        public int ResolvePickupsPerHit(int playerCount, int remainingPickups)
        {
            if (remainingPickups <= 0)
                return 0;

            if (DropRules != null)
            {
                for (int i = 0; i < DropRules.Length; i++)
                {
                    if (DropRules[i].PlayerCount != playerCount)
                        continue;

                    if (DropRules[i].DropAllRemaining)
                        return remainingPickups;

                    return Mathf.Clamp(DropRules[i].PickupsPerHit, 1, remainingPickups);
                }
            }

            return Mathf.Clamp(DefaultPickupsPerHit, 1, remainingPickups);
        }
    }
}