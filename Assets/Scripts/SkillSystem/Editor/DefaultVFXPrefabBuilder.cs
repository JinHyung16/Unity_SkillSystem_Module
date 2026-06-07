#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Jinhyeong_SkillSystem.EditorTools
{
    /// <summary>샘플 스킬의 Visual 키(vfx_explosion_small/vfx_strike/vfx_bolt)에 대응하는 임시 프리미티브 프리팹을 Resources/Prefabs에 일괄 생성하는 에디터 헬퍼.</summary>
    public static class DefaultVFXPrefabBuilder
    {
        private const string ResourcesFolder = "Assets/Resources";
        private const string PrefabFolder    = "Assets/Resources/Prefabs";

        [MenuItem("Tools/Skills/Create Default VFX Prefabs")]
        public static void CreateAll()
        {
            EnsureFolder(ResourcesFolder);
            EnsureFolder(PrefabFolder);

            CreateOne("vfx_explosion_small", PrimitiveType.Sphere,  new Color(1f, 0.4f, 0.2f, 0.7f), Vector3.one * 1f);
            CreateOne("vfx_strike",          PrimitiveType.Sphere,  new Color(1f, 1f,   0.3f, 0.7f), Vector3.one * 1f);
            CreateOne("vfx_bolt",            PrimitiveType.Capsule, new Color(0.3f, 0.7f, 1f, 0.8f), new Vector3(0.25f, 0.25f, 0.6f));

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[DefaultVFXPrefabBuilder] 3 placeholder VFX prefabs ready under Assets/Resources/Prefabs/");
        }

        private static void CreateOne(string name, PrimitiveType type, Color color, Vector3 scale)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;

            Collider col = go.GetComponent<Collider>();
            if (col != null)
            {
                Object.DestroyImmediate(col);
            }

            MeshRenderer mr = go.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                Material mat = new Material(Shader.Find("Sprites/Default"));
                mat.color = color;
                mr.sharedMaterial = mat;
            }

            go.transform.localScale = scale;

            string path = Path.Combine(PrefabFolder, name + ".prefab").Replace('\\', '/');
            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
        }

        private static void EnsureFolder(string path)
        {
            if (Directory.Exists(path)) return;
            Directory.CreateDirectory(path);
            AssetDatabase.Refresh();
        }
    }
}
#endif
