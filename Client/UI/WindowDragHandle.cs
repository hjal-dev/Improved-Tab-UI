using EFT.UI;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ImprovedTabUI.UI
{
    internal sealed class WindowDragHandle : MonoBehaviour, IBeginDragHandler, IDragHandler, IPointerClickHandler
    {
        internal RectTransform Window;

        private Canvas _canvas;

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_canvas == null && Window != null)
            {
                Canvas canvas = Window.GetComponentInParent<Canvas>();
                _canvas = canvas != null ? canvas.rootCanvas : null;
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (Window == null)
            {
                return;
            }

            float scale = _canvas != null && _canvas.scaleFactor > 0f ? _canvas.scaleFactor : 1f;
            Window.anchoredPosition += eventData.delta / scale;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (Window == null || eventData.button != PointerEventData.InputButton.Left || eventData.clickCount != 2)
            {
                return;
            }

            Window.anchoredPosition = Vector2.zero;
            UiSounds.Play(EUISoundType.ButtonClick);
        }
    }
}
