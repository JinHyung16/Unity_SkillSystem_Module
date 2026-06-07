using UnityEngine;
using Jinhyeong_Common;
using Jinhyeong_Input;
using Jinhyeong_Managers;
using Jinhyeong_SkillSystem;

namespace Jinhyeong_Character
{
    /// <summary>플레이어 루트 컴포넌트. 입력/모터/스킬 등의 하위 컴포넌트를 묶고 CommonConfig 값으로 초기화한 뒤 GameEvents에 스폰을 브로드캐스트.</summary>
    [DisallowMultipleComponent]
    public class Player : MonoBehaviour
    {
        [Header("Bound Components")]
        [SerializeField] private Damageable _damageable;
        [SerializeField] private SkillObject _skills;
        [SerializeField] private Playable _playable;
        [SerializeField] private KeyboardInputProvider _input;
        [SerializeField] private CharacterMotor _motor;
        [SerializeField] private CharacterFacing _facing;
        [SerializeField] private CharacterAttack _attack;
        [SerializeField] private PlayerController _controller;

        public Damageable Damageable { get { return _damageable; } }
        public SkillObject Skills { get { return _skills; } }

        private bool _spawnedNotified;

        private void Awake()
        {
            Init();
        }

        private void OnEnable()
        {
            if (_spawnedNotified == false)
            {
                _spawnedNotified = true;
                GameEvents.RaisePlayerSpawned(this);
            }
        }

        private void OnDisable()
        {
            if (_spawnedNotified)
            {
                _spawnedNotified = false;
                GameEvents.RaisePlayerDespawned(this);
            }
        }

        public void Init()
        {
            if (_damageable != null)
            {
                _damageable.MaxHp = CommonConfig.Player.Hp;
                _damageable.Hp = CommonConfig.Player.Hp;
                _damageable.Shield = 0f;
                _damageable.Stunned = false;
                _damageable.SpeedMultiplier = 1f;
                _damageable.IncomingDamageMultiplier = 1f;
                _damageable.NotifyHealthChanged();
            }
            if (_skills != null)
            {
                _skills.Stunned = false;
                _skills.OutgoingDamageMultiplier = 1f;
                _skills.AttackSpeedMultiplier = 1f;
            }
            if (_motor != null)
            {
                _motor.MoveSpeed = CommonConfig.Player.MoveSpeed;
                _motor.SpeedMultiplier = 1f;
                _motor.MoveAxis = Vector2.zero;
            }
            if (_facing != null)
            {
                _facing.TurnSpeed = CommonConfig.Player.TurnSpeed;
            }
            if (_attack != null)
            {
                _attack.Damage = CommonConfig.Player.AttackDamage;
                _attack.Range = CommonConfig.Player.AttackRange;
                _attack.HalfAngleDeg = CommonConfig.Player.AttackHalfAngleDeg;
                _attack.Cooldown = CommonConfig.Player.AttackCooldown;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_damageable == null) _damageable = GetComponent<Damageable>();
            if (_skills == null) _skills = GetComponent<SkillObject>();
            if (_playable == null) _playable = GetComponent<Playable>();
            if (_input == null) _input = GetComponent<KeyboardInputProvider>();
            if (_motor == null) _motor = GetComponent<CharacterMotor>();
            if (_facing == null) _facing = GetComponent<CharacterFacing>();
            if (_attack == null) _attack = GetComponent<CharacterAttack>();
            if (_controller == null) _controller = GetComponent<PlayerController>();
        }
#endif
    }
}
