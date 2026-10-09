using UnityEngine;

namespace BumperRoyale.Arena
{
    /// <summary>Marks a trigger volume below the arena. Cars entering it are eliminated.</summary>
    [RequireComponent(typeof(Collider))]
    public class KillZone : MonoBehaviour
    {
        private void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
        }
    }
}
