namespace Jinhyeong_AI.BehaviorTree
{
    /// <summary>자식들을 순차 실행하다가 하나라도 Failure면 즉시 Failure 반환. 모두 성공하면 Success.</summary>
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
