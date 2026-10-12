using Project.Player;
using UnityEngine;

namespace Project.Player.Camera
{
    public class CameraContext : MonoBehaviour
    {
        [SerializeField] private CameraController _cameraController;
        [SerializeField] private FirstPersonCameraController _firstPersonCameraController;
        [SerializeField] private ThirdPersonCameraController _thirdPersonCameraController;

        public Transform CameraTransform => _cameraController.CameraTransform;

        private void OnValidate()
        {
            if(_firstPersonCameraController == null)
                _firstPersonCameraController = GetComponentInChildren<FirstPersonCameraController>();

            if (_thirdPersonCameraController == null)
                _thirdPersonCameraController = GetComponentInChildren<ThirdPersonCameraController>();

            if(_cameraController == null)
                _cameraController = GetComponent<CameraController>();

        }

        public void Init(PlayerContext playerContext)
        {
            if (_cameraController == null)
            {
                Debug.LogError("CameraController is not assigned.", this);
                return;
            }

            _cameraController.Init(playerContext.transform, _firstPersonCameraController, _thirdPersonCameraController);
        }
    }
}
    
