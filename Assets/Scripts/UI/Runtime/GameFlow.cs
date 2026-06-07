using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Jinhyeong_UI
{
    /// <summary>게임 시작 게이트. Play 진입 시 timeScale=0으로 모든 로직을 멈추고, Resources/Prefabs/StartScreen.prefab을 Instantiate. 시작 버튼이 눌리면 timeScale=1로 재개한다. Main.unity 수정 불필요.</summary>
    public static class GameFlow
    {
        public const string StartScreenResourcePath = "Prefabs/StartScreen";

        public static bool IsRunning { get; private set; }
        public static event Action OnGameStarted;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            IsRunning = false;
            OnGameStarted = null;
            Time.timeScale = 0f;

            EnsureEventSystem();

            GameObject prefab = Resources.Load<GameObject>(StartScreenResourcePath);
            if (prefab == null)
            {
                // Editor에서는 StartScreenPrefabAutoBuilder가 첫 로드 시 자동 생성하므로 보통 안 뜸.
                // 이 분기는 빌드/스크립트 컴파일 직전 도메인 리로드 등 prefab을 못 찾는 경우의 안전망.
                Debug.LogWarning($"[GameFlow] Resources/{StartScreenResourcePath}.prefab을 못 찾아 시작 화면을 건너뜀. 게임이 바로 진행됩니다.");
                Time.timeScale = 1f;
                IsRunning = true;
                return;
            }
            UnityEngine.Object.Instantiate(prefab);
        }

        public static void StartGame()
        {
            if (IsRunning) return;
            IsRunning = true;
            Time.timeScale = 1f;
            if (OnGameStarted != null) OnGameStarted.Invoke();
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
            GameObject go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
        }
    }
}
