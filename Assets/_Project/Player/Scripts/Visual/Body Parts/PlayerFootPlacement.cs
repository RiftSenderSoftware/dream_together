using RootMotion.FinalIK;
using UnityEngine;

namespace Project.Player.Visual
{
    // Body yaw is applied at -50; head and eye tracking run after this at 0.
    [DefaultExecutionOrder(-25)]
    [DisallowMultipleComponent]
    public sealed class PlayerFootPlacement : MonoBehaviour
    {
        [SerializeField] private Animator _animator;
        [SerializeField] private CharacterController _controller;
        [SerializeField] private LayerMask _groundLayers = Physics.DefaultRaycastLayers;
        [SerializeField, Range(0f, 1f)] private float _weight = 1f;
        [Tooltip("Maximum surface height difference from the character's base, in metres.")]
        [SerializeField, Min(0.01f)] private float _maxStepHeight = 0.4f;
        [SerializeField, Min(0f)] private float _footSpeed = 15f;
        [SerializeField, Min(0f)] private float _pelvisSpeed = 10f;
        [SerializeField, Range(0f, 1f)] private float _lowerPelvisWeight = 1f;
        [SerializeField, Range(0f, 60f)] private float _maxFootAngle = 40f;

        private sealed class Leg
        {
            public readonly IKSolverLimb Solver;
            public readonly Transform[] Bones;
            public readonly Quaternion[] AnimatedRotations = new Quaternion[3];
            public Vector3 Target;
            public Quaternion RotationOffset = Quaternion.identity;
            public float HeightOffset;

            public Leg(Animator animator, bool left)
            {
                Bones = new[]
                {
                    animator.GetBoneTransform(left ? HumanBodyBones.LeftUpperLeg : HumanBodyBones.RightUpperLeg),
                    animator.GetBoneTransform(left ? HumanBodyBones.LeftLowerLeg : HumanBodyBones.RightLowerLeg),
                    animator.GetBoneTransform(left ? HumanBodyBones.LeftFoot : HumanBodyBones.RightFoot)
                };
                Solver = new IKSolverLimb(left ? AvatarIKGoal.LeftFoot : AvatarIKGoal.RightFoot)
                {
                    bendModifier = IKSolverLimb.BendModifier.Animation,
                    maintainRotationWeight = 1f,
                    IKRotationWeight = 0f
                };
            }
        }

        private readonly RaycastHit[] _hits = new RaycastHit[32];
        private Leg[] _legs;
        private Transform _pelvis;
        private Vector3 _animatedPelvisPosition;
        private float _pelvisOffset;
        private float _groundWeight;
        private float _airTime;
        private bool _poseApplied;

        private void Awake()
        {
            if (_animator == null) _animator = GetComponentInChildren<Animator>();
            if (_controller == null) _controller = GetComponentInParent<CharacterController>();
            if (_animator == null || !_animator.isHuman || _controller == null)
            {
                Debug.LogError("Foot placement requires a Humanoid Animator and CharacterController.", this);
                enabled = false;
                return;
            }

            _pelvis = _animator.GetBoneTransform(HumanBodyBones.Hips);
            _legs = new[] { new Leg(_animator, true), new Leg(_animator, false) };
            foreach (var leg in _legs)
            {
                // Forward is also a stable fallback for a perfectly straight initial knee.
                leg.Solver.bendNormal = -transform.right;
                if (!leg.Solver.SetChain(leg.Bones[0], leg.Bones[1], leg.Bones[2], transform))
                {
                    enabled = false;
                    return;
                }
            }
        }

        private void Update()
        {
            // Remove only our previous correction before Animator evaluates a fresh pose.
            // This also prevents accumulation when a bone has no animation curve.
            RestorePose();
        }

        private void LateUpdate()
        {
            if (_legs == null || !_animator.isActiveAndEnabled || !_controller.enabled) return;

            float dt = Time.deltaTime;
            _airTime = _controller.isGrounded ? 0f : _airTime + dt;
            float targetWeight = _airTime <= 0.12f ? _weight : 0f;
            _groundWeight = Mathf.MoveTowards(_groundWeight, targetWeight, dt * 10f);
            float footBlend = Blend(_footSpeed, dt);
            float baseY = _controller.transform.TransformPoint(_controller.center).y
                - _controller.height * Mathf.Abs(_controller.transform.lossyScale.y) * 0.5f;

            _animatedPelvisPosition = _pelvis.localPosition;
            foreach (var leg in _legs)
            {
                for (int i = 0; i < leg.Bones.Length; i++)
                    leg.AnimatedRotations[i] = leg.Bones[i].localRotation;

                var foot = leg.Bones[2];
                float offset = 0f;
                var rotation = Quaternion.identity;
                if (TryGetGround(foot.position, baseY, out var hit))
                {
                    // Add terrain height to the animated pose, preserving swing-foot lift
                    // and the rig's ankle-to-sole distance instead of pinning every frame.
                    offset = Mathf.Clamp(hit.point.y - baseY, -_maxStepHeight, _maxStepHeight);
                    rotation = Quaternion.RotateTowards(Quaternion.identity,
                        Quaternion.FromToRotation(Vector3.up, hit.normal), _maxFootAngle);
                }
                leg.HeightOffset = Mathf.Lerp(leg.HeightOffset, offset, footBlend);
                leg.RotationOffset = Quaternion.Slerp(leg.RotationOffset, rotation, footBlend);
                leg.Target = foot.position + Vector3.up * leg.HeightOffset;
                leg.Solver.MaintainBend();
                leg.Solver.MaintainRotation();
            }

            float lowestFoot = Mathf.Min(0f, Mathf.Min(_legs[0].HeightOffset, _legs[1].HeightOffset));
            _pelvisOffset = Mathf.Lerp(_pelvisOffset, lowestFoot * _lowerPelvisWeight, Blend(_pelvisSpeed, dt));
            _pelvis.position += Vector3.up * (_pelvisOffset * _groundWeight);
            foreach (var leg in _legs)
            {
                Quaternion animatedRotation = leg.Bones[2].rotation;
                leg.Solver.IKPosition = leg.Target;
                leg.Solver.IKPositionWeight = _groundWeight;
                leg.Solver.Update();
                leg.Bones[2].rotation = Quaternion.Slerp(Quaternion.identity,
                    leg.RotationOffset, _groundWeight) * animatedRotation;
            }
            _poseApplied = true;
        }

        private bool TryGetGround(Vector3 foot, float baseY, out RaycastHit nearest)
        {
            var origin = new Vector3(foot.x, baseY + _maxStepHeight + 0.01f, foot.z);
            float distance = _maxStepHeight * 2f + 0.02f;
            int count = Physics.RaycastNonAlloc(origin, Vector3.down, _hits, distance,
                _groundLayers, QueryTriggerInteraction.Ignore);
            // NonAlloc hits are unordered; a full buffer might omit the nearest surface.
            var hits = count == _hits.Length
                ? Physics.RaycastAll(origin, Vector3.down, distance, _groundLayers, QueryTriggerInteraction.Ignore)
                : _hits;
            if (hits != _hits) count = hits.Length;
            nearest = default;
            float closest = float.PositiveInfinity;
            float minimumUp = Mathf.Cos(_controller.slopeLimit * Mathf.Deg2Rad);
            for (int i = 0; i < count; i++)
            {
                var hit = hits[i];
                if (hit.collider.transform.IsChildOf(_controller.transform)
                    || hit.normal.y < minimumUp || hit.distance >= closest) continue;
                nearest = hit;
                closest = hit.distance;
            }
            return closest < float.PositiveInfinity;
        }

        private void RestorePose()
        {
            if (!_poseApplied) return;
            _pelvis.localPosition = _animatedPelvisPosition;
            foreach (var leg in _legs)
                for (int i = 0; i < leg.Bones.Length; i++)
                    leg.Bones[i].localRotation = leg.AnimatedRotations[i];
            _poseApplied = false;
        }

        private void OnDisable()
        {
            RestorePose();
            _groundWeight = _pelvisOffset = _airTime = 0f;
            if (_legs == null) return;
            foreach (var leg in _legs)
            {
                leg.HeightOffset = 0f;
                leg.RotationOffset = Quaternion.identity;
            }
        }

        private static float Blend(float speed, float dt) => speed <= 0f ? 1f : 1f - Mathf.Exp(-speed * dt);
    }
}
