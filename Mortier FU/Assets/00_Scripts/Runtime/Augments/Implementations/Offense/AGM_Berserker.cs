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
        }
        
        public AGM_Berserker(SO_Augment augmentData, PlayerCharacter owner, SO_AugmentDatabase db) : base(augmentData, owner, db)
        { }

        protected override float HealthThreshold => db.BerserkerParams.HealthThreshold;

        protected override void OnEnterThreshold()
        {
            stats.BombshellDamage.AddModifier(db.BerserkerParams.BombshellDamageMod.ToMod(this));
        }

        protected override void OnExitThreshold()
        {
            stats.BombshellDamage.RemoveAllModifiersFromSource(this);
        }

        public override void Dispose()
        {
            if(IsActive) OnExitThreshold();
            base.Dispose();
        }
    }
}