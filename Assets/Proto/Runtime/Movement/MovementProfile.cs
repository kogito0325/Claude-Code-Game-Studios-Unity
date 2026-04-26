using UnityEngine;

namespace Proto.Movement
{
    [CreateAssetMenu(fileName = "Movement_Default", menuName = "Proto/Movement/MovementProfile")]
    public class MovementProfile : ScriptableObject
    {
        [Header("Capsule")]
        public float radius = 0.4f;
        public float height = 1.8f;

        [Header("Speed")]
        public float maxSpeed = 5f;
        public float acceleration = 40f;

        [Header("Terrain")]
        [Tooltip("자동 점프 가능한 턱 높이(unit). 0=비활성. 너무 크면 작은 돌·펜스 위로 점프 발생.")]
        public float stepOffset = 0.1f;
        [Tooltip("slope 의 위쪽 슬라이드를 허용하는 각도. 이 이상이면 슬라이드 차단.")]
        public float slopeLimit = 45f;
        [Tooltip("step over 시 윗면이 수평에 얼마나 가까워야 하는지 (Vector3.up 과의 dot). 1=완전 평면, 0.85≈31°. 둥근 돌·기울어진 면 위로 점프되는 현상 방지.")]
        [Range(0f, 1f)] public float stepSurfaceMinFlatness = 0.85f;
        [Tooltip("이동 시 막아주는 충돌 레이어. ALL=~0 이면 모든 콜라이더와 충돌.")]
        public LayerMask blockingLayers = ~0;
        [Tooltip("ground 로 인식할 레이어. CheckGrounded·step over down sweep 에서 사용.")]
        public LayerMask groundLayers = 1;
        [Tooltip("벽 슬라이드 시 입력 반대 방향(>90°)으로 미끄러지는 것을 차단. true=입력 의도 보존, false=정통 character controller 동작.")]
        public bool blockReverseSlide = true;
        [Tooltip("한 번의 SweepAndSlide 가 이동할 수 있는 최대 거리(unit). 이보다 큰 delta 는 여러 substep 으로 분할 — fps 변동에 따른 stepOver/충돌 판정 불일치 방지. 0 이하면 분할 비활성.")]
        public float maxSubstepLength = 0.1f;

        [Header("Gravity")]
        public bool applyGravity = true;
        public float gravity = -20f;

        [Header("Resolution")]
        [Range(1, 8)] public int maxPushoutIterations = 3;
        public float skinWidth = 0.02f;
    }
}
