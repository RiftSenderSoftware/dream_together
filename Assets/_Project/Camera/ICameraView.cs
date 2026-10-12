using UnityEngine;

/// <summary>
/// A selectable view which uses the shared player camera rig.
/// </summary>
public interface ICameraView
{
    Transform CameraPoint { get; }
    void Init(Transform target);
    void SetActive(bool isActive);
    void UpdatePose();
}
