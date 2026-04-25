using UnityEngine;

namespace Proto.Sample.BlueArch
{
    /// <summary>
    /// 캐릭터의 Animator state 를 코드에서 직접 CrossFade 로 전환하는 헬퍼.
    /// 컨트롤러에 파라미터 없어도 state name 만 알면 동작.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Animator))]
    public sealed class BlueAnimDriver : MonoBehaviour
    {
        [SerializeField] private string _idleState = "Idle_Normal_NoWeapon";
        [SerializeField] private string _walkState = "MoveFWD_Normal_InPlace_NoWeapon";
        [SerializeField] private string _attackState = "Attack01_NoWeapon";
        [SerializeField] private string _dieState = "Die01_NoWeapon";
        [SerializeField] private float _speedThreshold = 0.05f;
        [SerializeField] private float _crossFadeTime = 0.15f;
        [SerializeField] private float _attackHoldDuration = 0.4f;

        private Animator _animator;
        private int _idleHash, _walkHash, _attackHash, _dieHash;
        private int _currentLoopHash;
        private float _attackHoldUntil;
        private bool _dead;

        public bool IsAttacking => Time.time < _attackHoldUntil;
        public bool IsDead => _dead;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            _idleHash = Animator.StringToHash(_idleState);
            _walkHash = Animator.StringToHash(_walkState);
            _attackHash = Animator.StringToHash(_attackState);
            _dieHash = Animator.StringToHash(_dieState);
        }

        public void DriveLocomotion(float speed)
        {
            if (_dead || _animator == null) return;
            if (IsAttacking) return;

            int desiredHash = speed > _speedThreshold ? _walkHash : _idleHash;
            if (desiredHash == _currentLoopHash) return;

            if (HasState(desiredHash))
            {
                _animator.CrossFade(desiredHash, _crossFadeTime, 0);
                _currentLoopHash = desiredHash;
            }
        }

        public void TriggerAttack()
        {
            if (_dead || _animator == null) return;
            if (!HasState(_attackHash)) return;
            _animator.CrossFade(_attackHash, 0.06f, 0);
            _attackHoldUntil = Time.time + _attackHoldDuration;
            _currentLoopHash = 0;
        }

        public void TriggerDie()
        {
            if (_dead || _animator == null) return;
            _dead = true;
            if (HasState(_dieHash)) _animator.CrossFade(_dieHash, 0.1f, 0);
        }

        private bool HasState(int hash)
        {
            return _animator != null && _animator.HasState(0, hash);
        }
    }
}
