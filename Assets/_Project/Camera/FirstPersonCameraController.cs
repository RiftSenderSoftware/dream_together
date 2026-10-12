using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class FirstPersonCameraController : MonoBehaviour, ICameraView
{
    [Header("Look")]
    [SerializeField, Min(0f)] private float _mouseSensitivity = 0.1f;
    [SerializeField] private float _minimumPitch = -80f;
    [SerializeField] private float _maximumPitch = 80f;
    [SerializeField] private Transform _yawPivot;
    [SerializeField] private Transform _pitchPivot;
    [FormerlySerializedAs("_cameraTransform")]
    [SerializeField] private Transform _cameraPoint;

    [Header("Follow")]
    [SerializeField] private Transform _cameraParent;
    [SerializeField] private Vector3 _cameraParentOffset = new(0f, 1.6f, 0f);

    private Transform _target;
    private float _yaw;
    private float _pitch;

    public Transform CameraPoint => _cameraPoint;

    public void Init(Transform target)
    {
        _target = target;
        if (_cameraParent == null)
            _cameraParent = target;

        _cameraPoint ??= _pitchPivot.Find("CameraPoint");
        _yaw = _yawPivot.localEulerAngles.y;
        _pitch = NormalizeAngle(_pitchPivot.localEulerAngles.x);
        enabled = false;
    }

    public void SetActive(bool isActive)
    {
        enabled = isActive;
    }

    private void Update()
    {
        if (_target == null || Mouse.current == null)
            return;

        var mouseDelta = Mouse.current.delta.ReadValue() * _mouseSensitivity;
        _yaw += mouseDelta.x;
        _pitch = Mathf.Clamp(_pitch - mouseDelta.y, _minimumPitch, _maximumPitch);

    }

    public void UpdatePose()
    {
        _yawPivot.position = _cameraParent.TransformPoint(_cameraParentOffset);
        _yawPivot.localRotation = Quaternion.Euler(0f, _yaw, 0f);
        _pitchPivot.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
    }

    private static float NormalizeAngle(float angle) => angle > 180f ? angle - 360f : angle;
}
