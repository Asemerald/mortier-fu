using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MortierFu
{
    public class SelectOnHighlight : MonoBehaviour, IPointerEnterHandler
    {
        private Selectable _selectable;
        
        private void Awake()
        {
            _selectable = GetComponent<Selectable>();
        }
        
        public void OnPointerEnter(PointerEventData eventData)
        {
            _selectable.Select();
        }
    }
}