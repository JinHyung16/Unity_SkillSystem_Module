using Jinhyeong_GeneratedEnums;
namespace Jinhyeong_SkillSystem
{

    public class SkillBuffData
    {
        public int Id;
        public string Name = string.Empty;
        public string Description = string.Empty;
        public EBuffType Type;
        public float Duration;
        public float TickInterval;
        public float Value0;
        public float Value1;
    }

    public class SkillDebuffData
    {
        public int Id;
        public string Name = string.Empty;
        public string Description = string.Empty;
        public EDebuffType Type;
        public float Duration;
        public float TickInterval;
        public float Value0;
        public float Value1;
    }
}
