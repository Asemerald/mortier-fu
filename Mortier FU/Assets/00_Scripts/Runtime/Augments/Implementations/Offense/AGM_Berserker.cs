using UnityEngine;
namespace MortierFu
{
    public class AGM_Berserker : AugmentHealthThresholdBase
    {
        [System.Serializable]
        public struct Params
        {
            [Range(0f, 1f)] public float HealthThreshold;
            public AugmentStatMod BombshellDamageMod;
            public AugmentStatMod MoveSpeedMod;
            public AugmentStatMod FireRateMod;

			public AugmentStatMod BombshellDamageModPreproc;
            public AugmentStatMod MoveSpeedModPreproc;
            public AugmentStatMod FireRateModPreproc;
        }
        
        public AGM_Berserker(SO_Augment augmentData, PlayerCharacter owner, SO_AugmentDatabase db) : base(augmentData, owner, db)
        { }

        protected override float HealthThreshold => db.BerserkerParams.HealthThreshold;
        
        public override void Initialize()
        {
            stats.BombshellDamage.AddModifier(db.BerserkerParams.BombshellDamageModPreproc.ToMod(this));
            stats.FireRate.AddModifier(db.BerserkerParams.FireRateModPreproc.ToMod(this));
            stats.MoveSpeed.AddModifier(db.BerserkerParams.MoveSpeedModPreproc.ToMod(this));
        }

        protected override void OnEnterThreshold()
        {
            stats.BombshellDamage.AddModifier(db.BerserkerParams.BombshellDamageMod.ToMod(this));
            stats.MoveSpeed.AddModifier(db.BerserkerParams.MoveSpeedMod.ToMod(this));
            stats.FireRate.AddModifier(db.BerserkerParams.FireRateMod.ToMod(this));
            AudioService.PlayOneShot(AudioService.FMODEvents.SFX_Augment_Buff, owner.transform.position);
        }

        protected override void OnExitThreshold()
        {
            stats.BombshellDamage.RemoveAllModifiersFromSource(this);
            stats.MoveSpeed.RemoveAllModifiersFromSource(this);
            stats.FireRate.RemoveAllModifiersFromSource(this);
        }

        public override void Dispose()
        {
            if(IsActive) OnExitThreshold();
            base.Dispose();
            stats.BombshellDamage.RemoveAllModifiersFromSource(this);
            stats.MoveSpeed.RemoveAllModifiersFromSource(this);
            stats.FireRate.RemoveAllModifiersFromSource(this);
        }
    }
}