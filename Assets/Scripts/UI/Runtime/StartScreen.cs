using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Jinhyeong_Character;
using Jinhyeong_Managers;
using Jinhyeong_SkillSystem;

namespace Jinhyeong_UI
{
    [Serializable]
    public class StartScreenSlot
    {
        public KeyCode Key = KeyCode.None;
        public Button Button;
        public Text Label;
    }

    /// <summary>게임 시작 화면 컨트롤러. Canvas/Button/Text는 prefab에서 구성하고 SerializeField로 참조만 받음(코드 자체 생성 없음). SkillRegistry 비동기 로드 → 슬롯 라벨 갱신 → GAME START 클릭 시 동적 SkillLoadout 생성 → Player.SkillObject에 장착 → GameFlow.StartGame(). GameFlow가 Resources/Prefabs/StartScreen.prefab을 자동 Instantiate하므로 Main.unity 직접 수정은 불필요.</summary>
    [DisallowMultipleComponent]
    public class StartScreen : MonoBehaviour
    {
        [Header("UI Refs")]
        [SerializeField] private GameObject _root;
        [SerializeField] private Text _statusText;
        [SerializeField] private Button _startButton;
        [SerializeField] private List<StartScreenSlot> _slots = new List<StartScreenSlot>();

        private readonly Dictionary<KeyCode, int> _slotSkillId = new Dictionary<KeyCode, int>(8);
        private readonly List<int> _availableSkillIds = new List<int>();

        private void Awake()
        {
            if (_startButton != null)
            {
                _startButton.interactable = false;
                _startButton.onClick.AddListener(OnStartClicked);
            }

            for (int i = 0; i < _slots.Count; i++)
            {
                StartScreenSlot s = _slots[i];
                if (s == null || s.Button == null) continue;
                KeyCode captured = s.Key;
                s.Button.onClick.AddListener(() => OnSlotClicked(captured));
            }
        }

        private async void Start()
        {
            if (SkillRegistry.IsLoaded == false)
            {
                SetStatus("스킬 데이터 로딩 중...");
                try { await SkillRegistry.LoadAsync(); }
                catch (Exception e) { SetStatus($"스킬 로드 실패: {e.Message}"); return; }
            }

            CollectAvailableSkills();
            AssignDefaults();
            RefreshSlotLabels();

            SetStatus(_availableSkillIds.Count > 0
                ? $"사용 가능 스킬 {_availableSkillIds.Count}개 · 슬롯을 클릭해 변경"
                : "사용 가능 스킬 없음 (Resources/GoogleSheetData 확인)");
            if (_startButton != null) _startButton.interactable = true;
        }

        private void CollectAvailableSkills()
        {
            _availableSkillIds.Clear();
            foreach (SkillDefinition def in SkillRegistry.All)
            {
                if (def == null || def.Meta == null) continue;
                _availableSkillIds.Add(def.Meta.Id);
            }
            _availableSkillIds.Sort();
        }

        private void AssignDefaults()
        {
            _slotSkillId.Clear();

            Player player = GameEvents.CurrentPlayer;
            SkillLoadout existing = null;
            if (player != null && player.Skills != null) existing = player.Skills.Loadout;

            if (existing != null && existing.Entries != null)
            {
                for (int i = 0; i < existing.Entries.Count; i++)
                {
                    EquippedSkillEntry e = existing.Entries[i];
                    if (e == null) continue;
                    if (e.SlotKey == KeyCode.None) continue;
                    if (HasSlot(e.SlotKey) == false) continue;
                    if (_availableSkillIds.Contains(e.SkillId) == false) continue;
                    _slotSkillId[e.SlotKey] = e.SkillId;
                }
            }

            int cursor = 0;
            for (int i = 0; i < _slots.Count; i++)
            {
                KeyCode k = _slots[i].Key;
                if (k == KeyCode.None) continue;
                if (_slotSkillId.ContainsKey(k)) continue;
                if (cursor < _availableSkillIds.Count)
                {
                    _slotSkillId[k] = _availableSkillIds[cursor++];
                }
                else
                {
                    _slotSkillId[k] = 0;
                }
            }
        }

        private bool HasSlot(KeyCode key)
        {
            for (int i = 0; i < _slots.Count; i++)
            {
                if (_slots[i].Key == key) return true;
            }
            return false;
        }

        private void OnSlotClicked(KeyCode slot)
        {
            if (_availableSkillIds.Count == 0) return;
            int current = _slotSkillId.TryGetValue(slot, out int v) ? v : 0;
            int idx = _availableSkillIds.IndexOf(current);
            idx = (idx + 1) % _availableSkillIds.Count;
            _slotSkillId[slot] = _availableSkillIds[idx];
            RefreshSlotLabels();
        }

        private void RefreshSlotLabels()
        {
            for (int i = 0; i < _slots.Count; i++)
            {
                StartScreenSlot s = _slots[i];
                if (s == null || s.Label == null) continue;

                int id = _slotSkillId.TryGetValue(s.Key, out int v) ? v : 0;
                string skillName = "(없음)";
                if (id != 0)
                {
                    SkillDefinition def = SkillRegistry.Get(id);
                    if (def != null && def.Meta != null && string.IsNullOrEmpty(def.Meta.Name) == false)
                    {
                        skillName = def.Meta.Name;
                    }
                    else skillName = $"#{id}";
                }
                s.Label.text = $"[{s.Key}]\n{skillName}";
            }
        }

        private void OnStartClicked()
        {
            SkillLoadout lo = ScriptableObject.CreateInstance<SkillLoadout>();
            lo.Entries = new List<EquippedSkillEntry>(_slotSkillId.Count);
            foreach (KeyValuePair<KeyCode, int> kv in _slotSkillId)
            {
                if (kv.Value == 0) continue;
                lo.Entries.Add(new EquippedSkillEntry { SkillId = kv.Value, Level = 1, SlotKey = kv.Key });
            }

            Player player = GameEvents.CurrentPlayer;
            if (player != null && player.Skills != null)
            {
                player.Skills.Loadout = lo;
                player.Skills.EquipAll();
            }

            GameFlow.StartGame();
            GameObject target = _root != null ? _root : gameObject;
            target.SetActive(false);
        }

        private void SetStatus(string text)
        {
            if (_statusText != null) _statusText.text = text;
        }
    }
}
