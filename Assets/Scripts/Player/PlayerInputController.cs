using BumperRoyale.Car;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BumperRoyale.Player
{
    /// <summary>
    /// Reads the project-wide Input System actions (Player/Move, Player/Jump) and feeds them to the car.
    /// W/S or Up/Down = throttle, A/D or Left/Right = steer, Space = boost. Gamepad works too.
    /// </summary>
    [RequireComponent(typeof(CarMotor), typeof(CarBoost))]
    public class PlayerInputController : MonoBehaviour
    {
        [SerializeField] private string moveActionPath = "Player/Move";
        [SerializeField] private string boostActionPath = "Player/Jump";

        private CarMotor motor;
        private CarBoost boost;
        private InputAction moveAction;
        private InputAction boostAction;

        private void Awake()
        {
            motor = GetComponent<CarMotor>();
            boost = GetComponent<CarBoost>();

            if (InputSystem.actions != null)
            {
                moveAction = InputSystem.actions.FindAction(moveActionPath);
                boostAction = InputSystem.actions.FindAction(boostActionPath);
            }
            if (moveAction == null || boostAction == null)
                Debug.LogWarning("PlayerInputController: project-wide input actions not found, using keyboard fallback.", this);
        }

        private void OnEnable()
        {
            moveAction?.Enable();
            boostAction?.Enable();
        }

        private void Update()
        {
            Vector2 move = moveAction != null ? moveAction.ReadValue<Vector2>() : ReadKeyboardFallback();
            motor.SetInput(move.y, move.x);

            bool boostPressed = boostAction != null
                ? boostAction.WasPressedThisFrame()
                : Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
            if (boostPressed)
                boost.TryBoost();
        }

        private static Vector2 ReadKeyboardFallback()
        {
            Keyboard kb = Keyboard.current;
            if (kb == null)
                return Vector2.zero;
            float x = (kb.dKey.isPressed || kb.rightArrowKey.isPressed ? 1f : 0f) - (kb.aKey.isPressed || kb.leftArrowKey.isPressed ? 1f : 0f);
            float y = (kb.wKey.isPressed || kb.upArrowKey.isPressed ? 1f : 0f) - (kb.sKey.isPressed || kb.downArrowKey.isPressed ? 1f : 0f);
            return new Vector2(x, y);
        }
    }
}
