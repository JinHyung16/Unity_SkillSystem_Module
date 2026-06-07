using UnityEngine;
using Jinhyeong_GeneratedEnums;
using Jinhyeong_Managers;

namespace Jinhyeong_SkillSystem
{
    /// <summary>Launch 노드를 읽어 Instant/Linear/Arc/Curve 모션 형태로 SkillEffect GO를 스폰. Visual 키로 풀 조회, 미스 시 primitive 폴백.</summary>
    public static class LaunchExecutor
    {
        public static void Execute(CompiledSkill c, SkillContext ctx)
        {
            if (c == null || c.LaunchNode == null) return;

            switch (c.LaunchNode.NodeType)
            {
                case ESkillNodeType.InstantLaunch:
                    LaunchInstant(c, ctx);
                    return;

                case ESkillNodeType.StraightLaunch:
                    LaunchLinear(c, ctx);
                    return;

                case ESkillNodeType.ParabolicLaunch:
                    LaunchArc(c, ctx);
                    return;

                case ESkillNodeType.CurveLaunch:
                    LaunchCurve(c, ctx);
                    return;
            }
        }

        private static void LaunchInstant(CompiledSkill c, SkillContext ctx)
        {
            if (ctx.Targets.Count == 0)
            {
                return;
            }
            Vector3 spawnPos = ctx.Targets[0].transform.position;

            string visual = c.LaunchNode.GetString(ESkillParamKey.Visual);
            (GameObject go, string key) = SpawnVisual(visual, spawnPos, Quaternion.identity, fallbackTint: new Color(1f, 0.4f, 0.2f));

            SkillEffect fx = EnsureSkillEffect(go);
            fx.PoolKey = key;
            fx.InitInstant(c, ctx);
            fx.TryImmediateHit(ctx.Targets);
        }

        private static void LaunchLinear(CompiledSkill c, SkillContext ctx)
        {
            Vector3 origin = OriginPos(ctx);
            Vector3 dir = ResolveDirection(ctx);
            Quaternion rot = Quaternion.LookRotation(dir);

            string visual = c.LaunchNode.GetString(ESkillParamKey.Visual);
            float speed = c.LaunchNode.GetFloat(ESkillParamKey.Speed, c.LevelData, 5f);
            float maxDist = c.LaunchNode.GetFloat(ESkillParamKey.MaxDistance, c.LevelData, 10f);

            (GameObject go, string key) = SpawnVisual(visual, origin, rot, fallbackTint: new Color(0.3f, 0.7f, 1f));
            SkillEffect fx = EnsureSkillEffect(go);
            fx.PoolKey = key;
            fx.InitLinear(c, ctx, dir, speed, maxDist);
        }

        private static void LaunchArc(CompiledSkill c, SkillContext ctx)
        {
            Vector3 origin = OriginPos(ctx);
            Vector3 endPos = ctx.Targets.Count > 0 ? ctx.Targets[0].transform.position : origin + ResolveDirection(ctx) * 5f;

            string visual = c.LaunchNode.GetString(ESkillParamKey.Visual);
            float speed = c.LaunchNode.GetFloat(ESkillParamKey.Speed, c.LevelData, 5f);
            float arcHeight = c.LaunchNode.GetFloat(ESkillParamKey.ArcHeight, c.LevelData, 2f);

            Vector3 toEnd = endPos - origin;
            Quaternion rot = toEnd.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(toEnd.normalized) : Quaternion.identity;

            (GameObject go, string key) = SpawnVisual(visual, origin, rot, fallbackTint: new Color(1f, 0.7f, 0.2f));
            SkillEffect fx = EnsureSkillEffect(go);
            fx.PoolKey = key;
            fx.InitArc(c, ctx, origin, endPos, speed, arcHeight);
        }

        private static void LaunchCurve(CompiledSkill c, SkillContext ctx)
        {
            Vector3 origin = OriginPos(ctx);
            Vector3 endPos = ctx.Targets.Count > 0 ? ctx.Targets[0].transform.position : origin + ResolveDirection(ctx) * 5f;

            string visual = c.LaunchNode.GetString(ESkillParamKey.Visual);
            float speed = c.LaunchNode.GetFloat(ESkillParamKey.Speed, c.LevelData, 5f);
            float wobbleAmplitude = c.LaunchNode.GetFloat(ESkillParamKey.ArcHeight, c.LevelData, 1f);

            Vector3 toEnd = endPos - origin;
            Quaternion rot = toEnd.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(toEnd.normalized) : Quaternion.identity;

            (GameObject go, string key) = SpawnVisual(visual, origin, rot, fallbackTint: new Color(0.7f, 0.4f, 1f));
            SkillEffect fx = EnsureSkillEffect(go);
            fx.PoolKey = key;
            fx.InitCurve(c, ctx, origin, endPos, speed, wobbleAmplitude);
        }

        private static Vector3 OriginPos(SkillContext ctx)
        {
            return ctx.Caster != null ? ctx.Caster.transform.position : ctx.OriginPosition;
        }

        private static Vector3 ResolveDirection(SkillContext ctx)
        {
            if (ctx.Direction.sqrMagnitude > 0.0001f)
            {
                return ctx.Direction.normalized;
            }
            if (ctx.Targets.Count > 0 && ctx.Caster != null)
            {
                Vector3 d = ctx.Targets[0].transform.position - ctx.Caster.transform.position;
                if (d.sqrMagnitude > 0.0001f) return d.normalized;
            }
            return ctx.Caster != null ? ctx.Caster.transform.forward : Vector3.right;
        }

        private static SkillEffect EnsureSkillEffect(GameObject go)
        {
            SkillEffect fx = go.GetComponent<SkillEffect>();
            if (fx == null) fx = go.AddComponent<SkillEffect>();
            return fx;
        }

        private static (GameObject, string) SpawnVisual(string visualKey, Vector3 pos, Quaternion rot, Color fallbackTint)
        {
            string key = string.IsNullOrEmpty(visualKey) ? PoolManager.KeyEmpty : visualKey;

            if (key != PoolManager.KeyEmpty && PoolManager.Instance != null)
            {
                GameObject pooled = PoolManager.Instance.Pool_Skill_Get(key);
                if (pooled != null)
                {
                    pooled.transform.SetPositionAndRotation(pos, rot);
                    return (pooled, key);
                }
            }

            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = string.IsNullOrEmpty(visualKey) ? "SkillEffect" : visualKey;
            Collider col = go.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);
            MeshRenderer mr = go.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                Material m = new Material(Shader.Find("Sprites/Default"));
                m.color = new Color(fallbackTint.r, fallbackTint.g, fallbackTint.b, 0.6f);
                mr.sharedMaterial = m;
            }
            go.transform.position = pos;
            go.transform.rotation = rot;
            go.transform.localScale = Vector3.one * 0.5f;
            return (go, PoolManager.KeyEmpty);
        }
    }
}
