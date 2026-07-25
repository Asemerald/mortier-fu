using UnityEngine;
using UnityEngine.EventSystems;

namespace MortierFu
{
    public sealed class UICancelBackReceiver : MonoBehaviour, ICancelHandler
    {
        private IUIBackHandler _backHandler;

        private void Awake()
        {
            _backHandler = GetComponentInParent<IUIBackHandler>();
        }

        public void OnCancel(BaseEventData eventData)
        {
            eventData.Use();

            if (_backHandler == null)
                return;

            _backHandler.HandleUIBack();
        }
    }
}