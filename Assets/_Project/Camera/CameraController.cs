using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    public enum ViewMode { FirstPerson, ThirdPerson }

    [SerializeField] private ViewMode _initialView = ViewMode.ThirdPerson;
    [SerializeField] private Key _switchViewKey = Key.V;
    [SerializeField] private Camera _camera;
    [SerializeField, Min(0f)] private float _viewTransitionDuration = 0.35f;

    public ViewMode CurrentView { get; private set; }
    public Transform CameraTransform => _camera.transform;

    private ICameraView _firstPersonView;
    private ICameraView _thirdPersonView;
    private ICameraView _activeView;
    private bool _isInitialized;
    private bool _isTransitioning;
    private float _transitionElapsed;
    private Vector3 _transitionStartPosition;
    private Quaternion _transitionStartRotation;
    private Transform _playerTransform;

    public void Init(Transform playerTransform, FirstPersonCameraController firstPersonView,
        ThirdPersonCameraController thirdPersonView)
    {
        _firstPersonView = firstPersonView;
        _thirdPersonView = thirdPersonView;

        if (_camera == null)
            _camera = GetComponentInChildren<Camera>();

        if (_camera == null || _firstPersonView == null || _thirdPersonView == null)
        {
            Debug.LogError("CameraController requires a Camera and both camera views.", this);
            enabled = false;
            return;
        }

        _playerTransform = playerTransform;
        transform.position = _playerTransform.position;
        _firstPersonView.Init(playerTransform);
        _thirdPersonView.Init(playerTransform);
        _isInitialized = true;
        SetView(_initialView, true);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void SetView(ViewMode viewMode) => SetView(viewMode, false);

    private void SetView(ViewMode viewMode, bool snapImmediately)
    {
        if (!_isInitialized)
            return;

        CurrentView = viewMode;
        _firstPersonView.SetActive(viewMode == ViewMode.FirstPerson);
        _thirdPersonView.SetActive(viewMode == ViewMode.ThirdPerson);
        _activeView = viewMode == ViewMode.FirstPerson ? _firstPersonView : _thirdPersonView;

        if (_activeView.CameraPoint == null)
        {
            Debug.LogError("The selected camera view has no CameraPoint.", this);
            return;
        }

        if (snapImmediately)
        {
            _activeView.UpdatePose();
            _camera.transform.SetPositionAndRotation(_activeView.CameraPoint.position, _activeView.CameraPoint.rotation);
            _isTransitioning = false;
            return;
        }

        _transitionStartPosition = _camera.transform.position - _playerTransform.position;
        _transitionStartRotation = _camera.transform.rotation;
        _transitionElapsed = 0f;
        _isTransitioning = _viewTransitionDuration > 0f;
    }

    private void Update()
    {
        if (!_isInitialized || Keyboard.current == null ||
            !Keyboard.current[_switchViewKey].wasPressedThisFrame)
            return;

        SetView(CurrentView == ViewMode.FirstPerson ? ViewMode.ThirdPerson : ViewMode.FirstPerson);
    }

    private void LateUpdate()
    {
        if (!_isInitialized || _activeView?.CameraPoint == null)
            return;

        transform.position = _playerTransform.position;
        _activeView.UpdatePose();
        var cameraPoint = _activeView.CameraPoint;
        if (!_isTransitioning)
        {
            _camera.transform.SetPositionAndRotation(cameraPoint.position, cameraPoint.rotation);
            return;
        }

        _transitionElapsed += Time.deltaTime;
        var transition = Mathf.Clamp01(_transitionElapsed / _viewTransitionDuration);
        var easedTransition = Mathf.SmoothStep(0f, 1f, transition);
        _camera.transform.SetPositionAndRotation(
            Vector3.Lerp(_playerTransform.position + _transitionStartPosition, cameraPoint.position, easedTransition),
            Quaternion.Slerp(_transitionStartRotation, cameraPoint.rotation, easedTransition));

        if (transition >= 1f)
            _isTransitioning = false;
    }
}
