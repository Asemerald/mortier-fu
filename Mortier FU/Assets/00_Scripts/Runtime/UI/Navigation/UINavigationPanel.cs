using System;
using System.Collections.Generic;
using MortierFu.Shared;
using UnityEngine;

namespace MortierFu
{
    public sealed class UINavigationPanel : MonoBehaviour, IPlayerUIInputHandler
    {
        [Header("Items")]
        [SerializeField] private List<UINavigationItem> _items = new();

        [Header("Navigation")]
        [SerializeField] private float _navigationThreshold = 0.3f;
        [SerializeField] private float _navigationCooldown = 0.2f;
        [SerializeField] private bool _wrapVerticalNavigation = true;

        private PlayerManager _activePlayer;
        private UINavigationRepeater _navigationRepeater;
        private Action<PlayerManager> _onCancelled;

        private int _selectedIndex;
        private bool _isOpen;
        private bool _canNavigate;

        private PlayerUIInputService UIInputService => ServiceManager.Instance?.Get<PlayerUIInputService>();

        private void Awake()
        {
            _navigationRepeater = new UINavigationRepeater(_navigationThreshold, _navigationCooldown);

            RefreshSelection();
        }

        private void OnDisable() => Close();

        private void OnDestroy() => Close();

        public void Open(PlayerManager player, Action<PlayerManager> onCancelled = null)
        {
            if (!player)
                return;

            Close();

            _activePlayer = player;
            _onCancelled = onCancelled;
            _isOpen = true;
            _canNavigate = true;
            _selectedIndex = GetFirstAvailableIndex();
            _navigationRepeater.Reset();

            UIInputService?.Push(_activePlayer, this);

            RefreshSelection();

            Logs.Log($"[UINavigationPanel] Opened for Player {player.PlayerIndex + 1}.", this);
        }

        public void Close()
        {
            if (!_isOpen && !_activePlayer)
                return;

            if (_activePlayer)
                UIInputService?.Remove(_activePlayer, this);
            else
                UIInputService?.RemoveFromAll(this);

            _activePlayer = null;
            _onCancelled = null;
            _isOpen = false;
            _canNavigate = false;
            _navigationRepeater?.Reset();

            ClearSelection();
        }

        public bool CanHandleUIInput(PlayerManager player) => _isOpen && _activePlayer && ReferenceEquals(_activePlayer, player);

        public bool HandleNavigate(PlayerManager player, Vector2 direction)
        {
            if (!CanHandleUIInput(player))
                return false;

            if (!_canNavigate)
                return true;

            if (!_navigationRepeater.TryGetNavigation(direction, out UINavigationInput navigation))
                return true;

            if (navigation.Axis == UINavigationAxis.Vertical)
                MoveSelection(-navigation.Direction);
            else if (navigation.Axis == UINavigationAxis.Horizontal)
                GetSelectedItem()?.HandleHorizontal(navigation.Direction);

            return true;
        }

        public bool HandleSubmit(PlayerManager player) => CanHandleUIInput(player);

        public bool HandleCancel(PlayerManager player)
        {
            if (!CanHandleUIInput(player))
                return false;

            UINavigationItem item = GetSelectedItem();

            if (item && item.HandleCancel())
                return true;

            _onCancelled?.Invoke(player);
            return true;
        }

        private void MoveSelection(int direction)
        {
            if (_items.Count == 0)
                return;

            int startIndex = _selectedIndex;

            for (int i = 0; i < _items.Count; i++)
            {
                int nextIndex = _selectedIndex + direction;

                nextIndex = _wrapVerticalNavigation ? WrapIndex(nextIndex, _items.Count) : Mathf.Clamp(nextIndex, 0, _items.Count - 1);

                _selectedIndex = nextIndex;

                if (IsItemAvailable(_selectedIndex))
                {
                    RefreshSelection();
                    return;
                }

                if (_selectedIndex == startIndex)
                    break;
            }
        }

        private int GetFirstAvailableIndex()
        {
            for (int i = 0; i < _items.Count; i++)
            {
                if (IsItemAvailable(i))
                    return i;
            }

            return 0;
        }

        private UINavigationItem GetSelectedItem()
        {
            if (_selectedIndex < 0 || _selectedIndex >= _items.Count)
                return null;

            UINavigationItem item = _items[_selectedIndex];
            return item && item.IsAvailable ? item : null;
        }

        private bool IsItemAvailable(int index)
        {
            if (index < 0 || index >= _items.Count)
                return false;

            UINavigationItem item = _items[index];
            return item && item.IsAvailable;
        }

        private void RefreshSelection()
        {
            for (int i = 0; i < _items.Count; i++)
            {
                if (_items[i])
                    _items[i].SetSelected(i == _selectedIndex);
            }
        }

        private void ClearSelection()
        {
            for (int i = 0; i < _items.Count; i++)
            {
                if (_items[i])
                    _items[i].SetSelected(false);
            }
        }

        private static int WrapIndex(int value, int count)
        {
            if (count <= 0)
                return 0;

            value %= count;

            if (value < 0)
                value += count;

            return value;
        }
    }
}