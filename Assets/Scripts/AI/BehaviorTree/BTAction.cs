using System;

namespace Jinhyeong_AI.BehaviorTree
{

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
            if (_action == null)
                return EBTStatus.Failure;
            return _action(deltaTime);
        }

        public override void Reset()
        {
            if (_onReset != null)
                _onReset.Invoke();
        }
    }
}
