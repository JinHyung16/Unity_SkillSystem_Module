namespace Jinhyeong_SkillSystem.BT
{

    public sealed class SelectorNode : BTNode
    {
        public SelectorNode(SkillNodeData data) : base(data) { }

        public override EBTStatus Tick(SkillContext ctx)
        {
            for (int i = 0; i < Children.Count; i++)
            {
                if (Children[i].Tick(ctx) == EBTStatus.Success)
                    return EBTStatus.Success;
            }
            return EBTStatus.Failure;
        }
    }
}
