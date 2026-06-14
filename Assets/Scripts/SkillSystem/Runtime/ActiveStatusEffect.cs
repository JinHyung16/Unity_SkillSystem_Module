using UnityEngine;
using Jinhyeong_GeneratedEnums;
using Jinhyeong_Common;

namespace Jinhyeong_SkillSystem
{
    /// <summary>Buff/Debuff를 대상 GO에 컴포넌트로 부착해 지속시간, 주기 틱, enter/exit 스탯 변경을 자체 관리하는 런타임 상태이상 인스턴스.</summary>
    public class ActiveStatusEffect : BaseBehaviour
    {
        public enum Kind { Buff, Debuff }

        public Kind EffectKind;
        public int EffectId;
        public string EffectName;
        public float Duration;
        public float TickInterval;
        public float Value0;
        public float Value1;

        public EBuffType BuffKind;
        public EDebuffType DebuffKind;

        public SkillObject SourceCaster;

        private float _startTime;
        private float _nextTickTime;
        private bool _entered;

        private Damageable _targetDamageable;
        private SkillObject _targetCaster;

        public static ActiveStatusEffect ApplyBuff(GameObject target, SkillBuffData data, SkillObject source)
        {
            if (target == null || data == null)
                return null;
            ActiveStatusEffect eff = target.AddComponent<ActiveStatusEffect>();
            eff.EffectKind = Kind.Buff;
            eff.EffectId = data.Id;
            eff.EffectName = data.Name;
            eff.Duration = data.Duration;
            eff.TickInterval = data.TickInterval;
            eff.Value0 = data.Value0;
            eff.Value1 = data.Value1;
            eff.BuffKind = data.Type;
            eff.SourceCaster = source;
            return eff;
        }

        public static ActiveStatusEffect ApplyDebuff(GameObject target, SkillDebuffData data, SkillObject source)
        {
            if (target == null || data == null)
                return null;
            ActiveStatusEffect eff = target.AddComponent<ActiveStatusEffect>();
            eff.EffectKind = Kind.Debuff;
            eff.EffectId = data.Id;
            eff.EffectName = data.Name;
            eff.Duration = data.Duration;
            eff.TickInterval = data.TickInterval;
            eff.Value0 = data.Value0;
            eff.Value1 = data.Value1;
            eff.DebuffKind = data.Type;
            eff.SourceCaster = source;
            return eff;
        }

        private void Start()
        {
            _targetDamageable = GetComponent<Damageable>();
            _targetCaster = GetComponent<SkillObject>();

            _startTime = Time.time;
            _nextTickTime = TickInterval > 0f ? Time.time + TickInterval : float.MaxValue;

            ApplyOnEnter();
            _entered = true;
        }

        private void Update()
        {
            if (Time.time - _startTime >= Duration)
            {
                Expire();
                return;
            }
            if (Time.time >= _nextTickTime)
            {
                ApplyTick();
                _nextTickTime += TickInterval;
            }
        }

        private void OnDestroy()
        {
            if (_entered)
            {
                ApplyOnExit();
                _entered = false;
            }
        }

        private void Expire()
        {
            Destroy(this);
        }

        private void ApplyOnEnter()
        {
            if (EffectKind == Kind.Buff)
            {
                ApplyBuffEnter();
            }
            else
            {
                ApplyDebuffEnter();
            }
        }

        private void ApplyOnExit()
        {
            if (EffectKind == Kind.Buff)
            {
                ApplyBuffExit();
            }
            else
            {
                ApplyDebuffExit();
            }
        }

        private void ApplyTick()
        {
            switch (EffectKind)
            {
                case Kind.Buff:
                    if (BuffKind == EBuffType.HealOverTime && _targetDamageable != null)
                    {
                        _targetDamageable.Hp += Value0;
                    }
                    break;

                case Kind.Debuff:
                    if (DebuffKind == EDebuffType.DamageOverTime && _targetDamageable != null)
                    {
                        _targetDamageable.TakeDamage(Value0, SourceCaster);
                    }
                    break;
            }
        }

        private void ApplyBuffEnter()
        {
            switch (BuffKind)
            {
                case EBuffType.DamageBoost:
                    if (_targetCaster != null)
                        _targetCaster.OutgoingDamageMultiplier += Value0;
                    break;
                case EBuffType.AttackSpeedBoost:
                    if (_targetCaster != null)
                        _targetCaster.AttackSpeedMultiplier += Value0;
                    break;
                case EBuffType.Shield:
                    if (_targetDamageable != null)
                        _targetDamageable.Shield += Value0;
                    break;
                case EBuffType.HealOverTime:
                    break;
            }
        }

        private void ApplyBuffExit()
        {
            switch (BuffKind)
            {
                case EBuffType.DamageBoost:
                    if (_targetCaster != null)
                        _targetCaster.OutgoingDamageMultiplier -= Value0;
                    break;
                case EBuffType.AttackSpeedBoost:
                    if (_targetCaster != null)
                        _targetCaster.AttackSpeedMultiplier -= Value0;
                    break;
                case EBuffType.Shield:
                    break;
                case EBuffType.HealOverTime:
                    break;
            }
        }

        private void ApplyDebuffEnter()
        {
            switch (DebuffKind)
            {
                case EDebuffType.SlowReduce:
                    if (_targetDamageable != null)
                        _targetDamageable.SpeedMultiplier *= (1f - Value0);
                    break;
                case EDebuffType.DamageReceiveIncrease:
                    if (_targetDamageable != null)
                        _targetDamageable.IncomingDamageMultiplier += Value0;
                    break;
                case EDebuffType.Stun:
                    if (_targetDamageable != null)
                        _targetDamageable.Stunned = true;
                    if (_targetCaster != null)
                        _targetCaster.Stunned = true;
                    break;
                case EDebuffType.DamageOverTime:
                    break;
            }
        }

        private void ApplyDebuffExit()
        {
            switch (DebuffKind)
            {
                case EDebuffType.SlowReduce:
                    if (_targetDamageable != null && Mathf.Approximately(Value0, 1f) == false)
                    {
                        _targetDamageable.SpeedMultiplier /= (1f - Value0);
                    }
                    break;
                case EDebuffType.DamageReceiveIncrease:
                    if (_targetDamageable != null)
                        _targetDamageable.IncomingDamageMultiplier -= Value0;
                    break;
                case EDebuffType.Stun:
                    if (_targetDamageable != null)
                        _targetDamageable.Stunned = false;
                    if (_targetCaster != null)
                        _targetCaster.Stunned = false;
                    break;
                case EDebuffType.DamageOverTime:
                    break;
            }
        }
    }
}
