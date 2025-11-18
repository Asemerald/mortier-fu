namespace MortierFu
{
    public abstract class AugmentPreLandingTriggerBase : AugmentBase
    {
        private EventBinding<TriggerShootBombshell> _shootBombshellBinding;
        private EventBinding<TriggerBombshellImpact> _bombshellImpactBinding;
        
        protected AugmentPreLandingTriggerBase(SO_Augment augmentData, PlayerCharacter owner, SO_AugmentDatabase db) : base(augmentData, owner, db)
        { }
        
        public override void Initialize()
        {
            _shootBombshellBinding = new EventBinding<TriggerShootBombshell>(OnShootBombshell);
            EventBus<TriggerShootBombshell>.Register(_shootBombshellBinding);
            
            _bombshellImpactBinding = new EventBinding<TriggerBombshellImpact>(OnBombshellImpacted);
            EventBus<TriggerBombshellImpact>.Register(_bombshellImpactBinding);
        }

        private void OnShootBombshell(TriggerShootBombshell evt)
        {
            if (evt.Character != Owner) return;
        }

        private void OnBombshellImpacted(TriggerBombshellImpact evt)
        {
            if (evt.Bombshell.Owner != Owner) return;
        }
        
        public override void Dispose()
        {
            EventBus<TriggerShootBombshell>.Deregister(_shootBombshellBinding);
            EventBus<TriggerBombshellImpact>.Deregister(_bombshellImpactBinding);
        }
    }
}