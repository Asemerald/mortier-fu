namespace MortierFu
{
    public abstract class AugmentHealthThresholdTriggerBase : AugmentBase
    {
        private EventBinding<TriggerHealthChanged> _healthChangedBinding;
        
        protected abstract float HealthThreshold { get; }

        protected AugmentHealthThresholdTriggerBase(SO_Augment augmentData, PlayerCharacter owner) : base(augmentData, owner)
        { }
        
        public override void Initialize()
        {
            _healthChangedBinding = new EventBinding<TriggerHealthChanged>(OnHealthChanged);
            EventBus<TriggerHealthChanged>.Register(_healthChangedBinding);
        }
        
        protected abstract void HealthThresholdTriggered();
        
        private void OnHealthChanged(TriggerHealthChanged evt)
        {
            if (evt.Character != owner) return;

            float oldRatio = evt.PreviousHealth / evt.NewHealth;
            float currentRatio = evt.NewHealth / evt.MaxHealth;
            
            if (oldRatio <= HealthThreshold) return;
            
            if (currentRatio <= HealthThreshold)
            {
                HealthThresholdTriggered();
            }
        }
        
        public override void Dispose()
        {
            EventBus<TriggerHealthChanged>.Deregister(_healthChangedBinding);
        }
    }
}