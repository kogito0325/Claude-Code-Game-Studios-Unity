using System;
using UnityEngine;

namespace Proto.Movement
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CapsuleCollider))]
    public sealed class MovementAgent : MonoBehaviour, IMovementAgent
    {
        [SerializeField] private MovementProfile _profile;

        private CapsuleCollider _selfCollider;
        private Vector3 _lastDelta;
        private float _verticalVelocity;
        private bool _isGrounded;

        private static readonly Collider[] s_overlapBuffer = new Collider[16];

        public Vector3 Position => transform.position;
        public Vector3 Velocity => Time.deltaTime > 0f ? _lastDelta / Time.deltaTime : Vector3.zero;
        public MovementProfile Profile => _profile;
        public bool IsGrounded => _isGrounded;

        public event Action<Vector3> Moved;
        public event Action<Collider> Collided;

        private void Awake()
        {
            _selfCollider = GetComponent<CapsuleCollider>();

            if (_profile == null)
            {
                Debug.LogError($"[MovementAgent] Profile not assigned on '{name}'. Disabling.", this);
                enabled = false;
                return;
            }

            if (TryGetComponent<Rigidbody>(out _))
            {
                Debug.LogError($"[MovementAgent] HR-1 violation: Rigidbody detected on '{name}'. Remove it.", this);
            }

            SyncColliderFromProfile();
        }

        private void OnValidate()
        {
            if (_profile != null && TryGetComponent(out _selfCollider))
            {
                SyncColliderFromProfile();
            }
        }

        private void SyncColliderFromProfile()
        {
            float h = Mathf.Max(_profile.height, _profile.radius * 2f + 0.01f);
            _selfCollider.direction = 1;
            _selfCollider.radius = _profile.radius;
            _selfCollider.height = h;
            _selfCollider.center = new Vector3(0f, h * 0.5f, 0f);
            _selfCollider.isTrigger = false;
        }

        private void Update()
        {
            if (_profile == null) return;

            if (_profile.applyGravity)
            {
                _isGrounded = CheckGrounded();

                if (_isGrounded && _verticalVelocity <= 0f)
                {
                    _verticalVelocity = 0f;
                }
                else
                {
                    _verticalVelocity += _profile.gravity * Time.deltaTime;
                }

                if (Mathf.Abs(_verticalVelocity) > 1e-5f)
                {
                    Vector3 gravityStep = new Vector3(0f, _verticalVelocity * Time.deltaTime, 0f);
                    MoveResult result = ApplyMove(gravityStep, fireEvents: false);
                    if (_verticalVelocity < 0f && result != MoveResult.Ok)
                    {
                        _verticalVelocity = 0f;
                    }
                }
            }
            else
            {
                _isGrounded = false;
            }
        }

        public MoveResult MoveBy(Vector3 delta)
        {
            if (!IsFinite(delta)) return MoveResult.Invalid;
            return ApplyMove(delta, fireEvents: true);
        }

        public MoveResult MoveTo(Vector3 worldTarget)
        {
            if (!IsFinite(worldTarget)) return MoveResult.Invalid;
            return ApplyMove(worldTarget - transform.position, fireEvents: true);
        }

        public MoveResult Teleport(Vector3 worldTarget)
        {
            if (!IsFinite(worldTarget)) return MoveResult.Invalid;
            transform.position = worldTarget;
            ResolvePenetration();
            _lastDelta = Vector3.zero;
            _verticalVelocity = 0f;
            return MoveResult.Ok;
        }

        public void Halt()
        {
            _lastDelta = Vector3.zero;
            _verticalVelocity = 0f;
        }

        private MoveResult ApplyMove(Vector3 delta, bool fireEvents)
        {
            if (delta.sqrMagnitude < 1e-10f) return MoveResult.Ok;

            Vector3 startPos = transform.position;
            Vector3 actualMove = SweepAndSlide(delta);
            transform.position = startPos + actualMove;
            ResolvePenetration();

            _lastDelta = transform.position - startPos;

            if (fireEvents && _lastDelta.sqrMagnitude > 1e-10f)
            {
                Moved?.Invoke(_lastDelta);
            }

            float desiredSq = delta.sqrMagnitude;
            float actualSq = actualMove.sqrMagnitude;
            if (actualSq < desiredSq * 0.01f) return MoveResult.Blocked;
            if (actualSq < desiredSq * 0.99f) return MoveResult.PartiallyMoved;
            return MoveResult.Ok;
        }

        private Vector3 SweepAndSlide(Vector3 delta)
        {
            Vector3 remaining = delta;
            Vector3 total = Vector3.zero;
            const int maxSlides = 3;

            for (int i = 0; i < maxSlides && remaining.sqrMagnitude > 1e-10f; i++)
            {
                float dist = remaining.magnitude;
                Vector3 dir = remaining / dist;

                GetCapsulePoints(transform.position + total, out Vector3 p0, out Vector3 p1);

                if (Physics.CapsuleCast(p0, p1, _profile.radius, dir, out RaycastHit hit,
                        dist + _profile.skinWidth, _profile.blockingLayers,
                        QueryTriggerInteraction.Ignore))
                {
                    if (hit.collider == _selfCollider)
                    {
                        total += remaining;
                        break;
                    }

                    if (TryStepOver(transform.position + total, dir, dist, out Vector3 stepResolved))
                    {
                        total += stepResolved;
                        break;
                    }

                    float safeDist = Mathf.Max(0f, hit.distance - _profile.skinWidth);
                    total += dir * safeDist;
                    Collided?.Invoke(hit.collider);

                    Vector3 consumed = dir * hit.distance;
                    Vector3 leftover = remaining - consumed;
                    Vector3 slid = Vector3.ProjectOnPlane(leftover, hit.normal);

                    float slopeAngle = Vector3.Angle(Vector3.up, hit.normal);
                    if (slopeAngle > _profile.slopeLimit && slid.y > 0f)
                    {
                        slid.y = 0f;
                    }

                    if (_profile.blockReverseSlide)
                    {
                        Vector3 slidH = new Vector3(slid.x, 0f, slid.z);
                        Vector3 dirH = new Vector3(dir.x, 0f, dir.z);
                        if (slidH.sqrMagnitude > 1e-8f && dirH.sqrMagnitude > 1e-8f
                            && Vector3.Dot(slidH.normalized, dirH.normalized) < 0f)
                        {
                            slid = Vector3.zero;
                        }
                    }

                    remaining = slid;
                }
                else
                {
                    total += remaining;
                    remaining = Vector3.zero;
                }
            }

            return total;
        }

        private bool TryStepOver(Vector3 from, Vector3 dir, float distance, out Vector3 resolved)
        {
            resolved = Vector3.zero;
            if (_profile.stepOffset <= 0f) return false;

            Vector3 upOffset = Vector3.up * _profile.stepOffset;
            GetCapsulePoints(from + upOffset, out Vector3 p0, out Vector3 p1);

            if (Physics.CapsuleCast(p0, p1, _profile.radius, dir, out _,
                    distance, _profile.blockingLayers, QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            Vector3 forwardEnd = from + upOffset + dir * distance;
            GetCapsulePoints(forwardEnd, out Vector3 d0, out Vector3 d1);

            if (Physics.CapsuleCast(d0, d1, _profile.radius, Vector3.down, out RaycastHit downHit,
                    _profile.stepOffset + _profile.skinWidth, _profile.groundLayers,
                    QueryTriggerInteraction.Ignore))
            {
                if (Vector3.Dot(downHit.normal, Vector3.up) < _profile.stepSurfaceMinFlatness)
                {
                    return false;
                }
                resolved = (forwardEnd + Vector3.up * (_profile.stepOffset - downHit.distance)) - from;
                return true;
            }

            return false;
        }

        private void ResolvePenetration()
        {
            for (int iter = 0; iter < _profile.maxPushoutIterations; iter++)
            {
                GetCapsulePoints(transform.position, out Vector3 p0, out Vector3 p1);
                int count = Physics.OverlapCapsuleNonAlloc(p0, p1, _profile.radius,
                    s_overlapBuffer, _profile.blockingLayers, QueryTriggerInteraction.Ignore);

                if (count == 0) return;

                bool pushedAny = false;
                for (int i = 0; i < count; i++)
                {
                    Collider other = s_overlapBuffer[i];
                    if (other == null || other == _selfCollider) continue;
                    if (other.transform.IsChildOf(transform)) continue;

                    if (Physics.ComputePenetration(
                            _selfCollider, transform.position, transform.rotation,
                            other, other.transform.position, other.transform.rotation,
                            out Vector3 pushDir, out float pushDist))
                    {
                        transform.position += pushDir * (pushDist + _profile.skinWidth);
                        pushedAny = true;
                    }
                }

                if (!pushedAny) return;
            }
        }

        private void GetCapsulePoints(Vector3 basePos, out Vector3 p0, out Vector3 p1)
        {
            float r = _profile.radius;
            float h = Mathf.Max(_profile.height, r * 2f + 0.01f);
            p0 = basePos + Vector3.up * r;
            p1 = basePos + Vector3.up * (h - r);
        }

        private bool CheckGrounded()
        {
            GetCapsulePoints(transform.position, out Vector3 p0, out Vector3 p1);
            float probe = _profile.skinWidth * 2f + 0.05f;
            return Physics.CapsuleCast(p0, p1, _profile.radius * 0.95f, Vector3.down, out _,
                probe, _profile.groundLayers, QueryTriggerInteraction.Ignore);
        }

        private static bool IsFinite(Vector3 v)
        {
            return !float.IsNaN(v.x) && !float.IsNaN(v.y) && !float.IsNaN(v.z)
                && !float.IsInfinity(v.x) && !float.IsInfinity(v.y) && !float.IsInfinity(v.z);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (_profile == null) return;
            Gizmos.color = Application.isPlaying && _isGrounded ? Color.green : Color.yellow;
            GetCapsulePoints(transform.position, out Vector3 p0, out Vector3 p1);
            Gizmos.DrawWireSphere(p0, _profile.radius);
            Gizmos.DrawWireSphere(p1, _profile.radius);
        }
#endif
    }
}
