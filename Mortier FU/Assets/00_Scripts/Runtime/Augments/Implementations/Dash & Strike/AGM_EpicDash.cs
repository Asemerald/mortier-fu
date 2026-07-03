using System.Runtime.InteropServices.ComTypes;
using UnityEngine;
using UnityEngine.Serialization;

namespace MortierFu
{
    public class AGM_EpicDash : AugmentBase
    {
        [System.Serializable]
        public struct Params
        {
            public float EpicDashDuration;
            public AugmentStatMod MoveSpeedMod;
            public AugmentStatMod BombshellSpeedMod;
            public AugmentStatMod BombshellDamageMod;
            public AugmentStatMod FireRateMod;
            public AugmentStatMod DashCooldownMod;
        }
        
        private CountdownTimer _epicDashTimer;
        private EventBinding<TriggerEndDash> _endDashBinding;
        
        public AGM_EpicDash(SO_Augment augmentData, PlayerCharacter owner, SO_AugmentDatabase db) : base(augmentData, owner, db)
        { }

        public override void Initialize()
        {
            _epicDashTimer = new CountdownTimer(Mathf.Clamp(db.EpicDashParams.EpicDashDuration,0,stats.DashCooldown.Value));
            _endDashBinding = new EventBinding<TriggerEndDash>(StartEndDashTimer);
            EventBus<TriggerEndDash>.Register(_endDashBinding);
            _epicDashTimer.OnTimerStop += () =>
            {
                stats.MoveSpeed.RemoveAllModifiersFromSource(this);
                stats.BombshellSpeed.RemoveAllModifiersFromSource(this);
                stats.BombshellDamage.RemoveAllModifiersFromSource(this);
            };
        }
        
        private void StartEndDashTimer()
        {
            _epicDashTimer.Start();
            stats.MoveSpeed.AddModifier(db.EpicDashParams.MoveSpeedMod.ToMod(this));
            stats.BombshellSpeed.AddModifier(db.EpicDashParams.BombshellSpeedMod.ToMod(this));
            stats.BombshellDamage.AddModifier(db.EpicDashParams.BombshellDamageMod.ToMod(this));
        }

        
        public override void Dispose()
        {
            stats.StrikePushForce.RemoveAllModifiersFromSource(this);
            stats.DashCooldown.RemoveAllModifiersFromSource(this);
            stats.MoveSpeed.RemoveAllModifiersFromSource(this);
            _epicDashTimer.Stop();
            _epicDashTimer.Dispose();
            EventBus<TriggerEndDash>.Deregister(_endDashBinding);
        }
    }
}