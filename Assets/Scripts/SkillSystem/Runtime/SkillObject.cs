using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Jinhyeong_GeneratedEnums;

namespace Jinhyeong_SkillSystem
{
    /// <summary>장착된 스킬 목록을 컴파일해 트리거(OnAttack/OnTick/슬롯키)별로 hook하고 발동 시 쿨다운/확률 체크 후 BuffSelf 적용과 LaunchExecutor 실행을 수행.</summary>
    [DisallowMultipleComponent]
    public class SkillObject : MonoBehaviour
    {
        public ESkillTeam Team
        {
            get
            {
                Damageable d = GetComponent<Damageable>();
                return d != null ? d.Team : ESkillTeam.Neutral;
            }
        }

        public float OutgoingDamageMultiplier = 1f;
        public float AttackSpeedMultiplier = 1f;
        public bool Stunned = false;

        [Tooltip("장착할 스킬 로드아웃 SO. Start 시점에 컴파일/슬롯 후킹.")]
        public SkillLoadout Loadout;

        public event Action OnAttack;

        public event Action OnOreBreak;

        private readonly List<CompiledSkill> _onAttackSkills = new List<CompiledSkill>();
        private readonly List<CompiledSkill> _onOreBreakSkills = new List<CompiledSkill>();
        private readonly Dictionary<KeyCode, CompiledSkill> _slotSkills = new Dictionary<KeyCode, CompiledSkill>(8);
        private readonly SkillContext _scratchCtx = new SkillContext();

        private async void Start()
        {
            if (SkillRegistry.IsLoaded == false)
            {
                try
                {
                    await SkillRegistry.LoadAsync();
                }
                catch (Exception e)
                {
                    Debug.LogError($"[SkillObject] SkillRegistry load failed: {e.Message}");
                    return;
                }
            }
            EquipAll();
        }

        public void RaiseAttack()
        {
            OnAttack?.Invoke();
        }

        public void RaiseOreBreak()
        {
            OnOreBreak?.Invoke();
        }

        public bool TryFireSlot(KeyCode key)
        {
            if (key == KeyCode.None) return false;
            if (_slotSkills.TryGetValue(key, out CompiledSkill c) == false) return false;
            return TryFireSlotInternal(c);
        }

        public void EquipAll()
        {
            OnAttack = null;
            OnOreBreak = null;
            _onAttackSkills.Clear();
            _onOreBreakSkills.Clear();
            _slotSkills.Clear();

            if (Loadout == null || Loadout.Entries == null) return;

            for (int i = 0; i < Loadout.Entries.Count; i++)
            {
                EquippedSkillEntry setup = Loadout.Entries[i];
                if (setup == null) continue;

                SkillDefinition def = SkillRegistry.Get(setup.SkillId);
                if (def == null)
                {
                    Debug.LogWarning($"[SkillObject] skill_id={setup.SkillId} not in registry — skipping");
                    continue;
                }

                int lv = Mathf.Clamp(setup.Level, 1, Mathf.Max(1, def.Meta != null ? def.Meta.MaxLevel : 1));
                CompiledSkill c = SkillCompiler.Compile(def, lv);

                if (setup.SlotKey != KeyCode.None)
                {
                    if (_slotSkills.ContainsKey(setup.SlotKey))
                    {
                        Debug.LogWarning($"[SkillObject] SlotKey '{setup.SlotKey}' 중복 — skill_id={setup.SkillId} 무시");
                        continue;
                    }
                    _slotSkills.Add(setup.SlotKey, c);
                }
                else
                {
                    HookTrigger(c);
                }
            }

            for (int i = 0; i < _onAttackSkills.Count; i++)
            {
                CompiledSkill captured = _onAttackSkills[i];
                OnAttack += () => TryFire(captured);
            }
            for (int i = 0; i < _onOreBreakSkills.Count; i++)
            {
                CompiledSkill captured = _onOreBreakSkills[i];
                OnOreBreak += () => TryFire(captured);
            }
        }

        private void HookTrigger(CompiledSkill c)
        {
            if (c.TriggerNode == null)
            {
                Debug.LogWarning($"[SkillObject] skill_id={c.SkillId} has no trigger node — won't fire");
                return;
            }

            switch (c.TriggerNode.NodeType)
            {
                case ESkillNodeType.OnAttackTrigger:
                    _onAttackSkills.Add(c);
                    break;

                case ESkillNodeType.OnOreBreakTrigger:
                    _onOreBreakSkills.Add(c);
                    break;

                case ESkillNodeType.OnTickTrigger:
                    StartCoroutine(TickLoop(c));
                    break;

                default:
                    Debug.LogWarning($"[SkillObject] unknown trigger '{c.TriggerNode.NodeType}' on skill {c.SkillId}");
                    break;
            }
        }

        private IEnumerator TickLoop(CompiledSkill c)
        {
            float interval = c.TriggerNode.GetFloat(ESkillParamKey.Cooldown, c.LevelData, 1f);
            while (true)
            {
                yield return new WaitForSeconds(Mathf.Max(0.05f, interval));
                if (this == null) yield break;
                Fire(c);
            }
        }

        private void TryFire(CompiledSkill c)
        {
            if (Stunned) return;

            SkillNodeData trig = c.TriggerNode;

            float chance = trig != null ? trig.GetFloat(ESkillParamKey.Chance, c.LevelData, 1f) : 1f;
            if (chance < 1f && UnityEngine.Random.value > chance)
            {
                return;
            }

            float cooldown = trig != null ? trig.GetFloat(ESkillParamKey.Cooldown, c.LevelData, 0f) : 0f;
            if (AttackSpeedMultiplier > 0.01f) cooldown /= AttackSpeedMultiplier;
            if (cooldown > 0f)
            {
                if (Time.time < c.NextReadyTime) return;
                c.NextReadyTime = Time.time + cooldown;
            }

            Fire(c);
        }

        private bool TryFireSlotInternal(CompiledSkill c)
        {
            if (c == null) return false;
            float before = c.NextReadyTime;
            TryFire(c);
            return c.NextReadyTime != before;
        }

        private void Fire(CompiledSkill c)
        {
            if (Stunned) return;

            ApplyBuffSelfNodes(c);

            _scratchCtx.Reset(this, c.Level);
            TargetingResolver.Resolve(c, _scratchCtx);

            if (_scratchCtx.Targets.Count == 0 &&
                c.LaunchNode != null &&
                c.LaunchNode.NodeType == ESkillNodeType.InstantLaunch)
            {
                return;
            }

            LaunchExecutor.Execute(c, _scratchCtx);
        }

        private void ApplyBuffSelfNodes(CompiledSkill c)
        {
            if (c.BuffSelfNodes == null || c.BuffSelfNodes.Count == 0) return;
            for (int i = 0; i < c.BuffSelfNodes.Count; i++)
            {
                int buffId = c.BuffSelfNodes[i].GetInt(ESkillParamKey.BuffId, null, 0);
                if (buffId <= 0) continue;
                SkillBuffData data = SkillBuffRegistry.GetBuff(buffId);
                if (data == null)
                {
                    Debug.LogWarning($"[SkillObject] BuffId={buffId} not in SkillBuff registry");
                    continue;
                }
                ActiveStatusEffect.ApplyBuff(gameObject, data, this);
            }
        }
    }
}
