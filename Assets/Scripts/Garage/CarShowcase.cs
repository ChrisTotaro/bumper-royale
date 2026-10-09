using System.Collections.Generic;
using BumperRoyale.Car;
using UnityEngine;

namespace BumperRoyale.Garage
{
    /// <summary>
    /// Shows the player car on a turntable in the main menu. Spawns a display-only copy of the car
    /// prefab (gameplay components stripped), keeps it at a fixed spot on screen, spins it slowly,
    /// and lets the player rotate it with <see cref="Drag"/>.
    /// </summary>
    public class CarShowcase : MonoBehaviour
    {
        private const float MaxSpinStep = 0.1f;

        [SerializeField] private GameObject carPrefab;
        [Tooltip("Rotated by dragging. The car is centered on it.")]
        [SerializeField] private Transform pivot;
        [Tooltip("Optional base placed under the car. Rotates with it.")]
        [SerializeField] private Transform pedestal;
        [SerializeField] private Camera viewCamera;

        [Header("Placement")]
        [Tooltip("Where the car's center sits on screen (0..1 viewport coordinates).")]
        [SerializeField] private Vector2 viewportPosition = new Vector2(0.3f, 0.5f);
        [Tooltip("Distance from the camera to the car's center (m).")]
        [SerializeField] private float distance = 5.5f;

        [Header("Rotation")]
        [Tooltip("Degrees turned by dragging across the full screen height.")]
        [SerializeField] private float degreesPerScreenHeight = 270f;
        [Tooltip("0 shows the front of the car.")]
        [SerializeField] private float startYaw = 35f;
        [SerializeField] private float startPitch = 20f;
        [SerializeField] private float minPitch = -5f;
        [SerializeField] private float maxPitch = 60f;
        [Tooltip("Idle spin speed (degrees per second).")]
        [SerializeField] private float autoSpinSpeed = 20f;
        [Tooltip("Seconds after the last drag before the idle spin resumes.")]
        [SerializeField] private float autoSpinDelay = 2.5f;

        private CarAppearance appearance;
        private float yaw;
        private float pitch;
        private float lastDragTime = float.NegativeInfinity;

        /// <summary>The displayed car. Spawned on first access.</summary>
        public CarAppearance Appearance
        {
            get
            {
                if (appearance == null)
                    Spawn();
                return appearance;
            }
        }

        private void Awake()
        {
            yaw = startYaw;
            pitch = startPitch;
            if (viewCamera == null)
                viewCamera = Camera.main;
            if (appearance == null)
                Spawn();
        }

        private void LateUpdate()
        {
            // Capped step: the first frame after a scene load (or an app resume) can be seconds long.
            if (Time.unscaledTime - lastDragTime > autoSpinDelay)
                yaw += autoSpinSpeed * Mathf.Min(Time.unscaledDeltaTime, MaxSpinStep);

            Transform cam = viewCamera.transform;
            pivot.position = viewCamera.ViewportToWorldPoint(
                new Vector3(viewportPosition.x, viewportPosition.y, distance));
            // Yaw spins the car on the turntable; positive pitch tilts its top toward the camera.
            Quaternion facingCamera = Quaternion.LookRotation(-cam.forward, cam.up);
            pivot.rotation = Quaternion.AngleAxis(-pitch, cam.right)
                             * Quaternion.AngleAxis(yaw, cam.up)
                             * facingCamera;
        }

        /// <summary>Rotates the car by a pointer drag, in screen pixels.</summary>
        public void Drag(Vector2 screenDelta)
        {
            float scale = degreesPerScreenHeight / Mathf.Max(1, Screen.height);
            // Dragging moves the near side of the car with the finger.
            yaw -= screenDelta.x * scale;
            pitch = Mathf.Clamp(pitch - screenDelta.y * scale, minPitch, maxPitch);
            lastDragTime = Time.unscaledTime;
        }

        private void Spawn()
        {
            // Instantiate under an inactive holder so no gameplay component ever wakes up.
            var holder = new GameObject("Holder");
            holder.SetActive(false);
            GameObject car = Instantiate(carPrefab, holder.transform);
            car.name = "DisplayCar";
            StripGameplayComponents(car);

            Quaternion savedRotation = pivot.rotation;
            pivot.rotation = Quaternion.identity;
            car.transform.SetParent(pivot, false);
            car.transform.localPosition = Vector3.zero;
            car.transform.localRotation = Quaternion.identity;
            Destroy(holder);

            // Center the car's visible bounds on the pivot so it spins about its middle.
            Bounds bounds = GetRendererBounds(car);
            Vector3 centerOffset = pivot.InverseTransformPoint(bounds.center);
            car.transform.localPosition = -centerOffset;
            if (pedestal != null)
            {
                float carBottom = pivot.InverseTransformPoint(bounds.min).y - centerOffset.y;
                float pedestalHalfHeight = GetRendererBounds(pedestal.gameObject).extents.y;
                pedestal.localPosition = new Vector3(0f, carBottom - pedestalHalfHeight, 0f);
            }
            pivot.rotation = savedRotation;

            appearance = car.GetComponent<CarAppearance>();
        }

        private static Bounds GetRendererBounds(GameObject root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        /// <summary>Removes everything except visuals and CarAppearance, respecting RequireComponent.</summary>
        private static void StripGameplayComponents(GameObject car)
        {
            var components = new List<Component>();
            bool removedAny = true;
            while (removedAny)
            {
                removedAny = false;
                car.GetComponentsInChildren(true, components);
                foreach (Component component in components)
                {
                    if (component == null || IsVisual(component) || IsRequiredByAnother(component, components))
                        continue;
                    DestroyImmediate(component);
                    removedAny = true;
                }
            }
        }

        private static bool IsVisual(Component c)
        {
            return c is Transform || c is MeshFilter || c is Renderer || c is CarAppearance;
        }

        private static bool IsRequiredByAnother(Component component, List<Component> all)
        {
            foreach (Component other in all)
            {
                if (other == null || other == component || other.gameObject != component.gameObject)
                    continue;
                foreach (RequireComponent require in other.GetType().GetCustomAttributes(typeof(RequireComponent), true))
                {
                    if (IsOfType(component, require.m_Type0) || IsOfType(component, require.m_Type1)
                        || IsOfType(component, require.m_Type2))
                        return true;
                }
            }
            return false;
        }

        private static bool IsOfType(Component component, System.Type type)
        {
            return type != null && type.IsInstanceOfType(component);
        }
    }
}
