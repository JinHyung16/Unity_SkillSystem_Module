namespace Jinhyeong_AI.BehaviorTree
{

    public class BTInverter : BTNode
    {
        private readonly BTNode _child;

        public BTInverter(BTNode child)
        {
            _child = child;
        }

        public override EBTStatus Tick(float deltaTime)
        {
            if (_child == null)
                return EBTStatus.Failure;

            EBTStatus s = _child.Tick(deltaTime);
            if (s == EBTStatus.Running)
                return EBTStatus.Running;
            return s == EBTStatus.Success ? EBTStatus.Failure : EBTStatus.Success;
        }

        public override void Reset()
        {
            if (_child != null)
                _child.Reset();
        }
    }
}
