namespace Jinhyeong_SkillSystem
{

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
