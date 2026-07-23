using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MortierFu
{
    public class UIPanel : MonoBehaviour
    {
        [field: SerializeField] public Selectable DefaultButton { get; private set; }

        public virtual void Show()
        {
            gameObject.SetActive(true);
            SelectDefaultButton();
        }

        public virtual void Hide() => gameObject.SetActive(false);

        public bool IsVisible() => gameObject.activeSelf;

        protected void SelectDefaultButton()
        {
            if (!EventSystem.current || !DefaultButton)
                return;

            if (!DefaultButton.gameObject.activeInHierarchy || !DefaultButton.IsInteractable())
                return;

            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(DefaultButton.gameObject);
        }
    }
}