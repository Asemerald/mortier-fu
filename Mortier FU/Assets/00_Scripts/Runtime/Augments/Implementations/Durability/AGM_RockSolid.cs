namespace MortierFu
{
    public class AGM_RockSolid : AugmentBase
    {
        [System.Serializable]
        public struct Params
        {
            public AugmentStatMod MaxHealthMod;
        }
        
        public AGM_RockSolid(SO_Augment augmentData, PlayerCharacter owner, SO_AugmentDatabase db) : base(augmentData, owner, db)
        { }

        public override void Initialize()
        {
            stats.MaxHealth.AddModifier(db.RockSolidParams.MaxHealthMod.ToMod(this));
        }
        
        public override void Dispose()
        {
            stats.MaxHealth.RemoveAllModifiersFromSource(this);
        }
    }
}