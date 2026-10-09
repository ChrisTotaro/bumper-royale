using System.Collections.Generic;
using UnityEngine;

namespace BumperRoyale.Car
{
    /// <summary>
    /// Arcade ramming. When this car drives into another car, it pushes the other car away with a
    /// velocity change that scales with the relative impact speed and with how much of the closing
    /// speed came from this car. Boosted rams knock farther. Values come from the rammer's CarStats.
    /// </summary>
    [RequireComponent(typeof(CarMotor))]
    public class CarKnockback : MonoBehaviour
    {
        private const float RepeatHitCooldown = 0.25f;

        private CarMotor motor;
        private CarBoost boost;
        private readonly Dictionary<CarKnockback, float> lastHitTimes = new Dictionary<CarKnockback, float>();

        /// <summary>Velocity before the latest physics step (OnCollisionEnter sees post-impact velocities).</summary>
        public Vector3 PreImpactVelocity { get; private set; }
        public CarMotor Motor => motor;

        private void Awake()
        {
            motor = GetComponent<CarMotor>();
            boost = GetComponent<CarBoost>();
        }

        private void FixedUpdate()
        {
            PreImpactVelocity = motor.Body.linearVelocity;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.rigidbody == null)
                return;
            CarKnockback other = collision.rigidbody.GetComponent<CarKnockback>();
            if (other == null || other == this)
                return;

            CarStats stats = motor.Stats;
            float impactSpeed = collision.relativeVelocity.magnitude;
            if (impactSpeed < stats.minImpactSpeed)
                return;

            if (lastHitTimes.TryGetValue(other, out float lastHit) && Time.time - lastHit < RepeatHitCooldown)
                return;

            Vector3 toOther = other.transform.position - transform.position;
            toOther.y = 0f;
            if (toOther.sqrMagnitude < 0.0001f)
                return;
            toOther.Normalize();

            // Who rammed whom: each car's closing speed along the line between them.
            float myApproach = Mathf.Max(0f, Vector3.Dot(PreImpactVelocity, toOther));
            float theirApproach = Mathf.Max(0f, Vector3.Dot(other.PreImpactVelocity, -toOther));
            if (myApproach < 0.1f)
                return; // We were hit, not hitting. The other car handles pushing us.
            float ramShare = myApproach / (myApproach + theirApproach);

            float strength = (stats.knockbackBase + impactSpeed * stats.knockbackPerRelativeSpeed) * ramShare;
            if (boost != null && boost.IsBoosting)
                strength *= stats.boostKnockbackMultiplier;
            strength = Mathf.Min(strength, stats.maxKnockback);

            Vector3 direction = (toOther + Vector3.up * stats.knockbackUpward).normalized;
            other.motor.Body.AddForce(direction * strength, ForceMode.VelocityChange);
            other.motor.Stun(stats.knockbackStunDuration);

            lastHitTimes[other] = Time.time;
        }
    }
}
