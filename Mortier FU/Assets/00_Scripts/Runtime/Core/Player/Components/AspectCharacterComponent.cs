using UnityEngine;

namespace MortierFu {
    public class AspectCharacterComponent : CharacterComponent
    {
        public Color PlayerColor { get; private set; }
        private float _hueOffset;
        private float _saturation;
        private float _value;
        
        public AspectCharacterComponent(PlayerCharacter character, float hueOffset, float saturation, float value) : base(character) {
            _hueOffset = hueOffset;
            _saturation = saturation;
            _value = value;
        }

        public override void Initialize() {
            var lobbyService = ServiceManager.Instance.Get<LobbyService>();
            int playerCount = lobbyService.GetPlayers().Count;
            
            // Retrieve this player's index
            int index = character.Owner.PlayerIndex;
            PlayerColor = GetColorForPlayerIndex(index, playerCount, _hueOffset, _saturation, _value);
            
            if (character.TryGetComponent(out Renderer renderer)) {
                renderer.material.color = PlayerColor;
            }
        }
        
        public override void Dispose()
        { }

        private static Color GetColorForPlayerIndex(int index, int totalPlayers,
            float hueOffset = 0f, float saturation = 1f, float value = 1f)
        {
            float segment = 1f / Mathf.Max(1, totalPlayers);
            float hue = (segment * index + hueOffset) % 1f;
            return Color.HSVToRGB(hue, saturation, value);
        }
    }
   
}