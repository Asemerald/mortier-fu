using UnityEngine;

namespace MortierFu
{
    public class UIPanel : MonoBehaviour
    {
        public virtual void Show()
        {
            gameObject.SetActive(true);
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