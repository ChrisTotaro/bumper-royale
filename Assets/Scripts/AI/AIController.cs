using BumperRoyale.Arena;
using BumperRoyale.Car;
using UnityEngine;

namespace BumperRoyale.AI
{
    /// <summary>
    /// Bot driver. Picks a target among the other live cars, steers at its predicted position,
    /// boosts when lined up, and steers back toward the center when close to the arena edge.
    /// Drives the same CarMotor/CarBoost API as the player.
    /// </summary>
    [RequireComponent(typeof(CarMotor), typeof(CarBoost))]
    public class AIController : MonoBehaviour
    {
        private const float FallbackArenaRadius = 15f;
        private const float BoostTravelEstimate = 6f;

        private CarMotor motor;
        private CarBoost boost;
        private CarElimination self;
        private CarElimination target;
        private float nextRetargetTime;

        public CarElimination Target => target;

        private void Awake()
        {
            motor = GetComponent<CarMotor>();
            boost = GetComponent<CarBoost>();
            self = GetComponent<CarElimination>();
        }

        private void Start()
        {
            // Stagger bots so they don't all retarget on the same frame.
            nextRetargetTime = Time.time + Random.Range(0f, 0.5f);
        }

        private void Update()
        {
            CarStats stats = motor.Stats;

            if (target == null || target.IsEliminated || Time.time >= nextRetargetTime)
            {
                target = PickTarget(stats);
                nextRetargetTime = Time.time + stats.targetRetargetInterval * Random.Range(0.75f, 1.25f);
            }

            Vector3 center = ArenaBounds.Instance != null ? ArenaBounds.Instance.Center : Vector3.zero;
            float radius = ArenaBounds.Instance != null ? ArenaBounds.Instance.Radius : FallbackArenaRadius;
            Vector3 position = transform.position;
            Vector3 forward = Flat(transform.forward).normalized;

            // Attack direction: lead the target a little based on distance.
            Vector3 desired;
            float targetDistance = float.MaxValue;
            if (target != null)
            {
                Vector3 targetPos = target.transform.position;
                targetDistance = Flat(targetPos - position).magnitude;
                float leadTime = Mathf.Clamp(targetDistance / Mathf.Max(motor.ForwardSpeed, 5f), 0f, 0.8f);
                Vector3 predicted = targetPos + Flat(target.Motor.Body.linearVelocity) * leadTime;
                desired = Flat(predicted - position).normalized;
            }
            else
            {
                desired = Flat(center - position).normalized;
            }

            // Edge avoidance: look ahead along the current velocity so fast cars react in time,
            // then blend toward the center when near the edge and heading or sliding outward.
            Vector3 toCenter = Flat(center - position);
            float distFromCenter = toCenter.magnitude;
            toCenter = distFromCenter > 0.01f ? toCenter / distFromCenter : Vector3.zero;
            Vector3 flatVelocity = Flat(motor.Body.linearVelocity);
            float predictedDist = Flat(position + flatVelocity * stats.aiEdgeLookahead - center).magnitude;
            float dangerDist = Mathf.Max(distFromCenter, predictedDist);
            float edgeStart = radius - stats.edgeAvoidMargin;
            bool inDanger = false;
            bool movingOutward = false;
            if (dangerDist > edgeStart)
            {
                float edgeT = Mathf.Clamp01((dangerDist - edgeStart) / Mathf.Max(stats.edgeAvoidMargin, 0.01f));
                Vector3 moveDir = flatVelocity.sqrMagnitude > 1f ? flatVelocity.normalized : forward;
                movingOutward = Vector3.Dot(moveDir, -toCenter) > 0.2f;
                float outward = Mathf.Max(Vector3.Dot(moveDir, -toCenter), Vector3.Dot(forward, -toCenter));
                if (outward > -0.2f)
                {
                    desired = (desired + toCenter * (stats.edgeAvoidStrength * edgeT * 2f)).normalized;
                    inDanger = edgeT > 0.3f && outward > 0.2f;
                }
            }

            float angle = Vector3.SignedAngle(forward, desired, Vector3.up);
            float steer = Mathf.Clamp(angle / Mathf.Max(stats.aiSteerAngleForFullInput, 1f), -1f, 1f);
            float throttle = Mathf.Lerp(0.6f, 1f, stats.aggression);
            if (Mathf.Abs(angle) > 90f)
                throttle *= 0.4f; // Ease off to turn tighter.

            if (inDanger)
            {
                bool facingOutward = Vector3.Dot(forward, -toCenter) > 0f;
                if (facingOutward && movingOutward && motor.ForwardSpeed > 1f)
                    throttle = -1f; // Brake hard before the edge.
                else if (Mathf.Abs(Vector3.SignedAngle(forward, toCenter, Vector3.up)) > 100f)
                    throttle = -1f; // Facing off the edge: back up toward the center instead.
            }

            // Reversing flips the yaw direction in CarMotor, so flip steering to keep turning toward 'desired'.
            if (motor.ForwardSpeed < -0.5f)
                steer = -steer;

            motor.SetInput(throttle, steer);

            if (target != null && !inDanger && boost.IsReady
                && targetDistance < stats.aiBoostRange
                && Mathf.Abs(angle) < stats.aiBoostAngle
                && Flat(position + forward * BoostTravelEstimate - center).magnitude < radius - 1f
                && Random.value < stats.aggression * Time.deltaTime * 4f)
            {
                boost.TryBoost();
            }
        }

        private CarElimination PickTarget(CarStats stats)
        {
            Vector3 center = ArenaBounds.Instance != null ? ArenaBounds.Instance.Center : Vector3.zero;
            float radius = ArenaBounds.Instance != null ? ArenaBounds.Instance.Radius : FallbackArenaRadius;

            CarElimination best = null;
            float bestScore = float.MaxValue;
            foreach (CarElimination car in CarElimination.Active)
            {
                if (car == self || car.IsEliminated)
                    continue;
                float distance = Flat(car.transform.position - transform.position).magnitude;
                // Prefer cars close to the edge: they're easier to knock off.
                float edgeness = Mathf.Clamp01(Flat(car.transform.position - center).magnitude / radius);
                // Low aggression = more random target choice; high aggression = nearest victim.
                float noise = Random.Range(1f, 1f + (1f - stats.aggression) * 2f);
                float score = distance * Mathf.Lerp(1f, 0.6f, edgeness) * noise;
                if (score < bestScore)
                {
                    bestScore = score;
                    best = car;
                }
            }
            return best;
        }

        private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
