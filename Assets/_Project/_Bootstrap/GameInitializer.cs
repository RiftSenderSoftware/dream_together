using UnityEngine;
using Project.Player;
using Project.Player.Camera;

/// <summary>
/// Single-player entry point for scenes with a pre-placed player. Networked scenes do not use it:
/// there each spawned player initializes itself in PlayerNetwork.
/// </summary>
public class GameInitializer : MonoBehaviour
{
    [SerializeField] private PlayerContext _playerContext;
    [SerializeField] private CameraContext _cameraContext;

    private void Awake()
    {
        Init();
    }

    private void Init()
    {
        _cameraContext.Init(_playerContext);
        _playerContext.InitLocal(_cameraContext.CameraTransform);
    }
}
