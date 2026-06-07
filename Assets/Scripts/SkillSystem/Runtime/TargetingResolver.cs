using System.Collections.Generic;
using UnityEngine;
using Jinhyeong_GeneratedEnums;

namespace Jinhyeong_SkillSystem
{
    /// <summary>CompiledSkill의 Targeting 노드 종류에 따라 적군을 탐색해 ctx.Targets/Direction을 채우는 정적 리졸버. Self/AreaNear/Far/Random/ScreenAll/NearestDir/Ray를 지원.</summary>
    public static class TargetingResolver
    {
        private static readonly List<Damageable> _scratch = new List<Damageable>(64);
        private static readonly List<Damageable> _candidates = new List<Damageable>(64);

        public static void Resolve(CompiledSkill c, SkillContext ctx)
        {
            ctx.Targets.Clear();
            if (c == null || c.TargetingNode == null)
            {
                return;
            }

            SkillNodeData node = c.TargetingNode;
            ESkillTeam enemyTeam = SkillTeamUtil.Opposite(ctx.Caster != null ? ctx.Caster.Team : ESkillTeam.Friend);
            float range = node.GetFloat(ESkillParamKey.Range, c.LevelData, 5f);
            float rangeSq = range * range;

            switch (node.NodeType)
            {
                case ESkillNodeType.SelfTargeting:
                {
                    Damageable self = ctx.Caster != null ? ctx.Caster.GetComponent<Damageable>() : null;
                    if (self != null)
                    {
                        ctx.Targets.Add(self);
                    }
                    return;
                }

                case ESkillNodeType.AreaNearTargeting:
                {
                    Damageable best = FindNearest(ctx.OriginPosition, enemyTeam, rangeSq, out _);
                    if (best != null)
                    {
                        ctx.Targets.Add(best);
                    }
                    return;
                }

                case ESkillNodeType.AreaFarTargeting:
                {
                    Damageable best = FindFarthest(ctx.OriginPosition, enemyTeam, rangeSq);
                    if (best != null)
                    {
                        ctx.Targets.Add(best);
                    }
                    return;
                }

                case ESkillNodeType.AreaRandomTargeting:
                {
                    int max = node.GetInt(ESkillParamKey.MaxPerTarget, null, 1);
                    GatherInRange(ctx.OriginPosition, enemyTeam, rangeSq, _candidates);
                    for (int i = 0; i < max && _candidates.Count > 0; i++)
                    {
                        int idx = Random.Range(0, _candidates.Count);
                        ctx.Targets.Add(_candidates[idx]);
                        _candidates.RemoveAt(idx);
                    }
                    return;
                }

                case ESkillNodeType.ScreenAllTargeting:
                {
                    int max = node.GetInt(ESkillParamKey.MaxPerTarget, null, int.MaxValue);
                    Damageable.GetAllOfTeam(enemyTeam, _candidates);
                    for (int i = 0; i < _candidates.Count && ctx.Targets.Count < max; i++)
                    {
                        ctx.Targets.Add(_candidates[i]);
                    }
                    return;
                }

                case ESkillNodeType.NearestDirectionTargeting:
                {
                    Damageable best = FindNearest(ctx.OriginPosition, enemyTeam, rangeSq, out _);
                    if (best != null)
                    {
                        Vector3 d = best.transform.position - ctx.OriginPosition;
                        if (d.sqrMagnitude > 0.0001f)
                        {
                            ctx.Direction = d.normalized;
                        }
                        ctx.Targets.Add(best);
                    }
                    return;
                }

                case ESkillNodeType.RayTargeting:
                {
                    float maxDist = node.GetFloat(ESkillParamKey.MaxDistance, c.LevelData, range);
                    float maxDistSq = maxDist * maxDist;
                    int max = node.GetInt(ESkillParamKey.MaxPerTarget, null, int.MaxValue);
                    Damageable.GetAllOfTeam(enemyTeam, _candidates);
                    for (int i = 0; i < _candidates.Count && ctx.Targets.Count < max; i++)
                    {
                        Damageable d = _candidates[i];
                        Vector3 to = d.transform.position - ctx.OriginPosition;
                        if (to.sqrMagnitude > maxDistSq) continue;
                        if (Vector3.Dot(to.normalized, ctx.Direction) < 0.7071f) continue;
                        ctx.Targets.Add(d);
                    }
                    return;
                }
            }
        }

        private static Damageable FindNearest(Vector3 origin, ESkillTeam team, float maxSq, out float bestSq)
        {
            Damageable best = null;
            bestSq = float.MaxValue;
            Damageable.GetAllOfTeam(team, _scratch);
            for (int i = 0; i < _scratch.Count; i++)
            {
                Damageable d = _scratch[i];
                float sq = (d.transform.position - origin).sqrMagnitude;
                if (sq <= maxSq && sq < bestSq)
                {
                    bestSq = sq;
                    best = d;
                }
            }
            return best;
        }

        private static Damageable FindFarthest(Vector3 origin, ESkillTeam team, float maxSq)
        {
            Damageable best = null;
            float bestSq = -1f;
            Damageable.GetAllOfTeam(team, _scratch);
            for (int i = 0; i < _scratch.Count; i++)
            {
                Damageable d = _scratch[i];
                float sq = (d.transform.position - origin).sqrMagnitude;
                if (sq <= maxSq && sq > bestSq)
                {
                    bestSq = sq;
                    best = d;
                }
            }
            return best;
        }

        private static void GatherInRange(Vector3 origin, ESkillTeam team, float maxSq, List<Damageable> outList)
        {
            outList.Clear();
            Damageable.GetAllOfTeam(team, _scratch);
            for (int i = 0; i < _scratch.Count; i++)
            {
                Damageable d = _scratch[i];
                if ((d.transform.position - origin).sqrMagnitude <= maxSq)
                {
                    outList.Add(d);
                }
            }
        }
    }
}
