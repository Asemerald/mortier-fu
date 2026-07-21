using UnityEngine;

namespace MortierFu
{
    public abstract class UIMatchSettingsItemBase : UINavigationItem
    {
        [Header("Match Setting")]
        [SerializeField] private CanvasGroup _contentGroup;
        [SerializeField] private float _readOnlyAlpha = 0.45f;

        protected LobbyMatchSettingsData Data { get; private set; }
        protected int PlayerCount { get; private set; }

        public void Bind(LobbyMatchSettingsData data, int playerCount)
        {
            Data = data;
            PlayerCount = Mathf.Max(1, playerCount);

            Refresh();
        }

        public abstract void Refresh();

        protected void SetReadOnlyVisual(bool editable)
        {
            if (!_contentGroup)
                return;

            _contentGroup.alpha = editable ? 1f : _readOnlyAlpha;
        }
    }
}