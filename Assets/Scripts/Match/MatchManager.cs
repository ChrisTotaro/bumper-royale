using System;
using System.Collections;
using System.Collections.Generic;
using BumperRoyale.Arena;
using BumperRoyale.CameraRig;
using BumperRoyale.Car;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BumperRoyale.Match
{
    /// <summary>
    /// Runs one match: spawns the player and bots evenly around the arena, counts down 3-2-1-GO
    /// (cars frozen until GO), tracks eliminations and decides Victory/Defeat.
    /// </summary>
    public class MatchManager : MonoBehaviour
    {
        public enum MatchState { Countdown, Playing, Ended }

        [Header("Spawning")]
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private GameObject botPrefab;
        [Tooltip("One color per bot, assigned in order.")]
        [SerializeField] private Material[] botMaterials;
        [SerializeField] private int totalCars = 6;
        [Tooltip("Distance from the arena center where cars spawn (m).")]
        [SerializeField] private float spawnRadius = 10f;
        [SerializeField] private float spawnHeight = 0.05f;

        [Header("Flow")]
        [SerializeField] private float countdownStepSeconds = 1f;
        [SerializeField] private float goLabelSeconds = 0.8f;

        [Header("References")]
        [SerializeField] private CameraController cameraController;

        private readonly List<CarMotor> motors = new List<CarMotor>();

        public static MatchManager Instance { get; private set; }

        public MatchState State { get; private set; } = MatchState.Countdown;
        public CarElimination Player { get; private set; }
        public bool PlayerWon { get; private set; }
        public int CarsRemaining => CarElimination.Active.Count;
        /// <summary>"3", "2", "1", "GO!" or empty.</summary>
        public string CountdownLabel { get; private set; } = string.Empty;

        public event Action<MatchState> StateChanged;

        private void Awake()
        {
            Instance = this;
        }

        private void OnEnable()
        {
            CarElimination.AnyEliminated += HandleCarEliminated;
        }

        private void OnDisable()
        {
            CarElimination.AnyEliminated -= HandleCarEliminated;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void Start()
        {
            SpawnCars();
            StartCoroutine(CountdownRoutine());
        }

        private void SpawnCars()
        {
            Vector3 center = ArenaBounds.Instance != null ? ArenaBounds.Instance.Center : Vector3.zero;

            for (int i = 0; i < totalCars; i++)
            {
                // Player starts at the bottom of the screen (nearest the camera), bots go around.
                float angle = (-90f + i * 360f / totalCars) * Mathf.Deg2Rad;
                Vector3 position = center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * spawnRadius;
                position.y = center.y + spawnHeight;
                Vector3 toCenter = center - position;
                toCenter.y = 0f;
                Quaternion rotation = Quaternion.LookRotation(toCenter.normalized, Vector3.up);

                bool isPlayer = i == 0;
                GameObject car = Instantiate(isPlayer ? playerPrefab : botPrefab, position, rotation);
                car.name = isPlayer ? "Player" : $"Bot {i}";

                if (!isPlayer && botMaterials != null && botMaterials.Length > 0)
                {
                    CarAppearance appearance = car.GetComponent<CarAppearance>();
                    if (appearance != null)
                        appearance.SetColorMaterial(botMaterials[(i - 1) % botMaterials.Length]);
                }

                CarMotor motor = car.GetComponent<CarMotor>();
                motor.InputEnabled = false;
                motors.Add(motor);

                if (isPlayer)
                {
                    Player = car.GetComponent<CarElimination>();
                    if (cameraController != null)
                        cameraController.Target = car.transform;
                }
            }
        }

        private IEnumerator CountdownRoutine()
        {
            SetState(MatchState.Countdown);
            for (int n = 3; n >= 1; n--)
            {
                CountdownLabel = n.ToString();
                yield return new WaitForSeconds(countdownStepSeconds);
            }

            CountdownLabel = "GO!";
            foreach (CarMotor motor in motors)
            {
                if (motor != null)
                    motor.InputEnabled = true;
            }
            if (State == MatchState.Countdown)
                SetState(MatchState.Playing);

            yield return new WaitForSeconds(goLabelSeconds);
            CountdownLabel = string.Empty;
        }

        private void HandleCarEliminated(CarElimination car)
        {
            if (State == MatchState.Ended)
                return;

            if (car == Player)
            {
                PlayerWon = false;
                SetState(MatchState.Ended);
                return;
            }

            IReadOnlyList<CarElimination> alive = CarElimination.Active;
            if (alive.Count == 1 && alive[0] == Player)
            {
                PlayerWon = true;
                SetState(MatchState.Ended);
            }
        }

        private void SetState(MatchState state)
        {
            State = state;
            StateChanged?.Invoke(state);
        }

        public void Restart()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
