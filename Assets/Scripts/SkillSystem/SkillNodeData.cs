using System.Collections.Generic;
using System.Globalization;
using Jinhyeong_GeneratedEnums;

namespace Jinhyeong_SkillSystem
{
    /// <summary>SkillBTNode 시트의 한 행. 스킬 시퀀스 노드 하나의 타입/순서와 파라미터 dict를 보관하고 GetFloat/Int/Bool/String 헬퍼로 레벨 모디파이어 폴백을 제공.</summary>
    public class SkillNodeData
    {
        public int SkillId;
        public int NodeId;
        public int Order;
        public ESkillNodeType NodeType;

        public readonly Dictionary<ESkillParamKey, string> Params =
            new Dictionary<ESkillParamKey, string>(8);

        public bool HasParam(ESkillParamKey key)
        {
            return Params.ContainsKey(key);
        }

        public bool HasStaticValue(ESkillParamKey key)
        {
            return Params.TryGetValue(key, out string v) && string.IsNullOrEmpty(v) == false;
        }

        public float GetFloat(ESkillParamKey key, SkillLevelData level = null, float fallback = 0f)
        {
            if (Params.TryGetValue(key, out string raw) == false)
            {
                return fallback;
            }
            if (string.IsNullOrEmpty(raw))
            {
                if (level != null && level.TryGet(key, out float v))
                {
                    return v;
                }
                return fallback;
            }
            if (float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed))
            {
                return parsed;
            }
            return fallback;
        }

        public int GetInt(ESkillParamKey key, SkillLevelData level = null, int fallback = 0)
        {
            if (Params.TryGetValue(key, out string raw) == false)
            {
                return fallback;
            }
            if (string.IsNullOrEmpty(raw))
            {
                if (level != null && level.TryGet(key, out float v))
                {
                    return (int)v;
                }
                return fallback;
            }
            if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed))
            {
                return parsed;
            }
            if (float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out float f))
            {
                return (int)f;
            }
            return fallback;
        }

        public bool GetBool(ESkillParamKey key, bool fallback = false)
        {
            if (Params.TryGetValue(key, out string raw) == false)
            {
                return fallback;
            }
            if (string.IsNullOrEmpty(raw))
            {
                return fallback;
            }
            if (bool.TryParse(raw, out bool parsed))
            {
                return parsed;
            }
            if (raw == "1") { return true; }
            if (raw == "0") { return false; }
            return fallback;
        }

        public string GetString(ESkillParamKey key, string fallback = "")
        {
            if (Params.TryGetValue(key, out string raw))
            {
                return raw ?? fallback;
            }
            return fallback;
        }
    }
}
