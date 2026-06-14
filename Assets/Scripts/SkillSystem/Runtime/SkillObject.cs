using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Jinhyeong_GeneratedEnums;
using Jinhyeong_Common;

namespace Jinhyeong_SkillSystem
{
    /// <summary>장착된 스킬 목록을 컴파일해 슬롯키/Trigger(OnTick·OnAttack)별로 hook하고, 발동 시 쿨다운/확률 체크 후 BuffSelf 적용·LaunchExecutor 실행·SubSkill 연계를 수행. 발동 시점은 SkillData.Trigger가 정의한다(노드 아님).</summary>
    public class SkillObject : BaseBehaviour
    {
        // 연계(SpawnSubSkill) 무한 루프 방지용 최대 깊이.
        private const int MaxComboDepth = 4;

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

        private readonly Dictionary<KeyCode, CompiledSkill> _slotSkills = new Dictionary<KeyCode, CompiledSkill>(8);
        private readonly List<CompiledSkill> _onAttackSkills = new List<CompiledSkill>(4);
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

            // StartScreen 경로 없이 곧장 게임플레이로 진입하는 경우(적 스폰 등)를 위한 안전망 워밍.
            // 이미 워밍된 키는 AddressableManager가 dedupe하므로 반복 호출 비용은 무시 가능.
            try
            {
                await SkillRegistry.PreloadVisualsAsync();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SkillObject] VFX 사전로드 실패(폴백으로 진행): {e.Message}");
            }

            EquipAll();
        }

        public bool TryFireSlot(KeyCode key)
        {
            if (key == KeyCode.None)
                return false;
            if (_slotSkills.TryGetValue(key, out CompiledSkill c) == false)
                return false;
            return TryFireSlotInternal(c);
        }

        /// <summary>기본공격이 실행되는 순간 CharacterAttack이 호출. OnAttack 트리거 스킬을 일괄 발동 시도.</summary>
        public void NotifyAttack()
        {
            if (_onAttackSkills.Count == 0)
                return;
            for (int i = 0; i < _onAttackSkills.Count; i++)
            {
                TryFire(_onAttackSkills[i]);
            }
        }

        public void EquipAll()
        {
            _slotSkills.Clear();
            _onAttackSkills.Clear();

            if (Loadout == null || Loadout.Entries == null)
                return;

            for (int i = 0; i < Loadout.Entries.Count; i++)
            {
                EquippedSkillEntry setup = Loadout.Entries[i];
                if (setup == null)
                    continue;

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
        }

        private void HookTrigger(CompiledSkill c)
        {
            switch (c.Trigger)
            {
                case ESkillTriggerType.OnTick:
                    StartCoroutine(TickLoop(c));
                    break;

                case ESkillTriggerType.OnAttack:
                    _onAttackSkills.Add(c);
                    break;

                default:
                    // SlotKey도 없고 자동 발동 경로도 없는 스킬(OnOreBreak는 채굴 시스템이 없어 미구현).
                    Debug.LogWarning($"[SkillObject] skill_id={c.SkillId}: SlotKey 없이 Trigger '{c.Trigger}'는 발동되지 않음");
                    break;
            }
        }

        private IEnumerator TickLoop(CompiledSkill c)
        {
            while (true)
            {
                float interval = ResolveCooldown(c, 1f);
                yield return new WaitForSeconds(Mathf.Max(0.05f, interval));
                if (this == null)
                    yield break;
                Fire(c);
            }
        }

        private void TryFire(CompiledSkill c)
        {
            if (Stunned)
                return;

            float chance = ResolveChance(c);
            if (chance < 1f && UnityEngine.Random.value > chance)
            {
                return;
            }

            float cooldown = ResolveCooldown(c, 0f);
            if (AttackSpeedMultiplier > 0.01f)
                cooldown /= AttackSpeedMultiplier;
            if (cooldown > 0f)
            {
                if (Time.time < c.NextReadyTime)
                    return;
                c.NextReadyTime = Time.time + cooldown;
            }

            Fire(c);
        }

        private bool TryFireSlotInternal(CompiledSkill c)
        {
            if (c == null)
                return false;
            float before = c.NextReadyTime;
            TryFire(c);
            return c.NextReadyTime != before;
        }

        // 발동 시점 파라미터(Chance/Cooldown)는 트리거 노드가 사라졌으므로 레벨 테이블에서 직접 읽는다.
        // DB의 Chance는 퍼센트(예: 10 = 10%)로 입력되므로 0~1 확률로 정규화한다. 미지정이면 100% 발동.
        private static float ResolveChance(CompiledSkill c)
        {
            if (c.LevelData != null && c.LevelData.TryGet(ESkillParamKey.Chance, out float v))
            {
                return Mathf.Clamp01(v / 100f);
            }
            return 1f;
        }

        private static float ResolveCooldown(CompiledSkill c, float fallback)
        {
            if (c.LevelData != null && c.LevelData.TryGet(ESkillParamKey.Cooldown, out float v))
            {
                return v;
            }
            return fallback;
        }

        private void Fire(CompiledSkill c)
        {
            _scratchCtx.Reset(this, c.Level, 0);
            FireWith(c, _scratchCtx);
        }

        private void FireWith(CompiledSkill c, SkillContext ctx)
        {
            if (Stunned)
                return;

            ApplyBuffSelfNodes(c);

            TargetingResolver.Resolve(c, ctx);

            if (ctx.Targets.Count == 0 &&
                c.LaunchNode != null &&
                c.LaunchNode.NodeType == ESkillNodeType.InstantLaunch)
            {
                return;
            }

            LaunchExecutor.Execute(c, ctx);
            FireSubSkills(c, ctx);
        }

        private void ApplyBuffSelfNodes(CompiledSkill c)
        {
            if (c.BuffSelfNodes == null || c.BuffSelfNodes.Count == 0)
                return;
            for (int i = 0; i < c.BuffSelfNodes.Count; i++)
            {
                int buffId = c.BuffSelfNodes[i].GetInt(ESkillParamKey.BuffId, null, 0);
                if (buffId <= 0)
                    continue;
                SkillBuffData data = SkillBuffRegistry.GetBuff(buffId);
                if (data == null)
                {
                    Debug.LogWarning($"[SkillObject] BuffId={buffId} not in SkillBuff registry");
                    continue;
                }
                ActiveStatusEffect.ApplyBuff(gameObject, data, this);
            }
        }

        // 연계: 이 스킬이 발동된 뒤 SubSkill 노드가 가리키는 skill_id를 Delay초 후 발동. ctx.Depth로 무한 연계 차단.
        private void FireSubSkills(CompiledSkill c, SkillContext ctx)
        {
            if (c.SubSkillNodes == null || c.SubSkillNodes.Count == 0)
                return;
            if (ctx.Depth >= MaxComboDepth)
            {
                Debug.LogWarning($"[SkillObject] 연계 깊이 한계({MaxComboDepth}) 도달 — skill_id={c.SkillId}의 SubSkill 중단");
                return;
            }

            for (int i = 0; i < c.SubSkillNodes.Count; i++)
            {
                SkillNodeData n = c.SubSkillNodes[i];
                int subId = n.GetInt(ESkillParamKey.SkillId, null, 0);
                if (subId <= 0)
                    continue;
                float delay = n.GetFloat(ESkillParamKey.Delay, null, 0f);
                StartCoroutine(SubSkillRoutine(subId, c.Level, ctx.Depth + 1, delay));
            }
        }

        private IEnumerator SubSkillRoutine(int skillId, int parentLevel, int depth, float delay)
        {
            if (delay > 0f)
                yield return new WaitForSeconds(delay);
            if (this == null)
                yield break;

            SkillDefinition def = SkillRegistry.Get(skillId);
            if (def == null)
            {
                Debug.LogWarning($"[SkillObject] SubSkill skill_id={skillId} not in registry");
                yield break;
            }

            int lv = Mathf.Clamp(parentLevel, 1, Mathf.Max(1, def.Meta != null ? def.Meta.MaxLevel : 1));
            CompiledSkill sub = SkillCompiler.Compile(def, lv);

            SkillContext subCtx = new SkillContext();
            subCtx.Reset(this, lv, depth);
            FireWith(sub, subCtx);
        }
    }
}
