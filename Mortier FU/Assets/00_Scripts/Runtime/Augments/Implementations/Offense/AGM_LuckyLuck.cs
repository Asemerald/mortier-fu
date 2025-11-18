namespace MortierFu
{
    public class AGM_LuckyLuck : AugmentBase
    {
        [System.Serializable]
        public struct Params
        {
            public AugmentStatMod FireRateMod;
            public int BaseValueSet;
        }
        
        public AGM_LuckyLuck(SO_Augment augmentData, PlayerCharacter owner, SO_AugmentDatabase db) : base(augmentData, owner, db)
        { }

        public override void Initialize()
        {
            stats.FireRate.AddModifier(db.LuckyLuckParams.FireRateMod.ToMod(this));
            // TODO Trouver une solution plus propre pour éviter d'hardset
            stats.MaxHealth.BaseValue = db.LuckyLuckParams.BaseValueSet;
        }
        
        public override void Dispose()
        {
            stats.FireRate.RemoveAllModifiersFromSource(this);
            // TODO Trouver une solution plus propre pour éviter d'hardset
            stats.MaxHealth.BaseValue = 3;
        }
    }
}