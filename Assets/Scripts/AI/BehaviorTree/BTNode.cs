namespace Jinhyeong_AI.BehaviorTree
{
    /// <summary>비헤이비어 트리의 모든 노드가 상속하는 추상 베이스. Tick과 Reset 인터페이스를 정의.</summary>
    public abstract class BTNode
    {
        public string Name;

        public abstract EBTStatus Tick(float deltaTime);

        public virtual void Reset() { }
    }
}
