using System;
using UnityEngine;

namespace MortierFu
{
    public sealed class LobbySettingsPanel : MonoBehaviour
    {
        [Header("Root")]
        [SerializeField] private GameObject _root;

        [Header("Data")]
        [SerializeField] private LobbyMatchSettingsData _matchSettingsData;

        [Header("Navigation")]
        [SerializeField] private UINavigationPanel _navigationPanel;

        [Header("Settings Items")]
        [SerializeField] private UIMatchSettingsItemBase[] _settingsItems;

        [Header("Optional")]
        [SerializeField] private TEMP_LobbyRecommendedScoreDisplay _recommendedScoreDisplay;

        private PlayerManager _activePlayer;
        private Action<PlayerManager> _onClosed;
        private int _currentPlayerCount = 1;

        private void Awake()
        {
            if (_root)
                _root.SetActive(false);
        }

        private void OnEnable()
        {
            if (!_matchSettingsData) return;
            
            _matchSettingsData.OnChanged -= Refresh;
            _matchSettingsData.OnChanged += Refresh;
        }

        private void OnDisable()
        {
            if (_matchSettingsData)
                _matchSettingsData.OnChanged -= Refresh;
        }

        public void Open(PlayerManager player, Action<PlayerManager> onClosed)
        {
            if (!player)
                return;

            _activePlayer = player;
            _onClosed = onClosed;
            _currentPlayerCount = GetCurrentLobbyPlayerCount();

            if (_matchSettingsData)
                _matchSettingsData.ApplyRecommendedForPlayerCount(_currentPlayerCount);

            if (_root)
                _root.SetActive(true);

            BindItems();
            Refresh();

            if (_navigationPanel)
                _navigationPanel.Open(player, CloseFromNavigation);
        }

        public void Close() => CloseInternal(notifyClosed: false);

        private void CloseFromNavigation(PlayerManager player)
        {
            if (!_activePlayer || !ReferenceEquals(_activePlayer, player))
                return;

            CloseInternal(notifyClosed: true);
        }

        private void CloseInternal(bool notifyClosed)
        {
            PlayerManager activePlayer = _activePlayer;
            Action<PlayerManager> onClosed = _onClosed;

            if (_navigationPanel)
                _navigationPanel.Close();

            if (_root)
                _root.SetActive(false);

            _activePlayer = null;
            _onClosed = null;

            if (notifyClosed && activePlayer)
                onClosed?.Invoke(activePlayer);
        }

        private void BindItems()
        {
            if (_settingsItems == null)
                return;

            for (int i = 0; i < _settingsItems.Length; i++)
            {
                if (_settingsItems[i])
                    _settingsItems[i].Bind(_matchSettingsData, _currentPlayerCount);
            }
        }

        private void Refresh()
        {
            if (_settingsItems != null)
            {
                for (int i = 0; i < _settingsItems.Length; i++)
                {
                    if (_settingsItems[i])
                        _settingsItems[i].Refresh();
                }
            }

            _recommendedScoreDisplay?.Refresh(_matchSettingsData ? _matchSettingsData.ScoreToWin : 0);
        }

        private static int GetCurrentLobbyPlayerCount()
        {
            LobbyService lobbyService = ServiceManager.Instance?.Get<LobbyService>();

            if (lobbyService == null)
                return 1;

            return Mathf.Max(1, lobbyService.CurrentPlayerCount);
        }
    }
}