namespace Jinhyeong_SkillSystem
{
    /// <summary>스킬의 아군/적군/중립 식별 태그. 타게팅과 히트 판정에서 사용.</summary>
    public enum ESkillTeam
    {
        Neutral = 0,
        Friend = 1,
        Enemy = 2,
    }

    public static class SkillTeamUtil
    {
        public static ESkillTeam Opposite(ESkillTeam team)
        {
            switch (team)
            {
                case ESkillTeam.Friend: return ESkillTeam.Enemy;
                case ESkillTeam.Enemy:  return ESkillTeam.Friend;
                default:                return ESkillTeam.Neutral;
            }
        }
    }
}
