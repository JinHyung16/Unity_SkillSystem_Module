using System;
using Jinhyeong_Character;

namespace Jinhyeong_Managers
{

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
