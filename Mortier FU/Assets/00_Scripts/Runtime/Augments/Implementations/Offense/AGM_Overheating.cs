namespace MortierFu
{
    public class AGM_Overheating : AugmentBase
    {
        [System.Serializable]
        public struct Params
        {
            public AugmentStatMod OnAttackBombshellSpeedMod;
            public float MaxStacksOver;
        }
        
        private EventBinding<TriggerShootBombshell> _shootBinding;
        private EventBinding<TriggerEndRound> _endRoundBinding;
        
        private int StackAmountOver;
        
        public AGM_Overheating(SO_Augment augmentData, PlayerCharacter owner,  SO_AugmentDatabase db) : base(augmentData, owner, db)
        { }
        
        public override void Initialize()
        {
            _shootBinding = new EventBinding<TriggerShootBombshell>(OnShoot);
            EventBus<TriggerShootBombshell>.Register(_shootBinding);
            
            _endRoundBinding = new EventBinding<TriggerEndRound>(OnEndRound);
            EventBus<TriggerEndRound>.Register(_endRoundBinding);

            StackAmountOver = 0;
        }
        
        private void OnShoot(TriggerShootBombshell evt)
        {
            if (evt.Character != owner) return;

            if (StackAmountOver <= db.OverheatingParams.MaxStacksOver)
            {
                stats.BombshellSpeed.AddModifier(db.OverheatingParams.OnAttackBombshellSpeedMod.ToMod(this));
                StackAmountOver++;
            }
            
        }
        
        private void OnEndRound(TriggerEndRound evt)
        {
            stats.BombshellSpeed.RemoveAllModifiersFromSource(this);
            StackAmountOver = 0;
        }
        
        public override void Dispose()
        {
            EventBus<TriggerShootBombshell>.Deregister(_shootBinding);
            EventBus<TriggerEndRound>.Deregister(_endRoundBinding);
            stats.BombshellSpeed.RemoveAllModifiersFromSource(this);
        }
    }
}