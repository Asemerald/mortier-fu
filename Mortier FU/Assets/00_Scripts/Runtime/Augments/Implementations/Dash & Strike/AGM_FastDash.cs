using UnityEngine.Serialization;
namespace MortierFu
{
    public class AGM_FastDash : AugmentBase
    {
        [System.Serializable]
        public struct Params
        {
            [FormerlySerializedAs("StrikeCooldownMod")]
            public AugmentStatMod DashCooldownMod;
        }
        
        public AGM_FastDash(SO_Augment augmentData, PlayerCharacter owner, SO_AugmentDatabase db) : base(augmentData, owner, db)
        { }

        public override void Initialize()
        {
            stats.DashCooldown.AddModifier(db.FastDashParams.DashCooldownMod.ToMod(this));
        }
        
        public override void Dispose()
        {
            stats.DashCooldown.RemoveAllModifiersFromSource(this);
        }
    }
}