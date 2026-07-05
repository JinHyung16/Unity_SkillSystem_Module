using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Jinhyeong_UI
{

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
