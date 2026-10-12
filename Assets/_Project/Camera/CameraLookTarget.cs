using UnityEngine;

/// <summary>
/// Keeps this transform on the view's look direction so head, eyes and body can follow it.
/// </summary>
// Runs before PlayerBodyLook, PlayerHead and PlayerEye read the target.
[DefaultExecutionOrder(-100)]
public class CameraLookTarget : MonoBehaviour
{
    [Tooltip("Point the look direction starts from: the character's head bone, not a static point.")]
    [SerializeField] private Transform _origin;
    [SerializeField, Min(0.5f)] private float _distance = 10f;

    // The local camera, or the network-driven view of a remote player.
    private Transform _view;

    public void Init(Transform view)
    {
        _view = view;
    }

    private void LateUpdate()
    {
        transform.position = _origin.position + _view.forward * _distance;
    }
}
