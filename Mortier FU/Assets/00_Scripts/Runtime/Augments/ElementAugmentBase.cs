namespace MortierFu
{
    public abstract class ElementAugmentBase : AugmentBase
    {
        private EventBinding<TriggerBombshellImpact> _bombshellImpactBinding;
        private EventBinding<TriggerEndRound> _endRoundBinding;
    
        protected ElementAugmentBase(SO_Augment augmentData, PlayerCharacter owner, SO_AugmentDatabase db) : base(augmentData, owner, db)
        { }

        public override void Initialize()
        {
            _bombshellImpactBinding = new EventBinding<TriggerBombshellImpact>(OnTriggerBombshellImpact);
            EventBus<TriggerBombshellImpact>.Register(_bombshellImpactBinding);

            _endRoundBinding = new EventBinding<TriggerEndRound>(OnTriggerEndRound);
            EventBus<TriggerEndRound>.Register(_endRoundBinding);
        }

        protected abstract void OnTriggerBombshellImpact(TriggerBombshellImpact evt);

        protected abstract void OnTriggerEndRound(TriggerEndRound evt);
    
        public override void Dispose()
        {
            EventBus<TriggerBombshellImpact>.Deregister(_bombshellImpactBinding);
            EventBus<TriggerEndRound>.Deregister(_endRoundBinding);
        }
    }   
}