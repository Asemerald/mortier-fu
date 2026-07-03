using UnityEngine.Serialization;

namespace MortierFu
{
    public class AGM_Traveler : AugmentBase
    {
        [System.Serializable]
        public struct Params
        {
            public AugmentStatMod DashForceMod;
        }
        
        public AGM_Traveler(SO_Augment augmentData, PlayerCharacter owner, SO_AugmentDatabase db) : base(augmentData, owner, db)
        { }

        public override void Initialize()
        {
            //stats.DashForce.AddModifier(db.TravelerParams.DashForceMod.ToMod(this));
        }
        
        public override void Dispose()
        {
        }
    }
}