using Jinhyeong_GeneratedEnums;

namespace Jinhyeong_SkillSystem
{
    /// <summary>Skill 시트 한 행의 메타데이터. id/이름/설명/최대 레벨과, "이 스킬이 무엇인지"를 한눈에 정의하는 Trigger(언제 발동)·Category(분류)를 보관. (spawn 비주얼 키는 런치 노드의 Visual param에서 읽음)</summary>
    public class SkillData
    {
        public int Id;
        public string Name = string.Empty;
        public string Description = string.Empty;
        public int MaxLevel = 1;

        /// <summary>발동 시점. OnAttack(기본공격 시) / OnTick(주기) / OnOreBreak(채굴 시, 라우팅 미구현).</summary>
        public ESkillTriggerType Trigger = ESkillTriggerType.None;

        /// <summary>스킬 성격 분류. DB 한 줄만 봐도 어떤 스킬인지 식별/필터하기 위한 메타.</summary>
        public ESkillCategory Category = ESkillCategory.None;
    }
}
