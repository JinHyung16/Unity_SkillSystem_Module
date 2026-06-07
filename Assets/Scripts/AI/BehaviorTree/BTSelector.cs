namespace Jinhyeong_AI.BehaviorTree
{
    /// <summary>자식들을 순차 실행하다가 하나라도 Success면 즉시 Success 반환. 모두 실패하면 Failure.</summary>
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
