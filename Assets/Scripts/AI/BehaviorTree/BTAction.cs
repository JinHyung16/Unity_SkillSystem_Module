using System;

namespace Jinhyeong_AI.BehaviorTree
{
    /// <summary>전달받은 델리게이트를 매 Tick마다 실행해 EBTStatus를 반환하는 리프 노드.</summary>
    public class BTAction : BTNode
    {
        private readonly Func<float, EBTStatus> _action;
        private readonly Action _onReset;

        public BTAction(Func<float, EBTStatus> action, string name = null, Action onReset = null)
        {
            _action = action;
            _onReset = onReset;
            Name = name;
        }

        public override EBTStatus Tick(float deltaTime)
        {
            if (_action == null) return EBTStatus.Failure;
            return _action(deltaTime);
        }

        public override void Reset()
        {
            if (_onReset != null) _onReset.Invoke();
        }
    }
}
