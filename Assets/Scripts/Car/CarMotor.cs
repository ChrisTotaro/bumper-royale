using UnityEngine;

namespace BumperRoyale.Car
{
    /// <summary>
    /// Arcade bumper-car handling. Takes throttle/steer input from any controller (player or AI)
    /// and applies it to the Rigidbody in FixedUpdate. Drive forces only apply while grounded,
    /// so a car that leaves the arena tumbles freely.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class CarMotor : MonoBehaviour
    {
        [SerializeField] private CarStats stats;

        private Rigidbody body;
        private float throttle;
        private float steer;
        private float stunnedUntil;

        public CarStats Stats => stats;
        public Rigidbody Body => body;

        /// <summary>When false, input is ignored (countdown, eliminated).</summary>
        public bool InputEnabled { get; set; } = true;
        public bool IsGrounded { get; private set; }
        public float ForwardSpeed => Vector3.Dot(body.linearVelocity, transform.forward);
        public bool IsStunned => Time.time < stunnedUntil;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.centerOfMass = stats.centerOfMassOffset;
        }

        /// <param name="throttleInput">-1 (reverse) .. 1 (forward)</param>
        /// <param name="steerInput">-1 (left) .. 1 (right)</param>
        public void SetInput(float throttleInput, float steerInput)
        {
            throttle = Mathf.Clamp(throttleInput, -1f, 1f);
            steer = Mathf.Clamp(steerInput, -1f, 1f);
        }

        /// <summary>Temporarily drops grip so a rammed car slides instead of sticking.</summary>
        public void Stun(float seconds)
        {
            stunnedUntil = Mathf.Max(stunnedUntil, Time.time + seconds);
        }

        private void FixedUpdate()
        {
            IsGrounded = CheckGround(out Vector3 groundNormal);
            if (!IsGrounded)
                return;

            float dt = Time.fixedDeltaTime;
            float t = InputEnabled ? throttle : 0f;
            float s = InputEnabled ? steer : 0f;

            Vector3 forward = Vector3.ProjectOnPlane(transform.forward, groundNormal).normalized;
            float speed = ForwardSpeed;

            // Throttle, capped at max speed. Boost can push past the cap; throttle just stops adding.
            if (t > 0f && speed < stats.maxSpeed || t < 0f && speed > -stats.reverseMaxSpeed)
                body.AddForce(forward * (t * stats.acceleration), ForceMode.Acceleration);
            else if (Mathf.Approximately(t, 0f))
                body.AddForce(-forward * (speed * stats.coastDrag), ForceMode.Acceleration);

            // Steering: drive yaw rate directly. Reverse flips steering like a real car.
            float speedFactor = Mathf.Max(Mathf.Clamp01(Mathf.Abs(speed) / stats.fullSteerSpeed), stats.steerAtLowSpeedFactor);
            float direction = speed < -0.5f ? -1f : 1f;
            float targetYawRate = s * stats.steerSpeed * Mathf.Deg2Rad * speedFactor * direction;
            Vector3 angular = body.angularVelocity;
            angular.y = Mathf.Lerp(angular.y, targetYawRate, stats.steerResponsiveness * dt);
            body.angularVelocity = angular;

            // Lateral grip: cancel sideways sliding (much less while stunned by a hit).
            float grip = IsStunned ? stats.lateralGrip * stats.stunnedGripFactor : stats.lateralGrip;
            float lateral = Vector3.Dot(body.linearVelocity, transform.right);
            body.AddForce(-transform.right * (lateral * grip), ForceMode.Acceleration);

            // Overspeed damping: bleed off horizontal speed above the cap (from hits or boosts).
            Vector3 horizontal = Vector3.ProjectOnPlane(body.linearVelocity, Vector3.up);
            float excess = horizontal.magnitude - stats.maxSpeed;
            if (excess > 0f)
                body.AddForce(-horizontal.normalized * (excess * stats.overspeedDrag), ForceMode.Acceleration);
        }

        private bool CheckGround(out Vector3 normal)
        {
            normal = Vector3.up;
            // Upside down or on its side: not grounded.
            if (Vector3.Dot(transform.up, Vector3.up) < 0.5f)
                return false;

            const float originHeight = 0.2f;
            Vector3 origin = transform.position + transform.up * originHeight;
            if (Physics.Raycast(origin, -transform.up, out RaycastHit hit, originHeight + stats.groundCheckDistance,
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                normal = hit.normal;
                return true;
            }
            return false;
        }
    }
}
