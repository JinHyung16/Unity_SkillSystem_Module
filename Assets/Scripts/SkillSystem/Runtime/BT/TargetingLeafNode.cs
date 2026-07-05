namespace Jinhyeong_SkillSystem.BT
{

    public sealed class TargetingLeafNode : BTNode
    {
        public TargetingLeafNode(SkillNodeData data) : base(data) { }

        public override EBTStatus Tick(SkillContext ctx)
        {
            TargetingResolver.Resolve(Data, ctx.LevelData, ctx);
            return EBTStatus.Success;
        }
    }
}
