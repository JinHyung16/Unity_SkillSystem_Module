using System.Collections.Generic;
using Jinhyeong_GeneratedEnums;

namespace Jinhyeong_SkillSystem
{
    /// <summary>SkillDefinition의 플랫 노드 리스트를 ESkillNodeType별 역할 슬롯으로 펼친 컴파일 결과. 런타임에서 타게팅/히트 노드를 즉시 조회. 발동 시점(Trigger)은 노드가 아니라 SkillData 메타에서 온다.</summary>
    public class CompiledSkill
    {
        public SkillDefinition Source;
        public int Level;
        public SkillLevelData LevelData;

        public SkillNodeData TargetingNode;
        public SkillNodeData HitNode;
        public SkillNodeData DespawnNode;
        public SkillNodeData LaunchNode;

        public readonly List<SkillNodeData> BuffSelfNodes = new List<SkillNodeData>(2);

        public readonly List<SkillNodeData> DebuffHitNodes = new List<SkillNodeData>(2);

        public readonly List<SkillNodeData> SubSkillNodes = new List<SkillNodeData>(2);

        public float NextReadyTime;

        public int SkillId { get { return Source != null && Source.Meta != null ? Source.Meta.Id : 0; } }
        public string Name  { get { return Source != null && Source.Meta != null ? Source.Meta.Name : string.Empty; } }
        public ESkillTriggerType Trigger { get { return Source != null && Source.Meta != null ? Source.Meta.Trigger : ESkillTriggerType.None; } }
    }
}
