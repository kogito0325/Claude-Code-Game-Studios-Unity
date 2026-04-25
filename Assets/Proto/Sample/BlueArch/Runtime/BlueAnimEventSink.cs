using UnityEngine;

namespace Proto.Sample.BlueArch
{
    /// <summary>
    /// CombatGirls 팩 애니메이션 클립에 박혀 있는 AnimationEvent들을 흡수하는 빈 스텁.
    /// 'SwitchSocket' 같은 이벤트가 수신자 없을 때 발생하는 경고를 막는다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BlueAnimEventSink : MonoBehaviour
    {
        public void SwitchSocket() { }
        public void SwitchSocket(string socket) { }
        public void SwitchSocket(int socket) { }
        public void SwitchSocket(float socket) { }
        public void SwitchSocket(Object socket) { }
    }
}
