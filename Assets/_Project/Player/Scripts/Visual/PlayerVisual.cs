using UnityEngine;
using UnityEngine.Serialization;

namespace Project.Player
{
    public class PlayerVisual : MonoBehaviour
    {
        private static readonly int MoveX = Animator.StringToHash("MoveX");
        private static readonly int MoveZ = Animator.StringToHash("MoveZ");
        [SerializeField] private Animator _animator;
        [SerializeField] private Visual.PlayerBodyLook _bodyLook;
        [Header("Animation Smoothing (seconds)")]
        [Tooltip("Smoothing when starting or increasing movement. Zero means instant.")]
        [SerializeField, Min(0f)] private float _startDampTime = 0.05f;
        [Tooltip("Smoothing when stopping or decreasing movement. Zero means instant.")]
        [SerializeField, Min(0f)] private float _stopDampTime = 0.1f;
        [Tooltip("Smoothing when changing movement direction. Zero means instant.")]
        [FormerlySerializedAs("_movementDampTime")]
        [SerializeField, Min(0f)] private float _directionDampTime = 0.1f;
        private bool _hasMovementParameters;
        private Vector2 _movementDirection;
        private float _movementAmount;

        public bool IsMoving { get; private set; }

        public void Init(Transform view)
        {
            if (_animator == null)
                _animator = GetComponentInChildren<Animator>(true);

            if (_bodyLook == null)
                _bodyLook = GetComponentInChildren<Visual.PlayerBodyLook>(true);
            _bodyLook.Init(this, view);

            _hasMovementParameters = false;
            if (_animator == null || _animator.runtimeAnimatorController == null)
                return;

            // CharacterController owns movement, including gravity.
            _animator.applyRootMotion = false;
            bool hasX = false, hasZ = false;
            foreach (var parameter in _animator.parameters)
            {
                if (parameter.type != AnimatorControllerParameterType.Float)
                    continue;
                hasX |= parameter.nameHash == MoveX;
                hasZ |= parameter.nameHash == MoveZ;
            }
            _hasMovementParameters = hasX && hasZ;
            ResetMovement();
        }

        public void SetMovement(Vector3 worldVelocity, float moveSpeed)
        {
            IsMoving = worldVelocity.sqrMagnitude > 0f;
            if (_animator == null || !_hasMovementParameters)
                return;

            var localVelocity = _animator.transform.InverseTransformDirection(worldVelocity);
            var movement = moveSpeed > 0f
                ? Vector2.ClampMagnitude(new Vector2(localVelocity.x, localVelocity.z) / moveSpeed, 1f)
                : Vector2.zero;
            float targetAmount = movement.magnitude;
            if (targetAmount > 0.0001f)
            {
                var targetDirection = movement / targetAmount;
                // Start in the requested direction immediately; only the amount fades in.
                // Blend reversals through the centre instead of rotating through unrelated strafes.
                _movementDirection = _movementAmount <= 0.0001f
                    ? targetDirection
                    : Vector2.Lerp(_movementDirection, targetDirection, GetBlend(_directionDampTime));
            }
            float amountDampTime = targetAmount > _movementAmount ? _startDampTime : _stopDampTime;
            _movementAmount = Mathf.Lerp(_movementAmount, targetAmount, GetBlend(amountDampTime));
            if (targetAmount == 0f && _movementAmount < 0.0001f)
                _movementAmount = 0f;

            _animator.SetFloat(MoveX, _movementDirection.x * _movementAmount);
            _animator.SetFloat(MoveZ, _movementDirection.y * _movementAmount);
        }

        private static float GetBlend(float dampTime)
        {
            return dampTime <= 0f ? 1f : 1f - Mathf.Exp(-Time.deltaTime / dampTime);
        }

        public void ResetMovement()
        {
            IsMoving = false;
            _movementAmount = 0f;
            _movementDirection = Vector2.zero;
            if (_animator == null || !_hasMovementParameters)
                return;

            _animator.SetFloat(MoveX, 0f);
            _animator.SetFloat(MoveZ, 0f);
        }
    }
}
