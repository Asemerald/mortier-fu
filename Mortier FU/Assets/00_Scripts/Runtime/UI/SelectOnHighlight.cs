using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MortierFu
{
    public class SelectOnHighlight : MonoBehaviour
    {
        private Selectable _selectable;
        
        [SerializeField] private GameObject _highlight;
        [SerializeField] private GameObject _unhighlight;
        
        private Button _button;
        
        private void Awake()
        {
            _selectable = GetComponent<Selectable>();
            _button = GetComponent<Button>();
        }

        private void Update()
        {
            if (_selectable == null) return;

            bool isSelected = EventSystem.current.currentSelectedGameObject == _selectable.gameObject;

            if (_highlight != null)
                _highlight.SetActive(isSelected);

            if (_unhighlight != null)
                _unhighlight.SetActive(!isSelected);
        }
    }
}