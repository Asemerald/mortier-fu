using UnityEngine.Serialization;

namespace MortierFu
{
    public class AGM_BigStrike : AugmentBase
    {
        [System.Serializable]
        public struct Params
        {
            public AugmentStatMod StrikePushForceMod;
            [FormerlySerializedAs("StrikeCooldownMod")]
            public AugmentStatMod DashCooldownMod;
        }
        
        public AGM_BigStrike(SO_Augment augmentData, PlayerCharacter owner, SO_AugmentDatabase db) : base(augmentData, owner, db)
        { }

        public override void Initialize()
        {
            stats.StrikePushForce.AddModifier(db.BigStrikeParams.StrikePushForceMod.ToMod(this));
            stats.DashCooldown.AddModifier(db.BigStrikeParams.DashCooldownMod.ToMod(this));
        }
        
        public override void Dispose()
        {
            stats.StrikePushForce.RemoveAllModifiersFromSource(this);
            stats.DashCooldown.RemoveAllModifiersFromSource(this);
        }
    }
}