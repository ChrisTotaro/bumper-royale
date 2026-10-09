using UnityEngine;

namespace BumperRoyale.Arena
{
    /// <summary>Describes the circular arena so AI and spawning can reason about the edge.</summary>
    public class ArenaBounds : MonoBehaviour
    {
        [Tooltip("Radius of the playable platform (m).")]
        [SerializeField] private float radius = 15f;

        public static ArenaBounds Instance { get; private set; }

        public float Radius => radius;
        public Vector3 Center => transform.position;

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        /// <summary>Horizontal distance from the arena center.</summary>
        public float DistanceFromCenter(Vector3 worldPosition)
        {
            Vector3 offset = worldPosition - Center;
            offset.y = 0f;
            return offset.magnitude;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            const int segments = 64;
            for (int i = 0; i < segments; i++)
            {
                float a0 = i * Mathf.PI * 2f / segments;
                float a1 = (i + 1) * Mathf.PI * 2f / segments;
                Gizmos.DrawLine(Center + new Vector3(Mathf.Cos(a0), 0f, Mathf.Sin(a0)) * radius,
                    Center + new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1)) * radius);
            }
        }
    }
}
