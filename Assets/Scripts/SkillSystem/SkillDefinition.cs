using System.Collections.Generic;

namespace Jinhyeong_SkillSystem
{
    /// <summary>스킬 메타 + 레벨 테이블 + Order로 정렬된 플랫 노드 시퀀스를 묶은 완성 정의. skill_id당 하나씩.</summary>
    public class SkillDefinition
    {
        public SkillData Meta;

        public readonly Dictionary<int, SkillLevelData> Levels =
            new Dictionary<int, SkillLevelData>(8);

        public readonly List<SkillNodeData> Nodes = new List<SkillNodeData>(8);

        public SkillLevelData GetLevel(int level)
        {
            return Levels.TryGetValue(level, out SkillLevelData data) ? data : null;
        }
    }
}
