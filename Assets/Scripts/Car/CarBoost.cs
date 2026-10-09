using UnityEngine;

namespace BumperRoyale.Car
{
    /// <summary>
    /// Forward burst with a cooldown. Controllers call TryBoost(); the impulse is applied in FixedUpdate.
    /// </summary>
    [RequireComponent(typeof(CarMotor))]
    public class CarBoost : MonoBehaviour
    {
        private CarMotor motor;
        private bool boostQueued;
        private float boostEndTime = float.NegativeInfinity;
        private float readyTime;

        public bool IsBoosting => Time.time < boostEndTime;
        public bool IsReady => Time.time >= readyTime;

        /// <summary>0 = just used, 1 = ready.</summary>
        public float CooldownProgress =>
            motor.Stats.boostCooldown <= 0f ? 1f : Mathf.Clamp01(1f - (readyTime - Time.time) / motor.Stats.boostCooldown);

        private void Awake()
        {
            motor = GetComponent<CarMotor>();
        }

        public bool TryBoost()
        {
            if (!motor.InputEnabled || !IsReady || !motor.IsGrounded)
                return false;

            boostQueued = true;
            boostEndTime = Time.time + motor.Stats.boostDuration;
            readyTime = Time.time + motor.Stats.boostCooldown;
            return true;
        }

        private void FixedUpdate()
        {
            if (!boostQueued)
                return;
            boostQueued = false;

            Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
            motor.Body.AddForce(forward * motor.Stats.boostForce, ForceMode.VelocityChange);
        }
    }
}
