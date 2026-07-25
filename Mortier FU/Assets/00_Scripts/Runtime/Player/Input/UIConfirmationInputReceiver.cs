using MortierFu.Shared;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MortierFu
{
    public sealed class UIConfirmationInputReceiver : MonoBehaviour, ISubmitHandler, ICancelHandler, IMoveHandler
    {
        [SerializeField] private UIConfirmationModalController _modalController;

        private void Awake()
        {
            if (!_modalController)
                _modalController = GetComponentInParent<UIConfirmationModalController>();
        }

        public void OnSubmit(BaseEventData eventData)
        {
            eventData.Use();

            if (_modalController)
                _modalController.RequestSubmitFromInput();
            else
                Logs.LogError("[UIConfirmationInputReceiver] ModalController reference is missing.");
        }

        public void OnCancel(BaseEventData eventData)
        {
            eventData.Use();

            if (_modalController)
                _modalController.RequestCancelFromInput();
            else
                Logs.LogError("[UIConfirmationInputReceiver] ModalController reference is missing.");
        }

        public void OnMove(AxisEventData eventData)
        {
            eventData.Use();
        }
    }
}