namespace MortierFu
{
    public class AGM_DoubleDash : AugmentBase
    {
        [System.Serializable]
        public struct Params
        {
            public int ExtraDashes;
        }

        public AGM_DoubleDash(SO_Augment augmentData, PlayerCharacter owner, SO_AugmentDatabase db) : base(augmentData, owner, db)
        { }
        
        public override void Initialize()
        {
            stats.DashCharges.AddModifier(new StatModifier(db.DoubleDashParams.ExtraDashes, E_StatModType.Flat));
        }
        
        public override void Dispose()
        {
            stats.DashCharges.RemoveAllModifiersFromSource(this);
        }
    }
}