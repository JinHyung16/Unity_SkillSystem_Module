namespace Jinhyeong_SkillSystem
{
    /// <summary>Skill 시트 한 행의 메타데이터. id/이름/설명/최대 레벨/비주얼 키를 보관.</summary>
    public class SkillData
    {
        public int Id;
        public string Name = string.Empty;
        public string Description = string.Empty;
        public int MaxLevel = 1;
        public string VisualPath = string.Empty;
    }
}
