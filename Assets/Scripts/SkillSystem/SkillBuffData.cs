using Jinhyeong_GeneratedEnums;
namespace Jinhyeong_SkillSystem
{
    /// <summary>SkillBuff 시트의 한 행에 대응하는 긍정 상태이상 정의. ActiveStatusEffect가 이 값을 읽어 캐스터에게 버프를 부여.</summary>
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