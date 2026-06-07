using System.Collections.Generic;
using UnityEngine;
using Jinhyeong_GeneratedEnums;
using Jinhyeong_SkillSystem;

namespace Jinhyeong_Managers
{
    /// <summary>CharacterManager의 스폰 이벤트로 Playable마다 핸들러를 부착해 OnSkillFire를 같은 GO의 SkillObject로 라우팅하는 싱글톤.</summary>
    [DisallowMultipleComponent]
    public class SkillManager : MonoBehaviour
    {
        public static SkillManager Instance { get; private set; }

        private readonly Dictionary<Playable, System.Action<Playable, ESkillTriggerType>> _bindings =
            new Dictionary<Playable, System.Action<Playable, ESkillTriggerType>>(16);

        private bool _hookedToCharacterManager;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnEnable()
        {
            HookCharacterManager();
        }

        private void OnDisable()
        {
            UnhookCharacterManager();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Clear();
                Instance = null;
            }
        }

        private void HookCharacterManager()
        {
            if (_hookedToCharacterManager) return;
            CharacterManager cm = CharacterManager.Instance;
            if (cm == null) return;
            cm.OnCharacterSpawned += HandleCharacterSpawned;
            cm.OnCharacterDespawned += HandleCharacterDespawned;
            _hookedToCharacterManager = true;
        }

        private void UnhookCharacterManager()
        {
            if (_hookedToCharacterManager == false) return;
            CharacterManager cm = CharacterManager.Instance;
            if (cm != null)
            {
                cm.OnCharacterSpawned -= HandleCharacterSpawned;
                cm.OnCharacterDespawned -= HandleCharacterDespawned;
            }
            _hookedToCharacterManager = false;
        }

        private void Start()
        {
            HookCharacterManager();
        }

        private void HandleCharacterSpawned(Playable p)
        {
            if (p == null) return;
            if (_bindings.ContainsKey(p)) return;

            System.Action<Playable, ESkillTriggerType> handler = HandleSkillFire;
            p.OnSkillFire += handler;
            _bindings[p] = handler;
        }

        private void HandleCharacterDespawned(Playable p)
        {
            if (p == null) return;
            if (_bindings.TryGetValue(p, out System.Action<Playable, ESkillTriggerType> handler))
            {
                p.OnSkillFire -= handler;
                _bindings.Remove(p);
            }
        }

        private void HandleSkillFire(Playable p, ESkillTriggerType type)
        {
            if (p == null) return;
            SkillObject so = p.GetComponent<SkillObject>();
            if (so == null)
            {
                Debug.LogWarning($"[SkillManager] '{p.name}'에 SkillObject가 없음 — 트리거 무시");
                return;
            }

            switch (type)
            {
                case ESkillTriggerType.OnAttack:
                    so.RaiseAttack();
                    break;
                case ESkillTriggerType.OnOreBreak:
                    so.RaiseOreBreak();
                    break;
            }
        }

        public void Clear()
        {
            UnhookCharacterManager();
            foreach (KeyValuePair<Playable, System.Action<Playable, ESkillTriggerType>> kv in _bindings)
            {
                if (kv.Key != null) kv.Key.OnSkillFire -= kv.Value;
            }
            _bindings.Clear();
        }
    }
}
