using System.Threading.Tasks;

namespace MortierFu
{
    public class AugmentSelectionSystem : IGameSystem
    {
        private AugmentPickup[] _pickups;
        
        private DA_Augment[] _augments;
        
        public async Task OnInitialize()
        {
            var lobby = ServiceManager.Instance.Get<LobbyService>();
            var playerCount = lobby.GetPlayers().Count;
            
            _pickups = new AugmentPickup[playerCount];
            _augments = new DA_Augment[playerCount];

            for (int i = 0; i < playerCount; i++)
            {
                var pickup = await SystemManager.Config.AugmentPickupPrefab.LoadAndInstantiate<AugmentPickup>();
               // _pickups[i] = pickup;
                
                _augments[i] = null;
            }
        }
        
        public bool IsInitialized { get; set; }

        public void Dispose()
        {
            
        }
    }
   
}