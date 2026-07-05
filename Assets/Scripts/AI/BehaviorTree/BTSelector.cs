namespace Jinhyeong_AI.BehaviorTree
{

    public class BTSelector : BTComposite
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
                if (s == EBTStatus.Success)
                {
                    Reset();
                    return EBTStatus.Success;
                }
                child.Reset();
                _currentIndex++;
            }

            Reset();
            return EBTStatus.Failure;
        }
    }
}
