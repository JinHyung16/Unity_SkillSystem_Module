using System.Collections.Generic;

namespace Jinhyeong_AI.BehaviorTree
{
    /// <summary>자식 노드 목록과 현재 인덱스를 관리하는 컴포지트 노드 베이스. Selector/Sequence 등의 공통 부모.</summary>
    public abstract class BTComposite : BTNode
    {
        protected readonly List<BTNode> _children = new List<BTNode>(4);
        protected int _currentIndex;

        public BTComposite Add(BTNode child)
        {
            if (child != null) _children.Add(child);
            return this;
        }

        public override void Reset()
        {
            _currentIndex = 0;
            for (int i = 0; i < _children.Count; i++)
            {
                _children[i].Reset();
            }
        }
    }
}
