namespace MortierFu
{
    public class AGM_WayFaster : AugmentBase
    {
        [System.Serializable]
        public struct Params
        {
            public AugmentStatMod MoveSpeedMod;
        }
        
        public AGM_WayFaster(SO_Augment augmentData, PlayerCharacter owner, SO_AugmentDatabase db) : base(augmentData, owner, db)
        { }

        public override void Initialize()
        {
            stats.MoveSpeed.AddModifier(db.WayFasterParams.MoveSpeedMod.ToMod(this));
        }
        
        public override void Dispose()
        {
            stats.MoveSpeed.RemoveAllModifiersFromSource(this);
        }
    }
}