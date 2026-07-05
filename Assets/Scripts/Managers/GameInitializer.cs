using UnityEngine;
using UnityEngine.SceneManagement;
using Jinhyeong_AI;
using Jinhyeong_SkillSystem;
using Jinhyeong_Common;

namespace Jinhyeong_Managers
{

    public class GameInitializer : BaseBehaviour
    {
        public static GameInitializer Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance != null)
                return;
            GameObject go = new GameObject("_GameInitializer");
            go.AddComponent<GameInitializer>();
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

        public void Init()
        {
            AddressableManager.Ensure();
            PoolManager.Ensure();
            EnemySpawner.Ensure();
        }

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
