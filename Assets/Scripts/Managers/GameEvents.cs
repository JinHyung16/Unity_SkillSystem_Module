using System;
using Jinhyeong_Character;

namespace Jinhyeong_Managers
{
    /// <summary>현재 활성 Player와 스폰/디스폰 이벤트를 들고 있는 정적 이벤트 허브. EnemySpawner 등 시스템이 이를 구독해 플레이어 위치를 추적.</summary>
    public static class GameEvents
    {
        public static event Action<Player> OnPlayerSpawned;
        public static event Action<Player> OnPlayerDespawned;

        public static Player CurrentPlayer { get; private set; }

        public static void RaisePlayerSpawned(Player p)
        {
            CurrentPlayer = p;
            if (OnPlayerSpawned != null)
                OnPlayerSpawned.Invoke(p);
        }

        public static void RaisePlayerDespawned(Player p)
        {
            if (CurrentPlayer == p)
                CurrentPlayer = null;
            if (OnPlayerDespawned != null)
                OnPlayerDespawned.Invoke(p);
        }

        public static void ClearAll()
        {
            OnPlayerSpawned = null;
            OnPlayerDespawned = null;
            CurrentPlayer = null;
        }
    }
}
