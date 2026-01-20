namespace MortierFu
{
    public class AGM_Overheating : AugmentBase
    {
        [System.Serializable]
        public struct Params
        {
            public AugmentStatMod OnAttackBombshellSpeedMod;
        }
        
        private EventBinding<TriggerShootBombshell> _shootBinding;
        private EventBinding<TriggerEndRound> _endRoundBinding;
        
        private int StackAmount;
        
        public AGM_Overheating(SO_Augment augmentData, PlayerCharacter owner,  SO_AugmentDatabase db) : base(augmentData, owner, db)
        { }
        
        public override void Initialize()
        {
            _shootBinding = new EventBinding<TriggerShootBombshell>(OnShoot);
            EventBus<TriggerShootBombshell>.Register(_shootBinding);
            
            _endRoundBinding = new EventBinding<TriggerEndRound>(OnEndRound);
            EventBus<TriggerEndRound>.Register(_endRoundBinding);

            StackAmount = 0;
        }
        
        private void OnShoot(TriggerShootBombshell evt)
        {
            if (evt.Character != owner) return;

            if (StackAmount <= 10)
            {
                stats.BombshellSpeed.AddModifier(db.OverheatingParams.OnAttackBombshellSpeedMod.ToMod(this));
                StackAmount++;
            }
            
        }
        
        private void OnEndRound(TriggerEndRound evt)
        {
            stats.BombshellSpeed.RemoveAllModifiersFromSource(this);
            StackAmount = 10;
        }
        
        public override void Dispose()
        {
            EventBus<TriggerShootBombshell>.Deregister(_shootBinding);
            EventBus<TriggerEndRound>.Deregister(_endRoundBinding);
            stats.BombshellSpeed.RemoveAllModifiersFromSource(this);
        }
    }
}