using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MortierFu
{
    public sealed class UIPauseSubPanelBackHandler : MonoBehaviour, IUIBackHandler, ICancelHandler
    {
        [SerializeField] private PauseUI _pauseUI;
        [SerializeField] private Selectable _returnSelection;

        private void Awake()
        {
            if (!_pauseUI)
                _pauseUI = GetComponentInParent<PauseUI>();
        }

        public void OnCancel(BaseEventData eventData)
        {
            eventData.Use();
            HandleUIBack();
        }

        public void HandleUIBack()
        {
            if (!_pauseUI)
                return;

            _pauseUI.ReturnToMainPanelFromSubPanel(_returnSelection);
        }
    }
}