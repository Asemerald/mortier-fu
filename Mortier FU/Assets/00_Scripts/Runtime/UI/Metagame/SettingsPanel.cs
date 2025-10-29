using UnityEngine;

namespace MortierFu
{
    public class SettingsPanel : MonoBehaviour
    {
        private void Start()
        {
            Hide();
        }
    
        public void Show()
        {
            gameObject.SetActive(true);
        }
    
        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}