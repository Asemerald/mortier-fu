namespace MortierFu
{
    public class AGM_Surprise : AugmentBase
    {
        [System.Serializable]
        public struct Params
        {
            public AugmentStatMod BombshellSpeedMod;
        }
        
        public AGM_Surprise(SO_Augment augmentData, PlayerCharacter owner, SO_AugmentDatabase db) : base(augmentData, owner, db)
        { }

        public override void Initialize()
        {
            stats.BombshellSpeed.AddModifier(db.SurpriseParams.BombshellSpeedMod.ToMod(this));
        }
        
        public override void Dispose()
        {
            stats.BombshellSpeed.RemoveAllModifiersFromSource(this);
        }
    }
}