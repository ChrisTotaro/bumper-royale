using BumperRoyale.Car;
using BumperRoyale.Match;
using UnityEngine;

namespace BumperRoyale.Player
{
    /// <summary>
    /// Ring on the ground under the player's car, like the player marker in sports games.
    /// Follows the player from MatchManager, lies flat on the surface below the car at a fixed size,
    /// and hides when there is no ground under the car or the player is eliminated.
    /// </summary>
    public class PlayerIndicator : MonoBehaviour
    {
        [Tooltip("The ring visual. Its mesh should have radius 1 so the scale equals the radius.")]
        [SerializeField] private Transform ring;
        [Tooltip("Ring radius (m). The car is about 2 x 2.7 m.")]
        [SerializeField] private float radius = 1.9f;
        [Tooltip("Height above the ground, to avoid z-fighting (m).")]
        [SerializeField] private float surfaceOffset = 0.02f;
        [Tooltip("How far below the car to look for ground (m).")]
        [SerializeField] private float maxGroundDistance = 1.5f;

        private void LateUpdate()
        {
            CarElimination player = MatchManager.Instance != null ? MatchManager.Instance.Player : null;
            if (player == null || player.IsEliminated || !TryFindGround(player.transform, out RaycastHit hit))
            {
                ring.gameObject.SetActive(false);
                return;
            }

            ring.gameObject.SetActive(true);
            ring.SetPositionAndRotation(
                hit.point + hit.normal * surfaceOffset,
                Quaternion.FromToRotation(Vector3.up, hit.normal));
            ring.localScale = Vector3.one * radius;
        }

        private bool TryFindGround(Transform car, out RaycastHit hit)
        {
            // Start inside the car's collider: a ray never hits the collider it starts in.
            Vector3 origin = car.position + Vector3.up * 0.3f;
            return Physics.Raycast(origin, Vector3.down, out hit, maxGroundDistance + 0.3f,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        }
    }
}
