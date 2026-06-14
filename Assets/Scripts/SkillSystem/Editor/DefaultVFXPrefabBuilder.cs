#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Jinhyeong_SkillSystem.EditorTools
{
    /// <summary>스킬 Visual 키(vfx_bolt/vfx_strike/vfx_explosion_small)에 대응하는 URP 파티클 프리팹을 생성하는 에디터 헬퍼.
    /// Assets/Prefabs/Skills의 기존 경로에 덮어써서 Addressables GUID를 유지한다(그룹 엔트리 무효화 방지).
    /// 런타임 머티리얼 생성 금지 규칙에 따라 머티리얼은 Assets/Materials에 에셋으로 캐싱.</summary>
    public static class DefaultVFXPrefabBuilder
    {
        private const string PrefabFolder   = "Assets/Prefabs/Skills";
        private const string MaterialFolder = "Assets/Materials";

        private static readonly Color BoltColor      = new Color(0.35f, 0.75f, 1f, 1f);
        private static readonly Color StrikeColor     = new Color(1f, 0.95f, 0.35f, 1f);
        private static readonly Color ExplosionColor  = new Color(1f, 0.5f, 0.2f, 1f);

        [MenuItem("Tools/Skills/Rebuild VFX Prefabs (URP Particles)")]
        public static void CreateAll()
        {
            EnsureFolder("Assets/Prefabs");
            EnsureFolder(PrefabFolder);
            EnsureFolder(MaterialFolder);

            BuildBolt();
            BuildStrike();
            BuildExplosion();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[DefaultVFXPrefabBuilder] URP 파티클 VFX 3종 재생성 완료 (Assets/Prefabs/Skills, GUID 유지)");
        }

        // 날아가는 투사체: 월드 공간 연속 방출로 이동 경로에 트레일을 남긴다.
        private static void BuildBolt()
        {
            GameObject go = new GameObject("vfx_bolt");
            ParticleSystem ps = go.AddComponent<ParticleSystem>();

            ParticleSystem.MainModule main = ps.main;
            main.duration = 1f;
            main.loop = true;
            main.startLifetime = 0.35f;
            main.startSpeed = 0f;
            main.startSize = 0.28f;
            main.startColor = BoltColor;
            main.gravityModifier = 0f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 256;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = 70f;

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.12f;

            ApplyFadeAndShrink(ps);
            ConfigureRenderer(go, GetOrCreateMaterial("SkillVfx_Bolt", BoltColor), ParticleSystemRenderMode.Billboard);

            SaveOver(go, "vfx_bolt");
        }

        // 즉발 단일 히트: 스폰 즉시 바깥으로 튀는 짧은 버스트.
        private static void BuildStrike()
        {
            GameObject go = new GameObject("vfx_strike");
            ParticleSystem ps = go.AddComponent<ParticleSystem>();

            ParticleSystem.MainModule main = ps.main;
            main.duration = 0.5f;
            main.loop = false;
            main.startLifetime = 0.3f;
            main.startSpeed = 4.5f;
            main.startSize = 0.18f;
            main.startColor = StrikeColor;
            main.gravityModifier = 0f;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.maxParticles = 64;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 24) });

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.2f;

            ApplyFadeAndShrink(ps);
            ConfigureRenderer(go, GetOrCreateMaterial("SkillVfx_Strike", StrikeColor), ParticleSystemRenderMode.Billboard);

            SaveOver(go, "vfx_strike");
        }

        // AoE/폭발: 넓은 방사형 버스트.
        private static void BuildExplosion()
        {
            GameObject go = new GameObject("vfx_explosion_small");
            ParticleSystem ps = go.AddComponent<ParticleSystem>();

            ParticleSystem.MainModule main = ps.main;
            main.duration = 0.6f;
            main.loop = false;
            main.startLifetime = 0.5f;
            main.startSpeed = 6f;
            main.startSize = 0.32f;
            main.startColor = ExplosionColor;
            main.gravityModifier = 0.1f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 128;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 40) });

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.3f;

            ApplyFadeAndShrink(ps);
            ConfigureRenderer(go, GetOrCreateMaterial("SkillVfx_Explosion", ExplosionColor), ParticleSystemRenderMode.Billboard);

            SaveOver(go, "vfx_explosion_small");
        }

        // 수명 동안 알파 페이드 + 크기 축소로 스파크/글로우 느낌.
        private static void ApplyFadeAndShrink(ParticleSystem ps)
        {
            ParticleSystem.ColorOverLifetimeModule col = ps.colorOverLifetime;
            col.enabled = true;
            Gradient g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            col.color = new ParticleSystem.MinMaxGradient(g);

            ParticleSystem.SizeOverLifetimeModule size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));
        }

        private static void ConfigureRenderer(GameObject go, Material mat, ParticleSystemRenderMode mode)
        {
            ParticleSystemRenderer r = go.GetComponent<ParticleSystemRenderer>();
            if (r == null)
                return;
            r.renderMode = mode;
            r.sharedMaterial = mat;
            r.alignment = ParticleSystemRenderSpace.View;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
        }

        private static Material GetOrCreateMaterial(string name, Color color)
        {
            string path = $"{MaterialFolder}/{name}.mat";
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);

            Shader sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (sh == null)
                sh = Shader.Find("Universal Render Pipeline/Unlit");
            if (sh == null)
                sh = Shader.Find("Sprites/Default");

            Material mat = existing != null ? existing : new Material(sh);
            mat.shader = sh;

            // URP/내장 양쪽에서 통하도록 색 프로퍼티를 방어적으로 모두 세팅.
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", color);
            if (mat.HasProperty("_TintColor"))
                mat.SetColor("_TintColor", color);
            mat.color = color;

            // 가산 블렌딩 + ZWrite off (URP Particles/Unlit 기준, 버전 차이는 무해하게 무시됨).
            if (mat.HasProperty("_Surface"))
                mat.SetFloat("_Surface", 1f); // Transparent
            if (mat.HasProperty("_Blend"))
                mat.SetFloat("_Blend", 2f);     // Additive
            if (mat.HasProperty("_ZWrite"))
                mat.SetFloat("_ZWrite", 0f);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

            if (existing == null)
                AssetDatabase.CreateAsset(mat, path);
            else
                EditorUtility.SetDirty(mat);
            return mat;
        }

        private static void SaveOver(GameObject go, string name)
        {
            string path = $"{PrefabFolder}/{name}.prefab";
            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            string leaf = Path.GetFileName(path);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
#endif
