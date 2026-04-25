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

            if (distSq > _touchRange * _touchRange)
            {
                Vector3 dir = toPlayer.normalized;
                Vector3 delta = dir * (_moveSpeed * Time.deltaTime);
                _agent.MoveBy(delta);

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
                }
            }
        }

        public void TakeDamage(int amount)
        {
            if (IsDead || amount <= 0) return;
            _hp = Mathf.Max(0, _hp - amount);
            if (_hp <= 0) Die();
        }

        private void Die()
        {
            Died?.Invoke(this);
            OnAnyKilled?.Invoke(this);
            Destroy(gameObject);
        }
    }
}
