using UnityEngine;
using Jinhyeong_GeneratedEnums;
using Jinhyeong_Managers;
using Jinhyeong_Common;

namespace Jinhyeong_SkillSystem
{

    public static class LaunchExecutor
    {

        public static bool Execute(SkillNodeData launchNode, SkillContext ctx)
        {
            if (launchNode == null)
                return false;

            switch (launchNode.NodeType)
            {
                case ESkillNodeType.LaunchInstant:
                    return LaunchInstant(launchNode, ctx);

                case ESkillNodeType.LaunchStraight:
                    LaunchLinear(launchNode, ctx);
                    return true;

                case ESkillNodeType.LaunchArc:
                    LaunchArc(launchNode, ctx);
                    return true;

                case ESkillNodeType.LaunchCurve:
                    LaunchCurve(launchNode, ctx);
                    return true;
            }
            return false;
        }

        private static bool LaunchInstant(SkillNodeData launchNode, SkillContext ctx)
        {
            if (ctx.Targets.Count == 0)
                return false;
            Vector3 spawnPos = TargetPos(ctx.Targets[0]);

            string visual = launchNode.GetString(ESkillParamKey.Visual);
            (GameObject go, string key) = SpawnVisual(visual, spawnPos, Quaternion.identity, fallbackTint: new Color(1f, 0.4f, 0.2f));

            SkillEffect fx = EnsureSkillEffect(go);
            fx.PoolKey = key;
            fx.InitInstant(ctx);
            fx.TryImmediateHit(ctx.Targets);
            return true;
        }

        private static void LaunchLinear(SkillNodeData launchNode, SkillContext ctx)
        {
            Vector3 origin = OriginPos(ctx);
            Vector3 dir = ResolveDirection(ctx);
            Quaternion rot = Quaternion.LookRotation(dir);

            string visual = launchNode.GetString(ESkillParamKey.Visual);
            float speed = launchNode.GetFloat(ESkillParamKey.Speed, ctx.LevelData, 5f);
            float maxDist = launchNode.GetFloat(ESkillParamKey.MaxDistance, ctx.LevelData, 10f);

            (GameObject go, string key) = SpawnVisual(visual, origin, rot, fallbackTint: new Color(0.3f, 0.7f, 1f));
            SkillEffect fx = EnsureSkillEffect(go);
            fx.PoolKey = key;
            fx.InitLinear(ctx, dir, speed, maxDist);
        }

        private static void LaunchArc(SkillNodeData launchNode, SkillContext ctx)
        {
            Vector3 origin = OriginPos(ctx);
            Vector3 endPos = ctx.Targets.Count > 0 ? TargetPos(ctx.Targets[0]) : origin + ResolveDirection(ctx) * 5f;

            string visual = launchNode.GetString(ESkillParamKey.Visual);
            float speed = launchNode.GetFloat(ESkillParamKey.Speed, ctx.LevelData, 5f);
            float arcHeight = launchNode.GetFloat(ESkillParamKey.ArcHeight, ctx.LevelData, 2f);

            Vector3 toEnd = endPos - origin;
            Quaternion rot = toEnd.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(toEnd.normalized) : Quaternion.identity;

            (GameObject go, string key) = SpawnVisual(visual, origin, rot, fallbackTint: new Color(1f, 0.7f, 0.2f));
            SkillEffect fx = EnsureSkillEffect(go);
            fx.PoolKey = key;
            fx.InitArc(ctx, origin, endPos, speed, arcHeight);
        }

        private static void LaunchCurve(SkillNodeData launchNode, SkillContext ctx)
        {
            Vector3 origin = OriginPos(ctx);
            Vector3 endPos = ctx.Targets.Count > 0 ? TargetPos(ctx.Targets[0]) : origin + ResolveDirection(ctx) * 5f;

            string visual = launchNode.GetString(ESkillParamKey.Visual);
            float speed = launchNode.GetFloat(ESkillParamKey.Speed, ctx.LevelData, 5f);
            float wobbleAmplitude = launchNode.GetFloat(ESkillParamKey.ArcHeight, ctx.LevelData, 1f);

            Vector3 toEnd = endPos - origin;
            Quaternion rot = toEnd.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(toEnd.normalized) : Quaternion.identity;

            (GameObject go, string key) = SpawnVisual(visual, origin, rot, fallbackTint: new Color(0.7f, 0.4f, 1f));
            SkillEffect fx = EnsureSkillEffect(go);
            fx.PoolKey = key;
            fx.InitCurve(ctx, origin, endPos, speed, wobbleAmplitude);
        }

        private static Vector3 OriginPos(SkillContext ctx)
        {
            if (ctx.Caster != null)
                return ctx.Caster.MuzzlePosition;
            return ctx.OriginPosition + Vector3.up * CommonConfig.Skill.MuzzleHeight;
        }

        private static Vector3 TargetPos(Damageable d)
        {
            return d.transform.position + Vector3.up * CommonConfig.Skill.MuzzleHeight;
        }

        private static Vector3 ResolveDirection(SkillContext ctx)
        {
            if (ctx.Direction.sqrMagnitude > 0.0001f)
                return ctx.Direction.normalized;
            if (ctx.Targets.Count > 0 && ctx.Caster != null)
            {
                Vector3 d = ctx.Targets[0].transform.position - ctx.Caster.transform.position;
                if (d.sqrMagnitude > 0.0001f)
                    return d.normalized;
            }
            return ctx.Caster != null ? ctx.Caster.transform.forward : Vector3.right;
        }

        private static SkillEffect EnsureSkillEffect(GameObject go)
        {
            SkillEffect fx = go.GetComponent<SkillEffect>();
            if (fx == null)
                fx = go.AddComponent<SkillEffect>();
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
            if (col != null)
                Object.Destroy(col);
            MeshRenderer mr = go.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                Material m = new Material(Shader.Find("Sprites/Default"));
                m.color = new Color(fallbackTint.r, fallbackTint.g, fallbackTint.b, 0.6f);
                mr.sharedMaterial = m;
            }
            go.transform.position = pos;
            go.transform.rotation = rot;
            go.transform.localScale = Vector3.one * 1.2f;
            return (go, PoolManager.KeyEmpty);
        }
    }
}
