using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Jinhyeong_Managers
{
    /// <summary>Addressables 키별 prefab을 비동기로 로드/캐싱하는 싱글톤. PoolManager가 풀 미스 시 여기서 prefab을 꺼내 Instantiate.</summary>
    [DisallowMultipleComponent]
    public class AddressableManager : MonoBehaviour
    {
        public static AddressableManager Instance { get; private set; }

        private readonly Dictionary<string, AsyncOperationHandle<GameObject>> _handles =
            new Dictionary<string, AsyncOperationHandle<GameObject>>(32);

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Clear();
                Instance = null;
            }
        }

        public async UniTask<GameObject> LoadAsync(string key)
        {
            if (string.IsNullOrEmpty(key) || key == PoolManager.KeyEmpty)
            {
                return null;
            }
            if (_handles.TryGetValue(key, out AsyncOperationHandle<GameObject> existing))
            {
                if (existing.IsValid())
                {
                    if (existing.Status == AsyncOperationStatus.Succeeded) return existing.Result;
                    await existing.ToUniTask();
                    return existing.Status == AsyncOperationStatus.Succeeded ? existing.Result : null;
                }
            }

            AsyncOperationHandle<GameObject> handle = Addressables.LoadAssetAsync<GameObject>(key);
            _handles[key] = handle;
            await handle.ToUniTask();
            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogWarning($"[AddressableManager] '{key}' 로드 실패");
                return null;
            }
            return handle.Result;
        }

        public async UniTask LoadAllAsync(IEnumerable<string> keys)
        {
            if (keys == null) return;
            var tasks = new List<UniTask<GameObject>>();
            foreach (string key in keys)
            {
                if (string.IsNullOrEmpty(key) || key == PoolManager.KeyEmpty) continue;
                tasks.Add(LoadAsync(key));
            }
            if (tasks.Count == 0) return;
            await UniTask.WhenAll(tasks);
        }

        public GameObject Get(string key)
        {
            if (string.IsNullOrEmpty(key) || key == PoolManager.KeyEmpty) return null;
            if (_handles.TryGetValue(key, out AsyncOperationHandle<GameObject> h) && h.IsValid())
            {
                return h.Status == AsyncOperationStatus.Succeeded ? h.Result : null;
            }
            return null;
        }

        public bool IsLoaded(string key)
        {
            if (string.IsNullOrEmpty(key)) return false;
            return _handles.TryGetValue(key, out AsyncOperationHandle<GameObject> h)
                   && h.IsValid()
                   && h.Status == AsyncOperationStatus.Succeeded;
        }

        public void Release(string key)
        {
            if (string.IsNullOrEmpty(key)) return;
            if (_handles.TryGetValue(key, out AsyncOperationHandle<GameObject> h))
            {
                if (h.IsValid()) Addressables.Release(h);
                _handles.Remove(key);
            }
        }

        public void Clear()
        {
            foreach (AsyncOperationHandle<GameObject> h in _handles.Values)
            {
                if (h.IsValid()) Addressables.Release(h);
            }
            _handles.Clear();
        }
    }
}
