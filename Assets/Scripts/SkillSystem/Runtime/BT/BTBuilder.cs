using System.Collections.Generic;
using UnityEngine;
using Jinhyeong_GeneratedEnums;

namespace Jinhyeong_SkillSystem.BT
{

    public static class BTBuilder
    {
        public static BTNode Build(List<SkillNodeData> nodes)
        {
            if (nodes == null || nodes.Count == 0)
                return null;

            Dictionary<int, BTNode> byId = new Dictionary<int, BTNode>(nodes.Count);
            for (int i = 0; i < nodes.Count; i++)
            {
                SkillNodeData n = nodes[i];
                if (byId.ContainsKey(n.NodeId))
                {
                    Debug.LogWarning($"[BTBuilder] skill {n.SkillId} 노드 id 중복 {n.NodeId} — 무시");
                    continue;
                }
                byId[n.NodeId] = Create(n);
            }

            List<BTNode> roots = new List<BTNode>(2);
            for (int i = 0; i < nodes.Count; i++)
            {
                SkillNodeData n = nodes[i];
                BTNode self = byId[n.NodeId];
                if (self == null)
                    continue;

                if (n.ParentId != 0 && byId.TryGetValue(n.ParentId, out BTNode parent) && parent != null)
                    parent.Children.Add(self);
                else
                    roots.Add(self);
            }

            if (roots.Count == 1)
                return roots[0];

            SequenceNode wrap = new SequenceNode(null);
            for (int i = 0; i < roots.Count; i++)
            {
                wrap.Children.Add(roots[i]);
            }
            return wrap;
        }

        private static BTNode Create(SkillNodeData n)
        {
            switch (n.NodeType)
            {
                case ESkillNodeType.Sequence: return new SequenceNode(n);
                case ESkillNodeType.Selector: return new SelectorNode(n);
                case ESkillNodeType.Parallel: return new ParallelNode(n);
                case ESkillNodeType.Inverter: return new InverterNode(n);

                case ESkillNodeType.Cooldown: return new CooldownDecoratorNode(n);
                case ESkillNodeType.Chance: return new ChanceDecoratorNode(n);

                case ESkillNodeType.TriggerOnTick:
                case ESkillNodeType.TriggerOnAttack:
                case ESkillNodeType.TriggerOnOreBreak:
                    return new TriggerNode(n);

                case ESkillNodeType.TargetSelf:
                case ESkillNodeType.TargetNearest:
                case ESkillNodeType.TargetFarthest:
                case ESkillNodeType.TargetRandom:
                case ESkillNodeType.TargetAll:
                case ESkillNodeType.TargetNearestForward:
                case ESkillNodeType.TargetCone:
                    return new TargetingLeafNode(n);

                case ESkillNodeType.HitSingle:
                case ESkillNodeType.HitArea:
                case ESkillNodeType.HitBeam:
                case ESkillNodeType.HitChain:
                case ESkillNodeType.HitDeathBurst:
                    return new HitLeafNode(n);

                case ESkillNodeType.DespawnAfterTime:
                case ESkillNodeType.DespawnAfterHits:
                case ESkillNodeType.DespawnAfterBounces:
                case ESkillNodeType.DespawnOnWall:
                    return new DespawnLeafNode(n);

                case ESkillNodeType.LaunchInstant:
                case ESkillNodeType.LaunchStraight:
                case ESkillNodeType.LaunchArc:
                case ESkillNodeType.LaunchCurve:
                    return new LaunchLeafNode(n);

                case ESkillNodeType.BuffSelf:
                    return new BuffSelfLeafNode(n);

                case ESkillNodeType.DebuffOnHit:
                    return new DebuffLeafNode(n);

                default:
                    Debug.LogWarning($"[BTBuilder] 미지원 node_type '{n.NodeType}' (skill {n.SkillId}) — 빈 노드로 통과 처리");
                    return new SequenceNode(n);
            }
        }
    }
}
