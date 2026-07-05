using System.Collections.Generic;
using UnityEngine;

namespace Jinhyeong_SkillSystem
{

    public class SkillContext
    {
        public SkillObject Caster;
        public Vector3 OriginPosition;
        public Vector3 Direction = Vector3.right;
        public int Level = 1;

        public SkillLevelData LevelData;

        public int Depth = 0;

        public bool AttackPending;

        public bool ManualCast;

        public readonly List<Damageable> Targets = new List<Damageable>(8);

        public SkillNodeData HitNode;
        public SkillNodeData DespawnNode;
        public readonly List<SkillNodeData> DebuffNodes = new List<SkillNodeData>(2);

        public void Reset(SkillObject caster, int level, SkillLevelData levelData, int depth = 0)
        {
            Caster = caster;
            Level = level;
            LevelData = levelData;
            Depth = depth;
            OriginPosition = caster != null ? caster.transform.position : Vector3.zero;
            Direction = caster != null ? caster.transform.forward : Vector3.right;
            Targets.Clear();
            HitNode = null;
            DespawnNode = null;
            DebuffNodes.Clear();
            AttackPending = false;
            ManualCast = false;
        }
    }
}
