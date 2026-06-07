using System;
using UnityEngine;
using Jinhyeong_AI.BehaviorTree;
using Jinhyeong_Character;
using Jinhyeong_Common;
using Jinhyeong_SkillSystem;

namespace Jinhyeong_AI
{
    /// <summary>적 캐릭터 루트 컴포넌트. Damageable/Motor/AI 등의 하위 컴포넌트를 묶고 풀에서 꺼낼 때 CommonConfig 값으로 초기화한다.</summary>
    [DisallowMultipleComponent]
    public class Enemy : MonoBehaviour
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

        private void Awake()
        {
            Init();
        }

        private void OnDisable()
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
            if (_damageable != null)
            {
                _damageable.MaxHp = CommonConfig.Enemy.Hp;
                _damageable.Hp = CommonConfig.Enemy.Hp;
                _damageable.Shield = 0f;
                _damageable.Stunned = false;
                _damageable.SpeedMultiplier = 1f;
                _damageable.IncomingDamageMultiplier = 1f;
                _damageable.DestroyOnDeath = false;
                _damageable.NotifyHealthChanged();
                SubscribeDeath();
            }
            if (_skills != null)
            {
                _skills.Stunned = false;
                _skills.OutgoingDamageMultiplier = 1f;
                _skills.AttackSpeedMultiplier = 1f;
            }
            if (_motor != null)
            {
                _motor.MoveSpeed = CommonConfig.Enemy.MoveSpeed;
                _motor.MoveAxis = Vector2.zero;
                _motor.SpeedMultiplier = 1f;
            }
            if (_facing != null)
            {
                _facing.TurnSpeed = CommonConfig.Enemy.TurnSpeed;
            }
            if (_attack != null)
            {
                _attack.Damage = CommonConfig.Enemy.AttackDamage;
                _attack.Range = CommonConfig.Enemy.AttackRange;
                _attack.HalfAngleDeg = CommonConfig.Enemy.AttackHalfAngleDeg;
                _attack.Cooldown = CommonConfig.Enemy.AttackCooldown;
            }
            if (_ai != null) _ai.OnRespawned();
        }

        private void SubscribeDeath()
        {
            if (_subscribed || _damageable == null) return;
            _damageable.OnDied += HandleDied;
            _subscribed = true;
        }

        private void UnsubscribeDeath()
        {
            if (_subscribed == false || _damageable == null) return;
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

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_damageable == null) _damageable = GetComponent<Damageable>();
            if (_skills == null) _skills = GetComponent<SkillObject>();
            if (_motor == null) _motor = GetComponent<CharacterMotor>();
            if (_facing == null) _facing = GetComponent<CharacterFacing>();
            if (_attack == null) _attack = GetComponent<CharacterAttack>();
            if (_btRunner == null) _btRunner = GetComponent<BehaviorTreeRunner>();
            if (_ai == null) _ai = GetComponent<EnemyAI>();
        }
#endif
    }
}
