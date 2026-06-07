using System;
using UnityEngine;
using Jinhyeong_GeneratedEnums;
using Jinhyeong_Managers;

namespace Jinhyeong_SkillSystem
{
    /// <summary>캐릭터 GO의 진입점 컴포넌트. 입력/AI에서 호출하는 스킬 발동 트리거와 디스폰 통지를 이벤트로 노출해 SkillManager/CharacterManager가 구독.</summary>
    [DisallowMultipleComponent]
    public class Playable : MonoBehaviour
    {
        public string PoolKey = PoolManager.KeyEmpty;

        public event Action<Playable, ESkillTriggerType> OnSkillFire;

        public void RaiseSkillFire(ESkillTriggerType type)
        {
            OnSkillFire?.Invoke(this, type);
        }

        public event Action<Playable> OnDespawning;

        public void NotifyDespawning()
        {
            OnDespawning?.Invoke(this);
        }

        public void Clear()
        {
            OnSkillFire = null;
            OnDespawning = null;
        }
    }
}
