using UnityEngine;

namespace Project.Player
{
    public class PlayerContext : MonoBehaviour
    {
        [SerializeField] private PlayerVisual _playerVisual;
        [SerializeField] private PlayerController _playerController;
        [SerializeField] private PlayerInputReader _playerInput;

        [SerializeField] private CharacterController _characterController;

        public PlayerVisual Visual => _playerVisual;
        public PlayerController Controller => _playerController;

        private void OnValidate()
        {
            if(_playerVisual == null)
                _playerVisual = GetComponent<PlayerVisual>();

            if(_characterController == null)
                _characterController = GetComponentInChildren<CharacterController>();

            if(_playerController == null)
                _playerController = GetComponentInChildren<PlayerController>();

            if(_playerInput == null)
                _playerInput = GetComponentInChildren<PlayerInputReader>();
        }

        /// <summary>
        /// The player controlled on this machine: reads input and moves relative to the camera.
        /// </summary>
        public void InitLocal(Transform view)
        {
            InitLookTargets(view);
            _playerVisual.Init(view);
            _playerController.Init(_playerInput, _characterController, _playerVisual, view);
            _playerInput.Init();
        }

        /// <summary>
        /// A player controlled on another machine: only the visual runs, driven by network state.
        /// </summary>
        public void InitRemote(Transform view)
        {
            InitLookTargets(view);
            _playerVisual.Init(view);
            // The reader polls the local keyboard and gamepad, so it must not run for other players.
            _playerInput.gameObject.SetActive(false);
            _playerController.enabled = false;
        }

        // Searched from the player root, so a target works wherever it sits in the prefab.
        private void InitLookTargets(Transform view)
        {
            foreach (var lookTarget in GetComponentsInChildren<CameraLookTarget>(true))
                lookTarget.Init(view);
        }
    }
}
