using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class ThirdPersonCameraController : MonoBehaviour, ICameraView
{
    [Header("Look")]
    [SerializeField, Min(0f)] private float _mouseSensitivity = 0.1f;
    [SerializeField] private float _minimumPitch = -30f;
    [SerializeField] private float _maximumPitch = 70f;
    [SerializeField] private Transform _yawPivot;
    [SerializeField] private Transform _pitchPivot;
    [FormerlySerializedAs("_cameraTransform")]
    [SerializeField] private Transform _cameraPoint;

    [Header("Follow")]
    [SerializeField] private Transform _cameraParent;
    [SerializeField] private Vector3 _cameraParentOffset = new(0f, 1.5f, 0f);
    [SerializeField, Min(0.1f)] private float _distance = 4f;
    [SerializeField, Min(0f)] private float _zoomSpeed = 1f;
    [SerializeField, Min(0.1f)] private float _minimumDistance = 1.25f;
    [SerializeField, Min(0.1f)] private float _maximumDistance = 8f;

    private Transform _target;
    private float _yaw;
    private float _pitch;
    private float _currentDistance;

    public Transform CameraPoint => _cameraPoint;

    public void Init(Transform target)
    {
        _target = target;
        if (_cameraParent == null)
            _cameraParent = target;
        ResolveRigReferences();

        _cameraPoint ??= _pitchPivot.Find("CameraPoint");
        _yaw = _yawPivot.localEulerAngles.y;
        _pitch = NormalizeAngle(_pitchPivot.localEulerAngles.x);
        _currentDistance = Mathf.Clamp(_distance, _minimumDistance, _maximumDistance);
        enabled = false;
    }

    private void ResolveRigReferences()
    {
        if (_yawPivot != null && _pitchPivot != null)
            return;

        var cameraContext = GetComponentInParent<Project.Player.Camera.CameraContext>();
        if (cameraContext == null)
            return;

        _yawPivot = cameraContext.transform.Find("Yaw");
        _pitchPivot = _yawPivot != null ? _yawPivot.Find("Pitch") : null;
    }

    public void SetActive(bool isActive) => enabled = isActive;

    private void Update()
    {
        if (_target == null || _cameraPoint == null || Mouse.current == null)
            return;

        var mouseDelta = Mouse.current.delta.ReadValue() * _mouseSensitivity;
        _yaw += mouseDelta.x;
        _pitch = Mathf.Clamp(_pitch - mouseDelta.y, _minimumPitch, _maximumPitch);
        _currentDistance = Mathf.Clamp(
            _currentDistance - Mouse.current.scroll.ReadValue().y * _zoomSpeed * 0.01f,
            _minimumDistance,
            _maximumDistance);

    }

    public void UpdatePose()
    {
        _yawPivot.position = _cameraParent.TransformPoint(_cameraParentOffset);
        _yawPivot.localRotation = Quaternion.Euler(0f, _yaw, 0f);
        _pitchPivot.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        _cameraPoint.localPosition = Vector3.back * _currentDistance;
        _cameraPoint.localRotation = Quaternion.identity;
    }

    private static float NormalizeAngle(float angle) => angle > 180f ? angle - 360f : angle;
}
