using UnityEngine.Serialization;
namespace MortierFu
{
    public class AGM_FastStrike : AugmentBase
    {
        [System.Serializable]
        public struct Params
        {
            [FormerlySerializedAs("StrikeCooldownMod")]
            public AugmentStatMod DashCooldownMod;
        }
        
        public AGM_FastStrike(SO_Augment augmentData, PlayerCharacter owner, SO_AugmentDatabase db) : base(augmentData, owner, db)
        { }

        public override void Initialize()
        {
            stats.DashCooldown.AddModifier(db.FastStrikeParams.DashCooldownMod.ToMod(this));
        }
        
        public override void Dispose()
        {
            stats.DashCooldown.RemoveAllModifiersFromSource(this);
        }
    }
}