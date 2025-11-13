using Cysharp.Threading.Tasks;

namespace MortierFu
{
    public class TriggerService : IGameService
    {
        
        
        public UniTask OnInitialize()
        {
            return UniTask.CompletedTask;
        }
     
        public void Dispose()
        {
        }
        
        public bool IsInitialized { get; set; }
    }
}
