using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Project.Player
{
    public class PlayerInputReader : MonoBehaviour
    {
        public event Action<Vector2> MoveChanged;

        private Vector2 _moveDirection;

        public void Init()
        {

        }

        private void Update()
        {
            var moveDirection = ReadMoveDirection();
            if (moveDirection == _moveDirection)
            {
                return;
            }

            _moveDirection = moveDirection;
            MoveChanged?.Invoke(_moveDirection);
        }

        private void OnDisable()
        {
            _moveDirection = Vector2.zero;
            MoveChanged?.Invoke(_moveDirection);
        }

        // This method reads the movement direction from both keyboard and gamepad inputs.
        private static Vector2 ReadMoveDirection()
        {
            var keyboard = Keyboard.current;
            var keyboardDirection = Vector2.zero;

            if (keyboard != null)
            {
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
                {
                    keyboardDirection.y += 1f;
                }

                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
                {
                    keyboardDirection.y -= 1f;
                }

                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
                {
                    keyboardDirection.x -= 1f;
                }

                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
                {
                    keyboardDirection.x += 1f;
                }
            }

            var gamepadDirection = Gamepad.current?.leftStick.ReadValue() ?? Vector2.zero;
            return Vector2.ClampMagnitude(keyboardDirection + gamepadDirection, 1f);
        }

    }
}
    
