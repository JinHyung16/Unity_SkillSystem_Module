namespace Jinhyeong_SkillSystem.BT
{

    public sealed class LaunchLeafNode : BTNode
    {
        public LaunchLeafNode(SkillNodeData data) : base(data) { }

        public override EBTStatus Tick(SkillContext ctx)
        {
            return LaunchExecutor.Execute(Data, ctx) ? EBTStatus.Success : EBTStatus.Failure;
        }
    }
}
