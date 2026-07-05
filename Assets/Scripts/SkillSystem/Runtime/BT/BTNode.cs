using System.Collections.Generic;

namespace Jinhyeong_SkillSystem.BT
{

    public abstract class BTNode
    {

        protected readonly SkillNodeData Data;

        public readonly List<BTNode> Children = new List<BTNode>(4);

        protected BTNode(SkillNodeData data)
        {
            Data = data;
        }

        public abstract EBTStatus Tick(SkillContext ctx);

        protected EBTStatus RunChildrenAsSequence(SkillContext ctx)
        {
            for (int i = 0; i < Children.Count; i++)
            {
                if (Children[i].Tick(ctx) == EBTStatus.Failure)
                    return EBTStatus.Failure;
            }
            return EBTStatus.Success;
        }
    }
}
