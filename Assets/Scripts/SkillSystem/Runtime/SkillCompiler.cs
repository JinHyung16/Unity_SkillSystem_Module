using Jinhyeong_GeneratedEnums;
namespace Jinhyeong_SkillSystem
{
    /// <summary>SkillDefinition의 플랫 노드를 순회해 ESkillNodeType 카테고리별 슬롯에 배치하는 컴파일러. 단일 슬롯(타게팅/히트/디스폰/런치)은 마지막 노드로 덮어쓰고, 반복 가능한 버프/디버프/서브스킬은 리스트로 누적. 발동 시점은 노드가 아닌 SkillData.Trigger가 책임진다.</summary>
    public static class SkillCompiler
    {
        public static CompiledSkill Compile(SkillDefinition def, int level)
        {
            CompiledSkill c = new CompiledSkill
            {
                Source = def,
                Level = level,
                LevelData = def != null ? def.GetLevel(level) : null,
            };

            if (def == null)
                return c;

            for (int i = 0; i < def.Nodes.Count; i++)
            {
                AssignByRole(c, def.Nodes[i]);
            }
            return c;
        }

        private static void AssignByRole(CompiledSkill c, SkillNodeData n)
        {
            switch (n.NodeType)
            {
                case ESkillNodeType.SelfTargeting:
                case ESkillNodeType.AreaNearTargeting:
                case ESkillNodeType.AreaFarTargeting:
                case ESkillNodeType.AreaRandomTargeting:
                case ESkillNodeType.ScreenAllTargeting:
                case ESkillNodeType.NearestDirectionTargeting:
                case ESkillNodeType.RayTargeting:
                    c.TargetingNode = n;
                    return;

                case ESkillNodeType.SingleHit:
                case ESkillNodeType.AoEHit:
                case ESkillNodeType.BeamHit:
                case ESkillNodeType.ChainLightningHit:
                case ESkillNodeType.DeathChainHit:
                    c.HitNode = n;
                    return;

                case ESkillNodeType.DurationDespawn:
                case ESkillNodeType.OnHitDespawn:
                    c.DespawnNode = n;
                    return;

                case ESkillNodeType.InstantLaunch:
                case ESkillNodeType.StraightLaunch:
                case ESkillNodeType.ParabolicLaunch:
                case ESkillNodeType.CurveLaunch:
                    c.LaunchNode = n;
                    return;

                case ESkillNodeType.ApplyBuffSelf:
                    c.BuffSelfNodes.Add(n);
                    return;

                case ESkillNodeType.ApplyDebuffOnHit:
                    c.DebuffHitNodes.Add(n);
                    return;

                case ESkillNodeType.SpawnSubSkill:
                    c.SubSkillNodes.Add(n);
                    return;
            }
        }
    }
}