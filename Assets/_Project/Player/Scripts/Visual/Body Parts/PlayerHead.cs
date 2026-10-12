using UnityEngine;

namespace Project.Player.Visual
{
    public class PlayerHead : MonoBehaviour
    {
        [SerializeField] private Transform _neck;

        [SerializeField] private Transform _lookTarget;

        [SerializeField, Min(0f)] private float _rotationSpeed = 10f;

        [SerializeField] private Vector3 _rotationOffset;

        [Header("Limits relative to the initial neck pose")]
        [SerializeField, Range(0f, 180f)] private float _leftLimit = 75f;
        [SerializeField, Range(0f, 180f)] private float _rightLimit = 75f;
        [SerializeField, Range(0f, 89f)] private float _upLimit = 45f;
        [SerializeField, Range(0f, 89f)] private float _downLimit = 35f;

        private Transform _initializedNeck;
        private Quaternion _initialLocalRotation;
        private float _yaw;
        private float _pitch;

        public Transform Neck => _neck;
        public float LeftLimit => _leftLimit;
        public float RightLimit => _rightLimit;

        public Transform LookTarget => _lookTarget;

        private void Awake()
        {
            InitializeNeck();
        }

        private void InitializeNeck()
        {
            if (_neck == null || _initializedNeck == _neck)
                return;

            _initializedNeck = _neck;
            _initialLocalRotation = _neck.localRotation;
            _yaw = 0f;
            _pitch = 0f;
        }

        private void LateUpdate()
        {
            if (_neck == null || _lookTarget == null)
                return;

            InitializeNeck();

            Vector3 direction = _lookTarget.position - _neck.position;
            if (direction.sqrMagnitude < 0.000001f)
                return;

            Quaternion parentRotation = _neck.parent != null
                ? _neck.parent.rotation
                : Quaternion.identity;
            Quaternion offset = Quaternion.Euler(_rotationOffset);
            Quaternion neutralLookRotation = parentRotation * _initialLocalRotation
                * Quaternion.Inverse(offset);
            Vector3 localDirection = Quaternion.Inverse(neutralLookRotation) * direction.normalized;
            float horizontalLength = new Vector2(localDirection.x, localDirection.z).magnitude;

            float targetYaw = horizontalLength > 0.0001f
                ? Mathf.Atan2(localDirection.x, localDirection.z) * Mathf.Rad2Deg
                : _yaw;
            float targetPitch = Mathf.Atan2(localDirection.y, horizontalLength) * Mathf.Rad2Deg;

            targetYaw = Mathf.Clamp(targetYaw, -_leftLimit, _rightLimit);
            targetPitch = Mathf.Clamp(targetPitch, -_downLimit, _upLimit);

            float blend = _rotationSpeed <= 0f
                ? 1f
                : 1f - Mathf.Exp(-_rotationSpeed * Time.deltaTime);

            // Smooth the angles separately so the rotation stays within the limits.
            _yaw = Mathf.Clamp(Mathf.Lerp(_yaw, targetYaw, blend), -_leftLimit, _rightLimit);
            _pitch = Mathf.Clamp(Mathf.Lerp(_pitch, targetPitch, blend), -_downLimit, _upLimit);

            _neck.rotation = neutralLookRotation * Quaternion.Euler(-_pitch, _yaw, 0f) * offset;
        }
    }
}
