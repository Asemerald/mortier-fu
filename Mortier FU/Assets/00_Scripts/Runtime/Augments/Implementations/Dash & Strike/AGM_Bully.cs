using UnityEngine.Serialization;

namespace MortierFu
{
    public class AGM_Bully : AugmentBase
    {
        [System.Serializable]
        public struct Params
        {
            public AugmentStatMod KnockbackStunDurationMod;
        }
        
        public AGM_Bully(SO_Augment augmentData, PlayerCharacter owner, SO_AugmentDatabase db) : base(augmentData, owner, db)
        { }

        public override void Initialize()
        {
            //stats.KnockbackStunDuration.AddModifier(db.BullyParams.KnockbackStunDurationMod.ToMod(this));
        }
        
        public override void Dispose()
        {
        }
    }
}