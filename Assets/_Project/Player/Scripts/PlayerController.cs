using UnityEngine;
using UnityEngine.Serialization;

namespace Project.Player
{
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement Speed")]
        [FormerlySerializedAs("_moveSpeed")]
        [SerializeField, Min(0f)] private float _forwardSpeed = 5f;
        [SerializeField, Min(0f)] private float _backwardSpeed = 3f;
        [SerializeField, Min(0f)] private float _sidewaysSpeed = 4f;
        [Header("Gravity")]
        [SerializeField] private float _gravity = -9.81f;

        private CharacterController _characterController;
        private PlayerInputReader _inputReader;
        private PlayerVisual _playerVisual;
        private Transform _view;
        private Vector2 _moveInput;
        private float _verticalVelocity;
        private float _timeWithoutGround;
        private const float GroundContactGraceTime = 0.12f;

        // What this controller last fed to PlayerVisual; remote copies replay it.
        public Vector3 MoveVelocity { get; private set; }
        public float MoveSpeed { get; private set; }

        public void Init(PlayerInputReader inputReader, CharacterController characterController, PlayerVisual playerVisual,
            Transform view)
        {
            if (_inputReader != null)
            {
                _inputReader.MoveChanged -= SetMoveInput;
            }

            _characterController = characterController;
            _playerVisual = playerVisual;
            _view = view;
            _timeWithoutGround = GroundContactGraceTime;
            _inputReader = inputReader;
            _inputReader.MoveChanged += SetMoveInput;
        }

        private void Update()
        {
            if (_characterController == null)
            {
                return;
            }

            var horizontalMovement = new Vector3(_moveInput.x, 0f, _moveInput.y);
            if (horizontalMovement.sqrMagnitude > 1f)
            {
                horizontalMovement.Normalize();
            }

            if (_characterController.isGrounded && _verticalVelocity < 0f)
            {
                _verticalVelocity = -2f;
            }

            _verticalVelocity += _gravity * Time.deltaTime;
            var longitudinalSpeed = horizontalMovement.z >= 0f ? _forwardSpeed : _backwardSpeed;
            var velocity = new Vector3(
                horizontalMovement.x * _sidewaysSpeed,
                0f,
                horizontalMovement.z * longitudinalSpeed);
            // Use the full-input speed in this direction to preserve animation blending.
            var inputMagnitude = horizontalMovement.magnitude;
            var directionalSpeed = inputMagnitude > 0f ? velocity.magnitude / inputMagnitude : 0f;
            // Input is relative to where the camera looks.
            velocity = Quaternion.Euler(0f, _view.eulerAngles.y, 0f) * velocity;
            velocity.y = _verticalVelocity;
            _characterController.Move(velocity * Time.deltaTime);
            var actualVelocity = _characterController.velocity;
            actualVelocity.y = 0f;
            // Ignore brief contact losses at seams and steps, but stop walking in a fall.
            _timeWithoutGround = _characterController.isGrounded
                ? 0f
                : _timeWithoutGround + Time.deltaTime;
            if (_timeWithoutGround >= GroundContactGraceTime || actualVelocity.sqrMagnitude <= 0.01f)
                actualVelocity = Vector3.zero;
            MoveVelocity = actualVelocity;
            MoveSpeed = directionalSpeed;
            _playerVisual.SetMovement(actualVelocity, directionalSpeed);
        }

        private void OnDisable()
        {
            MoveVelocity = Vector3.zero;
            MoveSpeed = 0f;
            if (_playerVisual != null)
            {
                _playerVisual.ResetMovement();
            }
        }

        private void OnDestroy()
        {
            if (_inputReader != null)
            {
                _inputReader.MoveChanged -= SetMoveInput;
            }
        }

        private void SetMoveInput(Vector2 moveInput)
        {
            _moveInput = moveInput;
        }
    }
}
