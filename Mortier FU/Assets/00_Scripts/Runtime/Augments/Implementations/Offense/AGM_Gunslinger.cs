namespace MortierFu
{
    public class AGM_Gunslinger : AugmentBase
    {
        [System.Serializable]
        public struct Params
        {
            public AugmentStatMod FireRateMod;
            public int BaseValueSet;
        }
        
        public AGM_Gunslinger(SO_Augment augmentData, PlayerCharacter owner, SO_AugmentDatabase db) : base(augmentData, owner, db)
        { }

        public override void Initialize()
        {
            stats.FireRate.AddModifier(db.GunslingerParams.FireRateMod.ToMod(this));
            // TODO Trouver une solution plus propre pour éviter d'hardset
            stats.MaxHealth.BaseValue = db.GunslingerParams.BaseValueSet;
        }
        
        public override void Dispose()
        {
            stats.FireRate.RemoveAllModifiersFromSource(this);
            // TODO Trouver une solution plus propre pour éviter d'hardset
            stats.MaxHealth.BaseValue = 3;
        }
    }
}