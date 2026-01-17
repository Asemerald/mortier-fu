using UnityEngine;
using UnityEngine.UI;

namespace MortierFu
{
    public class UIPanel : MonoBehaviour
    {
        [field:SerializeField] public Selectable DefaultButton { get; private set; }
        public virtual void Show()
        {
            gameObject.SetActive(true);
            if (DefaultButton != null)
            {
                DefaultButton.Select();
            }
        }
        
        public virtual void Hide()
        {
            gameObject.SetActive(false);
        }
        
        public bool IsVisible()
        {
            return gameObject.activeSelf;
        }
    }
}