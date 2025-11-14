using UnityEngine;

namespace MortierFu
{
    public class UIPanel : MonoBehaviour
    {
        public void Show()
        {
            gameObject.SetActive(true);
        }
        
        public void Hide()
        {
            gameObject.SetActive(false);
        }
        
        public bool IsVisible()
        {
            return gameObject.activeSelf;
        }
    }
}