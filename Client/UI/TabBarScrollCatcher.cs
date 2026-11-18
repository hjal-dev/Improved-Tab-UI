using UnityEngine;
using UnityEngine.EventSystems;

namespace ImprovedTabUI.UI
{
    internal sealed class TabBarScrollCatcher : MonoBehaviour, IScrollHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        internal TabBar Bar;

        private bool _dragging;
        private float _lastX;

        public void OnScroll(PointerEventData eventData)
        {
            if (Bar != null)
            {
                Bar.Scroll(eventData.scrollDelta);
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _dragging = eventData.button == PointerEventData.InputButton.Left && TryLocalX(eventData, out _lastX);

            if (_dragging && Bar != null)
            {
                Bar.BeginDrag();
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_dragging || Bar == null || !TryLocalX(eventData, out float x))
            {
                return;
            }

            Bar.Drag(x - _lastX);
            _lastX = x;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (_dragging && Bar != null)
            {
                Bar.EndDrag();
            }

            _dragging = false;
        }

        private bool TryLocalX(PointerEventData eventData, out float x)
        {
            bool hit = RectTransformUtility.ScreenPointToLocalPointInRectangle(
                (RectTransform)transform, eventData.position, eventData.pressEventCamera, out Vector2 local);

            x = local.x;
            return hit;
        }
    }
}
