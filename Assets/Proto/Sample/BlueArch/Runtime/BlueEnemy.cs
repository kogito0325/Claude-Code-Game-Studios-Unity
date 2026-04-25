using Proto.Movement;
using UnityEngine;

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

        private MovementAgent _agent;
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
            if (_animDriver == null) _animDriver = GetComponent<BlueAnimDriver>();
            _hp = _maxHp;
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
            if (distSq > _touchRange * _touchRange)
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
            else
            {
                if (Time.time - _lastTouchDamageTime >= _touchDamageInterval)
                {
                    _player.TakeDamage(_touchDamage);
                    _lastTouchDamageTime = Time.time;
                    if (_animDriver != null) _animDriver.TriggerAttack();
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
            // Disable AI movement immediately, wait for death anim before destroy.
            this.enabled = false;
            Destroy(gameObject, _destroyDelayOnDeath);
        }
    }
}
