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

                SkillData meta = new SkillData
                {
                    Id = id,
                    Name = t.GetString(r, "name"),
                    Description = t.GetString(r, "desc"),
                    MaxLevel = Math.Max(1, t.GetInt(r, "max_level")),
                    VisualPath = t.GetString(r, "visual_path"),
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
                    ParentId = t.GetInt(r, "parent_id"),
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
