using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Jinhyeong_JsonParsing;
using Jinhyeong_Managers;
using UnityEngine;
using Jinhyeong_GeneratedEnums;

namespace Jinhyeong_SkillSystem
{
    /// <summary>skill_id 기준 SkillDefinition 캐시. DataManager로부터 Skill/SkillLevel/SkillBTNode 3종 테이블을 한 번에 로드해 노드 정렬과 버프 사이드카 로딩까지 처리.</summary>
    public static class SkillRegistry
    {
        public const string TableSkill       = "Skill";
        public const string TableSkillLevel  = "SkillLevel";
        public const string TableSkillBTNode = "SkillBTNode";

        private static readonly Dictionary<int, SkillDefinition> _byId =
            new Dictionary<int, SkillDefinition>(32);

        private static Task _loadingTask;

        public static int Count           { get { return _byId.Count; } }
        public static IEnumerable<SkillDefinition> All { get { return _byId.Values; } }
        public static bool IsLoaded       { get; private set; }

        public static void Add(SkillDefinition def)
        {
            if (def == null || def.Meta == null)
                return;
            _byId[def.Meta.Id] = def;
        }

        public static SkillDefinition Get(int skillId)
        {
            return _byId.TryGetValue(skillId, out SkillDefinition def) ? def : null;
        }

        public static bool TryGet(int skillId, out SkillDefinition def)
        {
            return _byId.TryGetValue(skillId, out def);
        }

        public static void Clear()
        {
            _byId.Clear();
            IsLoaded = false;
        }

        public static Task LoadAsync()
        {
            if (IsLoaded)
            {
                return Task.CompletedTask;
            }
            if (_loadingTask != null)
            {
                return _loadingTask;
            }
            _loadingTask = LoadInternalAsync();
            return _loadingTask;
        }

        private static async Task LoadInternalAsync()
        {
            try
            {
                DataManager dm = DataManager.Instance;
                if (dm.IsInitialized == false)
                {
                    await dm.InitializeAsync();
                }
                LoadFromDataManager(dm);
            }
            finally
            {
                _loadingTask = null;
            }
        }

        public static void LoadFromDataManager(DataManager dm)
        {
            if (dm == null)
                throw new ArgumentNullException(nameof(dm));

            DataTable skill = dm.GetTable(TableSkill);
            DataTable level = dm.GetTable(TableSkillLevel);
            DataTable nodes = dm.GetTable(TableSkillBTNode);

            Clear();

            BuildSkills(skill);
            BuildLevels(level);
            BuildNodes(nodes);
            SortNodes();

            SkillBuffRegistry.LoadFromDataManager(dm);

            IsLoaded = true;
        }

        private static void BuildSkills(DataTable t)
        {
            if (t == null)
                return;
            for (int r = 0; r < t.RowCount; r++)
            {
                int id = t.GetInt(r, "id");
                if (id <= 0)
                    continue;

                ESkillTriggerType trigger = ESkillTriggerType.None;
                string triggerRaw = t.GetString(r, "trigger");
                if (string.IsNullOrEmpty(triggerRaw) == false
                    && Enum.TryParse(triggerRaw, true, out ESkillTriggerType parsedTrigger))
                {
                    trigger = parsedTrigger;
                }

                ESkillCategory category = ESkillCategory.None;
                string categoryRaw = t.GetString(r, "category");
                if (string.IsNullOrEmpty(categoryRaw) == false
                    && Enum.TryParse(categoryRaw, true, out ESkillCategory parsedCategory))
                {
                    category = parsedCategory;
                }

                SkillData meta = new SkillData
                {
                    Id = id,
                    Name = t.GetString(r, "name"),
                    Description = t.GetString(r, "desc"),
                    MaxLevel = Math.Max(1, t.GetInt(r, "max_level")),
                    Trigger = trigger,
                    Category = category,
                };
                Add(new SkillDefinition { Meta = meta });
            }
        }

        private static void BuildLevels(DataTable t)
        {
            if (t == null)
                return;
            for (int r = 0; r < t.RowCount; r++)
            {
                int skillId = t.GetInt(r, "skill_id");
                if (skillId <= 0)
                    continue;
                SkillDefinition def = Get(skillId);
                if (def == null)
                    continue;

                SkillLevelData lv = new SkillLevelData
                {
                    Id = t.GetInt(r, "id"),
                    SkillId = skillId,
                    Level = t.GetInt(r, "level"),
                };

                for (int slot = 0; ; slot++)
                {
                    string keyCol = "modifier" + slot.ToString(CultureInfo.InvariantCulture);
                    string valCol = "value" + slot.ToString(CultureInfo.InvariantCulture);
                    if (t.TryGetColumnIndex(keyCol, out _) == false)
                        break;

                    string keyRaw = t.GetString(r, keyCol);
                    if (string.IsNullOrEmpty(keyRaw))
                        continue;
                    if (Enum.TryParse(keyRaw, true, out ESkillParamKey key) == false)
                        continue;
                    if (key == ESkillParamKey.None)
                        continue;

                    lv.Modifiers[key] = t.GetFloat(r, valCol);
                }

                if (lv.Level >= 1)
                {
                    def.Levels[lv.Level] = lv;
                    if (lv.Level > def.Meta.MaxLevel)
                    {
                        def.Meta.MaxLevel = lv.Level;
                    }
                }
            }
        }

        private static void BuildNodes(DataTable t)
        {
            if (t == null)
                return;
            for (int r = 0; r < t.RowCount; r++)
            {
                int skillId = t.GetInt(r, "skill_id");
                if (skillId <= 0)
                    continue;
                SkillDefinition def = Get(skillId);
                if (def == null)
                    continue;

                string typeRaw = t.GetString(r, "node_type");
                if (string.IsNullOrEmpty(typeRaw))
                    continue;
                if (Enum.TryParse(typeRaw, true, out ESkillNodeType nodeType) == false)
                {
                    Debug.LogWarning($"[SkillRegistry] unknown node_type '{typeRaw}' on skill {skillId} row {r}");
                    nodeType = ESkillNodeType.None;
                }

                SkillNodeData node = new SkillNodeData
                {
                    SkillId = skillId,
                    NodeId = t.GetInt(r, "node_id"),
                    Order = t.GetInt(r, "order"),
                    NodeType = nodeType,
                };

                for (int slot = 0; ; slot++)
                {
                    string keyCol = "param" + slot.ToString(CultureInfo.InvariantCulture);
                    string valCol = "value" + slot.ToString(CultureInfo.InvariantCulture);
                    if (t.TryGetColumnIndex(keyCol, out _) == false)
                        break;

                    string keyRaw = t.GetString(r, keyCol);
                    if (string.IsNullOrEmpty(keyRaw))
                        continue;
                    if (Enum.TryParse(keyRaw, true, out ESkillParamKey key) == false)
                        continue;
                    if (key == ESkillParamKey.None)
                        continue;

                    node.Params[key] = t.GetString(r, valCol) ?? string.Empty;
                }

                def.Nodes.Add(node);
            }
        }

        private static void SortNodes()
        {
            foreach (SkillDefinition def in _byId.Values)
            {
                def.Nodes.Sort((a, b) => a.Order.CompareTo(b.Order));
            }
        }

        /// <summary>로드된 전 스킬의 노드 Visual param을 distinct 수집. spawn 시 실제 사용되는 키만 모아 Addressables 사전로드에 쓴다.</summary>
        public static void CollectVisualKeys(ICollection<string> buffer)
        {
            if (buffer == null)
                return;
            foreach (SkillDefinition def in _byId.Values)
            {
                if (def == null)
                    continue;

                for (int i = 0; i < def.Nodes.Count; i++)
                {
                    SkillNodeData node = def.Nodes[i];
                    if (node == null)
                        continue;
                    string visual = node.GetString(ESkillParamKey.Visual);
                    if (string.IsNullOrEmpty(visual))
                        continue;
                    buffer.Add(visual);
                }
            }
        }

        /// <summary>스킬 VFX 프리팹을 Addressables로 사전로드해 spawn 시 동기 조회가 가능하도록 캐시를 워밍한다. 반드시 메인 스레드에서 호출. AddressableManager가 키를 dedupe하므로 반복 호출 안전.</summary>
        public static async UniTask PreloadVisualsAsync()
        {
            HashSet<string> keys = new HashSet<string>();
            CollectVisualKeys(keys);
            if (keys.Count == 0)
                return;

            AddressableManager am = AddressableManager.Ensure();
            await am.LoadAllAsync(keys);
        }
    }
}
