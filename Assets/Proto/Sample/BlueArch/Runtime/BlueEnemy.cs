using Proto.Movement;
using UnityEngine;
using UnityEngine.AI;

namespace Proto.Sample.BlueArch
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MovementAgent))]
    public sealed class BlueEnemy : MonoBehaviour
    {
        [SerializeField] private float _moveSpeed = 3f;
        [SerializeField] private int _maxHp = 30;
        [SerializeField] private int _touchDamage = 8;
        [SerializeField] private float _touchDamageInterval = 0.5f;
        [SerializeField] private float _touchRange = 1.2f;
        [SerializeField] private BlueHitFlash _hitFlash;
        [SerializeField] private BlueAnimDriver _animDriver;
        [SerializeField] private float _destroyDelayOnDeath = 0.6f;

        [Header("NavMesh (선택) — 있으면 우선 사용")]
        [SerializeField] private float _navAcceleration = 12f;
        [SerializeField] private float _navAngularSpeed = 720f;

        private MovementAgent _agent;
        private NavMeshAgent _navAgent;
        private BluePlayerController _player;
        private int _hp;
        private float _lastTouchDamageTime = -999f;

        public int Hp => _hp;
        public bool IsDead => _hp <= 0;

        public System.Action<BlueEnemy> Died;
        public static System.Action<BlueEnemy> OnAnyKilled;

        private void Awake()
        {
            _agent = GetComponent<MovementAgent>();
            _navAgent = GetComponent<NavMeshAgent>();
            if (_animDriver == null) _animDriver = GetComponent<BlueAnimDriver>();
            _hp = _maxHp;

            if (_navAgent != null)
            {
                _navAgent.speed = _moveSpeed;
                _navAgent.acceleration = _navAcceleration;
                _navAgent.angularSpeed = _navAngularSpeed;
                _navAgent.stoppingDistance = _touchRange * 0.9f;
                _navAgent.autoBraking = true;
                _navAgent.updateRotation = false;
            }
        }

        public void Initialize(BluePlayerController player)
        {
            _player = player;
        }

        private void Update()
        {
            if (IsDead || _player == null || _player.IsDead) return;

            Vector3 toPlayer = _player.transform.position - transform.position;
            toPlayer.y = 0f;
            float distSq = toPlayer.sqrMagnitude;
            float walkSpeed = 0f;

            bool useNav = _navAgent != null && _navAgent.enabled && _navAgent.isOnNavMesh;

            if (distSq > _touchRange * _touchRange)
            {
                if (useNav)
                {
                    _navAgent.isStopped = false;
                    _navAgent.SetDestination(_player.transform.position);

                    Vector3 vel = _navAgent.velocity;
                    walkSpeed = vel.magnitude;
                    if (vel.sqrMagnitude > 1e-4f)
                    {
                        Vector3 lookDir = vel; lookDir.y = 0f;
                        if (lookDir.sqrMagnitude > 1e-4f)
                        {
                            transform.rotation = Quaternion.Slerp(transform.rotation,
                                Quaternion.LookRotation(lookDir.normalized, Vector3.up),
                                10f * Time.deltaTime);
                        }
                    }
                }
                else
                {
                    Vector3 dir = toPlayer.normalized;
                    Vector3 delta = dir * (_moveSpeed * Time.deltaTime);
                    _agent.MoveBy(delta);
                    walkSpeed = _moveSpeed;

                    if (dir.sqrMagnitude > 1e-4f)
                    {
                        transform.rotation = Quaternion.Slerp(transform.rotation,
                            Quaternion.LookRotation(dir, Vector3.up), 10f * Time.deltaTime);
                    }
                }
            }
            else
            {
                if (useNav)
                {
                    _navAgent.isStopped = true;
                    _navAgent.ResetPath();
                }

                if (Time.time - _lastTouchDamageTime >= _touchDamageInterval)
                {
                    _player.TakeDamage(_touchDamage);
                    _lastTouchDamageTime = Time.time;
                    if (_animDriver != null) _animDriver.TriggerAttack();
                }

                Vector3 faceDir = toPlayer; faceDir.y = 0f;
                if (faceDir.sqrMagnitude > 1e-4f)
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation,
                        Quaternion.LookRotation(faceDir.normalized, Vector3.up),
                        10f * Time.deltaTime);
                }
            }

            if (_animDriver != null) _animDriver.DriveLocomotion(walkSpeed);
        }

        public void TakeDamage(int amount)
        {
            if (IsDead || amount <= 0) return;
            _hp = Mathf.Max(0, _hp - amount);
            if (_hitFlash != null) _hitFlash.Flash();
            if (_hp <= 0) Die();
        }

        private void Die()
        {
            Died?.Invoke(this);
            OnAnyKilled?.Invoke(this);
            if (_animDriver != null) _animDriver.TriggerDie();
            if (_navAgent != null) _navAgent.enabled = false;
            this.enabled = false;
            Destroy(gameObject, _destroyDelayOnDeath);
        }
    }
}
