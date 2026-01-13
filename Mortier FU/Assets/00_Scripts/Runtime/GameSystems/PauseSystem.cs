using Cysharp.Threading.Tasks;

namespace MortierFu
{
    public class PauseSystem : IGameSystem
    {
        public void Dispose()
        { }

        public UniTask OnInitialize()
        {
            return UniTask.CompletedTask;
        }

        public bool IsInitialized { get; set; }
    }
}