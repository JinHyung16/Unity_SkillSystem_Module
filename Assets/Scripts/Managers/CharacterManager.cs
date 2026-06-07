using System;
using System.Collections.Generic;
using UnityEngine;
using Jinhyeong_SkillSystem;

namespace Jinhyeong_Managers
{
    /// <summary>PoolManager에서 캐릭터 GO를 꺼내고 Playable 스폰/디스폰 이벤트를 브로드캐스트하는 싱글톤. SkillManager가 이 이벤트로 OnSkillFire를 hook in한다.</summary>
    [DisallowMultipleComponent]
    public class CharacterManager : MonoBehaviour
    {
        public static CharacterManager Instance { get; private set; }

        public event Action<Playable> OnCharacterSpawned;
        public event Action<Playable> OnCharacterDespawned;

        private readonly List<Playable> _spawned = new List<Playable>(16);

        public IReadOnlyList<Playable> Spawned { get { return _spawned; } }

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

        public Playable Spawn(string key = PoolManager.KeyEmpty, Vector3 position = default, Quaternion rotation = default)
        {
            if (PoolManager.Instance == null)
            {
                Debug.LogWarning("[CharacterManager] PoolManager.Instance == null");
                return null;
            }

            GameObject go = PoolManager.Instance.Pool_Character_Get(key);
            if (go == null)
            {
                Debug.LogWarning($"[CharacterManager] '{key}' 캐릭터 스폰 실패");
                return null;
            }

            go.transform.SetPositionAndRotation(position, rotation == default ? Quaternion.identity : rotation);

            Playable p = go.GetComponent<Playable>();
            if (p == null)
            {
                Debug.LogWarning($"[CharacterManager] '{key}' prefab에 Playable 컴포넌트 없음");
                PoolManager.Instance.Pool_Character_Return(key, go);
                return null;
            }
            p.PoolKey = key;

            _spawned.Add(p);
            OnCharacterSpawned?.Invoke(p);
            return p;
        }

        public void Despawn(Playable p)
        {
            if (p == null) return;

            p.NotifyDespawning();
            OnCharacterDespawned?.Invoke(p);

            _spawned.Remove(p);

            if (PoolManager.Instance != null)
            {
                PoolManager.Instance.Pool_Character_Return(p.PoolKey, p.gameObject);
            }
            else if (p.gameObject != null)
            {
                Destroy(p.gameObject);
            }
        }

        public void Clear()
        {
            for (int i = _spawned.Count - 1; i >= 0; i--)
            {
                Playable p = _spawned[i];
                if (p == null) continue;
                p.NotifyDespawning();
                OnCharacterDespawned?.Invoke(p);
                if (PoolManager.Instance != null)
                {
                    PoolManager.Instance.Pool_Character_Return(p.PoolKey, p.gameObject);
                }
            }
            _spawned.Clear();
            OnCharacterSpawned = null;
            OnCharacterDespawned = null;
        }
    }
}
