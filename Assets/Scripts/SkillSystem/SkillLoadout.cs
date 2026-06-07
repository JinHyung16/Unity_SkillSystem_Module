using System;
using System.Collections.Generic;
using UnityEngine;

namespace Jinhyeong_SkillSystem
{
    /// <summary>직업/캐릭터 단위로 장착 스킬을 묶은 ScriptableObject. SkillObject가 이 SO를 참조해 EquipAll에 사용.</summary>
    [CreateAssetMenu(menuName = "Jinhyeong/Skill/Skill Loadout", fileName = "SkillLoadout")]
    public class SkillLoadout : ScriptableObject
    {
        public List<EquippedSkillEntry> Entries = new List<EquippedSkillEntry>();
    }

    /// <summary>로드아웃 한 줄. SkillId + Level + (선택)SlotKey.</summary>
    [Serializable]
    public class EquippedSkillEntry
    {
        public int SkillId = 0;
        [Min(1)] public int Level = 1;
        public KeyCode SlotKey = KeyCode.None;
    }
}
