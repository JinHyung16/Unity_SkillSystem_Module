namespace Jinhyeong_SkillSystem.BT
{

    public sealed class DebuffLeafNode : BTNode
    {
        public DebuffLeafNode(SkillNodeData data) : base(data) { }

        public override EBTStatus Tick(SkillContext ctx)
        {
            ctx.DebuffNodes.Add(Data);
            return EBTStatus.Success;
        }
    }
}
