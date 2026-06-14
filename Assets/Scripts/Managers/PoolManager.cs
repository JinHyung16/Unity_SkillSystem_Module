using System.Collections.Generic;
using UnityEngine;
using Jinhyeong_Common;

namespace Jinhyeong_Managers
{
    /// <summary>스킬 VFX의 Queue 풀을 관리하는 싱글톤. 풀 미스 시 AddressableManager 캐시에서 Instantiate. Addressables 미로드 시 Resources/Prefabs 경로로 자동 폴백.</summary>
    public partial class PoolManager : BaseBehaviour
    {
        public const string KeyEmpty = "Empty";

        // 키 → 시도해볼 Resources 경로(prefix 없음, Resources/ 기준).
        // 일치하는 항목이 없으면 키 자체를 마지막으로 시도한 뒤 한 번만 경고 후 캐시.
        private static readonly string[] ResourcesPathPrefixes =
        {
            "Prefabs/Skills/",
            "Prefabs/",
            "",
        };

        private static readonly Dictionary<string, GameObject> _resourcesCache =
            new Dictionary<string, GameObject>(16);

        private static readonly HashSet<string> _missingWarned = new HashSet<string>();

        public static PoolManager Instance { get; private set; }

        [Tooltip("풀에서 꺼낸 인스턴스의 부모. null이면 풀 미스 시 root에 스폰됨.")]
        public Transform PoolRoot;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            if (PoolRoot == null)
                PoolRoot = transform;
        }

        /// <summary>씬에 없을 때 코드로 싱글톤을 생성·보장. 반드시 메인 스레드에서 호출.</summary>
        public static PoolManager Ensure()
        {
            if (Instance != null)
                return Instance;
            GameObject go = new GameObject("_PoolManager");
            return go.AddComponent<PoolManager>();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Clear();
                Instance = null;
            }
        }

        public void Clear()
        {
            Pool_Skill_Clear();
        }

        private GameObject InstantiateFromAddressable(string key)
        {
            GameObject prefab = ResolvePrefab(key);
            if (prefab == null)
                return null;
            return Instantiate(prefab, PoolRoot);
        }

        private GameObject ResolvePrefab(string key)
        {
            // 1) Addressables 캐시
            AddressableManager am = AddressableManager.Instance;
            GameObject prefab = am != null ? am.Get(key) : null;
            if (prefab != null)
                return prefab;

            // 2) Resources 폴백 (메모리 캐시)
            if (_resourcesCache.TryGetValue(key, out prefab) && prefab != null)
                return prefab;

            for (int i = 0; i < ResourcesPathPrefixes.Length; i++)
            {
                prefab = Resources.Load<GameObject>(ResourcesPathPrefixes[i] + key);
                if (prefab != null)
                {
                    _resourcesCache[key] = prefab;
                    return prefab;
                }
            }

            // 3) 모두 실패 — 키마다 한 번만 경고
            if (_missingWarned.Add(key))
            {
                Debug.LogWarning($"[PoolManager] '{key}' prefab을 Addressables / Resources에서 찾을 수 없음 (이 키는 한 번만 경고)");
            }
            return null;
        }
    }
}
