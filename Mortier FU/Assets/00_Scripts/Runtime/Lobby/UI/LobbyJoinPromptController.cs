using MortierFu.Shared;
using UnityEngine;

namespace MortierFu
{
    public sealed class LobbyJoinPromptController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private LobbyJoinController _joinController;

        [Header("Visual Root")]
        [SerializeField] private GameObject _visualRoot;

        [Header("Slots")]
        [SerializeField] private LobbyJoinPromptSlot[] _slots;

        [Header("Text")]
        [SerializeField] private string _joinText = "Press A to join";

        private void Awake() => HideAll();

        private void OnEnable()
        {
            if (!_joinController)
            {
                Logs.LogError("[LobbyJoinPromptController] JoinController reference is missing.", this);
                HideAll();
                return;
            }

            _joinController.OnPromptStateChanged += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            if (_joinController)
                _joinController.OnPromptStateChanged -= Refresh;

            HideAll();
        }

        private void Refresh()
        {
            if (_slots is null || _slots.Length == 0)
            {
                HideAll();
                return;
            }

            bool hasVisibleSlot = false;

            for (int i = 0; i < _slots.Length; i++)
            {
                if (!_joinController || !_joinController.ShouldShowPromptForSlot(i)) continue;
                
                hasVisibleSlot = true;
                break;
            }

            if (hasVisibleSlot && _visualRoot)
                _visualRoot.SetActive(true);

            for (int i = 0; i < _slots.Length; i++)
            {
                LobbyJoinPromptSlot slot = _slots[i];

                if (!slot)
                    continue;

                bool shouldShow = _joinController && _joinController.ShouldShowPromptForSlot(i);

                if (shouldShow)
                    slot.Show(_joinText);
                else
                    slot.Hide();
            }

            if (!hasVisibleSlot && _visualRoot)
                _visualRoot.SetActive(false);
        }

        private void HideAll()
        {
            if (_visualRoot)
                _visualRoot.SetActive(false);

            if (_slots is null)
                return;

            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i])
                    _slots[i].Hide();
            }
        }
    }
}