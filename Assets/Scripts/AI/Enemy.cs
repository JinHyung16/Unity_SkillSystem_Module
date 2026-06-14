using System;
using UnityEngine;
using Jinhyeong_AI.BehaviorTree;
using Jinhyeong_Character;
using Jinhyeong_Common;
using Jinhyeong_SkillSystem;

namespace Jinhyeong_AI
{
    /// <summary>적 캐릭터 루트 컴포넌트. 하위 컴포넌트(SerializeField로 명시 바인딩)를 묶고 풀에서 꺼낼 때 CommonConfig 값으로 초기화한다.</summary>
    public class Enemy : BaseBehaviour
    {
        [Header("Bound Components")]
        [SerializeField] private Damageable _damageable;
        [SerializeField] private SkillObject _skills;
        [SerializeField] private CharacterMotor _motor;
        [SerializeField] private CharacterFacing _facing;
        [SerializeField] private CharacterAttack _attack;
        [SerializeField] private BehaviorTreeRunner _btRunner;
        [SerializeField] private EnemyAI _ai;

        public Damageable Damageable { get { return _damageable; } }
        public SkillObject Skills { get { return _skills; } }
        public CharacterMotor Motor { get { return _motor; } }
        public CharacterFacing Facing { get { return _facing; } }
        public CharacterAttack Attack { get { return _attack; } }
        public BehaviorTreeRunner BTRunner { get { return _btRunner; } }
        public EnemyAI AI { get { return _ai; } }

        public event Action<Enemy> OnDespawnRequested;

        private bool _subscribed;
        private bool _bindingValid;

        private void Awake()
        {
            // 필수 바인딩 검증 — fail-fast. 자동 GetComponent 폴백 없음.
            _bindingValid =
                RequireRef(_damageable, nameof(_damageable)) &&
                RequireRef(_skills, nameof(_skills)) &&
                RequireRef(_motor, nameof(_motor)) &&
                RequireRef(_facing, nameof(_facing)) &&
                RequireRef(_attack, nameof(_attack)) &&
                RequireRef(_btRunner, nameof(_btRunner)) &&
                RequireRef(_ai, nameof(_ai));
            if (_bindingValid == false)
                return;

            Init();
        }

        protected override void OnDisabled()
        {
            UnsubscribeDeath();
        }

        public void Init(Vector3 position)
        {
            transform.position = position;
            transform.rotation = Quaternion.identity;
            Init();
        }

        public void Init()
        {
            if (_bindingValid == false)
                return;

            _damageable.MaxHp = CommonConfig.Enemy.Hp;
            _damageable.Hp = CommonConfig.Enemy.Hp;
            _damageable.Shield = 0f;
            _damageable.Stunned = false;
            _damageable.SpeedMultiplier = 1f;
            _damageable.IncomingDamageMultiplier = 1f;
            _damageable.DestroyOnDeath = false;
            _damageable.NotifyHealthChanged();
            SubscribeDeath();

            _skills.Stunned = false;
            _skills.OutgoingDamageMultiplier = 1f;
            _skills.AttackSpeedMultiplier = 1f;

            _motor.MoveSpeed = CommonConfig.Enemy.MoveSpeed;
            _motor.MoveAxis = Vector2.zero;
            _motor.SpeedMultiplier = 1f;

            _facing.TurnSpeed = CommonConfig.Enemy.TurnSpeed;

            _attack.Damage = CommonConfig.Enemy.AttackDamage;
            _attack.Range = CommonConfig.Enemy.AttackRange;
            _attack.HalfAngleDeg = CommonConfig.Enemy.AttackHalfAngleDeg;
            _attack.Cooldown = CommonConfig.Enemy.AttackCooldown;

            _ai.OnRespawned();
        }

        private void SubscribeDeath()
        {
            if (_subscribed)
                return;
            _damageable.OnDied += HandleDied;
            _subscribed = true;
        }

        private void UnsubscribeDeath()
        {
            if (_subscribed == false)
                return;
            _damageable.OnDied -= HandleDied;
            _subscribed = false;
        }

        private void HandleDied(Damageable d, SkillObject src)
        {
            if (OnDespawnRequested != null)
            {
                OnDespawnRequested.Invoke(this);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }
}
