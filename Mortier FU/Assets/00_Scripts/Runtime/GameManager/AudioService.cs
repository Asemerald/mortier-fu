using MortierFu.Shared;

namespace MortierFu
{
    public class AudioService : IGameService
    {

        public void Initialize()
        {
            Logs.Log("[AudioService] Initialized!");
        }

        public void Tick()
        {
        }
        
        public void Dispose()
        {
            
        }

    }
}