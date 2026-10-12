using UnityEngine;

namespace Project.Player.Visual
{
    
    [DefaultExecutionOrder(-50)]
    public class PlayerBodyLook : MonoBehaviour
    {
        private static readonly int IsTurning = Animator.StringToHash("IsTurning");
        private static readonly int TurnDirection = Animator.StringToHash("TurnDirection");

        [SerializeField] private Animator _animator;
        [Tooltip("The head's look target defines where the body looks; this component only reads it.")]
        [SerializeField] private PlayerHead _head;

        [Header("Spine")]
        [SerializeField, Range(0f, 60f)] private float _spineLimit = 30f;
        [SerializeField, Min(0f)] private float _spineSpeed = 8f;
        [SerializeField] private Vector3 _spineWeights = new(0.2f, 0.35f, 0.45f);

        [Header("Body Turn")]
        [SerializeField, Range(0f, 45f)] private float _turnThreshold = 10f;
        [SerializeField, Range(0f, 30f)] private float _turnStopAngle = 3f;
        [Tooltip("Degrees per second while stepping in place. Constant speed keeps the body in sync with the stepping animation.")]
        [SerializeField, Min(0f)] private float _turnInPlaceSpeed = 120f;
        [SerializeField, Min(0f)] private float _turnDirectionDampTime = 0.1f;
        [SerializeField, Min(0f)] private float _moveTurnSpeed = 12f;

        private readonly Transform[] _spineBones = new Transform[3];
        private PlayerVisual _visual;
        private Transform _body;
        private Transform _view;
        private float _bodyYaw;
        private float _spineYaw;
        private bool _isTurning;

        public void Init(PlayerVisual visual, Transform view)
        {
            _visual = visual;
            _body = visual.transform;
            _view = view;
            _bodyYaw = _body.eulerAngles.y;
            _spineYaw = 0f;
            _isTurning = false;

            if (_animator == null)
                _animator = visual.GetComponentInChildren<Animator>(true);
            // Chest and UpperChest are optional humanoid bones, so they may be null.
            _spineBones[0] = _animator.GetBoneTransform(HumanBodyBones.Spine);
            _spineBones[1] = _animator.GetBoneTransform(HumanBodyBones.Chest);
            _spineBones[2] = _animator.GetBoneTransform(HumanBodyBones.UpperChest);
        }

        private void LateUpdate()
        {
            float viewYaw = GetYaw(_view.forward, _view.up);
            float lookYaw = GetYaw(_head.LookTarget.position - _head.Neck.position, _body.forward);

            UpdateBodyYaw(viewYaw, lookYaw);
            UpdateSpine(lookYaw);
        }

        private void UpdateBodyYaw(float viewYaw, float lookYaw)
        {
            float delta;
            if (_visual.IsMoving)
            {
                // Movement is camera-relative, so the body runs where the camera looks.
                _isTurning = false;
                delta = Mathf.DeltaAngle(_bodyYaw, viewYaw);
                _bodyYaw += delta * GetBlend(_moveTurnSpeed);
            }
            else
            {
                delta = Mathf.DeltaAngle(_bodyYaw, lookYaw);
                if (Mathf.Abs(delta) > GetHeadLimit(delta) + _spineLimit + _turnThreshold)
                    _isTurning = true;

                if (_isTurning)
                {
                    _bodyYaw = Mathf.MoveTowardsAngle(_bodyYaw, lookYaw, _turnInPlaceSpeed * Time.deltaTime);
                    if (Mathf.Abs(Mathf.DeltaAngle(_bodyYaw, lookYaw)) <= _turnStopAngle)
                        _isTurning = false;
                }
            }

            // Root motion is off, so the turn clips only step the feet while the body is rotated here.
            _animator.SetBool(IsTurning, _isTurning);
            if (_isTurning)
                _animator.SetFloat(TurnDirection, Mathf.Sign(delta), _turnDirectionDampTime, Time.deltaTime);

            _bodyYaw = Mathf.Repeat(_bodyYaw, 360f);
            _body.rotation = Quaternion.Euler(0f, _bodyYaw, 0f);
        }

        private void UpdateSpine(float lookYaw)
        {
            float delta = Mathf.DeltaAngle(_bodyYaw, lookYaw);

            // spine should only pick up what the head cannot turn to, so we calculate the excess angle beyond the head limit.
            float excess = Mathf.Max(0f, Mathf.Abs(delta) - GetHeadLimit(delta));
            float targetYaw = _visual.IsMoving ? 0f : Mathf.Sign(delta) * Mathf.Min(excess, _spineLimit);
            _spineYaw = Mathf.Lerp(_spineYaw, targetYaw, GetBlend(_spineSpeed));

            float totalWeight = 0f;
            for (int i = 0; i < _spineBones.Length; i++)
                if (_spineBones[i] != null)
                    totalWeight += Mathf.Max(0f, _spineWeights[i]);
            if (totalWeight <= 0f)
                return;

            Vector3 up = _body.up;
            for (int i = 0; i < _spineBones.Length; i++)
            {
                Transform bone = _spineBones[i];
                if (bone == null)
                    continue;

                float angle = _spineYaw * Mathf.Max(0f, _spineWeights[i]) / totalWeight;
                bone.rotation = Quaternion.AngleAxis(angle, up) * bone.rotation;
            }
        }

        private static float GetYaw(Vector3 direction, Vector3 fallback)
        {
            Vector3 flat = Vector3.ProjectOnPlane(direction, Vector3.up);
            if (flat.sqrMagnitude < 0.0001f)
                flat = Vector3.ProjectOnPlane(fallback, Vector3.up);
            return Mathf.Atan2(flat.x, flat.z) * Mathf.Rad2Deg;
        }

        private float GetHeadLimit(float delta)
        {
            return delta >= 0f ? _head.RightLimit : _head.LeftLimit;
        }

        private static float GetBlend(float speed)
        {
            return speed <= 0f ? 1f : 1f - Mathf.Exp(-speed * Time.deltaTime);
        }
    }
}
