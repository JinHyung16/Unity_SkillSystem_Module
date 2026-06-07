namespace Jinhyeong_AI.BehaviorTree
{
    /// <summary>자식 노드의 Success/Failure 결과를 뒤집어 반환하는 데코레이터 노드. Running은 그대로 통과.</summary>
    public class BTInverter : BTNode
    {
        private readonly BTNode _child;

        public BTInverter(BTNode child)
        {
            _child = child;
        }

        public override EBTStatus Tick(float deltaTime)
        {
            if (_child == null) return EBTStatus.Failure;

            EBTStatus s = _child.Tick(deltaTime);
            if (s == EBTStatus.Running) return EBTStatus.Running;
            return s == EBTStatus.Success ? EBTStatus.Failure : EBTStatus.Success;
        }

        public override void Reset()
        {
            if (_child != null) _child.Reset();
        }
    }
}
