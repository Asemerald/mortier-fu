using UnityEngine;

namespace MortierFu
{
    public sealed class GhostVisualComponent : GhostPawnComponent
    {
        private readonly GameObject _rootVisual;

        public GhostVisualComponent(PlayerGhostPawn pawn, GameObject rootVisual) : base(pawn)
        {
            _rootVisual = rootVisual;
        }

        public override void Initialize() => Hide();

        private void Show()
        {
            if (_rootVisual)
                _rootVisual.SetActive(true);
        }

        public void Hide()
        {
            if (_rootVisual)
                _rootVisual.SetActive(false);
        }

        public void PlaySpawnFeedback() => Show();

        public override void Reset() => Hide();
    }
}