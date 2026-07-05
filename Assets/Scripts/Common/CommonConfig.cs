using UnityEngine;

namespace Jinhyeong_Common
{

    public static class CommonConfig
    {
        public static class Physics
        {
            public const float Gravity = -9.81f;
        }

        public static class Player
        {
            public const float Hp = 100f;
            public const float MoveSpeed = 5f;
            public const float TurnSpeed = 720f;

            public const float AttackDamage = 10f;
            public const float AttackRange = 2.2f;
            public const float AttackHalfAngleDeg = 60f;
            public const float AttackCooldown = 0.35f;

            public const float AutoAttackRange = 2.2f;
        }

        public static class Enemy
        {
            public const float Hp = 50f;
            public const float MoveSpeed = 3.5f;
            public const float TurnSpeed = 360f;

            public const float AttackDamage = 5f;
            public const float AttackRange = 2.0f;
            public const float AttackHalfAngleDeg = 60f;
            public const float AttackCooldown = 1.0f;

            public const float DetectionRange = 8f;
            public const float LoseSightRange = 12f;
            public const float AIAttackRange = 1.9f;
            public const float AIAttackInterval = 1.0f;

            public const float StandoffDistance = 1.3f;
            public static readonly KeyCode AttackSkillKey = KeyCode.Alpha1;

            public const float FleeHpPercent = 0.2f;
            public const float FleeSpeedMultiplier = 1.3f;

            public const float PatrolRadius = 5f;
            public const float PatrolArrivalDistance = 0.4f;
            public const float PatrolWaitTime = 1.5f;

            public const float ChaseSpeedMultiplier = 1.0f;
        }

        public static class Skill
        {
            public const float MuzzleHeight = 1.0f;
        }

        public static class Spawner
        {
            public const int InitialEnemyCount = 5;
            public const float InitialSpawnRadius = 7f;
            public const int PrewarmCount = 0;
        }
    }
}
