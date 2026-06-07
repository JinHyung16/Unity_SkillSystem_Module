using System.Collections.Generic;
using Jinhyeong_GeneratedEnums;

namespace Jinhyeong_SkillSystem
{
    /// <summary>SkillLevel 시트의 한 행. 레벨별 ESkillParamKey 모디파이어 값을 보관해 노드의 빈 파라미터 슬롯을 채운다.</summary>
    public class SkillLevelData
    {
        public int Id;
        public int SkillId;
        public int Level;

        public readonly Dictionary<ESkillParamKey, float> Modifiers =
            new Dictionary<ESkillParamKey, float>(8);

        public bool TryGet(ESkillParamKey key, out float value)
        {
            return Modifiers.TryGetValue(key, out value);
        }

        public float GetOrDefault(ESkillParamKey key, float fallback = 0f)
        {
            return Modifiers.TryGetValue(key, out float v) ? v : fallback;
        }
    }
}
