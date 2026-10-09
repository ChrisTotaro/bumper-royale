using System;
using System.Collections.Generic;
using BumperRoyale.Arena;
using UnityEngine;

namespace BumperRoyale.Car
{
    /// <summary>
    /// Tracks which cars are still in the match. A car is eliminated when it enters a KillZone
    /// (or falls below a fallback height), after which it stops taking input and is removed.
    /// </summary>
    [RequireComponent(typeof(CarMotor))]
    public class CarElimination : MonoBehaviour
    {
        private static readonly List<CarElimination> active = new List<CarElimination>();

        [Tooltip("Eliminate the car if it falls below this height even without touching a kill zone.")]
        [SerializeField] private float fallbackKillHeight = -15f;
        [Tooltip("Seconds after elimination before the car is destroyed.")]
        [SerializeField] private float destroyDelay = 3f;

        private CarMotor motor;

        /// <summary>Cars that are in play and not eliminated.</summary>
        public static IReadOnlyList<CarElimination> Active => active;

        /// <summary>Raised once for any car that gets eliminated.</summary>
        public static event Action<CarElimination> AnyEliminated;

        public bool IsEliminated { get; private set; }
        public CarMotor Motor => motor;

        private void Awake()
        {
            motor = GetComponent<CarMotor>();
        }

        private void OnEnable()
        {
            if (!IsEliminated && !active.Contains(this))
                active.Add(this);
        }

        private void OnDisable()
        {
            active.Remove(this);
        }

        private void Update()
        {
            if (!IsEliminated && transform.position.y < fallbackKillHeight)
                Eliminate();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsEliminated && other.GetComponent<KillZone>() != null)
                Eliminate();
        }

        public void Eliminate()
        {
            if (IsEliminated)
                return;
            IsEliminated = true;
            motor.InputEnabled = false;
            active.Remove(this);
            AnyEliminated?.Invoke(this);
            Destroy(gameObject, destroyDelay);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            // Support "Enter Play Mode" without domain reload.
            active.Clear();
            AnyEliminated = null;
        }
    }
}
