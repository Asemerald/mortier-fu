namespace MortierFu.Stats
{
    public class AGM_Overheating : AugmentBase
    {
        private EventBinding<TriggerShootBombshell> _shootBinding;
        private EventBinding<TriggerEndRound> _endRoundBinding;
        
        public AGM_Overheating(SO_Augment augmentData, PlayerCharacter owner) : base(augmentData, owner)
        { }
        
        public override void Initialize()
        {
            _shootBinding = new EventBinding<TriggerShootBombshell>(OnShoot);
            EventBus<TriggerShootBombshell>.Register(_shootBinding);
            
            _endRoundBinding = new EventBinding<TriggerEndRound>(OnEndRound);
            EventBus<TriggerEndRound>.Register(_endRoundBinding);
            
            stats.BombshellTimeTravel.AddModifier(new StatModifier(-0.9f, E_StatModType.PercentMult, this));
        }
        
        private void OnShoot(TriggerShootBombshell evt)
        {
            if (evt.Character != owner) return;
            
            stats.BombshellTimeTravel.AddModifier(new StatModifier(0.05f, E_StatModType.PercentMult, this));
        }
        
        private void OnEndRound(TriggerEndRound evt)
        {
            stats.BombshellTimeTravel.RemoveAllModifiersFromSource(this);
            stats.BombshellTimeTravel.AddModifier(new StatModifier(-0.9f, E_StatModType.PercentMult, this));
        }
        
        public override void Dispose()
        {
            EventBus<TriggerShootBombshell>.Deregister(_shootBinding);
            EventBus<TriggerEndRound>.Deregister(_endRoundBinding);
            stats.BombshellTimeTravel.RemoveAllModifiersFromSource(this);
        }
    }
}