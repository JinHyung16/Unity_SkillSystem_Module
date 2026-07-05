using System;

namespace Jinhyeong_AI.BehaviorTree
{

    public class BTCondition : BTNode
    {
        private readonly Func<bool> _predicate;

        public BTCondition(Func<bool> predicate, string name = null)
        {
            _predicate = predicate;
            Name = name;
        }

        public override EBTStatus Tick(float deltaTime)
        {
            if (_predicate == null)
                return EBTStatus.Failure;
            return _predicate() ? EBTStatus.Success : EBTStatus.Failure;
        }
    }
}
