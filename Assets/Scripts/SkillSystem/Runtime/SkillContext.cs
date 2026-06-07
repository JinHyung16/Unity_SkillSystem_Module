using System.Collections.Generic;
using UnityEngine;

namespace Jinhyeong_SkillSystem
{
    /// <summary>스킬 캐스팅 1회의 임시 스크래치 데이터. Targeting 단계가 채우고 Launch/Hit 단계가 소비.</summary>
    public class SkillContext
    {
        public SkillObject Caster;
        public Vector3 OriginPosition;
        public Vector3 Direction = Vector3.right;
        public int Level = 1;

        public readonly List<Damageable> Targets = new List<Damageable>(8);

        public void Reset(SkillObject caster, int level)
        {
            Caster = caster;
            Level = level;
            OriginPosition = caster != null ? caster.transform.position : Vector3.zero;
            Direction = caster != null ? caster.transform.forward : Vector3.right;
            Targets.Clear();
        }
    }
}
