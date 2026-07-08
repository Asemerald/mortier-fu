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
        [SerializeField] private UIIntStepperItem _scoreToWinItem;

        private PlayerManager _activePlayer;
        private Action<PlayerManager> _onClosed;

        private void Awake()
        {
            if (_root)
                _root.SetActive(false);
        }

        private void OnEnable()
        {
            if (!_scoreToWinItem) return;
            _scoreToWinItem.OnValueChanged -= SetScoreToWin;
            _scoreToWinItem.OnValueChanged += SetScoreToWin;
        }

        private void OnDisable()
        {
            if (_scoreToWinItem)
                _scoreToWinItem.OnValueChanged -= SetScoreToWin;
        }

        public void Open(PlayerManager player, Action<PlayerManager> onClosed)
        {
            if (!player)
                return;

            _activePlayer = player;
            _onClosed = onClosed;

            if (_root)
                _root.SetActive(true);

            if (_scoreToWinItem && _matchSettingsData)
                _scoreToWinItem.SetValue(_matchSettingsData.ScoreToWin, notify: false);

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
            var onClosed = _onClosed;

            if (_navigationPanel)
                _navigationPanel.Close();

            if (_root)
                _root.SetActive(false);

            _activePlayer = null;
            _onClosed = null;

            if (notifyClosed && activePlayer)
                onClosed?.Invoke(activePlayer);
        }

        private void SetScoreToWin(int score)
        {
            if (!_matchSettingsData)
                return;

            _matchSettingsData.SetScoreToWin(score);
        }
    }
}