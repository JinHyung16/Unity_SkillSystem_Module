namespace Jinhyeong_AI.BehaviorTree
{

    public abstract class BTNode
    {
        public string Name;

        public abstract EBTStatus Tick(float deltaTime);

        public virtual void Reset() { }
    }
}
