using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MortierFu
{
    public sealed class UISelectedScrollFollower : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ScrollRect _scrollRect;
        [SerializeField] private RectTransform _viewport;
        [SerializeField] private RectTransform _content;

        [Header("Settings")]
        [SerializeField] private float _padding = 12f;
        [SerializeField] private bool _followInLateUpdate = true;

        private GameObject _lastSelected;

        private void Awake() => ResolveReferences();

        private void LateUpdate()
        {
            if (_followInLateUpdate)
                FollowSelectedNow();
        }

        public void ResetToTop()
        {
            ResolveReferences();

            if (!_scrollRect || !_content)
                return;

            Canvas.ForceUpdateCanvases();

            Vector2 position = _content.anchoredPosition;
            position.y = 0f;
            _content.anchoredPosition = position;

            _scrollRect.verticalNormalizedPosition = 1f;
            _scrollRect.velocity = Vector2.zero;

            _lastSelected = null;
        }

        public void FollowSelectedNow()
        {
            ResolveReferences();

            if (!_scrollRect || !_viewport || !_content)
                return;

            EventSystem eventSystem = EventSystem.current;

            if (!eventSystem)
                return;

            GameObject selected = eventSystem.currentSelectedGameObject;

            if (!selected)
                return;

            RectTransform selectedRect = selected.transform as RectTransform;

            if (!selectedRect)
                return;

            if (!selectedRect.IsChildOf(_content))
                return;

            Canvas.ForceUpdateCanvases();

            Rect viewportRect = _viewport.rect;
            Bounds itemBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(_viewport, selectedRect);

            float viewportTop = viewportRect.yMax;
            float viewportBottom = viewportRect.yMin;

            float itemTop = itemBounds.max.y + _padding;
            float itemBottom = itemBounds.min.y - _padding;

            float scrollDelta = 0f;

            if (itemTop > viewportTop)
                scrollDelta = -(itemTop - viewportTop);
            else if (itemBottom < viewportBottom)
                scrollDelta = viewportBottom - itemBottom;

            if (Mathf.Approximately(scrollDelta, 0f) && ReferenceEquals(_lastSelected, selected))
                return;

            _lastSelected = selected;

            if (Mathf.Approximately(scrollDelta, 0f))
                return;

            Vector2 contentPosition = _content.anchoredPosition;
            contentPosition.y += scrollDelta;
            contentPosition.y = ClampContentY(contentPosition.y);

            _content.anchoredPosition = contentPosition;
            _scrollRect.velocity = Vector2.zero;
        }

        private float ClampContentY(float y)
        {
            if (!_viewport || !_content)
                return y;

            float maxY = Mathf.Max(0f, _content.rect.height - _viewport.rect.height);
            return Mathf.Clamp(y, 0f, maxY);
        }

        private void ResolveReferences()
        {
            if (!_scrollRect)
                _scrollRect = GetComponent<ScrollRect>();

            if (!_scrollRect) return;
            
            if (!_viewport)
                _viewport = _scrollRect.viewport;

            if (!_content)
                _content = _scrollRect.content;
        }
    }
}