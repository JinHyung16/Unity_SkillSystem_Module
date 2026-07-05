#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace Jinhyeong_SkillSystem.EditorTools
{

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

            Build("vfx_chain_lightning", new Color(0.6f, 0.85f, 1f, 1f), false, 0.35f, 5f, 0.35f, 0f, 40, 0.3f, 0f, false);
            Build("vfx_meteor", new Color(1f, 0.45f, 0.15f, 1f), false, 0.5f, 3f, 0.75f, 0f, 50, 0.6f, 0.2f, true);
            Build("vfx_lance", new Color(0.5f, 0.9f, 1f, 1f), true, 0.3f, 0f, 0.45f, 110f, 0, 0.2f, 0f, true);
            Build("vfx_cleave", new Color(1f, 0.9f, 0.4f, 1f), false, 0.35f, 7f, 0.5f, 0f, 55, 0.5f, 0f, false);
            Build("vfx_bomb", new Color(1f, 0.4f, 0.15f, 1f), false, 0.55f, 6.5f, 0.8f, 0f, 60, 0.6f, 0.15f, true);
            Build("vfx_orb", new Color(0.7f, 0.45f, 1f, 1f), true, 0.4f, 0f, 0.55f, 80f, 0, 0.25f, 0f, false);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            RegisterAddressables();

            Debug.Log("[DefaultVFXPrefabBuilder] URP 파티클 VFX 9종 재생성 + 어드레서블 등록 완료 (Assets/Prefabs/Skills, GUID 유지)");
        }

        private static void BuildBolt()
        {
            GameObject go = new GameObject("vfx_bolt");
            ParticleSystem ps = go.AddComponent<ParticleSystem>();

            ParticleSystem.MainModule main = ps.main;
            main.duration = 1f;
            main.loop = true;
            main.startLifetime = 0.35f;
            main.startSpeed = 0f;
            main.startSize = 0.6f;
            main.startColor = BoltColor;
            main.gravityModifier = 0f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 256;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = 70f;

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.25f;

            ApplyFadeAndShrink(ps);
            ConfigureRenderer(go, GetOrCreateMaterial("SkillVfx_Bolt", BoltColor), ParticleSystemRenderMode.Billboard);

            SaveOver(go, "vfx_bolt");
        }

        private static void BuildStrike()
        {
            GameObject go = new GameObject("vfx_strike");
            ParticleSystem ps = go.AddComponent<ParticleSystem>();

            ParticleSystem.MainModule main = ps.main;
            main.duration = 0.5f;
            main.loop = false;
            main.startLifetime = 0.3f;
            main.startSpeed = 4.5f;
            main.startSize = 0.42f;
            main.startColor = StrikeColor;
            main.gravityModifier = 0f;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.maxParticles = 64;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 32) });

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.4f;

            ApplyFadeAndShrink(ps);
            ConfigureRenderer(go, GetOrCreateMaterial("SkillVfx_Strike", StrikeColor), ParticleSystemRenderMode.Billboard);

            SaveOver(go, "vfx_strike");
        }

        private static void BuildExplosion()
        {
            GameObject go = new GameObject("vfx_explosion_small");
            ParticleSystem ps = go.AddComponent<ParticleSystem>();

            ParticleSystem.MainModule main = ps.main;
            main.duration = 0.6f;
            main.loop = false;
            main.startLifetime = 0.5f;
            main.startSpeed = 6f;
            main.startSize = 0.7f;
            main.startColor = ExplosionColor;
            main.gravityModifier = 0.1f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 128;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 50) });

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.6f;

            ApplyFadeAndShrink(ps);
            ConfigureRenderer(go, GetOrCreateMaterial("SkillVfx_Explosion", ExplosionColor), ParticleSystemRenderMode.Billboard);

            SaveOver(go, "vfx_explosion_small");
        }

        private static void Build(string name, Color color, bool loop, float lifetime, float startSpeed,
            float startSize, float rateOverTime, int burst, float shapeRadius, float gravity, bool worldSpace)
        {
            GameObject go = new GameObject(name);
            ParticleSystem ps = go.AddComponent<ParticleSystem>();

            ParticleSystem.MainModule main = ps.main;
            main.duration = loop ? 1f : Mathf.Max(0.4f, lifetime + 0.1f);
            main.loop = loop;
            main.startLifetime = lifetime;
            main.startSpeed = startSpeed;
            main.startSize = startSize;
            main.startColor = color;
            main.gravityModifier = gravity;
            main.simulationSpace = worldSpace ? ParticleSystemSimulationSpace.World : ParticleSystemSimulationSpace.Local;
            main.maxParticles = 256;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = rateOverTime;
            if (burst > 0)
                emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)burst) });

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = shapeRadius;

            ApplyFadeAndShrink(ps);
            ConfigureRenderer(go, GetOrCreateMaterial("SkillVfx_" + name, color), ParticleSystemRenderMode.Billboard);

            SaveOver(go, name);
        }

        private static void RegisterAddressables()
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogWarning("[DefaultVFXPrefabBuilder] Addressable 설정을 찾을 수 없어 등록을 건너뜀");
                return;
            }

            AddressableAssetGroup group = settings.FindGroup("Skill");
            if (group == null)
                group = settings.DefaultGroup;

            for (int i = 0; i < AllKeys.Length; i++)
            {
                string key = AllKeys[i];
                string path = $"{PrefabFolder}/{key}.prefab";
                string guid = AssetDatabase.AssetPathToGUID(path);
                if (string.IsNullOrEmpty(guid))
                {
                    Debug.LogWarning($"[DefaultVFXPrefabBuilder] '{path}' GUID 조회 실패 — 등록 건너뜀");
                    continue;
                }
                AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, group);
                entry.address = key;
            }

            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryModified, null, true);
            AssetDatabase.SaveAssets();
        }

        private static readonly string[] AllKeys =
        {
            "vfx_bolt", "vfx_strike", "vfx_explosion_small",
            "vfx_chain_lightning", "vfx_meteor", "vfx_lance",
            "vfx_cleave", "vfx_bomb", "vfx_orb",
        };

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

            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", color);
            if (mat.HasProperty("_TintColor"))
                mat.SetColor("_TintColor", color);
            mat.color = color;

            if (mat.HasProperty("_Surface"))
                mat.SetFloat("_Surface", 1f);
            if (mat.HasProperty("_Blend"))
                mat.SetFloat("_Blend", 2f);
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
