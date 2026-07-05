using System;
using System.Collections.Generic;
using UnityEngine;

namespace Jinhyeong_SkillSystem
{

    [CreateAssetMenu(menuName = "Jinhyeong/Skill/Skill Loadout", fileName = "SkillLoadout")]
    public class SkillLoadout : ScriptableObject
    {
        public List<EquippedSkillEntry> Entries = new List<EquippedSkillEntry>();
    }

    [Serializable]
    public class EquippedSkillEntry
    {
        public int SkillId = 0;
        [Min(1)] public int Level = 1;
        public KeyCode SlotKey = KeyCode.None;
    }
}
