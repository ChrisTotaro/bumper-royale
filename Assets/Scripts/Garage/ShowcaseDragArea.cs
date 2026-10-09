using UnityEngine;
using UnityEngine.EventSystems;

namespace BumperRoyale.Garage
{
    /// <summary>
    /// Invisible UI surface around the showcased car. Dragging on it (touch or mouse) rotates the car.
    /// Needs a raycast-target Graphic on the same object.
    /// </summary>
    public class ShowcaseDragArea : MonoBehaviour, IPointerDownHandler, IDragHandler
    {
        [SerializeField] private CarShowcase showcase;

        // Pressing alone pauses the idle spin so the car holds still under the finger.
        public void OnPointerDown(PointerEventData eventData) => showcase.Drag(Vector2.zero);

        public void OnDrag(PointerEventData eventData) => showcase.Drag(eventData.delta);
    }
}
