using Cysharp.Threading.Tasks;
using UnityEngine;
using Jinhyeong_Character;
using Jinhyeong_SkillSystem;

namespace Jinhyeong_Managers
{

    public static class WorldSpawner
    {
        public const string KeyPlayer = "obj_player";
        public const string KeyEnemy  = "obj_enemy";

        private static readonly string[] PreloadKeys = { KeyPlayer, KeyEnemy };

        public static async UniTask<Player> SpawnPlayerWorldAsync(SkillLoadout loadout, Vector3 spawnPosition)
        {
            AddressableManager am = AddressableManager.Ensure();
            await am.LoadAllAsync(PreloadKeys);

            return SpawnPlayer(loadout, spawnPosition);
        }

        private static Player SpawnPlayer(SkillLoadout loadout, Vector3 spawnPosition)
        {
            AddressableManager am = AddressableManager.Instance;
            GameObject prefab = am != null ? am.Get(KeyPlayer) : null;
            if (prefab == null)
            {
                Debug.LogError("[WorldSpawner] obj_player 프리팹을 캐시에서 못 찾음 — 플레이어 스폰 실패");
                return null;
            }

            GameObject go = Object.Instantiate(prefab, spawnPosition, Quaternion.identity);
            Player player = go.GetComponent<Player>();
            if (player == null)
            {
                Debug.LogError("[WorldSpawner] obj_player 프리팹에 Player 컴포넌트가 없음");
                return null;
            }

            if (loadout != null && player.Skills != null)
            {
                player.Skills.Loadout = loadout;
                player.Skills.EquipAll();
            }
            return player;
        }
    }
}
