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
        [SerializeField] private float _navigationPressThreshold = 0.55f;
        [SerializeField] private float _navigationReleaseThreshold = 0.25f;
        [SerializeField] private float _axisDominanceMargin = 0.2f;
        [SerializeField] private float _initialRepeatDelay = 0.35f;
        [SerializeField] private float _repeatCooldown = 0.12f;
        [SerializeField] private bool _wrapVerticalNavigation = true;

        private PlayerManager _activePlayer;
        private UINavigationRepeater _navigationRepeater;

        private Action<PlayerManager> _onCancelled;
        private Action<PlayerManager> _onSubmitted;

        private int _selectedIndex;
        private bool _isOpen;
        private bool _canNavigate;

        private PlayerUIInputService UIInputService => ServiceManager.Instance?.Get<PlayerUIInputService>();

        private void Awake()
        {
            EnsureInitialized();
            RefreshSelection();
        }

        private void OnDisable() => Close();

        private void OnDestroy() => Close();

        public void Open(PlayerManager player, Action<PlayerManager> onCancelled = null, Action<PlayerManager> onSubmitted = null)
        {
            if (!player)
                return;

            EnsureInitialized();

            Close();

            _activePlayer = player;
            _onCancelled = onCancelled;
            _onSubmitted = onSubmitted;
            _isOpen = true;
            _canNavigate = true;
            _selectedIndex = GetFirstAvailableIndex();

            _navigationRepeater.Reset();

            PlayerUIInputService inputService = UIInputService;

            if (inputService == null)
                Logs.LogError("[UINavigationPanel] PlayerUIInputService is missing.", this);
            else
                inputService.Push(_activePlayer, this);

            RefreshSelection();
        }

        public void Close()
        {
            EnsureInitialized();

            if (!_isOpen && !_activePlayer)
                return;

            PlayerUIInputService inputService = UIInputService;

            if (inputService != null)
            {
                if (_activePlayer)
                    inputService.Remove(_activePlayer, this);
                else
                    inputService.RemoveFromAll(this);
            }

            _activePlayer = null;
            _onCancelled = null;
            _onSubmitted = null;
            _isOpen = false;
            _canNavigate = false;

            _navigationRepeater.Reset();

            ClearSelection();
        }

        public void SetCanNavigate(bool canNavigate)
        {
            EnsureInitialized();

            _canNavigate = canNavigate;

            if (!canNavigate)
                _navigationRepeater.Reset();
        }

        public bool CanHandleUIInput(PlayerManager player) => _isOpen && _activePlayer && ReferenceEquals(_activePlayer, player);

        public bool HandleNavigate(PlayerManager player, Vector2 direction)
        {
            if (!CanHandleUIInput(player))
                return false;

            if (!_canNavigate)
                return true;

            EnsureInitialized();

            if (!_navigationRepeater.TryGetNavigation(direction, out UINavigationInput navigation))
                return true;

            if (navigation.Axis == UINavigationAxis.Vertical)
                MoveSelection(-navigation.Direction);
            else if (navigation.Axis == UINavigationAxis.Horizontal)
                GetSelectedItem()?.HandleHorizontal(navigation.Direction);

            return true;
        }

        public bool HandleSubmit(PlayerManager player)
        {
            if (!CanHandleUIInput(player))
                return false;

            UINavigationItem item = GetSelectedItem();

            if (item && item.HandleSubmit())
                return true;

            _onSubmitted?.Invoke(player);
            return true;
        }

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

        private void EnsureInitialized()
        {
            _navigationRepeater ??= new UINavigationRepeater(_navigationPressThreshold, _navigationReleaseThreshold, _axisDominanceMargin, _initialRepeatDelay, _repeatCooldown);

            _items ??= new List<UINavigationItem>();
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
            if (_items == null)
                return;

            for (int i = 0; i < _items.Count; i++)
            {
                if (_items[i])
                    _items[i].SetSelected(i == _selectedIndex);
            }
        }

        private void ClearSelection()
        {
            if (_items == null)
                return;

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