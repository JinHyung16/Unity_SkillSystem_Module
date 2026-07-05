using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Jinhyeong_Character.Editor
{

    public static class SmoothNormalBaker
    {
        private const string MenuPath = "Jinhyeong/Tools/Bake Smooth Normals to Vertex Color";
        private const float DefaultEpsilon = 0.0001f;
        private const string SuffixSmoothNormal = "_SmoothNormal";

        [MenuItem(MenuPath, true)]
        private static bool ValidateBake()
        {
            return Selection.activeObject is Mesh
                || (Selection.activeObject is GameObject go && (go.GetComponent<MeshFilter>() != null || go.GetComponent<SkinnedMeshRenderer>() != null));
        }

        [MenuItem(MenuPath)]
        public static void BakeSelection()
        {
            if (Selection.activeObject is Mesh meshDirect)
            {
                BakeAndSaveAsset(meshDirect);
                return;
            }

            if (Selection.activeObject is GameObject root)
            {
                int count = 0;
                MeshFilter[] mfs = root.GetComponentsInChildren<MeshFilter>(true);
                for (int i = 0; i < mfs.Length; i++)
                {
                    if (mfs[i].sharedMesh != null)
                    {
                        BakeAndSaveAsset(mfs[i].sharedMesh);
                        count++;
                    }
                }
                SkinnedMeshRenderer[] smrs = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                for (int i = 0; i < smrs.Length; i++)
                {
                    if (smrs[i].sharedMesh != null)
                    {
                        BakeAndSaveAsset(smrs[i].sharedMesh);
                        count++;
                    }
                }

                Debug.Log($"[SmoothNormalBaker] '{root.name}' 하위 메시 {count}개 처리");
                return;
            }

            Debug.LogWarning("[SmoothNormalBaker] 메시 또는 메시를 가진 GameObject를 선택해주세요.");
        }

        public static Mesh BakeMesh(Mesh source, float epsilon = DefaultEpsilon)
        {
            if (source == null)
                return null;

            Vector3[] vertices = source.vertices;
            Vector3[] normals = source.normals;
            if (vertices == null || vertices.Length == 0)
            {
                Debug.LogWarning($"[SmoothNormalBaker] '{source.name}' vertex 없음");
                return null;
            }
            if (normals == null || normals.Length != vertices.Length)
            {
                Debug.LogWarning($"[SmoothNormalBaker] '{source.name}' normal이 없거나 vertex와 길이 불일치 — 자동 계산 시도");
                source.RecalculateNormals();
                normals = source.normals;
            }

            Dictionary<Vector3Int, List<int>> buckets = new Dictionary<Vector3Int, List<int>>(vertices.Length);
            float invEps = 1f / epsilon;
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 p = vertices[i];
                Vector3Int key = new Vector3Int(
                    Mathf.RoundToInt(p.x * invEps),
                    Mathf.RoundToInt(p.y * invEps),
                    Mathf.RoundToInt(p.z * invEps));
                if (buckets.TryGetValue(key, out List<int> list) == false)
                {
                    list = new List<int>(4);
                    buckets[key] = list;
                }
                list.Add(i);
            }

            Color[] colors = new Color[vertices.Length];

            foreach (KeyValuePair<Vector3Int, List<int>> kv in buckets)
            {
                List<int> indices = kv.Value;

                Vector3 sum = Vector3.zero;
                for (int j = 0; j < indices.Count; j++)
                {
                    sum += normals[indices[j]];
                }
                Vector3 averaged = sum.sqrMagnitude > 0.0001f ? sum.normalized : Vector3.up;

                Color packed = new Color(
                    averaged.x * 0.5f + 0.5f,
                    averaged.y * 0.5f + 0.5f,
                    averaged.z * 0.5f + 0.5f,
                    1f);

                for (int j = 0; j < indices.Count; j++)
                {
                    colors[indices[j]] = packed;
                }
            }

            Mesh copy = Object.Instantiate(source);
            copy.name = source.name + SuffixSmoothNormal;
            copy.colors = colors;
            return copy;
        }

        private static void BakeAndSaveAsset(Mesh source)
        {
            if (source == null)
                return;
            Mesh baked = BakeMesh(source);
            if (baked == null)
                return;

            string sourcePath = AssetDatabase.GetAssetPath(source);
            string folder;
            if (string.IsNullOrEmpty(sourcePath))
            {
                folder = "Assets";
            }
            else
            {
                folder = Path.GetDirectoryName(sourcePath).Replace('\\', '/');
                if (string.IsNullOrEmpty(folder))
                    folder = "Assets";
            }

            string targetPath = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{baked.name}.asset");
            AssetDatabase.CreateAsset(baked, targetPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[SmoothNormalBaker] 베이크 완료 → {targetPath}  ({source.vertexCount} verts)");
            EditorGUIUtility.PingObject(baked);
        }
    }
}
