using UnityEngine;

namespace BumperRoyale.Car
{
    /// <summary>
    /// All tuning values for a car. Player and bots each use their own asset so they can be tuned independently.
    /// </summary>
    [CreateAssetMenu(fileName = "CarStats", menuName = "Bumper Royale/Car Stats")]
    public class CarStats : ScriptableObject
    {
        [Header("Driving")]
        [Tooltip("Top forward speed (m/s) that throttle can reach. Boost can exceed it.")]
        public float maxSpeed = 14f;
        [Tooltip("Top reverse speed (m/s).")]
        public float reverseMaxSpeed = 7f;
        [Tooltip("Throttle acceleration (m/s^2).")]
        public float acceleration = 18f;
        [Tooltip("Deceleration applied while no throttle is held (1/s, fraction of forward speed removed per second).")]
        public float coastDrag = 1.2f;

        [Header("Steering")]
        [Tooltip("Maximum yaw rate (degrees/s).")]
        public float steerSpeed = 150f;
        [Tooltip("Forward speed (m/s) at which steering reaches full strength.")]
        public float fullSteerSpeed = 4f;
        [Tooltip("Steering strength while (nearly) stationary, 0..1. Bumper cars can turn on the spot.")]
        [Range(0f, 1f)] public float steerAtLowSpeedFactor = 0.5f;
        [Tooltip("How quickly the yaw rate follows the steering input.")]
        public float steerResponsiveness = 10f;
        [Tooltip("How strongly sideways sliding is cancelled while grounded (1/s). Higher = grippier.")]
        public float lateralGrip = 6f;

        [Header("Physics")]
        [Tooltip("Rigidbody center of mass, local space. Lower = more stable on the arena.")]
        public Vector3 centerOfMassOffset = new Vector3(0f, -0.4f, 0f);
        [Tooltip("Extra ray length below the car used to detect ground.")]
        public float groundCheckDistance = 0.35f;

        [Header("Boost")]
        [Tooltip("Instant forward velocity added by a boost (m/s).")]
        public float boostForce = 12f;
        [Tooltip("How long a boost counts as 'boosting' for knockback (s).")]
        public float boostDuration = 0.6f;
        [Tooltip("Time between boosts (s).")]
        public float boostCooldown = 3f;

        [Header("Knockback")]
        [Tooltip("Approach speed (m/s) below which a contact is not treated as a ram.")]
        public float minImpactSpeed = 1.5f;
        [Tooltip("Flat knockback velocity applied to the rammed car (m/s).")]
        public float knockbackBase = 3f;
        [Tooltip("Extra knockback per m/s of relative impact speed.")]
        public float knockbackPerRelativeSpeed = 0.8f;
        [Tooltip("Knockback multiplier when the rammer is boosting.")]
        public float boostKnockbackMultiplier = 1.8f;
        [Tooltip("Upward share of the knockback direction (0 = flat).")]
        public float knockbackUpward = 0.15f;
        [Tooltip("How long a rammed car loses grip so it slides (s).")]
        public float knockbackStunDuration = 0.6f;
        [Tooltip("Grip multiplier while stunned by a hit.")]
        [Range(0f, 1f)] public float stunnedGripFactor = 0.1f;

        [Header("AI")]
        [Tooltip("0..1. Higher = more throttle, more boosting and prefers the closest target.")]
        [Range(0f, 1f)] public float aggression = 0.7f;
        [Tooltip("Seconds between target re-evaluations.")]
        public float targetRetargetInterval = 2.5f;
        [Tooltip("Distance from the arena edge (m) where bots start steering back to the center.")]
        public float edgeAvoidMargin = 4f;
        [Tooltip("How strongly the edge-avoid direction overrides the attack direction.")]
        public float edgeAvoidStrength = 1.5f;
        [Tooltip("Max distance to target (m) at which a bot will boost.")]
        public float aiBoostRange = 7f;
        [Tooltip("Max heading error (degrees) at which a bot will boost.")]
        public float aiBoostAngle = 12f;
        [Tooltip("Steering gain: degrees of heading error that give full steer input.")]
        public float aiSteerAngleForFullInput = 35f;
    }
}
