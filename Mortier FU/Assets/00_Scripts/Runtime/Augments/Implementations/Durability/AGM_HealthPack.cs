namespace MortierFu
{
    public class AGM_HealthPack : AugmentBase
    {
        [System.Serializable]
        public struct Params
        {
            public AugmentStatMod MaxHealthMod;
        }
        
        public AGM_HealthPack(SO_Augment augmentData, PlayerCharacter owner, SO_AugmentDatabase db) : base(augmentData, owner, db)
        { }

        public override void Initialize()
        {
            stats.MaxHealth.AddModifier(db.HealthPackParams.MaxHealthMod.ToMod(this));
        }
        
        public override void Dispose()
        {
            stats.MaxHealth.RemoveAllModifiersFromSource(this);
        }
    }
}