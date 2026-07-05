using UnityEngine;
using Jinhyeong_GeneratedEnums;

namespace Jinhyeong_SkillSystem.BT
{

    public sealed class BuffSelfLeafNode : BTNode
    {
        public BuffSelfLeafNode(SkillNodeData data) : base(data) { }

        public override EBTStatus Tick(SkillContext ctx)
        {
            if (ctx.Caster == null)
                return EBTStatus.Failure;

            int buffId = Data.GetInt(ESkillParamKey.BuffId, ctx.LevelData, 0);
            if (buffId <= 0)
                return EBTStatus.Failure;

            SkillBuffData data = SkillBuffRegistry.GetBuff(buffId);
            if (data == null)
            {
                Debug.LogWarning($"[BuffSelfLeafNode] BuffId={buffId} not in registry");
                return EBTStatus.Failure;
            }
            ActiveStatusEffect.ApplyBuff(ctx.Caster.gameObject, data, ctx.Caster);
            return EBTStatus.Success;
        }
    }
}
