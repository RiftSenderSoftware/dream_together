using Project.Player.Camera;
using Unity.Netcode;
using UnityEngine;

namespace Project.Player.Network
{
    /// <summary>
    /// Sends the owner's look direction and movement; other machines replay them on the procedural visual.
    /// Position is synced by an owner-authoritative NetworkTransform on the same object.
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    public class PlayerNetwork : NetworkBehaviour
    {
        [SerializeField] private PlayerContext _playerContext;
        [SerializeField, Min(0f)] private float _remoteViewSpeed = 15f;

        // x = pitch, y = yaw of the owner's camera.
        private readonly NetworkVariable<Vector2> _look = new(
            writePerm: NetworkVariableWritePermission.Owner);
        // Horizontal velocity on the XZ plane, as PlayerController feeds it to PlayerVisual.
        private readonly NetworkVariable<Vector2> _moveVelocity = new(
            writePerm: NetworkVariableWritePermission.Owner);
        private readonly NetworkVariable<float> _moveSpeed = new(
            writePerm: NetworkVariableWritePermission.Owner);

        private Transform _localView;
        private Transform _remoteView;

        private void OnValidate()
        {
            if (_playerContext == null)
                _playerContext = GetComponent<PlayerContext>();
        }

        public override void OnNetworkSpawn()
        {
            if (IsOwner)
            {
                var cameraContext = FindFirstObjectByType<CameraContext>();
                cameraContext.Init(_playerContext);
                _localView = cameraContext.CameraTransform;
                _playerContext.InitLocal(_localView);
                return;
            }

            // Only the rotation of the view is read, so it can live anywhere in the hierarchy.
            _remoteView = new GameObject("RemoteView").transform;
            _remoteView.SetParent(transform, false);
            _remoteView.rotation = GetLookRotation();
            _playerContext.InitRemote(_remoteView);
        }

        private void Update()
        {
            if (!IsSpawned)
                return;

            if (IsOwner)
                WriteState();
            else
                ApplyState();
        }

        private void WriteState()
        {
            // NetworkVariable only marks itself dirty when the value changes, so idle players send nothing.
            var euler = _localView.eulerAngles;
            _look.Value = new Vector2(euler.x, euler.y);

            var controller = _playerContext.Controller;
            _moveVelocity.Value = new Vector2(controller.MoveVelocity.x, controller.MoveVelocity.z);
            _moveSpeed.Value = controller.MoveSpeed;
        }

        private void ApplyState()
        {
            float blend = _remoteViewSpeed <= 0f ? 1f : 1f - Mathf.Exp(-_remoteViewSpeed * Time.deltaTime);
            _remoteView.rotation = Quaternion.Slerp(_remoteView.rotation, GetLookRotation(), blend);

            var velocity = _moveVelocity.Value;
            _playerContext.Visual.SetMovement(new Vector3(velocity.x, 0f, velocity.y), _moveSpeed.Value);
        }

        private Quaternion GetLookRotation()
        {
            return Quaternion.Euler(_look.Value.x, _look.Value.y, 0f);
        }
    }
}
