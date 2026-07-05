namespace Jinhyeong_SkillSystem.BT
{

    public sealed class DespawnLeafNode : BTNode
    {
        public DespawnLeafNode(SkillNodeData data) : base(data) { }

        public override EBTStatus Tick(SkillContext ctx)
        {
            ctx.DespawnNode = Data;
            return EBTStatus.Success;
        }
    }
}
