using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MortierFu
{
    public class SelectOnHighlight : MonoBehaviour, IPointerEnterHandler
    {
        private Selectable _selectable;
        
        [SerializeField] private GameObject _highlight;
        
        private Button _button;
        
        private void Awake()
        {
            _selectable = GetComponent<Selectable>();
            _button = GetComponent<Button>();
            
        }
        
        public void OnPointerEnter(PointerEventData eventData)
        {
            _selectable.Select();
        }
        
        
    }
}