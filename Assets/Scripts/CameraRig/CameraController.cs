using UnityEngine;

namespace BumperRoyale.CameraRig
{
    /// <summary>
    /// Angled top-down camera. Looks at a point between the arena center and the target,
    /// so it drifts toward the player but keeps the whole arena readable. Rotation is fixed.
    /// </summary>
    public class CameraController : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 arenaCenter = Vector3.zero;
        [Tooltip("0 = always look at the arena center, 1 = fully follow the target.")]
        [Range(0f, 1f)] [SerializeField] private float followFactor = 0.35f;
        [SerializeField] private float pitch = 55f;
        [SerializeField] private float yaw = 0f;
        [SerializeField] private float distance = 30f;
        [SerializeField] private float smoothTime = 0.35f;

        private Vector3 velocity;

        public Transform Target
        {
            get => target;
            set => target = value;
        }

        private void Start()
        {
            transform.SetPositionAndRotation(DesiredPosition(), Quaternion.Euler(pitch, yaw, 0f));
        }

        private void LateUpdate()
        {
            transform.position = Vector3.SmoothDamp(transform.position, DesiredPosition(), ref velocity, smoothTime);
            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }

        private Vector3 DesiredPosition()
        {
            Vector3 focus = arenaCenter;
            // Stop following once the target has fallen well below the arena.
            if (target != null && target.position.y > arenaCenter.y - 3f)
                focus = Vector3.Lerp(arenaCenter, target.position, followFactor);
            focus.y = arenaCenter.y;
            return focus + Quaternion.Euler(pitch, yaw, 0f) * Vector3.back * distance;
        }
    }
}
