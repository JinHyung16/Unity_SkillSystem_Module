using System;
using System.Collections.Generic;
using UnityEngine;
using Jinhyeong_Common;

namespace Jinhyeong_SkillSystem
{
    /// <summary>피격 가능한 엔티티 컴포넌트. HP/Shield/스턴/속도배율 등 상태이상 영향 필드를 보유하고 전역 인스턴스 목록을 유지해 타게팅이 FindObjects 없이 조회 가능.</summary>
    public class Damageable : BaseBehaviour
    {
        public ESkillTeam Team = ESkillTeam.Enemy;
        public float Hp = 100f;
        public float MaxHp = 100f;
        public bool DestroyOnDeath = true;

        public float IncomingDamageMultiplier = 1f;
        public float SpeedMultiplier = 1f;
        public float Shield = 0f;
        public bool Stunned = false;

        public event Action<Damageable, SkillObject> OnDied;
        public event Action<Damageable> OnHealthChanged;

        private static readonly List<Damageable> _all = new List<Damageable>(64);
        public static IReadOnlyList<Damageable> All { get { return _all; } }

        public bool IsAlive { get { return Hp > 0f; } }

        protected override void OnEnabled()
        {
            if (_all.Contains(this) == false)
            {
                _all.Add(this);
            }
        }

        protected override void OnDisabled()
        {
            _all.Remove(this);
        }

        public static List<Damageable> GetAllOfTeam(ESkillTeam team, List<Damageable> buffer = null)
        {
            List<Damageable> list = buffer != null ? buffer : new List<Damageable>(_all.Count);
            list.Clear();
            for (int i = 0; i < _all.Count; i++)
            {
                Damageable d = _all[i];
                if (d == null)
                    continue;
                if (d.IsAlive == false)
                    continue;
                if (d.Team != team)
                    continue;
                list.Add(d);
            }
            return list;
        }

        public bool TakeDamage(float damage, SkillObject source)
        {
            if (IsAlive == false)
                return false;

            float incoming = damage * IncomingDamageMultiplier;
            if (Shield > 0f)
            {
                float absorbed = Mathf.Min(Shield, incoming);
                Shield -= absorbed;
                incoming -= absorbed;
            }
            Hp -= incoming;

            if (OnHealthChanged != null)
                OnHealthChanged.Invoke(this);

            bool died = IsAlive == false;

            if (died)
            {
                if (OnDied != null)
                    OnDied.Invoke(this, source);
                if (DestroyOnDeath)
                    Destroy(gameObject);
            }
            return died;
        }

        public void NotifyHealthChanged()
        {
            if (OnHealthChanged != null)
                OnHealthChanged.Invoke(this);
        }
    }
}
