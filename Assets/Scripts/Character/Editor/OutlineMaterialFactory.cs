using System.IO;
using UnityEditor;
using UnityEngine;

namespace Jinhyeong_Character.Editor
{
    /// <summary>Outline_Lit 셰이더 기반 머티리얼을 Assets/Materials 폴더에 캐싱 생성/갱신하는 에디터 팩토리.</summary>
    public static class OutlineMaterialFactory
    {
        public const string ShaderName = "Jinhyeong/Outline_Lit";
        public const string MaterialsFolder = "Assets/Materials";
        public const string SmoothNormalKeyword = "_USE_SMOOTH_NORMAL";
        public const float DefaultOutlineWidthPx = 1.5f;

        public static Material GetOrCreate(
            string materialName,
            Color baseColor,
            Color outlineColor,
            float outlineWidthPx = DefaultOutlineWidthPx,
            bool useSmoothNormal = false)
        {
            EnsureFolder(MaterialsFolder);
            string assetPath = $"{MaterialsFolder}/{materialName}.mat";

            Shader shader = Shader.Find(ShaderName);
            if (shader == null)
            {
                Debug.LogError($"[OutlineMaterialFactory] '{ShaderName}' 셰이더를 찾을 수 없음");
                return null;
            }

            Material existing = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (existing != null)
            {
                if (existing.shader != shader)
                {
                    existing.shader = shader;
                }
                ApplyProperties(existing, baseColor, outlineColor, outlineWidthPx, useSmoothNormal);
                EditorUtility.SetDirty(existing);
                return existing;
            }

            Material mat = new Material(shader) { name = materialName };
            ApplyProperties(mat, baseColor, outlineColor, outlineWidthPx, useSmoothNormal);
            AssetDatabase.CreateAsset(mat, assetPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[OutlineMaterialFactory] 머티리얼 생성 → {assetPath}");
            return mat;
        }

        private static void ApplyProperties(
            Material mat,
            Color baseColor,
            Color outlineColor,
            float outlineWidthPx,
            bool useSmoothNormal)
        {
            mat.SetColor("_BaseColor", baseColor);
            mat.SetColor("_OutlineColor", outlineColor);
            mat.SetFloat("_OutlineWidth", outlineWidthPx);
            mat.SetFloat("_Smoothness", 0.4f);
            mat.SetFloat("_Metallic", 0f);
            mat.SetFloat("_OutlineFadeStart", 30f);
            mat.SetFloat("_OutlineFadeEnd", 60f);

            SetKeyword(mat, SmoothNormalKeyword, useSmoothNormal);
            mat.SetFloat("_UseSmoothNormal", useSmoothNormal ? 1f : 0f);
        }

        private static void SetKeyword(Material mat, string keyword, bool enabled)
        {
            if (enabled)
                mat.EnableKeyword(keyword);
            else
                mat.DisableKeyword(keyword);
        }

        private static void EnsureFolder(string assetPath)
        {
            if (AssetDatabase.IsValidFolder(assetPath))
                return;
            string parent = Path.GetDirectoryName(assetPath).Replace('\\', '/');
            string leaf = Path.GetFileName(assetPath);
            if (AssetDatabase.IsValidFolder(parent) == false)
            {
                EnsureFolder(parent);
            }
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
