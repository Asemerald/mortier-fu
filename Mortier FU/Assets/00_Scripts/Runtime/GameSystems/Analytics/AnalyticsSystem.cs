using Codice.Client.BaseCommands;
using Cysharp.Threading.Tasks;

namespace MortierFu.Analytics
{
    public class AnalyticsSystem : IGameSystem
    {
        private EventBinding<TriggerHit> _triggerHitBinding;
        private EventBinding<TriggerShootBombshell> _triggerShootBombshellBinding;
        private EventBinding<TriggerHealthChanged> _triggerHealthChangedBinding;

        public UniTask OnInitialize()
        {
            throw new System.NotImplementedException();
        }

        private void RegisterEvents()
        {
            _triggerShootBombshellBinding = new EventBinding<TriggerShootBombshell>(OnTriggerShootBombshell);
            EventBus<TriggerShootBombshell>.Register(_triggerShootBombshellBinding);
            
            _triggerHitBinding = new EventBinding<TriggerHit>(OnTriggerHit);
            EventBus<TriggerHit>.Register(_triggerHitBinding);
            
            _triggerHealthChangedBinding = new EventBinding<TriggerHealthChanged>(OnTriggerHealthChanged);
            EventBus<TriggerHealthChanged>.Register(_triggerHealthChangedBinding);
        }

        private void OnTriggerShootBombshell(TriggerShootBombshell shootBombshell)
        {
            
        }
        private void OnTriggerHit(TriggerHit hit)
        {
            
        }
        
        private void OnTriggerHealthChanged(TriggerHealthChanged healthChanged)
        {
            
        }
        

        public bool IsInitialized { get; set; }
        
        public void Dispose()
        {
            throw new System.NotImplementedException();
        }
    }
}