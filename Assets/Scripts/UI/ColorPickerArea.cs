using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BumperRoyale.UI
{
    /// <summary>
    /// A press-and-drag surface that reports the pointer position as 0..1 within its rect.
    /// Needs a raycast-target Graphic on the same object.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class ColorPickerArea : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        /// <summary>Pointer position, clamped to 0..1 on each axis (0,0 = bottom-left).</summary>
        public event Action<Vector2> Changed;
        public event Action Released;

        public void OnPointerDown(PointerEventData eventData) => Report(eventData);

        public void OnDrag(PointerEventData eventData) => Report(eventData);

        public void OnPointerUp(PointerEventData eventData) => Released?.Invoke();

        private void Report(PointerEventData eventData)
        {
            RectTransform rect = (RectTransform)transform;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    rect, eventData.position, eventData.pressEventCamera, out Vector2 local))
                return;
            Rect r = rect.rect;
            Vector2 normalized = new Vector2(
                Mathf.Clamp01((local.x - r.xMin) / r.width),
                Mathf.Clamp01((local.y - r.yMin) / r.height));
            Changed?.Invoke(normalized);
        }
    }
}
