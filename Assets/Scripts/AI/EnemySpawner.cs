using System.Collections.Generic;
using UnityEngine;
using Jinhyeong_Character;
using Jinhyeong_Common;
using Jinhyeong_Managers;

namespace Jinhyeong_AI
{
    /// <summary>Enemy 프리팹 풀을 관리하며 플레이어 등장 시 주변에 원형 배치로 초기 스폰한다. 사망 요청 시 비활성화 후 풀로 반환.</summary>
    [DisallowMultipleComponent]
    public class EnemySpawner : MonoBehaviour
    {
        [Header("Pool")]
        [SerializeField] private Enemy _prefab;
        [SerializeField] private string _resourcePath = "Prefabs/OBJ_Enemy";
        [SerializeField] private Transform _poolRoot;
        [SerializeField] private Transform _spawnAroundTarget;

        private readonly Queue<Enemy> _pool = new Queue<Enemy>(16);
        private readonly List<Enemy> _active = new List<Enemy>(16);

        private bool _initialSpawnDone;

        public IReadOnlyList<Enemy> Active { get { return _active; } }

        private void Awake()
        {
            if (_poolRoot == null) _poolRoot = transform;
        }

        private void OnEnable()
        {
            GameEvents.OnPlayerSpawned += HandlePlayerSpawned;
            GameEvents.OnPlayerDespawned += HandlePlayerDespawned;
            if (GameEvents.CurrentPlayer != null) HandlePlayerSpawned(GameEvents.CurrentPlayer);
        }

        private void OnDisable()
        {
            GameEvents.OnPlayerSpawned -= HandlePlayerSpawned;
            GameEvents.OnPlayerDespawned -= HandlePlayerDespawned;
        }

        private void Start()
        {
            Prewarm();
            TryInitialSpawn();
        }

        private void HandlePlayerSpawned(Player p)
        {
            if (p == null) return;
            if (_spawnAroundTarget == null) _spawnAroundTarget = p.transform;
            TryInitialSpawn();
        }

        private void HandlePlayerDespawned(Player p)
        {
            if (p != null && _spawnAroundTarget == p.transform)
            {
                _spawnAroundTarget = null;
            }
        }

        private void Prewarm()
        {
            for (int i = 0; i < CommonConfig.Spawner.PrewarmCount; i++)
            {
                Enemy e = CreateInstance();
                if (e == null) return;
                e.gameObject.SetActive(false);
                _pool.Enqueue(e);
            }
        }

        private void TryInitialSpawn()
        {
            if (_initialSpawnDone) return;
            if (_spawnAroundTarget == null) return;

            Vector3 center = _spawnAroundTarget.position;
            int count = CommonConfig.Spawner.InitialEnemyCount;
            for (int i = 0; i < count; i++)
            {
                float angle = (360f / Mathf.Max(1, count)) * i * Mathf.Deg2Rad;
                float radius = CommonConfig.Spawner.InitialSpawnRadius + Random.value * 2f;
                Vector3 pos = center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                Spawn(pos);
            }
            _initialSpawnDone = true;
        }

        public Enemy Spawn(Vector3 position)
        {
            Enemy e;
            if (_pool.Count > 0)
            {
                e = _pool.Dequeue();
                if (e == null) return Spawn(position);
                e.gameObject.SetActive(true);
            }
            else
            {
                e = CreateInstance();
                if (e == null) return null;
            }

            e.OnDespawnRequested -= HandleEnemyDespawnRequested;
            e.OnDespawnRequested += HandleEnemyDespawnRequested;

            e.Init(position);
            _active.Add(e);
            return e;
        }

        public void Despawn(Enemy enemy)
        {
            if (enemy == null) return;
            enemy.OnDespawnRequested -= HandleEnemyDespawnRequested;

            _active.Remove(enemy);
            enemy.gameObject.SetActive(false);
            enemy.transform.SetParent(_poolRoot, false);
            _pool.Enqueue(enemy);
        }

        private void HandleEnemyDespawnRequested(Enemy e)
        {
            Despawn(e);
        }

        private Enemy CreateInstance()
        {
            Enemy prefab = ResolvePrefab();
            if (prefab == null) return null;
            return Instantiate(prefab, _poolRoot, false);
        }

        private Enemy ResolvePrefab()
        {
            if (_prefab != null) return _prefab;
            if (string.IsNullOrEmpty(_resourcePath))
            {
                Debug.LogError("[EnemySpawner] prefab/_resourcePath 모두 비어 있음");
                return null;
            }
            GameObject go = Resources.Load<GameObject>(_resourcePath);
            if (go == null)
            {
                Debug.LogError($"[EnemySpawner] Resources/{_resourcePath} 못 찾음");
                return null;
            }
            _prefab = go.GetComponent<Enemy>();
            if (_prefab == null)
            {
                Debug.LogError($"[EnemySpawner] '{_resourcePath}' prefab에 Enemy 컴포넌트 없음");
            }
            return _prefab;
        }
    }
}
