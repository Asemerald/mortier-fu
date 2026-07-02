namespace MortierFu
{
    public class AGM_Vampire : AugmentBase
    {
        [System.Serializable]
        public struct Params
        {
            
        }

        private EventBinding<TriggerHit> _hitBinding;
        private EventBinding<TriggerEndRound> _endRoundBinding;
        
        public AGM_Vampire(SO_Augment augmentData, PlayerCharacter owner, SO_AugmentDatabase db) : base(augmentData, owner, db)
        { }

        public override void Initialize()
        {
            stats.BombshellImpactRadius.AddModifier(db.ImpactParams.BombshellImpactRadiusMod.ToMod(this));
        }
        
        public override void Dispose()
        {
            stats.BombshellImpactRadius.RemoveAllModifiersFromSource(this);
        }
    }
}