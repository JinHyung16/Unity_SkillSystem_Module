using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Jinhyeong_UI
{
    /// <summary>게임 시작 게이트. Play 진입 시 timeScale=0으로 모든 로직을 멈추고, 씬에 미리 박혀있는 StartScreen이 시작 버튼을 처리. 시작 시 timeScale=1로 재개한다.</summary>
    public static class GameFlow
    {
        public static bool IsRunning { get; private set; }
        public static event Action OnGameStarted;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            IsRunning = false;
            OnGameStarted = null;
            Time.timeScale = 0f;

            EnsureEventSystem();

            StartScreen existing = UnityEngine.Object.FindFirstObjectByType<StartScreen>(FindObjectsInactive.Include);
            if (existing == null)
            {
                Debug.LogWarning("[GameFlow] 씬에 StartScreen이 없어 시작 게이트를 건너뜀. 게임이 바로 진행됩니다.");
                Time.timeScale = 1f;
                IsRunning = true;
            }
        }

        public static void StartGame()
        {
            if (IsRunning)
                return;
            IsRunning = true;
            Time.timeScale = 1f;
            if (OnGameStarted != null)
                OnGameStarted.Invoke();
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null)
                return;
            GameObject go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
        }
    }
}
