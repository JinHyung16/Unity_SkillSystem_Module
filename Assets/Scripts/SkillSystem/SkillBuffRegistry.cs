using System;
using System.Collections.Generic;
using Jinhyeong_JsonParsing;
using UnityEngine;
using Jinhyeong_GeneratedEnums;

namespace Jinhyeong_SkillSystem
{
    /// <summary>Buff/Debuff 정의를 id로 캐싱하는 메모리 저장소. DataManager에서 SkillBuff/SkillDebuff 테이블을 읽어 EBuffType/EDebuffType 파싱과 함께 적재.</summary>
    public static class SkillBuffRegistry
    {
        public const string TableBuff   = "SkillBuff";
        public const string TableDebuff = "SkillDebuff";

        private static readonly Dictionary<int, SkillBuffData>   _buffs   = new Dictionary<int, SkillBuffData>(16);
        private static readonly Dictionary<int, SkillDebuffData> _debuffs = new Dictionary<int, SkillDebuffData>(16);

        public static int BuffCount   { get { return _buffs.Count; } }
        public static int DebuffCount { get { return _debuffs.Count; } }

        public static SkillBuffData GetBuff(int id)
        {
            return _buffs.TryGetValue(id, out SkillBuffData v) ? v : null;
        }

        public static SkillDebuffData GetDebuff(int id)
        {
            return _debuffs.TryGetValue(id, out SkillDebuffData v) ? v : null;
        }

        public static void Clear()
        {
            _buffs.Clear();
            _debuffs.Clear();
        }

        public static void LoadFromDataManager(DataManager dm)
        {
            if (dm == null)
                throw new ArgumentNullException(nameof(dm));
            Clear();
            BuildBuffs(dm.GetTable(TableBuff));
            BuildDebuffs(dm.GetTable(TableDebuff));
        }

        private static void BuildBuffs(DataTable t)
        {
            if (t == null)
                return;
            for (int r = 0; r < t.RowCount; r++)
            {
                int id = t.GetInt(r, "id");
                if (id <= 0)
                    continue;

                string typeRaw = t.GetString(r, "type");
                if (Enum.TryParse(typeRaw, true, out EBuffType type) == false)
                {
                    Debug.LogWarning($"[SkillBuffRegistry] unknown buff type '{typeRaw}' at row {r}");
                    continue;
                }
                _buffs[id] = new SkillBuffData
                {
                    Id = id,
                    Name = t.GetString(r, "name"),
                    Description = t.GetString(r, "desc"),
                    Type = type,
                    Duration = t.GetFloat(r, "duration"),
                    TickInterval = t.GetFloat(r, "tick_interval"),
                    Value0 = t.GetFloat(r, "value0"),
                    Value1 = t.GetFloat(r, "value1"),
                };
            }
        }

        private static void BuildDebuffs(DataTable t)
        {
            if (t == null)
                return;
            for (int r = 0; r < t.RowCount; r++)
            {
                int id = t.GetInt(r, "id");
                if (id <= 0)
                    continue;

                string typeRaw = t.GetString(r, "type");
                if (Enum.TryParse(typeRaw, true, out EDebuffType type) == false)
                {
                    Debug.LogWarning($"[SkillBuffRegistry] unknown debuff type '{typeRaw}' at row {r}");
                    continue;
                }
                _debuffs[id] = new SkillDebuffData
                {
                    Id = id,
                    Name = t.GetString(r, "name"),
                    Description = t.GetString(r, "desc"),
                    Type = type,
                    Duration = t.GetFloat(r, "duration"),
                    TickInterval = t.GetFloat(r, "tick_interval"),
                    Value0 = t.GetFloat(r, "value0"),
                    Value1 = t.GetFloat(r, "value1"),
                };
            }
        }
    }
}
