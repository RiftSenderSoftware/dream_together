using UnityEngine;
        
namespace Project.Player.Visual
{
    public class EyeBlink : MonoBehaviour
    {
        [SerializeField] private SkinnedMeshRenderer _headRenderer;
        [SerializeField] private string _blendShapeName = "eyes_closed.01";
        [SerializeField, Min(0.1f)] private float _interval = 10f;
        [SerializeField, Min(0.01f)] private float _closeDuration = 0.08f;
        [SerializeField, Min(0.01f)] private float _openDuration = 0.12f;
        [SerializeField, Range(0f, 100f)] private float _closedWeight = 100f;

        private SkinnedMeshRenderer _renderer;
        private Mesh _mesh;
        private int _shapeIndex = -1;
        private float _openWeight;
        private float _elapsed;
        private float _blinkElapsed;
        private bool _blinking;

        private void OnEnable()
        {
            _elapsed = 0f;
            _blinkElapsed = 0f;
            _blinking = false;
            _shapeIndex = -1;
            _renderer = _headRenderer;
            _mesh = _renderer != null ? _renderer.sharedMesh : null;
            if (_mesh == null || string.IsNullOrEmpty(_blendShapeName))
                return;

            _shapeIndex = _mesh.GetBlendShapeIndex(_blendShapeName);
            if (_shapeIndex < 0)
            {
                for (int i = 0; i < _mesh.blendShapeCount; i++)
                {
                    if (_mesh.GetBlendShapeName(i).EndsWith("." + _blendShapeName,
                        System.StringComparison.Ordinal))
                    {
                        _shapeIndex = i;
                        break;
                    }
                }
            }

            if (_shapeIndex < 0)
            {
                Debug.LogWarning($"EyeBlink: missing blend shape '{_blendShapeName}' on '{_mesh.name}'.", this);
                return;
            }

            _openWeight = _renderer.GetBlendShapeWeight(_shapeIndex);
        }

        private void LateUpdate()
        {
            if (_renderer == null || _renderer.sharedMesh != _mesh || _shapeIndex < 0)
                return;

            _elapsed += Time.deltaTime;
            if (!_blinking)
            {
                if (_elapsed < Mathf.Max(0.1f, _interval))
                    return;

                _elapsed = 0f;
                _blinkElapsed = 0f;
                _blinking = true;
            }

            _blinkElapsed += Time.deltaTime;
            float closeDuration = Mathf.Max(0.01f, _closeDuration);
            float openDuration = Mathf.Max(0.01f, _openDuration);
            float closure;
            if (_blinkElapsed < closeDuration)
                closure = Mathf.SmoothStep(0f, 1f, _blinkElapsed / closeDuration);
            else
                closure = 1f - Mathf.SmoothStep(0f, 1f, (_blinkElapsed - closeDuration) / openDuration);

            _renderer.SetBlendShapeWeight(_shapeIndex, Mathf.Lerp(_openWeight, _closedWeight, closure));
            if (_blinkElapsed >= closeDuration + openDuration)
                _blinking = false;
        }

        private void OnDisable()
        {
            if (_renderer != null && _renderer.sharedMesh == _mesh && _shapeIndex >= 0)
                _renderer.SetBlendShapeWeight(_shapeIndex, _openWeight);
        }
    }
}
