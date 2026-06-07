using UnityEngine;

namespace Jinhyeong_Common
{
    /// <summary>
    /// Player/Enemy/Spawner의 기본 스탯과 물리 상수를 모아둔 정적 보관소.
    /// Init 시점에 각 컴포넌트로 주입되며 추후 DB lookup 결과로 대체 가능.
    /// </summary>
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
            public const float AttackRange = 1.8f;
            public const float AttackHalfAngleDeg = 60f;
            public const float AttackCooldown = 0.35f;
        }

        public static class Enemy
        {
            public const float Hp = 50f;
            public const float MoveSpeed = 3.5f;
            public const float TurnSpeed = 360f;

            public const float AttackDamage = 5f;
            public const float AttackRange = 1.6f;
            public const float AttackHalfAngleDeg = 60f;
            public const float AttackCooldown = 1.0f;

            public const float DetectionRange = 8f;
            public const float LoseSightRange = 12f;
            public const float AIAttackRange = 2.0f;
            public const float AIAttackInterval = 1.0f;
            public static readonly KeyCode AttackSkillKey = KeyCode.Alpha1;

            public const float FleeHpPercent = 0.2f;
            public const float FleeSpeedMultiplier = 1.3f;

            public const float PatrolRadius = 5f;
            public const float PatrolArrivalDistance = 0.4f;
            public const float PatrolWaitTime = 1.5f;

            public const float ChaseSpeedMultiplier = 1.0f;

            public static readonly LayerMask ObstacleMask = 1;
            public const float AIRayHeight = 0.5f;
        }

        public static class Spawner
        {
            public const int InitialEnemyCount = 5;
            public const float InitialSpawnRadius = 7f;
            public const int PrewarmCount = 0;
        }
    }
}
