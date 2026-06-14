using Cysharp.Threading.Tasks;
using UnityEngine;
using Jinhyeong_Character;
using Jinhyeong_SkillSystem;

namespace Jinhyeong_Managers
{
    /// <summary>GAME START 시점에 obj_player/obj_camera/obj_enemy를 Addressables로 사전로드한 뒤 플레이어→카메라 순으로 스폰하는 정적 부트스트랩.
    /// 적은 EnemySpawner가 GameEvents.OnPlayerSpawned를 받아 자동 스폰하므로 여기선 키 사전로드만 책임진다. 반드시 메인 스레드에서 호출.</summary>
    public static class WorldSpawner
    {
        public const string KeyPlayer = "obj_player";
        public const string KeyCamera = "obj_camera";
        public const string KeyEnemy  = "obj_enemy";

        private static readonly string[] PreloadKeys = { KeyPlayer, KeyCamera, KeyEnemy };

        /// <summary>플레이어와 카메라를 스폰하고 로드아웃을 장착한다. 적 스폰은 EnemySpawner가 플레이어 스폰 이벤트를 받아 처리.</summary>
        public static async UniTask<Player> SpawnPlayerWorldAsync(SkillLoadout loadout, Vector3 spawnPosition)
        {
            AddressableManager am = AddressableManager.Ensure();
            await am.LoadAllAsync(PreloadKeys);

            Player player = SpawnPlayer(loadout, spawnPosition);
            if (player == null)
                return null;

            SpawnCamera(player);
            return player;
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

            // 플레이어는 1회 스폰 후 디스폰되지 않으므로 풀링 없이 직접 Instantiate.
            // 위치를 Instantiate 인자로 넘기면 Awake/OnEnable(RaisePlayerSpawned) 시점에 이미 올바른 좌표라
            // 적이 플레이어 위치 기준으로 정확히 배치된다.
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

        private static void SpawnCamera(Player player)
        {
            AddressableManager am = AddressableManager.Instance;
            GameObject prefab = am != null ? am.Get(KeyCamera) : null;
            if (prefab == null)
            {
                Debug.LogWarning("[WorldSpawner] obj_camera 프리팹을 캐시에서 못 찾음 — 카메라 스폰 건너뜀");
                return;
            }

            // CameraRoot가 바인딩돼 있으면 그 월드 위치, 아니면 플레이어 위치 기준으로 배치.
            // 배치 후엔 CameraFollow가 Awake에서 루트로 detach하고 CurrentPlayer를 추적한다.
            Playable playable = player.GetComponent<Playable>();
            Transform camRoot = playable != null ? playable.CameraRoot : null;
            Vector3 pos = camRoot != null ? camRoot.position : player.transform.position;
            Quaternion rot = camRoot != null ? camRoot.rotation : Quaternion.identity;

            Object.Instantiate(prefab, pos, rot);
        }
    }
}
