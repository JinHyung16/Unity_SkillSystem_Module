using UnityEngine;
using Jinhyeong_Common;
using Jinhyeong_Input;
using Jinhyeong_Managers;
using Jinhyeong_SkillSystem;

namespace Jinhyeong_Character
{
    /// <summary>플레이어 루트 컴포넌트. 하위 컴포넌트(SerializeField로 명시 바인딩)를 묶고 CommonConfig 값으로 초기화한 뒤 GameEvents에 스폰을 브로드캐스트.</summary>
    public class Player : BaseBehaviour
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
            // 필수 바인딩 검증 — 하나라도 비면 시끄럽게 죽고(fail-fast) 자동 GetComponent 폴백은 하지 않는다.
            if (RequireRef(_damageable, nameof(_damageable)) == false)
                return;
            if (RequireRef(_skills, nameof(_skills)) == false)
                return;
            if (RequireRef(_playable, nameof(_playable)) == false)
                return;
            if (RequireRef(_input, nameof(_input)) == false)
                return;
            if (RequireRef(_motor, nameof(_motor)) == false)
                return;
            if (RequireRef(_facing, nameof(_facing)) == false)
                return;
            if (RequireRef(_attack, nameof(_attack)) == false)
                return;
            if (RequireRef(_controller, nameof(_controller)) == false)
                return;

            Init();
        }

        protected override void OnEnabled()
        {
            if (_spawnedNotified)
                return;
            _spawnedNotified = true;
            GameEvents.RaisePlayerSpawned(this);
        }

        protected override void OnDisabled()
        {
            if (_spawnedNotified == false)
                return;
            _spawnedNotified = false;
            GameEvents.RaisePlayerDespawned(this);
        }

        public void Init()
        {
            _damageable.MaxHp = CommonConfig.Player.Hp;
            _damageable.Hp = CommonConfig.Player.Hp;
            _damageable.Shield = 0f;
            _damageable.Stunned = false;
            _damageable.SpeedMultiplier = 1f;
            _damageable.IncomingDamageMultiplier = 1f;
            _damageable.NotifyHealthChanged();

            _skills.Stunned = false;
            _skills.OutgoingDamageMultiplier = 1f;
            _skills.AttackSpeedMultiplier = 1f;

            _motor.MoveSpeed = CommonConfig.Player.MoveSpeed;
            _motor.SpeedMultiplier = 1f;
            _motor.MoveAxis = Vector2.zero;

            _facing.TurnSpeed = CommonConfig.Player.TurnSpeed;

            _attack.Damage = CommonConfig.Player.AttackDamage;
            _attack.Range = CommonConfig.Player.AttackRange;
            _attack.HalfAngleDeg = CommonConfig.Player.AttackHalfAngleDeg;
            _attack.Cooldown = CommonConfig.Player.AttackCooldown;
        }
    }
}
