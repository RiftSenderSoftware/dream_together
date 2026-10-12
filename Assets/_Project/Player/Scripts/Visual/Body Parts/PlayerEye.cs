using UnityEngine;

namespace Project.Player.Visual
{
    // Read the final head pose after PlayerHead.LateUpdate.
    [DefaultExecutionOrder(100)]
    public class PlayerEye : MonoBehaviour
    {
        [SerializeField] private SkinnedMeshRenderer _headRenderer;
        [SerializeField] private Transform _head;
        [SerializeField] private Transform _lookTarget;
        [SerializeField] private Vector3 _eyeOffset;
        [SerializeField] private Vector3 _rotationOffset;
        [SerializeField, Min(0f)] private float _rotationSpeed = 15f;

        [Header("Angles corresponding to full eye blend shape weights")]
        [SerializeField, Range(1f, 89f)] private float _leftLimit = 30f;
        [SerializeField, Range(1f, 89f)] private float _rightLimit = 30f;
        [SerializeField, Range(1f, 89f)] private float _upLimit = 20f;
        [SerializeField, Range(1f, 89f)] private float _downLimit = 20f;

        private static readonly string[] ShapeNames =
        {
            "eye_look_left.L", "eye_look_left.R",
            "eye_look_right.L", "eye_look_right.R",
            "eye_look_up.L", "eye_look_up.R",
            "eye_look_down.L", "eye_look_down.R"
        };

        private readonly int[] _shapeIndices = new int[8];
        private SkinnedMeshRenderer _cachedRenderer;
        private Mesh _cachedMesh;
        private Vector3 _initialFaceCentre;
        private Transform _cachedHead;
        private Vector3 _headLocalEyeCentre;
        private Quaternion _headLocalLookRotation;
        private float _yaw;
        private float _pitch;

        public Transform LookTarget
        {
            get => _lookTarget;
            set => _lookTarget = value;
        }

        private void Awake()
        {
            CacheShapes();
            InitializeHead();
        }

        private void InitializeHead()
        {
            if (_head == null || _cachedHead == _head)
                return;

            _cachedHead = _head;
            _headLocalEyeCentre = _head.InverseTransformPoint(
                transform.TransformPoint(_initialFaceCentre));
            // Preserve the controller's neutral gaze axes despite the imported bone axes.
            _headLocalLookRotation = Quaternion.Inverse(_head.rotation) * transform.rotation;
        }

        private bool CacheShapes()
        {
            Mesh mesh = _headRenderer != null ? _headRenderer.sharedMesh : null;
            if (mesh == null)
                return false;
            if (_cachedRenderer == _headRenderer && _cachedMesh == mesh)
                return true;

            ResetWeights();
            _cachedRenderer = _headRenderer;
            _cachedMesh = mesh;
            _cachedHead = null;

            // Capture once in the independent controller's space, not in a head bone's space.
            // Using the controller's pivot directly would measure pitch from the character's feet.
            _initialFaceCentre = transform.InverseTransformPoint(
                _headRenderer.transform.TransformPoint(mesh.bounds.center));

            for (int i = 0; i < ShapeNames.Length; i++)
            {
                _shapeIndices[i] = mesh.GetBlendShapeIndex(ShapeNames[i]);
                // Some FBX import settings prefix names with the mesh name.
                if (_shapeIndices[i] < 0)
                {
                    for (int j = 0; j < mesh.blendShapeCount; j++)
                    {
                        if (mesh.GetBlendShapeName(j).EndsWith("." + ShapeNames[i],
                            System.StringComparison.Ordinal))
                        {
                            _shapeIndices[i] = j;
                            break;
                        }
                    }
                }

                if (_shapeIndices[i] < 0)
                    Debug.LogWarning($"PlayerEye: missing blend shape '{ShapeNames[i]}' on '{mesh.name}'.", this);
            }

            return true;
        }

        private void LateUpdate()
        {
            if (!CacheShapes())
                return;

            InitializeHead();

            float targetYaw = 0f;
            float targetPitch = 0f;
            if (_lookTarget != null)
            {
                Quaternion gazeBasis = _head != null
                    ? _head.rotation * _headLocalLookRotation
                    : transform.rotation;
                Vector3 eyeCentre = _head != null
                    ? _head.TransformPoint(_headLocalEyeCentre)
                        + gazeBasis * Vector3.Scale(_eyeOffset, transform.lossyScale)
                    : transform.TransformPoint(_initialFaceCentre + _eyeOffset);
                Vector3 direction = _lookTarget.position - eyeCentre;
                if (direction.sqrMagnitude > 0.000001f)
                {
                    Quaternion lookRotation = gazeBasis * Quaternion.Euler(_rotationOffset);
                    Vector3 localDirection = Quaternion.Inverse(lookRotation) * direction.normalized;
                    float horizontal = new Vector2(localDirection.x, localDirection.z).magnitude;
                    targetYaw = horizontal > 0.0001f
                        ? Mathf.Atan2(localDirection.x, localDirection.z) * Mathf.Rad2Deg
                        : _yaw;
                    targetPitch = Mathf.Atan2(localDirection.y, horizontal) * Mathf.Rad2Deg;
                }
            }

            float blend = _rotationSpeed <= 0f ? 1f : 1f - Mathf.Exp(-_rotationSpeed * Time.deltaTime);
            _yaw = Mathf.Lerp(_yaw, Mathf.Clamp(targetYaw, -_leftLimit, _rightLimit), blend);
            _pitch = Mathf.Lerp(_pitch, Mathf.Clamp(targetPitch, -_downLimit, _upLimit), blend);

            SetPair(0, -_yaw / Mathf.Max(1f, _leftLimit));
            SetPair(2, _yaw / Mathf.Max(1f, _rightLimit));
            SetPair(4, _pitch / Mathf.Max(1f, _upLimit));
            SetPair(6, -_pitch / Mathf.Max(1f, _downLimit));
        }

        private void SetPair(int index, float weight)
        {
            weight = Mathf.Clamp01(weight) * 100f;
            for (int i = index; i < index + 2; i++)
                if (_shapeIndices[i] >= 0)
                    _cachedRenderer.SetBlendShapeWeight(_shapeIndices[i], weight);
        }

        private void ResetWeights()
        {
            if (_cachedRenderer == null || _cachedMesh == null || _cachedRenderer.sharedMesh != _cachedMesh)
                return;

            for (int i = 0; i < _shapeIndices.Length; i++)
                if (_shapeIndices[i] >= 0)
                    _cachedRenderer.SetBlendShapeWeight(_shapeIndices[i], 0f);
        }

        private void OnDisable()
        {
            ResetWeights();
            _yaw = 0f;
            _pitch = 0f;
        }
    }
}
