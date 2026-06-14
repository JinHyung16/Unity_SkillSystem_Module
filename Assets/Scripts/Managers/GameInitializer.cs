using UnityEngine;
using UnityEngine.SceneManagement;
using Jinhyeong_AI;
using Jinhyeong_SkillSystem;
using Jinhyeong_Common;

namespace Jinhyeong_Managers
{
    /// <summary>게임 전역 수명주기의 단일 소유자. 플레이 시작 시 매니저 싱글톤과 정적 상태를 Init하고,
    /// 앱 종료(OnApplicationQuit) / 재시작(Restart) 시 의존성 역순으로 전부 Clear한다.
    /// 매니저는 전부 DontDestroyOnLoad 싱글톤이라 씬에 GameObject로 둘 필요가 없다 — 이 클래스가 코드로 생성·보장.</summary>
    public class GameInitializer : BaseBehaviour
    {
        public static GameInitializer Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance != null)
                return;
            GameObject go = new GameObject("_GameInitializer");
            go.AddComponent<GameInitializer>(); // Awake에서 Init() 호출
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Init();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        /// <summary>매니저 싱글톤을 의존성 순서대로 보장한다(Addressable → Pool → Spawner). 멱등.</summary>
        public void Init()
        {
            AddressableManager.Ensure();
            PoolManager.Ensure();
            EnemySpawner.Ensure();
        }

        /// <summary>Init의 역순으로 전부 정리한다. 풀 인스턴스를 먼저 파괴한 뒤 Addressable 핸들을 해제하고, 마지막에 정적 데이터 레지스트리를 비운다.
        /// GameEvents 구독은 각 구독자(EnemySpawner는 영속, CameraFollow는 씬 스코프)가 자체 lifecycle에서 관리하므로 여기서 건드리지 않는다.</summary>
        public void ClearAll()
        {
            if (EnemySpawner.Instance != null)
                EnemySpawner.Instance.Clear();
            if (PoolManager.Instance != null)
                PoolManager.Instance.Clear();
            if (AddressableManager.Instance != null)
                AddressableManager.Instance.Clear();

            SkillRegistry.Clear();
            SkillBuffRegistry.Clear();
        }

        /// <summary>전체 재시작: 상태를 모두 Clear한 뒤 현재 씬을 리로드한다.
        /// DontDestroyOnLoad 매니저가 들고있던 적/VFX는 ClearAll이 파괴하고, 씬 스코프인 Player/Camera/StartScreen은 리로드가 재생성한다.</summary>
        public void Restart()
        {
            ClearAll();
            Scene scene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(scene.buildIndex);
        }

        private void OnApplicationQuit()
        {
            ClearAll();
        }
    }
}
