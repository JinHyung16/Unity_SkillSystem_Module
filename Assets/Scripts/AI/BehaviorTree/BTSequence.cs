namespace Jinhyeong_AI.BehaviorTree
{

    public class BTSequence : BTComposite
    {
        public override EBTStatus Tick(float deltaTime)
        {
            while (_currentIndex < _children.Count)
            {
                BTNode child = _children[_currentIndex];
                EBTStatus s = child.Tick(deltaTime);
                if (s == EBTStatus.Running)
                {
                    return EBTStatus.Running;
                }
                if (s == EBTStatus.Failure)
                {
                    Reset();
                    return EBTStatus.Failure;
                }
                child.Reset();
                _currentIndex++;
            }

            Reset();
            return EBTStatus.Success;
        }
    }
}
