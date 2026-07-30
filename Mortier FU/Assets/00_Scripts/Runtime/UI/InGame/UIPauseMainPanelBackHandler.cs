using UnityEngine;
using UnityEngine.EventSystems;

namespace MortierFu
{
    public sealed class UIPauseMainPanelBackHandler : MonoBehaviour, ICancelHandler
    {
        [SerializeField] private PauseUI _pauseUI;

        private void Awake()
        {
            if (!_pauseUI)
                _pauseUI = GetComponentInParent<PauseUI>();
        }

        public void OnCancel(BaseEventData eventData)
        {
            if (!_pauseUI)
                return;

            eventData.Use();
            _pauseUI.HandleMainPanelCancel();
        }
    }
}